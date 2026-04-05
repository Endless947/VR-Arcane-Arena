using System;
using System.Collections.Generic;
using UnityEngine;
using VRArcaneArena.DataStructures;

namespace VRArcaneArena.Managers
{
    /// <summary>
    /// Singleton MonoBehaviour responsible for spatial registration and queries through the arena octree.
    /// </summary>
    public sealed class OctreeManager : MonoBehaviour
    {
        /// <summary>
        /// Global singleton instance.
        /// </summary>
        public static OctreeManager Instance;

        /// <summary>
        /// Half-size of the cubic arena bounds centered at world origin.
        /// </summary>
        public float arenaHalfSize = 25f;

        /// <summary>
        /// Maximum number of entities in a leaf before subdivision.
        /// </summary>
        public int maxEntitiesPerNode = 4;

        /// <summary>
        /// Maximum octree depth.
        /// </summary>
        public int maxDepth = 8;

        /// <summary>
        /// Toggles editor gizmo visualization for octree nodes.
        /// </summary>
        public bool showDebugOverlay = false;

        private Octree _octree;
        private Dictionary<string, GameObject> _trackedObjects;
        private List<LineRenderer> _activeLines = new List<LineRenderer>();
        private GameObject _lineContainer;

        /// <summary>
        /// Initializes singleton state and creates the underlying octree.
        /// </summary>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        public void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            showDebugOverlay = true;

            _octree = new Octree(new[] { 0f, 0f, 0f }, Mathf.Max(0.01f, arenaHalfSize), maxEntitiesPerNode, maxDepth);
            _trackedObjects = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        }

        public void Update()
        {
            if (showDebugOverlay)
                RefreshOverlay();
            else if (_lineContainer != null)
            {
                foreach (var lr in _activeLines)
                    if (lr != null) Destroy(lr.gameObject);
                _activeLines.Clear();
            }
        }

        /// <summary>
        /// Registers a game object into the octree at its current transform position.
        /// </summary>
        /// <param name="id">Unique entity identifier.</param>
        /// <param name="obj">Game object to track.</param>
        /// <remarks>
        /// Complexity: O(log n) average due to octree insertion.
        /// </remarks>
        public void RegisterEntity(string id, GameObject obj)
        {
            if (string.IsNullOrWhiteSpace(id) || obj == null)
            {
                return;
            }

            if (_trackedObjects.ContainsKey(id))
            {
                UnregisterEntity(id);
            }

            _trackedObjects[id] = obj;

            var entity = new OctreeEntity(id, ToFloatArray(obj.transform.position), obj);
            _octree.Insert(entity);
        }

        /// <summary>
        /// Removes a tracked entity from the octree.
        /// </summary>
        /// <param name="id">Identifier to remove.</param>
        /// <remarks>
        /// Complexity: O(log n) average.
        /// </remarks>
        public void UnregisterEntity(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            _octree.Remove(id);
            _trackedObjects.Remove(id);
        }

        /// <summary>
        /// Updates a tracked entity position in the octree using its current transform.
        /// </summary>
        /// <param name="id">Entity identifier.</param>
        /// <param name="obj">Current game object reference.</param>
        /// <remarks>
        /// Complexity: O(log n) average.
        /// </remarks>
        public void UpdateEntityPosition(string id, GameObject obj)
        {
            if (string.IsNullOrWhiteSpace(id) || obj == null)
            {
                return;
            }

            if (!_trackedObjects.ContainsKey(id))
            {
                RegisterEntity(id, obj);
                return;
            }

            _trackedObjects[id] = obj;
            _octree.UpdatePosition(id, ToFloatArray(obj.transform.position));
        }

