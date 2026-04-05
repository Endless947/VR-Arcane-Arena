using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRArcaneArena.Managers;

namespace VRArcaneArena.UI
{
    /// <summary>
    /// Displays the spell trie as a floating world-space UI graph on the left wrist.
    /// </summary>
    public sealed class TrieVisualizer : MonoBehaviour
    {
        public GestureDetector gestureDetector;
        public Transform leftHandAnchor;
        public float nodeRadius = 0.02f;

        private Canvas _canvas;
        private Dictionary<string, Image> _nodeImages;
        private List<Image> _edgeImages;

        private readonly Dictionary<string, string> _spellSequences = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Fireball", "FP" },
            { "Blizzard", "OOS" },
            { "Lightning Bolt", "PPF" },
            { "Arcane Shield", "SO" },
            { "Meteor Strike", "FFF" },
            { "Gravity Well", "OPS" },
            { "Frost Nova", "PO" },
            { "Void Blast", "SFF" }
        };

        private readonly Dictionary<string, Vector2> _nodePositions = new Dictionary<string, Vector2>(StringComparer.Ordinal)
        {
            { string.Empty, new Vector2(0f, 110f) },
            { "F", new Vector2(-180f, 55f) },
            { "O", new Vector2(-60f, 55f) },
            { "P", new Vector2(60f, 55f) },
            { "S", new Vector2(180f, 55f) },
            { "FF", new Vector2(-210f, 0f) },
            { "FP", new Vector2(-150f, 0f) },
            { "OO", new Vector2(-80f, 0f) },
            { "OP", new Vector2(-20f, 0f) },
            { "PP", new Vector2(40f, 0f) },
            { "PO", new Vector2(100f, 0f) },
            { "SO", new Vector2(160f, 0f) },
            { "SF", new Vector2(210f, 0f) },
            { "FFF", new Vector2(-210f, -60f) },
            { "OOS", new Vector2(-80f, -60f) },
            { "OPS", new Vector2(-20f, -60f) },
            { "PPF", new Vector2(40f, -60f) },
            { "SFF", new Vector2(210f, -60f) }
        };

        private static readonly Color GreyColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color GoldColor = new Color(1f, 0.84f, 0f, 1f);
        private static readonly Color RedColor = new Color(1f, 0.2f, 0.2f, 1f);

        private static readonly string[] AllSequences =
        {
            "FP",
            "OOS",
            "PPF",
            "SO",
            "FFF",
            "OPS",
            "PO",
            "SFF"
        };

        private string _currentActiveSequence = string.Empty;
        private HashSet<string> _reachableSequences;
        private Coroutine _flashRoutine;

        /// <summary>
        /// Creates the canvas and trie graph.
        /// </summary>
        public void Start()
        {
            Debug.Log("TrieVisualizer Start() called");

            if (gestureDetector == null)
            {
                gestureDetector = GetComponent<GestureDetector>();
            }

            BuildCanvas();
            BuildGraph();
            SubscribeToEvents();

            if (gestureDetector != null)
            {
                HandleReachableSpellsUpdated(gestureDetector.GetReachableSpells());
            }
            else
            {
                _reachableSequences = new HashSet<string>(AllSequences, StringComparer.Ordinal);
                _currentActiveSequence = string.Empty;
                RefreshVisualState();
            }
        }

