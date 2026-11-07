using System;
using UnityEngine;

namespace ScientistCat.Combat
{
    /// <summary>
    /// Damages targets touching this object's collider bounds (boss bodies, spikes, hazards).
    /// Uses an overlap query, so it works with CharacterController targets without triggers.
    /// Repeat rate is limited by the target's invulnerability time.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ContactDamage : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField] private LayerMask targetLayers;
        [Tooltip("Extra reach around the collider bounds. Keep above the CharacterController skin width (0.08).")]
        [SerializeField] private float padding = 0.15f;
        [Tooltip("Extra reach upward. Needed for flat floor hazards: a CharacterController's physics shape " +
                 "floats ~0.3 units above the ground, so a thin pad alone never touches it.")]
        [SerializeField] private float extraHeight = 0.6f;

        /// <summary>Fired each time damage is actually applied (for hazard/boss feedback).</summary>
        public event Action<Health> DamageDealt;

        private Collider ownCollider;
        private readonly Collider[] results = new Collider[8];

        private void Awake()
        {
            ownCollider = GetComponent<Collider>();
        }

        private void FixedUpdate()
        {
            Bounds b = ownCollider.bounds;
            Vector3 center = b.center + Vector3.up * (extraHeight * 0.5f);
            Vector3 extents = b.extents + Vector3.one * padding + Vector3.up * (extraHeight * 0.5f);
            int count = Physics.OverlapBoxNonAlloc(center, extents, results,
                Quaternion.identity, targetLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                var health = results[i].GetComponentInParent<Health>();
                if (health != null && health.TakeDamage(damage, transform.position)) DamageDealt?.Invoke(health);
            }
        }
    }
}