        /// <summary>
        /// Queries all tracked objects whose octree positions fall within a sphere.
        /// </summary>
        /// <param name="center">Sphere center in world space.</param>
        /// <param name="radius">Sphere radius.</param>
        /// <returns>Matching game objects.</returns>
        /// <remarks>
        /// Complexity: O(log n + k) average, where k is result size.
        /// </remarks>
        public List<GameObject> QuerySphere(Vector3 center, float radius)
        {
            var entities = _octree.QuerySphere(ToFloatArray(center), Mathf.Max(0f, radius));
            var result = new List<GameObject>(entities.Count);

            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity != null && _trackedObjects.TryGetValue(entity.id, out var go) && go != null)
                {
                    result.Add(go);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the current number of octree nodes.
        /// </summary>
        /// <returns>Total node count.</returns>
        /// <remarks>
        /// Complexity: O(m), where m is node count in octree.
        /// </remarks>
        public int GetNodeCount()
        {
            return _octree == null ? 0 : _octree.GetNodeCount();
        }

        private void DrawNodeLines(Octree.OctreeNode node)
        {
            if (node == null) return;

            Color lineColor;
            if (node.entities.Count == 0)
                lineColor = new Color(0.5f, 0f, 1f, 0.5f);
            else if (node.entities.Count <= 3)
                lineColor = new Color(0.8f, 0f, 1f, 1f);
            else
                lineColor = new Color(1f, 1f, 1f, 1f);

            var c = ToVector3(node.center);
            var h = node.halfSize;

            Vector3[] corners = new Vector3[]
            {
                new Vector3(c.x-h, c.y-h, c.z-h),
                new Vector3(c.x+h, c.y-h, c.z-h),
                new Vector3(c.x+h, c.y-h, c.z+h),
                new Vector3(c.x-h, c.y-h, c.z+h),
                new Vector3(c.x-h, c.y+h, c.z-h),
                new Vector3(c.x+h, c.y+h, c.z-h),
                new Vector3(c.x+h, c.y+h, c.z+h),
                new Vector3(c.x-h, c.y+h, c.z+h),
            };

            int[][] edges = new int[][]
            {
                new[]{0,1}, new[]{1,2}, new[]{2,3}, new[]{3,0},
                new[]{4,5}, new[]{5,6}, new[]{6,7}, new[]{7,4},
                new[]{0,4}, new[]{1,5}, new[]{2,6}, new[]{3,7}
            };

            foreach (var edge in edges)
            {
                var go = new GameObject("OctreeLine");
                go.transform.SetParent(_lineContainer.transform);
                var lr = go.AddComponent<LineRenderer>();
                var mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = lineColor;
                lr.material = mat;
                lr.startColor = lineColor;
                lr.endColor = lineColor;
                lr.startWidth = 0.08f;
                lr.endWidth = 0.08f;
                lr.positionCount = 2;
                lr.SetPosition(0, corners[edge[0]]);
                lr.SetPosition(1, corners[edge[1]]);
                lr.useWorldSpace = true;
                _activeLines.Add(lr);
            }

            if (node.children == null) return;
            foreach (var child in node.children)
                DrawNodeLines(child);
        }

        private void RefreshOverlay()
        {
            if (_lineContainer == null)
            {
                _lineContainer = new GameObject("OctreeOverlay");
            }

            foreach (var lr in _activeLines)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            _activeLines.Clear();

            if (_octree != null && _octree.root != null)
                DrawNodeLines(_octree.root);
        }

        /// <summary>
        /// Converts a Unity vector into float array coordinates.
        /// </summary>
        /// <param name="v">Source vector.</param>
        /// <returns>Float array [x, y, z].</returns>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        private float[] ToFloatArray(Vector3 v)
        {
            return new[] { v.x, v.y, v.z };
        }

        /// <summary>
        /// Converts float array coordinates into a Unity vector.
        /// </summary>
        /// <param name="f">Float array [x, y, z].</param>
        /// <returns>Converted vector, or Vector3.zero for invalid input.</returns>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        private Vector3 ToVector3(float[] f)
        {
            if (f == null || f.Length < 3)
            {
                return Vector3.zero;
            }

            return new Vector3(f[0], f[1], f[2]);
        }
    }
}
