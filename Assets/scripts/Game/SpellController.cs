using System;
using System.Collections.Generic;
using UnityEngine;
using VRArcaneArena.Managers;

namespace VRArcaneArena.Game
{
    /// <summary>
    /// Handles gesture-driven spell casting, area queries, and cooldown registration.
    /// </summary>
    public sealed class SpellController : MonoBehaviour
    {
        /// <summary>
        /// Gesture detector source that emits spell cast names.
        /// </summary>
        public GestureDetector gestureDetector;

        /// <summary>
        /// Fireball effect radius.
        /// </summary>
        public float fireballRadius = 5f;

        /// <summary>
        /// Fireball damage per enemy.
        /// </summary>
        public float fireballDamage = 50f;

        /// <summary>
        /// Blizzard effect radius.
        /// </summary>
        public float blizzardRadius = 8f;

        /// <summary>
        /// Blizzard damage per enemy.
        /// </summary>
        public float blizzardDamage = 20f;

        /// <summary>
        /// Lightning Bolt damage per target.
        /// </summary>
        public float lightningDamage = 75f;

        /// <summary>
        /// Meteor Strike effect radius.
        /// </summary>
        public float meteorRadius = 12f;

        /// <summary>
        /// Meteor Strike damage per enemy.
        /// </summary>
        public float meteorDamage = 100f;

        /// <summary>
        /// Gravity Well effect radius.
        /// </summary>
        public float gravityWellRadius = 15f;

        private Dictionary<string, float> _spellCooldowns;

