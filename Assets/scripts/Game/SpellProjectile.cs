using UnityEngine;

namespace VRArcaneArena.Game
{
    /// <summary>
    /// MonoBehaviour that moves a spell projectile toward a target and explodes on arrival.
    /// </summary>
    public class SpellProjectile : MonoBehaviour
    {
        public float speed = 15f;
        public float explosionRadius = 0.8f;
        public int trailParticleCount = 5;
        public float damage = 50f;

        private Transform _target;
        private Color _color;
        private ParticleSystem _ps;

        /// <summary>
        /// Initializes the projectile with a target and color.
        /// Creates and configures a particle system for the trail effect.
        /// </summary>
        /// <param name="target">Target transform to move toward.</param>
        /// <param name="color">Color for the particle trail.</param>
        public void Init(Transform target, Color color)
        {
            _target = target;
            _color = color;

            _ps = gameObject.AddComponent<ParticleSystem>();
            var main = _ps.main;
            main.startColor = color;
            main.startSize = 0.25f;
            main.startSpeed = 0.5f;
            main.startLifetime = 0.3f;
            main.loop = true;

            var emission = _ps.emission;
            emission.rateOverTime = 40f;

            var renderer = gameObject.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = _color;
            renderer.material = mat;

            _ps.Play();
        }

        /// <summary>
        /// Moves the projectile toward the target each frame.
        /// Explodes when reaching the target or if target is destroyed.
        /// </summary>
        public void Update()
        {
            if (_target == null)
            {
                Explode();
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, _target.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _target.position) < 0.5f)
            {
                Explode();
            }
        }

        /// <summary>
        /// Stops trail emission and creates an explosion burst at current position.
        /// Destroys the gameObject after 1 second.
        /// </summary>
        private void Explode()
        {
            var emission = _ps.emission;
            emission.enabled = false;

            var enemy = _target != null ? _target.GetComponent<Enemy>() : null;
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            var main = _ps.main;
            main.startSize = 0.6f;
            _ps.Emit(80);

            Destroy(gameObject, 1f);
        }
    }
}
