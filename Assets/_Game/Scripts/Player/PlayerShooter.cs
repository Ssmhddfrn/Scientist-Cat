using ScientistCat.Combat;
using ScientistCat.TopDown;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScientistCat.Player
{
    /// <summary>
    /// Top-down photon gun. Hold Attack to fire toward the mouse cursor on the ground plane,
    /// or toward the gamepad right stick. Falls back to the move direction.
    /// </summary>
    public class PlayerShooter : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference fireAction;
        [Tooltip("Defaults to Camera.main.")]
        [SerializeField] private Camera viewCamera;

        [Header("Weapon")]
        [SerializeField] private Projectile projectilePrefab;
        [Tooltip("Spawn point; its height is the shooting plane.")]
        [SerializeField] private Transform firePoint;
        [Tooltip("Shots per second while holding fire.")]
        [SerializeField, Min(0.1f)] private float fireRate = 6f;
        [SerializeField] private float projectileSpeed = 20f;
        [SerializeField, Min(1)] private int damage = 1;
        [Tooltip("Random angle spread in degrees.")]
        [SerializeField] private float spread = 2f;
        [Tooltip("Spawn distance in front of the fire point.")]
        [SerializeField] private float muzzleOffset = 0.6f;

        public Vector3 AimDirection { get; private set; } = Vector3.forward;

        private TopDownPlayerController mover;
        private float nextFireTime;

        private void Awake()
        {
            mover = GetComponent<TopDownPlayerController>();
            if (viewCamera == null) viewCamera = Camera.main;
            if (firePoint == null) firePoint = transform;
        }

        private void OnEnable()
        {
            fireAction?.action.Enable();
        }

        private void Update()
        {
            AimDirection = ReadAim();

            bool canShoot = mover == null || mover.ControlEnabled;
            if (!canShoot || fireAction == null || projectilePrefab == null) return;

            if (fireAction.action.IsPressed() && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Fire(AimDirection);
            }
        }

        private Vector3 ReadAim()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.09f) return CameraRelative(stick);
            }

            var mouse = Mouse.current;
            if (mouse != null && viewCamera != null)
            {
                Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
                var plane = new Plane(Vector3.up, firePoint.position);
                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 toCursor = ray.GetPoint(enter) - firePoint.position;
                    toCursor.y = 0f;
                    if (toCursor.sqrMagnitude > 0.01f) return toCursor.normalized;
                }
            }

            return mover != null ? mover.FacingDirection : transform.forward;
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            if (viewCamera == null) return new Vector3(input.x, 0f, input.y).normalized;
            Vector3 forward = viewCamera.transform.forward; forward.y = 0f;
            Vector3 right = viewCamera.transform.right; right.y = 0f;
            return (forward.normalized * input.y + right.normalized * input.x).normalized;
        }

        private void Fire(Vector3 direction)
        {
            direction = Quaternion.Euler(0f, Random.Range(-spread, spread), 0f) * direction;
            Vector3 spawnPos = firePoint.position + direction * muzzleOffset;

            var projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            projectile.Launch(direction, projectileSpeed, damage);
        }
    }
}
