using UnityEngine;

namespace ScientistCat.TopDown
{
    /// <summary>
    /// Sprite character visual: always faces the camera, flips toward the move direction
    /// and fakes a walk cycle with squash &amp; stretch bouncing (no sprite frames needed).
    /// Put this on the child that holds the SpriteRenderer, pivot at the feet.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TopDownSpriteVisual : MonoBehaviour
    {
        [SerializeField] private TopDownPlayerController mover;
        [Tooltip("Defaults to Camera.main.")]
        [SerializeField] private Camera viewCamera;
        [Tooltip("Off = only turn around the Y axis (stays upright). On = match the camera tilt too.")]
        [SerializeField] private bool matchCameraPitch = true;
        [Tooltip("Only when Match Camera Pitch is off: stretches the upright sprite so it looks like it faces the camera. " +
                 "Upright sprites never lean into objects behind them, so nothing clips through boxes or walls.")]
        [SerializeField] private bool compensateUprightPitch = false;
        [SerializeField] private float maxPitchStretch = 2.2f;
        [Tooltip("Enable if the source art faces left instead of right.")]
        [SerializeField] private bool artFacesLeft = false;

        [Header("Walk (squash & stretch)")]
        [Tooltip("Bounces per second at full speed.")]
        [SerializeField] private float walkFrequency = 4f;
        [Tooltip("How much taller/thinner the sprite gets per step.")]
        [SerializeField] private float walkSquash = 0.12f;
        [Tooltip("Side-to-side tilt in degrees per step.")]
        [SerializeField] private float walkTilt = 6f;

        [Header("Idle (breathing)")]
        [SerializeField] private float idleFrequency = 1.2f;
        [SerializeField] private float idleSquash = 0.03f;

        [Tooltip("How fast the visual blends between idle and walk.")]
        [SerializeField] private float blendSpeed = 10f;

        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;
        private float walkPhase;
        private float idlePhase;
        private float walkBlend;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (mover == null) mover = GetComponentInParent<TopDownPlayerController>();
            if (viewCamera == null) viewCamera = Camera.main;
            baseScale = transform.localScale;
        }

        private void LateUpdate()
        {
            if (viewCamera == null) return;

            float speed01 = mover != null && mover.MoveSpeed > 0f
                ? Mathf.Clamp01(mover.Velocity.magnitude / mover.MoveSpeed)
                : 0f;
            walkBlend = Mathf.MoveTowards(walkBlend, speed01, blendSpeed * Time.deltaTime);

            walkPhase += Time.deltaTime * walkFrequency * Mathf.PI * Mathf.Max(speed01, 0.3f);
            idlePhase += Time.deltaTime * idleFrequency * Mathf.PI * 2f;

            // Walk: |sin| gives one bounce per step; stretch up while lifting, squash on landing.
            float step = Mathf.Abs(Mathf.Sin(walkPhase));
            float walkStretch = (step * 2f - 1f) * walkSquash;
            float idleStretch = Mathf.Sin(idlePhase) * idleSquash;
            float stretch = Mathf.Lerp(idleStretch, walkStretch, walkBlend);

            float tilt = Mathf.Cos(walkPhase) * walkTilt * walkBlend;

            UpdateFlip();

            float pitchStretch = 1f;
            if (!matchCameraPitch && compensateUprightPitch)
            {
                // An upright plane seen from pitch p looks cos(p) times shorter; undo that.
                float cosPitch = new Vector2(viewCamera.transform.forward.x, viewCamera.transform.forward.z).magnitude;
                pitchStretch = Mathf.Min(maxPitchStretch, 1f / Mathf.Max(0.01f, cosPitch));
            }

            transform.localScale = new Vector3(baseScale.x * (1f - stretch), baseScale.y * (1f + stretch) * pitchStretch, baseScale.z);
            transform.rotation = FacingRotation() * Quaternion.Euler(0f, 0f, tilt);
        }

        private Quaternion FacingRotation()
        {
            Transform cam = viewCamera.transform;
            if (matchCameraPitch) return cam.rotation;

            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            return forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward, Vector3.up) : transform.rotation;
        }

        private void UpdateFlip()
        {
            if (mover == null || !mover.IsMoving) return;

            float sideways = Vector3.Dot(mover.Velocity, viewCamera.transform.right);
            if (Mathf.Abs(sideways) < 0.1f) return;

            bool movingLeft = sideways < 0f;
            spriteRenderer.flipX = artFacesLeft ? !movingLeft : movingLeft;
        }
    }
}
