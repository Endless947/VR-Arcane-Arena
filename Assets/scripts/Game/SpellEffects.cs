using UnityEngine;

namespace VRArcaneArena.Game
{
    /// <summary>
    /// Utility helpers for one-shot spell visual effects.
    /// </summary>
    public static class SpellEffects
    {
        /// <summary>
        /// Plays a burst particle effect at a world position for the given spell.
        /// </summary>
        /// <param name="spellName">Spell identifier.</param>
        /// <param name="position">World-space position for the effect.</param>
        public static void PlayEffect(string spellName, Vector3 position)
        {
            // Special handling for Fireball: try to find nearest enemy for a projectile effect
            if (spellName == "Fireball")
            {
                GameObject target = null;
                if (VRArcaneArena.Managers.ThreatManager.Instance != null)
                    target = VRArcaneArena.Managers.ThreatManager.Instance.GetHighestThreat();

                if (target == null)
                {
                    var enemies = GameObject.FindGameObjectsWithTag("Enemy");
                    float nearestDist = float.MaxValue;
                    foreach (var e in enemies)
                    {
                        float dist = Vector3.Distance(position, e.transform.position);
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            target = e;
                        }
                    }
                }

                if (target != null)
                {
                    var projectileObject = new GameObject("FireballProjectile");
                    projectileObject.transform.position = position;
                    var projectile = projectileObject.AddComponent<SpellProjectile>();
                    projectile.Init(target.transform, new Color(1f, 0.4f, 0f));
                    return;
                }
                // Fall through to static burst if no enemy found
            }

            var effectObject = new GameObject($"SpellEffect_{spellName}");
            effectObject.transform.position = position;

            var particleSystem = effectObject.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startSpeed = 5f;
            main.startSize = 0.3f;
            main.startLifetime = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var color = GetSpellColor(spellName);
            var particleCount = GetParticleCount(spellName);
            main.startColor = color;

            var renderer = effectObject.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = color;
            renderer.material = mat;

            var emission = particleSystem.emission;
            emission.enabled = false;

            particleSystem.Emit(particleCount);
            particleSystem.Play();

            Object.Destroy(effectObject, 2f);
        }

        private static Color GetSpellColor(string spellName)
        {
            switch (spellName)
            {
                case "Fireball":
                    return new Color(1f, 0.4f, 0f);
                case "Blizzard":
                    return Color.cyan;
                case "LightningBolt":
                    return Color.yellow;
                case "ArcaneShield":
                    return Color.white;
                case "MeteorStrike":
                    return Color.red;
                case "GravityWell":
                    return Color.magenta;
                case "FrostNova":
                    return new Color(0.5f, 0.8f, 1f);
                case "VoidBlast":
                    return new Color(0.4f, 0f, 0.6f);
                default:
                    return Color.white;
            }
        }

        private static int GetParticleCount(string spellName)
        {
            switch (spellName)
            {
                case "Fireball":
                    return 40;
                case "Blizzard":
                    return 60;
                case "LightningBolt":
                    return 30;
                case "ArcaneShield":
                    return 20;
                case "MeteorStrike":
                    return 80;
                case "GravityWell":
                    return 50;
                case "FrostNova":
                    return 45;
                case "VoidBlast":
                    return 70;
                default:
                    return 20;
            }
        }
    }
}