        /// <summary>
        /// Unsubscribes from gesture events when this component is disabled.
        /// </summary>
        public void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Unsubscribes from gesture events and stops any active flash animation.
        /// </summary>
        public void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }
        }

        private void BuildCanvas()
        {
            var panelObject = new GameObject("TriePanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cam = Camera.main;
            panelObject.transform.SetParent(cam.transform, false);

            panelObject.transform.localPosition = new Vector3(-0.42f, -0.05f, 1.4f);
            panelObject.transform.localRotation = Quaternion.identity;
            panelObject.transform.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);

            _canvas = panelObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;

            var canvasRect = _canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(500f, 280f);

            var scaler = panelObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            _nodeImages = new Dictionary<string, Image>(StringComparer.Ordinal);
            _edgeImages = new List<Image>();

            Debug.Log("TriePanel built, parent is: " + (_canvas != null ? _canvas.gameObject.name : "NULL"));
        }

        private void BuildGraph()
        {
            var rootSprite = (Sprite)null;
            var lineSprite = (Sprite)null;

            foreach (var pair in _nodePositions)
            {
                var node = CreateNode(pair.Key, pair.Value, rootSprite);
                _nodeImages[pair.Key] = node;
            }

            foreach (var pair in _nodePositions)
            {
                if (pair.Key.Length == 0)
                {
                    continue;
                }

                var parentSequence = pair.Key.Substring(0, pair.Key.Length - 1);
                if (!_nodePositions.ContainsKey(parentSequence))
                {
                    continue;
                }

                CreateEdge(parentSequence, pair.Key, lineSprite);
            }

            RefreshVisualState();
        }

        private void SubscribeToEvents()
        {
            if (gestureDetector == null)
            {
                return;
            }

            gestureDetector.onReachableSpellsUpdated.AddListener(HandleReachableSpellsUpdated);
            gestureDetector.onSpellCast.AddListener(HandleSpellCast);
            gestureDetector.onInvalidGesture.AddListener(HandleInvalidGesture);
        }

        private void UnsubscribeFromEvents()
        {
            if (gestureDetector == null)
            {
                return;
            }

            gestureDetector.onReachableSpellsUpdated.RemoveListener(HandleReachableSpellsUpdated);
            gestureDetector.onSpellCast.RemoveListener(HandleSpellCast);
            gestureDetector.onInvalidGesture.RemoveListener(HandleInvalidGesture);
        }

        private Image CreateNode(string sequence, Vector2 position, Sprite sprite)
        {
            var spellNames = new Dictionary<string, string>
            {
                {"FP","Fireball"}, {"OOS","Blizzard"}, {"PPF","Lightning"},
                {"SO","Shield"}, {"FFF","Meteor"}, {"OPS","Gravity"},
                {"PO","Frost"}, {"SFF","Void"}
            };

            var nodeObject = new GameObject(sequence.Length == 0 ? "RootNode" : $"Node_{sequence}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            nodeObject.transform.SetParent(_canvas.transform, false);

            var rectTransform = nodeObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(55f, 30f);

            var image = nodeObject.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.color = GreyColor;
            image.raycastTarget = false;
            image.preserveAspect = true;

            var labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
            labelObj.transform.SetParent(nodeObject.transform, false);
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObj.GetComponent<UnityEngine.UI.Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.color = Color.black;

            string nodeLabel = sequence.Length == 0 ? "ROOT" : sequence[sequence.Length - 1].ToString();
            nodeLabel = nodeLabel.Replace("F", "Fist").Replace("P", "Point").Replace("O", "Open").Replace("S", "Spread");
            label.text = nodeLabel;

            if (spellNames.ContainsKey(sequence))
            {
                var spellLabelObj = new GameObject("SpellLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
                spellLabelObj.transform.SetParent(nodeObject.transform, false);
                var spellLabelRect = spellLabelObj.GetComponent<RectTransform>();
                spellLabelRect.anchorMin = new Vector2(0.5f, 0f);
                spellLabelRect.anchorMax = new Vector2(0.5f, 0f);
                spellLabelRect.pivot = new Vector2(0.5f, 1f);
                spellLabelRect.anchoredPosition = new Vector2(0f, -18f);
                spellLabelRect.sizeDelta = new Vector2(80f, 14f);

                var spellLabel = spellLabelObj.GetComponent<UnityEngine.UI.Text>();
                spellLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                spellLabel.fontSize = 14;
                spellLabel.alignment = TextAnchor.MiddleCenter;
                spellLabel.raycastTarget = false;
                spellLabel.color = new Color(1f, 0.84f, 0f);
                spellLabel.text = spellNames[sequence];
            }

            return image;
        }

        private void CreateEdge(string parentSequence, string childSequence, Sprite sprite)
        {
            var parentPosition = _nodePositions[parentSequence];
            var childPosition = _nodePositions[childSequence];
            var edgeObject = new GameObject($"Edge_{parentSequence}_{childSequence}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            edgeObject.transform.SetParent(_canvas.transform, false);
            edgeObject.transform.SetSiblingIndex(0);

            var rectTransform = edgeObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = (parentPosition + childPosition) * 0.5f;

            var delta = childPosition - parentPosition;
            var length = delta.magnitude;
            rectTransform.sizeDelta = new Vector2(length, 3f);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            var image = edgeObject.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            image.raycastTarget = false;

            _edgeImages.Add(image);
        }

        private void HandleReachableSpellsUpdated(List<string> reachableSpells)
        {
            if (reachableSpells == null)
            {
                _reachableSequences = new HashSet<string>(AllSequences, StringComparer.Ordinal);
                _currentActiveSequence = string.Empty;
                RefreshVisualState();
                return;
            }

            _reachableSequences = new HashSet<string>(StringComparer.Ordinal);
            foreach (var spell in reachableSpells)
            {
                if (_spellSequences.TryGetValue(spell, out var sequence))
                {
                    _reachableSequences.Add(sequence);
                }
            }

            _currentActiveSequence = GetCommonPrefix(_reachableSequences);
            RefreshVisualState();
        }

        private void HandleSpellCast(string spellName)
        {
            if (_spellSequences.TryGetValue(spellName, out var sequence))
            {
                _currentActiveSequence = sequence;
            }

            if (_reachableSequences == null || _reachableSequences.Count == 0)
            {
                _reachableSequences = new HashSet<string>(AllSequences, StringComparer.Ordinal);
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashTerminalNode());
        }

        private void HandleInvalidGesture()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashInvalidState());
        }

        private IEnumerator FlashTerminalNode()
        {
            RefreshVisualState();
            yield return new WaitForSeconds(0.5f);
            _currentActiveSequence = string.Empty;
            _reachableSequences = new HashSet<string>(AllSequences, StringComparer.Ordinal);
            RefreshVisualState();
            _flashRoutine = null;
        }

        private IEnumerator FlashInvalidState()
        {
            var activeSequenceBeforeInvalid = _currentActiveSequence;
            SetNodesForInvalidFlash(activeSequenceBeforeInvalid);
            yield return new WaitForSeconds(0.3f);
            _currentActiveSequence = string.Empty;
            _reachableSequences = new HashSet<string>(AllSequences, StringComparer.Ordinal);
            RefreshVisualState();
            _flashRoutine = null;
        }

        private void SetNodesForInvalidFlash(string activeSequence)
        {
            if (_nodeImages == null)
            {
                return;
            }

            foreach (var pair in _nodeImages)
            {
                if (pair.Key.Length == 0)
                {
                    pair.Value.color = RedColor;
                    continue;
                }

                pair.Value.color = activeSequence.StartsWith(pair.Key, StringComparison.Ordinal) ? RedColor : GreyColor;
            }
        }

        private void RefreshVisualState()
        {
            if (_nodeImages == null || _nodeImages.Count == 0)
            {
                return;
            }

            var activeSequence = _currentActiveSequence ?? string.Empty;
            var reachableSequences = _reachableSequences ?? new HashSet<string>(AllSequences, StringComparer.Ordinal);

            foreach (var pair in _nodeImages)
            {
                var sequence = pair.Key;
                var image = pair.Value;

                if (sequence.Length == 0)
                {
                    image.color = GoldColor;
                    continue;
                }

                if (activeSequence.StartsWith(sequence, StringComparison.Ordinal))
                {
                    image.color = GoldColor;
                    continue;
                }

                var isReachable = false;
                foreach (var reachableSequence in reachableSequences)
                {
                    if (reachableSequence.StartsWith(sequence, StringComparison.Ordinal))
                    {
                        isReachable = true;
                        break;
                    }
                }

                image.color = isReachable ? Color.white : GreyColor;
            }
        }

        private string GetCommonPrefix(ICollection<string> sequences)
        {
            if (sequences == null || sequences.Count == 0)
            {
                return string.Empty;
            }

            string prefix = null;
            foreach (var sequence in sequences)
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    prefix = sequence;
                    continue;
                }

                var limit = Mathf.Min(prefix.Length, sequence.Length);
                var index = 0;
                while (index < limit && prefix[index] == sequence[index])
                {
                    index++;
                }

                prefix = prefix.Substring(0, index);
                if (prefix.Length == 0)
                {
                    break;
                }
            }

            return prefix ?? string.Empty;
        }
    }
}
