using UnityEngine;
using VRArcaneArena.Managers;

namespace VRArcaneArena.Game
{
    /// <summary>
    /// Controls enemy movement toward the player and manager registration lifecycle.
    /// </summary>
    public sealed class Enemy : MonoBehaviour
    {
        /// <summary>
        /// Current player transform target.
        /// </summary>
        public Transform playerTarget;

        /// <summary>
        /// Maximum detection range for movement behavior.
        /// </summary>
        public float detectionRange = 30f;

        public float dropDuration = 1.2f;
        public float pauseAfterLanding = 2.5f;
        public bool startMovingImmediately = false;

        private EnemyStats _stats;
        private bool _isRegistered;
        private bool _hasLanded = false;
        private bool _isPausing = false;
        private float _pauseEndTime = 0f;
        private Vector3 _landingPosition;
        private float _dropStartY;
        private float _dropEndY;
        private float _dropStartTime;
        private bool _isDropping = false;
        private GameObject _threatRing;
        private Renderer _ringRenderer;
        private Color _originalColor;
        private bool _originalColorCaptured = false;

        /// <summary>
        /// Caches required local components.
        /// </summary>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        public void Awake()
        {
            _stats = GetComponent<EnemyStats>();
        }

        /// <summary>
        /// Resolves the player target and registers this enemy with spatial and threat managers.
        /// </summary>
        /// <remarks>
        /// Complexity: O(1) average.
        /// </remarks>
        public void Start()
        {
            var player = GameObject.FindWithTag("Player");
            CreateThreatRing();
            playerTarget = player != null ? player.transform : null;

            if (_stats == null || string.IsNullOrWhiteSpace(_stats.enemyId)) return;

            if (OctreeManager.Instance != null)
                OctreeManager.Instance.RegisterEntity(_stats.enemyId, gameObject);

            if (ThreatManager.Instance != null)
                ThreatManager.Instance.RegisterEnemy(_stats.enemyId, gameObject);

            _isRegistered = true;

            if (!startMovingImmediately)
            {
                // Start dropping from current Y position down to Y=1.5
                _dropStartY = transform.position.y;
                _dropEndY = 0.5f;
                _landingPosition = new Vector3(transform.position.x, _dropEndY, transform.position.z);
                _dropStartTime = Time.time;
                _isDropping = true;
            }
            else
            {
                _hasLanded = true;
            }
        }

        /// <summary>
        /// Creates a flat glowing ring around the enemy base to show threat level.
        /// Ring is a scaled cylinder primitive with zero Y scale to make it flat.
        /// </summary>
        private void CreateThreatRing()
        {
            _threatRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _threatRing.name = "ThreatRing";

            // Remove collider so it doesnt interfere with gameplay
            var col = _threatRing.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Make it a flat disc around the enemy base
            _threatRing.transform.SetParent(transform);
            _threatRing.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            _threatRing.transform.localScale = new Vector3(2f, 0.03f, 2f);

            // Create unlit material for the ring
            var ringMat = new Material(Shader.Find("Unlit/Color"));
            ringMat.color = Color.blue;
            _ringRenderer = _threatRing.GetComponent<Renderer>();
            _ringRenderer.material = ringMat;
        }

        /// <summary>
        /// Moves toward the player while alive, updates octree position, and resolves contact behavior.
        /// </summary>
        /// <remarks>
        /// Complexity: O(1) per frame.
        /// </remarks>
        public void Update()
        {
            if (_stats == null || _stats.IsDead()) return;

            // Phase 1 - Drop from sky
            if (_isDropping)
            {
                var elapsed = Time.time - _dropStartTime;
                var t = Mathf.Clamp01(elapsed / dropDuration);
                // Use ease-in curve so drop accelerates
                t = t * t;
                transform.position = new Vector3(
                    transform.position.x,
                    Mathf.Lerp(_dropStartY, _dropEndY, t),
                    transform.position.z);

                if (t >= 1f)
                {
                    _isDropping = false;
                    _hasLanded = true;
                    _isPausing = true;
                    _pauseEndTime = Time.time + pauseAfterLanding;
                    transform.position = _landingPosition;
                    if (_isRegistered && OctreeManager.Instance != null)
                        OctreeManager.Instance.UpdateEntityPosition(_stats.enemyId, gameObject);
                }
                return;
            }

            // Phase 2 - Pause after landing
            if (_isPausing)
            {
                if (Time.time < _pauseEndTime) return;
                _isPausing = false;
            }

            // Phase 3 - Move toward player
            if (playerTarget == null) return;

            var toPlayer = playerTarget.position - transform.position;
            var distance = toPlayer.magnitude;
            if (distance > detectionRange) return;

            var targetPosition = new Vector3(
                playerTarget.position.x,
                transform.position.y,
                playerTarget.position.z);

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                Mathf.Max(0f, _stats.speed) * Time.deltaTime);

