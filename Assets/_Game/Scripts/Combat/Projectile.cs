using UnityEngine;

namespace ScientistCat.Combat
{
    /// <summary>
    /// Straight-flying projectile used by both the player and bosses.
    /// Who it can hit is decided by its layer (PlayerProjectile / EnemyProjectile) and the Physics2D collision matrix.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 15f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField] private float lifetime = 3f;
        [Tooltip("Keep flying after hitting something damageable.")]
        [SerializeField] private bool pierce = false;
        [Tooltip("Optional effect spawned on impact.")]
        [SerializeField] private GameObject hitEffectPrefab;

        private Rigidbody2D body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            Destroy(gameObject, lifetime);
            if (body.linearVelocity == Vector2.zero) Launch(transform.right);
        }

        /// <summary>Negative values keep the prefab's defaults.</summary>
        public void Launch(Vector2 direction, float speedOverride = -1f, int damageOverride = -1)
        {
            if (speedOverride > 0f) speed = speedOverride;
            if (damageOverride > 0) damage = damageOverride;

            direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            body.linearVelocity = direction * speed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<Health>();

            // Ignore zone triggers (arenas, portals...) that can't take damage.
            if (other.isTrigger && health == null) return;

            if (health != null)
            {
                health.TakeDamage(damage, transform.position);
                if (pierce) return;
            }
            Impact();
        }

        private void Impact()
        {
            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