        /// <summary>
        /// Initializes spell cooldown durations.
        /// </summary>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        public void Awake()
        {
            _spellCooldowns = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "Fireball", 3f },
                { "Blizzard", 8f },
                { "Lightning Bolt", 5f },
                { "Arcane Shield", 10f },
                { "Meteor Strike", 20f },
                { "Gravity Well", 12f },
                { "Frost Nova", 6f },
                { "Void Blast", 15f }
            };
        }

        /// <summary>
        /// Locates the gesture detector if needed and subscribes to spell-cast events.
        /// </summary>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        public void Start()
        {
            if (gestureDetector == null)
            {
                gestureDetector = FindObjectOfType<GestureDetector>();
            }

            if (gestureDetector != null)
            {
                gestureDetector.onSpellCast.AddListener(OnSpellCast);
            }
        }

        /// <summary>
        /// Unsubscribes event listeners when this component is destroyed.
        /// </summary>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        public void OnDestroy()
        {
            if (gestureDetector != null)
            {
                gestureDetector.onSpellCast.RemoveListener(OnSpellCast);
            }
        }

        /// <summary>
        /// Dispatches incoming spell names, enforces cooldown checks, and registers cooldowns after cast.
        /// </summary>
        /// <param name="spellName">Resolved spell name from gesture input.</param>
        /// <remarks>
        /// Complexity: O(log n + q), where n is cooldown entries and q is spell effect query cost.
        /// </remarks>
        public void OnSpellCast(string spellName)
        {
            if (string.IsNullOrWhiteSpace(spellName))
            {
                return;
            }

            var tracker = CooldownTracker.Instance ?? FindObjectOfType<CooldownTracker>();
            if (tracker != null && tracker.IsOnCooldown(spellName))
            {
                Debug.Log("spell on cooldown: " + spellName);
                return;
            }

            switch (spellName)
            {
                case "Fireball":
                    CastFireball();
                    break;
                case "Blizzard":
                    CastBlizzard();
                    break;
                case "Lightning Bolt":
                    CastLightningBolt();
                    break;
                case "Arcane Shield":
                    CastArcaneShield();
                    break;
                case "Meteor Strike":
                    CastMeteorStrike();
                    break;
                case "Gravity Well":
                    CastGravityWell();
                    break;
                case "Frost Nova":
                    CastFrostNova();
                    break;
                case "Void Blast":
                    CastVoidBlast();
                    break;
                default:
                    return;
            }

            if (_spellCooldowns.TryGetValue(spellName, out var cooldownDuration))
            {
                var cooldownTracker = CooldownTracker.Instance ?? FindObjectOfType<CooldownTracker>();
                if (cooldownTracker != null)
                    cooldownTracker.AddCooldown(spellName, spellName, cooldownDuration);
                else
                    Debug.LogWarning("CooldownTracker not found for spell: " + spellName);
            }
        }

        /// <summary>
        /// Casts Fireball, damaging enemies within fireball radius around the player.
        /// </summary>
        /// <remarks>
        /// Complexity: O(log n + k), where k is enemies in radius.
        /// </remarks>
        public void CastFireball()
        {
            var player = GetPlayerTransform();
            if (player == null || OctreeManager.Instance == null)
            {
                return;
            }

            var hits = OctreeManager.Instance.QuerySphere(player.position, fireballRadius);
            ApplyDamageToTargets(hits, fireballDamage);
            Debug.Log($"Fireball hit {hits.Count} enemies");
            SpellEffects.PlayEffect("Fireball", player.position);
        }

        /// <summary>
        /// Casts Blizzard, damaging enemies within blizzard radius around the player.
        /// </summary>
        /// <remarks>
        /// Complexity: O(log n + k), where k is enemies in radius.
        /// </remarks>
        public void CastBlizzard()
        {
            var player = GetPlayerTransform();
            if (player == null || OctreeManager.Instance == null)
            {
                return;
            }

            var hits = OctreeManager.Instance.QuerySphere(player.position, blizzardRadius);
            ApplyDamageToTargets(hits, blizzardDamage);
            SpellEffects.PlayEffect("Blizzard", player.position);
            LaunchTargetedProjectile(player, blizzardDamage, new Color(0f, 1f, 1f));
        }

        /// <summary>
        /// Casts Lightning Bolt, damaging up to three enemies within 30 units.
        /// </summary>
        /// <remarks>
        /// Complexity: O(log n + k), where k is enemies in query results.
        /// </remarks>
        public void CastLightningBolt()
        {
            var player = GetPlayerTransform();
            if (player == null || OctreeManager.Instance == null)
            {
                return;
            }

            var hits = OctreeManager.Instance.QuerySphere(player.position, 30f);
            var limit = Mathf.Min(3, hits.Count);
            for (var i = 0; i < limit; i++)
            {
                DealDamage(hits[i], lightningDamage);
            }

            SpellEffects.PlayEffect("LightningBolt", player.position);
            LaunchTargetedProjectile(player, lightningDamage, Color.yellow);
        }

        /// <summary>
        /// Casts Arcane Shield effect.
        /// </summary>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        public void CastArcaneShield()
        {
            var playerTransform = GetPlayerTransform();
            if (playerTransform == null)
            {
                return;
            }

            Debug.Log("Arcane Shield activated");
            SpellEffects.PlayEffect("ArcaneShield", playerTransform.position);
        }

        /// <summary>
        /// Casts Meteor Strike, damaging enemies within meteor radius around the player.
        /// </summary>
        /// <remarks>
        /// Complexity: O(log n + k), where k is enemies in radius.
        /// </remarks>
        public void CastMeteorStrike()
        {
            var player = GetPlayerTransform();
            if (player == null || OctreeManager.Instance == null)
            {
                return;
            }

            var hits = OctreeManager.Instance.QuerySphere(player.position, meteorRadius);
            ApplyDamageToTargets(hits, meteorDamage);
            SpellEffects.PlayEffect("MeteorStrike", player.position);
            LaunchTargetedProjectile(player, meteorDamage, Color.red);
        }

        /// <summary>
        /// Casts Gravity Well and pulls enemies in radius toward the player.
        /// </summary>
        /// <remarks>
        /// Complexity: O(log n + k), where k is enemies in radius.
        /// </remarks>
        public void CastGravityWell()
        {
            var player = GetPlayerTransform();
            if (player == null || OctreeManager.Instance == null)
            {
                return;
            }

            var hits = OctreeManager.Instance.QuerySphere(player.position, gravityWellRadius);
            for (var i = 0; i < hits.Count; i++)
            {
                var enemy = hits[i];
                if (enemy == null)
                {
                    continue;
                }

                enemy.transform.position = Vector3.Lerp(enemy.transform.position, player.position, 0.5f);
            }

            SpellEffects.PlayEffect("GravityWell", player.position);
        }

        /// <summary>
        /// Casts Frost Nova, damaging nearby enemies and logging frozen count.
        /// </summary>
        /// <remarks>
        /// Complexity: O(log n + k), where k is enemies in radius.
        /// </remarks>
        public void CastFrostNova()
        {
            var player = GetPlayerTransform();
            if (player == null || OctreeManager.Instance == null)
            {
                return;
            }

            var hits = OctreeManager.Instance.QuerySphere(player.position, 6f);
            ApplyDamageToTargets(hits, 30f);
            Debug.Log($"Frost Nova frozen {hits.Count} enemies");
            SpellEffects.PlayEffect("FrostNova", player.position);
            LaunchTargetedProjectile(player, 30f, new Color(0.5f, 0.8f, 1f));
        }

        /// <summary>
        /// Casts Void Blast, damaging enemies within 20 units around the player.
        /// </summary>
        /// <remarks>
        /// Complexity: O(log n + k), where k is enemies in radius.
        /// </remarks>
        public void CastVoidBlast()
        {
            var player = GetPlayerTransform();
            if (player == null || OctreeManager.Instance == null)
            {
                return;
            }

            var hits = OctreeManager.Instance.QuerySphere(player.position, 20f);
            ApplyDamageToTargets(hits, 150f);
            SpellEffects.PlayEffect("VoidBlast", player.position);
            LaunchTargetedProjectile(player, 150f, new Color(0.4f, 0f, 0.6f));
        }

        /// <summary>
        /// Finds and returns the player transform using the Player tag.
        /// </summary>
        /// <returns>Player transform when found; otherwise null.</returns>
        /// <remarks>
        /// Complexity: O(1) average for tag lookup.
        /// </remarks>
        private Transform GetPlayerTransform()
        {
            var player = GameObject.FindWithTag("Player");
            return player != null ? player.transform : null;
        }

        /// <summary>
        /// Applies damage to each enemy object in a target list.
        /// </summary>
        /// <param name="targets">Target game objects.</param>
        /// <param name="damage">Damage to apply.</param>
        /// <remarks>
        /// Complexity: O(k), where k is number of targets.
        /// </remarks>
        private static void ApplyDamageToTargets(List<GameObject> targets, float damage)
        {
            if (targets == null)
            {
                return;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                DealDamage(targets[i], damage);
            }
        }

        /// <summary>
        /// Applies damage to one enemy through its Enemy component.
        /// </summary>
        /// <param name="target">Target game object.</param>
        /// <param name="damage">Damage amount.</param>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        private static void DealDamage(GameObject target, float damage)
        {
            if (target == null)
            {
                return;
            }

            var enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(Mathf.Max(0f, damage));
            }
        }

        /// <summary>
        /// Finds the highest threat enemy or falls back to nearest enemy by distance.
        /// </summary>
        /// <param name="player">Player transform used for the nearest fallback.</param>
        /// <returns>Target enemy object, or null if none available.</returns>
        private static GameObject FindTargetEnemy(Transform player)
        {
            GameObject target = null;
            if (ThreatManager.Instance != null)
                target = ThreatManager.Instance.GetHighestThreat();

            if (target == null && player != null)
            {
                var enemies = GameObject.FindGameObjectsWithTag("Enemy");
                float nearestDist = float.MaxValue;
                foreach (var e in enemies)
                {
                    if (e == null) continue;

                    float dist = Vector3.Distance(player.position, e.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        target = e;
                    }
                }
            }

            return target;
        }

        /// <summary>
        /// Launches a projectile toward the current highest-threat target, with nearest-enemy fallback.
        /// </summary>
        /// <param name="player">Player transform used for projectile spawn and fallback search.</param>
        /// <param name="damage">Projectile damage value.</param>
        /// <param name="color">Projectile color.</param>
        private static void LaunchTargetedProjectile(Transform player, float damage, Color color)
        {
            if (player == null)
            {
                return;
            }

            var target = FindTargetEnemy(player);
            if (target == null)
            {
                return;
            }

            var projObj = new GameObject("SpellProjectile");
            projObj.transform.position = player.position;
            var proj = projObj.AddComponent<SpellProjectile>();
            proj.damage = damage;
            proj.speed = 18f;
            proj.Init(target.transform, color);
        }
    }
}
