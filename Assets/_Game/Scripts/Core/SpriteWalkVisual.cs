using UnityEngine;

namespace ScientistCat.Core
{
    /// <summary>
    /// Walk/idle animation for sprite characters that aren't driven by TopDownPlayerController (bosses, NPCs):
    /// stays upright facing the camera and fakes a walk with squash &amp; stretch + tilt, like Wilson.
    /// Speed is measured from how the parent body moves, so it works with any movement code.
    /// Put it on the child that holds the SpriteRenderer, pivot at the feet.
    /// </summary>
    [ExecuteAlways]
    public class SpriteWalkVisual : MonoBehaviour
    {
        [Tooltip("Object whose movement drives the walk. Defaults to the root of this hierarchy.")]
        [SerializeField] private Transform body;
        [Tooltip("At this speed (units/s) the walk is at full strength.")]
        [SerializeField] private float referenceSpeed = 3f;

        [Header("Walk (squash & stretch)")]
        [SerializeField] private float walkFrequency = 2.5f;
        [SerializeField] private float walkSquash = 0.08f;
        [SerializeField] private float walkTilt = 4f;

        [Header("Idle (breathing)")]
        [SerializeField] private float idleFrequency = 0.8f;
        [SerializeField] private float idleSquash = 0.025f;
        [SerializeField] private float blendSpeed = 8f;

        [Header("Facing")]
        [Tooltip("Flip the sprite toward the screen-space move direction.")]
        [SerializeField] private bool flipToMovement = false;
        [SerializeField] private bool artFacesLeft = false;
        [SerializeField] private float maxPitchStretch = 2.2f;

        private Camera viewCamera;
        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;
        private Vector3 lastBodyPos;
        private Vector3 velocity;
        private float walkPhase;
        private float idlePhase;
        private float walkBlend;

        private void Awake()
        {
            if (body == null) body = transform.root;
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            lastBodyPos = body.position;
        }

        private void OnEnable() => FaceCameraNow();

        /// <summary>Turns to the camera with no animation (edit mode, first frame).</summary>
        private void FaceCameraNow()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;
            Vector3 forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(forward, Vector3.up);
            // Only write when it changed, so edit mode doesn't keep marking the scene dirty.
            if (Quaternion.Angle(transform.rotation, target) > 0.01f) transform.rotation = target;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                FaceCameraNow();
                return;
            }

            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null || Time.deltaTime <= 0f) return;

            Vector3 delta = body.position - lastBodyPos;
            delta.y = 0f;
            lastBodyPos = body.position;
            velocity = Vector3.Lerp(velocity, delta / Time.deltaTime, 15f * Time.deltaTime);

            float speed01 = Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.01f, referenceSpeed));
            walkBlend = Mathf.MoveTowards(walkBlend, speed01, blendSpeed * Time.deltaTime);

            walkPhase += Time.deltaTime * walkFrequency * Mathf.PI * Mathf.Max(speed01, 0.3f);
            idlePhase += Time.deltaTime * idleFrequency * Mathf.PI * 2f;

            float step = Mathf.Abs(Mathf.Sin(walkPhase));
            float stretch = Mathf.Lerp(Mathf.Sin(idlePhase) * idleSquash, (step * 2f - 1f) * walkSquash, walkBlend);
            float tilt = Mathf.Cos(walkPhase) * walkTilt * walkBlend;

            if (flipToMovement && spriteRenderer != null)
            {
                float sideways = Vector3.Dot(velocity, viewCamera.transform.right);
                if (Mathf.Abs(sideways) > 0.2f)
                {
                    bool movingLeft = sideways < 0f;
                    spriteRenderer.flipX = artFacesLeft ? !movingLeft : movingLeft;
                }
            }

            // Upright, turned to the camera, stretched so it looks camera-facing.
            Vector3 forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up);
            float pitchStretch = Mathf.Min(maxPitchStretch, 1f / Mathf.Max(0.01f, forward.magnitude));
            if (forward.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0f, 0f, tilt);
            transform.localScale = new Vector3(baseScale.x * (1f - stretch), baseScale.y * (1f + stretch) * pitchStretch, baseScale.z);
        }
    }
}
