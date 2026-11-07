using UnityEngine;

namespace ScientistCat.Core
{
    /// <summary>Simple billboard for projectiles, effects and props: always faces the camera.</summary>
    public class FaceCamera : MonoBehaviour
    {
        [Tooltip("Off = only turn around Y (stays upright).")]
        [SerializeField] private bool matchPitch = true;

        private Transform cam;

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
        }
    }
}
