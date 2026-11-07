using UnityEngine;
using UnityEngine.InputSystem;

namespace ScientistCat.TopDown
{
    /// <summary>Top-down survival-style movement: camera-relative WASD / arrow keys / gamepad stick.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class TopDownPlayerController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Camera used for camera-relative movement. Defaults to Camera.main.")]
        [SerializeField] private Camera viewCamera;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float acceleration = 40f;
        [SerializeField] private float deceleration = 50f;
        [SerializeField] private float gravity = -25f;

        public bool IsMoving => planarVelocity.sqrMagnitude > 0.01f;
        public Vector3 Velocity => planarVelocity;
        public float MoveSpeed => moveSpeed;
        /// <summary>Last non-zero move direction on the ground plane. The root never rotates (sprite character).</summary>
        public Vector3 FacingDirection { get; private set; } = Vector3.back;

        /// <summary>Disable during cutscenes / menus / death.</summary>
        public bool ControlEnabled { get; set; } = true;

        private CharacterController controller;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private Vector3 knockbackVelocity;
        private float knockbackUntil;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (viewCamera == null) viewCamera = Camera.main;
        }

        private void Update()
        {
            bool knockedBack = Time.time < knockbackUntil;
            Vector3 desiredDirection = ControlEnabled && !knockedBack ? CameraRelative(ReadMoveInput()) : Vector3.zero;

            Vector3 targetVelocity = desiredDirection * moveSpeed;
            float rate = desiredDirection.sqrMagnitude > 0f ? acceleration : deceleration;
            planarVelocity = knockedBack
                ? knockbackVelocity
                : Vector3.MoveTowards(planarVelocity, targetVelocity, rate * Time.deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;

            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (desiredDirection.sqrMagnitude > 0.0001f) FacingDirection = desiredDirection.normalized;
        }

        private static Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null) input += gamepad.leftStick.ReadValue();

            return Vector2.ClampMagnitude(input, 1f);
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            if (viewCamera == null) return new Vector3(input.x, 0f, input.y);

            Vector3 forward = Flatten(viewCamera.transform.forward).normalized;
            Vector3 right = Flatten(viewCamera.transform.right).normalized;
            return forward * input.y + right * input.x;
        }

        private static Vector3 Flatten(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>Stops all movement instantly.</summary>
        public void Stop()
        {
            planarVelocity = Vector3.zero;
            knockbackUntil = 0f;
        }

        /// <summary>Pushes the character on the ground plane for a short time, ignoring input.</summary>
        public void AddKnockback(Vector3 velocity, float duration)
        {
            knockbackVelocity = new Vector3(velocity.x, 0f, velocity.z);
            knockbackUntil = Time.time + duration;
        }
    }
}
