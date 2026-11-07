using ScientistCat.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScientistCat.Player
{
    /// <summary>Photon gun: hold Attack to fire in the facing direction. Hold Up to aim upward (diagonal when also moving).</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShooter : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference fireAction;

        [Header("Weapon")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform firePointUp;
        [Tooltip("Shots per second while holding fire.")]
        [SerializeField, Min(0.1f)] private float fireRate = 6f;
        [SerializeField] private float projectileSpeed = 18f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField] private bool allowAimUp = true;
        [Tooltip("Small random angle spread in degrees.")]
        [SerializeField] private float spread = 2f;

        private PlayerController controller;
        private float nextFireTime;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            fireAction?.action.Enable();
        }

        private void Update()
        {
            if (!controller.ControlEnabled || fireAction == null || projectilePrefab == null) return;
            if (fireAction.action.IsPressed() && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Fire();
            }
        }

        private void Fire()
        {
            Vector2 input = controller.MoveInput;
            bool aimUp = allowAimUp && input.y > 0.5f;

            Vector2 direction = new Vector2(controller.FacingDirection, 0f);
            Transform origin = firePoint;
            if (aimUp)
            {
                bool diagonal = Mathf.Abs(input.x) > 0.3f;
                direction = diagonal ? new Vector2(controller.FacingDirection, 1f) : Vector2.up;
                if (firePointUp != null) origin = firePointUp;
            }

            direction = Quaternion.Euler(0f, 0f, Random.Range(-spread, spread)) * direction;
            Vector3 spawnPos = origin != null ? origin.position : transform.position;

            var projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            projectile.Launch(direction, projectileSpeed, damage);
        }
    }
}
