using UnityEngine;

namespace ScientistCat.Combat
{
    /// <summary>
    /// Straight-flying projectile for the player and bosses. Moves with sphere casts each frame,
    /// so it never tunnels through walls and needs no Rigidbody or collision-matrix setup.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 15f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float radius = 0.2f;
        [Tooltip("Layers this projectile damages (e.g. Enemy for the player, Player for bosses).")]
        [SerializeField] private LayerMask damageLayers;
        [Tooltip("Layers that stop the projectile (walls, boxes, ground).")]
        [SerializeField] private LayerMask obstacleLayers = 1;
        [Tooltip("Keep flying after hitting something damageable.")]
        [SerializeField] private bool pierce = false;
        [Tooltip("Optional effect spawned on impact.")]
        [SerializeField] private GameObject hitEffectPrefab;

        private Vector3 direction = Vector3.forward;
        private float dieTime;

        private void Start()
        {
            dieTime = Time.time + lifetime;
        }

        /// <summary>Direction is flattened onto the ground plane. Negative values keep the prefab's defaults.</summary>
        public void Launch(Vector3 dir, float speedOverride = -1f, int damageOverride = -1)
        {
            if (speedOverride > 0f) speed = speedOverride;
            if (damageOverride > 0) damage = damageOverride;
            dir.y = 0f;
            direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
        }

        private void Update()
        {
            if (Time.time >= dieTime)
            {
                Destroy(gameObject);
                return;
            }

            float step = speed * Time.deltaTime;
            int mask = damageLayers | obstacleLayers;

            if (Physics.SphereCast(transform.position, radius, direction, out RaycastHit hit, step, mask, QueryTriggerInteraction.Ignore))
            {
                bool damageable = (damageLayers.value & (1 << hit.collider.gameObject.layer)) != 0;
                var health = damageable ? hit.collider.GetComponentInParent<Health>() : null;
                if (health != null)
                {
                    health.TakeDamage(damage, transform.position);
                    if (pierce)
                    {
                        transform.position += direction * step;
                        return;
                    }
                }
                transform.position = hit.point;
                Impact();
                return;
            }

            transform.position += direction * step;
        }

        private void Impact()
        {
            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
