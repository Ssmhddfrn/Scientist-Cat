using UnityEngine;

namespace ScientistCat.Combat
{
    /// <summary>Damages targets on touch (boss bodies, spikes, hazards). Repeat rate is limited by the target's invulnerability time.</summary>
    public class ContactDamage : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField] private LayerMask targetLayers;

        private void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);
        private void OnTriggerStay2D(Collider2D other) => TryDamage(other);

        private void TryDamage(Collider2D other)
        {
            if (!enabled || (targetLayers.value & (1 << other.gameObject.layer)) == 0) return;
            var health = other.GetComponentInParent<Health>();
            if (health != null) health.TakeDamage(damage, transform.position);
        }
    }
}
