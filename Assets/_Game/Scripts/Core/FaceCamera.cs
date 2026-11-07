using UnityEngine;

namespace ScientistCat.Core
{
    /// <summary>Simple billboard for projectiles, effects, props and sprite characters: always faces the camera.</summary>
    public class FaceCamera : MonoBehaviour
    {
        [Tooltip("Off = only turn around Y (stays upright).")]
        [SerializeField] private bool matchPitch = true;
        [Tooltip("Only when Match Pitch is off: stretches the upright sprite so it looks camera-facing (same as Wilson).")]
        [SerializeField] private bool compensateUprightPitch = false;
        [SerializeField] private float maxPitchStretch = 2.2f;

        private Transform cam;
        private Vector3 baseScale;

        private void Awake() => baseScale = transform.localScale;

        private void LateUpdate()
        {
            if (cam == null)
            {
                if (Camera.main == null) return;
                cam = Camera.main.transform;
            }

            if (matchPitch)
            {
                transform.rotation = cam.rotation;
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            if (forward.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            if (compensateUprightPitch)
            {
                // An upright plane seen from pitch p looks cos(p) times shorter; undo that.
                float stretch = Mathf.Min(maxPitchStretch, 1f / Mathf.Max(0.01f, forward.magnitude));
                transform.localScale = new Vector3(baseScale.x, baseScale.y * stretch, baseScale.z);
            }
        }
    }
}
