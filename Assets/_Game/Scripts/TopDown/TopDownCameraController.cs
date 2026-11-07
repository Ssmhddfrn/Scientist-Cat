using UnityEngine;
using UnityEngine.InputSystem;

namespace ScientistCat.TopDown
{
    /// <summary>
    /// Angled top-down follow camera with a fixed heading: mouse wheel zooms
    /// (the camera tilts more top-down as it zooms out). Call Shake() for screen shake.
    /// </summary>
    public class TopDownCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Point on the target the camera looks at.")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f);
        [Tooltip("Higher = camera trails further behind the target before catching up.")]
        [SerializeField] private float followSmoothTime = 0.35f;
        [Tooltip("Fixed camera heading in degrees.")]
        [SerializeField] private float yaw = 0f;

        [Header("Zoom (Mouse Wheel)")]
        [SerializeField] private float minDistance = 8f;
        [SerializeField] private float maxDistance = 30f;
        [SerializeField] private float startDistance = 18f;
        [SerializeField] private float zoomStep = 2f;
        [SerializeField] private float zoomSmoothTime = 0.12f;
        [Tooltip("Pitch when fully zoomed in.")]
        [SerializeField] private float minPitch = 35f;
        [Tooltip("Pitch when fully zoomed out.")]
        [SerializeField] private float maxPitch = 60f;

        [Header("Shake")]
        [SerializeField] private float maxShakeOffset = 0.6f;
        [SerializeField] private float maxShakeAngle = 2f;
        [SerializeField] private float shakeFrequency = 25f;
        [Tooltip("How fast trauma fades per second.")]
        [SerializeField] private float shakeDecay = 1.5f;

        public Transform Target { get => target; set => target = value; }
        public float Distance => currentDistance;

        private Vector3 focusPoint;
        private Vector3 focusVelocity;
        private float targetDistance;
        private float currentDistance;
        private float distanceVelocity;
        private float trauma;
        private float shakeSeed;

        private void Awake()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
            }

            shakeSeed = Random.value * 100f;
            targetDistance = currentDistance = Mathf.Clamp(startDistance, minDistance, maxDistance);
            SnapToTarget();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    targetDistance = Mathf.Clamp(targetDistance - Mathf.Sign(scroll) * zoomStep, minDistance, maxDistance);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            focusPoint = Vector3.SmoothDamp(focusPoint, target.position + targetOffset, ref focusVelocity, followSmoothTime);
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);
            trauma = Mathf.Max(0f, trauma - shakeDecay * Time.deltaTime);

            ApplyTransform();
        }

        /// <summary>Adds screen shake. 0.2 = light hit, 0.5 = heavy hit, 1 = explosion.</summary>
        public void Shake(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        /// <summary>Jump straight to the target (e.g. on scene start or respawn).</summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            focusPoint = target.position + targetOffset;
            focusVelocity = Vector3.zero;
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            float zoom01 = Mathf.InverseLerp(minDistance, maxDistance, currentDistance);
            float pitch = Mathf.Lerp(minPitch, maxPitch, zoom01);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 position = focusPoint - rotation * Vector3.forward * currentDistance;

            // Squared trauma feels better: small hits barely shake, big ones really do.
            float shake = trauma * trauma;
            if (shake > 0f)
            {
                float t = Time.time * shakeFrequency;
                position += rotation * new Vector3(Noise(t, 0f), Noise(t, 1f), 0f) * (maxShakeOffset * shake);
                rotation *= Quaternion.Euler(0f, 0f, Noise(t, 2f) * maxShakeAngle * shake);
            }

            transform.SetPositionAndRotation(position, rotation);
        }

        private float Noise(float t, float channel)
        {
            return Mathf.PerlinNoise(shakeSeed + channel * 10f, t) * 2f - 1f;
        }
    }
}
