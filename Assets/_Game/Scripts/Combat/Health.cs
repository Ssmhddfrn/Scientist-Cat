using System;
using UnityEngine;

namespace ScientistCat.Combat
{
    /// <summary>Shared health component for the player, bosses and anything damageable.</summary>
    public class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 5;
        [Tooltip("Seconds of invulnerability after taking damage. 0 = none.")]
        [SerializeField, Min(0f)] private float invulnerabilityTime = 0f;
        [SerializeField] private bool destroyOnDeath = false;

        /// <summary>(current, max)</summary>
        public event Action<int, int> Changed;
        /// <summary>(amount, position the hit came from)</summary>
        public event Action<int, Vector2> Damaged;
        public event Action Died;

        public int Current { get; private set; }
        public int Max => maxHealth;
        public float Normalized => (float)Current / maxHealth;
        public bool IsDead => Current <= 0;
        public bool IsInvulnerable => forcedInvulnerable || Time.time < invulnerableUntil;

        private float invulnerableUntil;
        private bool forcedInvulnerable;

        private void Awake()
        {
            Current = maxHealth;
        }

        public bool TakeDamage(int amount) => TakeDamage(amount, transform.position);

        /// <summary>Returns true if the damage was applied.</summary>
        public bool TakeDamage(int amount, Vector2 hitFrom)
        {
            if (amount <= 0 || IsDead || IsInvulnerable) return false;

            Current = Mathf.Max(0, Current - amount);
            if (invulnerabilityTime > 0f) invulnerableUntil = Time.time + invulnerabilityTime;

            Damaged?.Invoke(amount, hitFrom);
            Changed?.Invoke(Current, maxHealth);

            if (IsDead)
            {
                Died?.Invoke();
                if (destroyOnDeath) Destroy(gameObject);
            }
            return true;
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(Current, maxHealth);
        }

        public void ResetHealth()
        {
            Current = maxHealth;
            invulnerableUntil = 0f;
            Changed?.Invoke(Current, maxHealth);
        }

        /// <summary>Use for cutscenes, phase transitions, etc.</summary>
        public void SetInvulnerable(bool value) => forcedInvulnerable = value;
    }
}
