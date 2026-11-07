using System.Collections;
using UnityEngine;

namespace ScientistCat.Combat
{
    /// <summary>Placeholder hit feedback: tints sprites when hit and blinks during invulnerability.</summary>
    [RequireComponent(typeof(Health))]
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private float flashDuration = 0.1f;
        [SerializeField] private bool blinkWhileInvulnerable = true;
        [SerializeField] private float blinkInterval = 0.08f;

        private Health health;
        private SpriteRenderer[] renderers;
        private Color[] originalColors;
        private Coroutine routine;

        private void Awake()
        {
            health = GetComponent<Health>();
            CacheRenderers();
        }

        /// <summary>Call after swapping sprites/children at runtime.</summary>
        public void CacheRenderers()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) originalColors[i] = renderers[i].color;
        }

        private void OnEnable() => health.Damaged += OnDamaged;
        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            Restore();
        }

        private void OnDamaged(int amount, Vector2 from)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            SetColor(flashColor);
            yield return new WaitForSeconds(flashDuration);
            Restore();

            if (!blinkWhileInvulnerable) yield break;
            bool visible = true;
            while (health.IsInvulnerable && !health.IsDead)
            {
                visible = !visible;
                SetAlpha(visible ? 1f : 0.3f);
                yield return new WaitForSeconds(blinkInterval);
            }
            Restore();
            routine = null;
        }

        private void SetColor(Color c)
        {
            foreach (var r in renderers) if (r) r.color = c;
        }

        private void SetAlpha(float a)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i]) continue;
                var c = originalColors[i];
                c.a *= a;
                renderers[i].color = c;
            }
        }

        private void Restore()
        {
            for (int i = 0; i < renderers.Length; i++) if (renderers[i]) renderers[i].color = originalColors[i];
        }
    }
}
