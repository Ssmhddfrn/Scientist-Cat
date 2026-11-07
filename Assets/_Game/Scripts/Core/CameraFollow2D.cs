using UnityEngine;

namespace ScientistCat.Core
{
    /// <summary>Smooth 2D follow with look-ahead and optional level bounds.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = new Vector2(0f, 1.5f);
        [SerializeField] private float smoothTime = 0.15f;
        [Tooltip("How far the camera leads in the direction the target faces.")]
        [SerializeField] private float lookAhead = 1.5f;

        [Header("Bounds (world space)")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-20f, -5f);
        [SerializeField] private Vector2 maxBounds = new Vector2(20f, 10f);

        private Camera cam;
        private Vector3 velocity;

        public Transform Target { get => target; set => target = value; }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, GetDesiredPosition(), ref velocity, smoothTime);
        }

        /// <summary>Jump straight to the target (e.g. on scene start).</summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            transform.position = GetDesiredPosition();
            velocity = Vector3.zero;
        }

        private Vector3 GetDesiredPosition()
        {
            float facing = Mathf.Sign(target.lossyScale.x);
            Vector3 desired = new Vector3(
                target.position.x + offset.x + facing * lookAhead,
                target.position.y + offset.y,
                transform.position.z);

            if (useBounds)
            {
                float halfH = cam.orthographicSize;
                float halfW = halfH * cam.aspect;
                desired.x = ClampAxis(desired.x, minBounds.x + halfW, maxBounds.x - halfW);
                desired.y = ClampAxis(desired.y, minBounds.y + halfH, maxBounds.y - halfH);
            }
            return desired;
        }

        private static float ClampAxis(float value, float min, float max)
        {
            return min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
        }

        private void OnDrawGizmosSelected()
        {
            if (!useBounds) return;
            Gizmos.color = Color.cyan;
            Vector3 center = (minBounds + maxBounds) * 0.5f;
            Gizmos.DrawWireCube(center, maxBounds - minBounds);
        }
    }
}
