using ScientistCat.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScientistCat.Player
{
    /// <summary>Side-view movement: run, jump (coyote time + jump buffer + variable height), knockback.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;

        [Header("Run")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float acceleration = 70f;
        [SerializeField] private float deceleration = 80f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.8f;

        [Header("Jump")]
        [SerializeField] private float jumpVelocity = 14f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;
        [Tooltip("Upward velocity multiplier when jump is released early.")]
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float fallGravityMultiplier = 1.6f;
        [SerializeField] private float maxFallSpeed = 20f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.7f, 0.1f);
        [SerializeField] private LayerMask groundLayers;

        [Header("Knockback")]
        [SerializeField] private Vector2 knockbackVelocity = new Vector2(7f, 6f);
        [SerializeField] private float knockbackDuration = 0.2f;

        public bool IsGrounded { get; private set; }
        public int FacingDirection { get; private set; } = 1;
        public Vector2 MoveInput { get; private set; }

        /// <summary>Disable during cutscenes / death. Also stops shooting.</summary>
        public bool ControlEnabled { get; set; } = true;

        private Rigidbody2D body;
        private Health health;
        private float baseGravity;
        private float lastGroundedTime = -999f;
        private float lastJumpPressedTime = -999f;
        private float knockbackUntil;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            baseGravity = body.gravityScale;
        }

        private void OnEnable()
        {
            moveAction?.action.Enable();
            jumpAction?.action.Enable();
            if (health != null) health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= OnDamaged;
        }

        private void Update()
        {
            MoveInput = ControlEnabled && moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

            if (ControlEnabled && jumpAction != null)
            {
                if (jumpAction.action.WasPressedThisFrame()) lastJumpPressedTime = Time.time;

                // Variable jump height: releasing early cuts the ascent.
                if (jumpAction.action.WasReleasedThisFrame() && body.linearVelocity.y > 0f)
                    body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * jumpCutMultiplier);
            }

            if (Mathf.Abs(MoveInput.x) > 0.1f && Time.time >= knockbackUntil)
                SetFacing(MoveInput.x > 0f ? 1 : -1);
        }

        private void FixedUpdate()
        {
            IsGrounded = groundCheck != null &&
                         Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayers) != null;
            if (IsGrounded) lastGroundedTime = Time.time;

            Vector2 velocity = body.linearVelocity;

            if (Time.time >= knockbackUntil)
            {
                float target = MoveInput.x * moveSpeed;
                float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
                if (!IsGrounded) rate *= airControl;
                velocity.x = Mathf.MoveTowards(velocity.x, target, rate * Time.fixedDeltaTime);
            }

            bool canJump = Time.time - lastGroundedTime <= coyoteTime;
            bool wantsJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
            if (canJump && wantsJump && ControlEnabled)
            {
                velocity.y = jumpVelocity;
                lastJumpPressedTime = -999f;
                lastGroundedTime = -999f;
            }

            body.gravityScale = velocity.y < 0f ? baseGravity * fallGravityMultiplier : baseGravity;
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
            body.linearVelocity = velocity;
        }

        public void SetFacing(int direction)
        {
            if (direction == 0 || direction == FacingDirection) return;
            FacingDirection = direction;
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * direction;
            transform.localScale = s;
        }

        /// <summary>Instantly stops all movement (e.g. when a cutscene starts).</summary>
        public void Stop()
        {
            body.linearVelocity = Vector2.zero;
            MoveInput = Vector2.zero;
        }

        private void OnDamaged(int amount, Vector2 from)
        {
            if (health.IsDead) return;
            float dir = Mathf.Sign(transform.position.x - from.x);
            if (dir == 0f) dir = -FacingDirection;
            body.linearVelocity = new Vector2(knockbackVelocity.x * dir, knockbackVelocity.y);
            knockbackUntil = Time.time + knockbackDuration;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}
