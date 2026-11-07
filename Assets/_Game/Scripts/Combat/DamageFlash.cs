using System.Collections;
using UnityEngine;

namespace ScientistCat.Combat
{
    /// <summary>
    /// Placeholder hit feedback: tints sprites and meshes when hit and blinks during invulnerability.
    /// Sprites use SpriteRenderer.color, meshes use a MaterialPropertyBlock (_BaseColor) so materials stay shared.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DamageFlash : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color flashColor = new Color(1f, 0.15f, 0.15f);
        [SerializeField] private float flashDuration = 0.12f;
        [Tooltip("How many red flashes per hit.")]
        [SerializeField, Min(1)] private int flashCount = 2;
        [SerializeField] private bool blinkWhileInvulnerable = true;
        [SerializeField] private float blinkInterval = 0.08f;

        private Health health;
        private Renderer[] renderers;
        private Color[] originalColors;
        private MaterialPropertyBlock block;
        private Coroutine routine;

        private void Awake()
        {
            health = GetComponent<Health>();
            block = new MaterialPropertyBlock();
            CacheRenderers();
        }

        /// <summary>Call after swapping sprites/children at runtime.</summary>
        public void CacheRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) originalColors[i] = ReadColor(renderers[i]);
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            StopAllCoroutines();
            routine = null;
            Restore();
        }

        // Stop flashing so death visuals (tint, fade...) aren't overwritten.
        private void OnDied()
        {
            StopAllCoroutines();
            routine = null;
            Restore();
        }

        private void OnDamaged(int amount, Vector3 from)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            for (int n = 0; n < flashCount; n++)
            {
                for (int i = 0; i < renderers.Length; i++) WriteColor(renderers[i], flashColor);
                yield return new WaitForSeconds(flashDuration);
                Restore();
                if (n < flashCount - 1) yield return new WaitForSeconds(flashDuration * 0.6f);
            }

            if (blinkWhileInvulnerable)
            {
                bool visible = true;
                while (health.IsInvulnerable && !health.IsDead)
                {
                    visible = !visible;
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        var c = originalColors[i];
                        c.a *= visible ? 1f : 0.3f;
                        WriteColor(renderers[i], c);
                    }
                    yield return new WaitForSeconds(blinkInterval);
                }
                Restore();
            }
            routine = null;
        }

        private void Restore()
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++) WriteColor(renderers[i], originalColors[i]);
        }

        private Color ReadColor(Renderer r)
        {
            if (r is SpriteRenderer sr) return sr.color;
            var mat = r.sharedMaterial;
            return mat != null && mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white;
        }

        private void WriteColor(Renderer r, Color c)
        {
            if (!r) return;
            if (r is SpriteRenderer sr)
            {
                sr.color = c;
                return;
            }
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(block);
        }
    }
}
