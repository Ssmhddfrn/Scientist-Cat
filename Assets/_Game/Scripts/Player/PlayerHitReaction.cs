using ScientistCat.Combat;
using ScientistCat.TopDown;
using UnityEngine;

namespace ScientistCat.Player
{
    /// <summary>Knockback + camera shake when the player takes damage.</summary>
    [RequireComponent(typeof(Health), typeof(TopDownPlayerController))]
    public class PlayerHitReaction : MonoBehaviour
    {
        [SerializeField] private float knockbackSpeed = 9f;
        [SerializeField] private float knockbackDuration = 0.15f;
        [SerializeField, Range(0f, 1f)] private float cameraShake = 0.35f;

        private Health health;
        private TopDownPlayerController mover;
        private TopDownCameraController cameraController;

        private void Awake()
        {
            health = GetComponent<Health>();
            mover = GetComponent<TopDownPlayerController>();
            if (Camera.main != null) cameraController = Camera.main.GetComponent<TopDownCameraController>();
        }

        private void OnEnable() => health.Damaged += OnDamaged;
        private void OnDisable() => health.Damaged -= OnDamaged;

        private void OnDamaged(int amount, Vector3 from)
        {
            Vector3 away = transform.position - from;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = -mover.FacingDirection;
            mover.AddKnockback(away.normalized * knockbackSpeed, knockbackDuration);

            if (cameraController != null) cameraController.Shake(cameraShake);
        }
    }
}