            if (_isRegistered && OctreeManager.Instance != null)
                OctreeManager.Instance.UpdateEntityPosition(_stats.enemyId, gameObject);

            var flatPlayerPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
            if (Vector3.Distance(transform.position, flatPlayerPos) < 1.5f)
                DealDamageToPlayer();
        }

        /// <summary>
        /// Executes the current contact-damage behavior and destroys this enemy instance.
        /// </summary>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        public void DealDamageToPlayer()
        {
            if (PlayerHealth.Instance != null)
                PlayerHealth.Instance.TakeDamage(_stats.damage);
            if (GameManager.Instance != null)
                GameManager.Instance.OnEnemyKilled();
            UnregisterFromManagers();
            Destroy(gameObject);
        }

        /// <summary>
        /// Applies damage and kills this enemy when health reaches zero.
        /// </summary>
        /// <param name="amount">Damage amount.</param>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        public void TakeDamage(float amount)
        {
            if (_stats == null)
            {
                return;
            }

            _stats.TakeDamage(amount);
            if (_stats.IsDead())
            {
                Die();
            }
        }

        /// <summary>
        /// Unregisters this enemy from manager systems and destroys its game object.
        /// </summary>
        /// <remarks>
        /// Complexity: O(log n) amortized due to manager structure removals.
        /// </remarks>
        public void Die()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnEnemyKilled();
            UnregisterFromManagers();
            Destroy(gameObject);
        }

        /// <summary>
        /// Performs safe cleanup when this object is destroyed.
        /// </summary>
        /// <remarks>
        /// Complexity: O(log n) amortized due to manager structure removals.
        /// </remarks>
        public void OnDestroy()
        {
            if (_isRegistered)
            {
                UnregisterFromManagers();
            }
        }

        /// <summary>
        /// Updates the threat ring color based on threat rank.
        /// Original enemy material color is never modified.
        /// White glowing ring = highest threat (boss).
        /// Other enemies show blue to red gradient on ring.
        /// </summary>
        public void SetThreatColor(float threatScore, bool isHighestThreat)
        {
            if (_ringRenderer == null) return;

            Color ringColor;
            if (isHighestThreat)
                ringColor = Color.white;
            else if (threatScore > 10f)
                ringColor = Color.red;
            else if (threatScore >= 4f)
                ringColor = new Color(1f, 0.5f, 0f, 1f);
            else if (threatScore >= 1f)
                ringColor = Color.yellow;
            else if (threatScore > 0f)
                ringColor = Color.green;
            else
                ringColor = Color.blue;

            _ringRenderer.material.color = ringColor;
        }

        /// <summary>
        /// Removes this enemy from all manager systems if possible.
        /// </summary>
        /// <remarks>
        /// Complexity: O(log n) amortized.
        /// </remarks>
        private void UnregisterFromManagers()
        {
            if (_stats == null || string.IsNullOrWhiteSpace(_stats.enemyId))
            {
                _isRegistered = false;
                return;
            }

            if (OctreeManager.Instance != null)
            {
                OctreeManager.Instance.UnregisterEntity(_stats.enemyId);
            }

            if (ThreatManager.Instance != null)
            {
                ThreatManager.Instance.UnregisterEnemy(_stats.enemyId);
            }

            _isRegistered = false;
        }
    }
}
