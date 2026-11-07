using System.Collections;
using ScientistCat.Combat;
using ScientistCat.TopDown;
using ScientistCat.UI;
using UnityEngine;

namespace ScientistCat.Player
{
    /// <summary>On death: stop control, gray out and topple the sprite, then show the death screen.</summary>
    [RequireComponent(typeof(Health), typeof(TopDownPlayerController))]
    public class PlayerDeath : MonoBehaviour
    {
        [SerializeField] private float deathScreenDelay = 1f;
        [SerializeField] private Color deadTint = new Color(0.45f, 0.45f, 0.5f);
        [SerializeField] private float toppleDuration = 0.35f;

        private Health health;
        private TopDownPlayerController mover;

        private void Awake()
        {
            health = GetComponent<Health>();
            mover = GetComponent<TopDownPlayerController>();
        }

        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDied()
        {
            mover.ControlEnabled = false;
            mover.Stop();
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            yield return null; // let hit-flash effects reset first
            var visual = GetComponentInChildren<TopDownSpriteVisual>();
            var sprite = GetComponentInChildren<SpriteRenderer>();
            if (visual != null) visual.enabled = false;
            if (sprite != null) sprite.color = deadTint;

            if (sprite != null)
            {
                Transform t = sprite.transform;
                Quaternion from = t.rotation;
                Quaternion to = from * Quaternion.Euler(0f, 0f, 90f);
                for (float e = 0f; e < toppleDuration; e += Time.deltaTime)
                {
                    t.rotation = Quaternion.Slerp(from, to, e / toppleDuration);
                    yield return null;
                }
                t.rotation = to;
            }

            yield return new WaitForSeconds(deathScreenDelay);
            if (DeathScreen.Instance != null) DeathScreen.Instance.Show();
            else Debug.LogWarning("[PlayerDeath] No DeathScreen in the scene.");
        }
    }
}
