using ScientistCat.TopDown;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScientistCat.Companion
{
    /// <summary>
    /// The cat that trots after Wilson. Spawns itself in every scene that has a TopDownPlayerController,
    /// so no scene setup is needed and scene rebuilds can't lose it.
    /// Sprite: Assets/_Game/Resources/Cat_idle_down.png (pivot at the feet).
    /// </summary>
    public class CatCompanion : MonoBehaviour
    {
        private const string SpriteResource = "Cat_idle_down";
        private const string WalkSpriteResource = "Cat_walk";

        [Header("Follow")]
        [Tooltip("Where the cat wants to sit, relative to Wilson: x = to his side, z = behind him.")]
        [SerializeField] private Vector2 followOffset = new Vector2(1.1f, 0.9f);
        [Tooltip("Closer than this to its spot, the cat sits still.")]
        [SerializeField] private float stopDistance = 0.35f;
        [SerializeField] private float maxSpeed = 7.5f;
        [SerializeField] private float acceleration = 30f;
        [Tooltip("Farther than this (e.g. after a portal), the cat just appears next to Wilson.")]
        [SerializeField] private float teleportDistance = 14f;
        [Tooltip("Enable if the cat art faces left instead of right.")]
        [SerializeField] private bool artFacesLeft = false;

        [Header("Sprites")]
        [SerializeField] private Sprite sitSprite;
        [Tooltip("Shown while the cat moves (standing pose).")]
        [SerializeField] private Sprite walkSprite;
        [Tooltip("Keeps the standing pose this long after stopping, so it doesn't flicker on tiny stops.")]
        [SerializeField] private float sitDelay = 0.2f;

        [Header("Look")]
        [SerializeField] private float hopFrequency = 5f;
        [SerializeField] private float hopSquash = 0.12f;
        [SerializeField] private float walkTilt = 7f;
        [SerializeField] private float idleFrequency = 1.4f;
        [SerializeField] private float idleSquash = 0.03f;
        [SerializeField] private float maxPitchStretch = 2.2f;

        private TopDownPlayerController player;
        private SpriteRenderer spriteRenderer;
        private Transform visual;
        private Camera viewCamera;
        private Vector3 velocity;
        private Vector3 baseScale = Vector3.one;
        private float hopPhase;
        private float idlePhase;
        private float walkBlend;
        private float sideSign = 1f;
        private float lastMoveTime = float.NegativeInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SpawnForScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => SpawnForScene();

        private static void SpawnForScene()
        {
            if (FindAnyObjectByType<CatCompanion>() != null) return;
            var player = FindAnyObjectByType<TopDownPlayerController>();
            if (player == null) return;

            var sprite = Resources.Load<Sprite>(SpriteResource);
            if (sprite == null)
            {
                Debug.LogWarning($"[CatCompanion] Sprite '{SpriteResource}' not found in a Resources folder.");
                return;
            }

            var root = new GameObject("Cat");
            SceneManager.MoveGameObjectToScene(root, player.gameObject.scene);
            var cat = root.AddComponent<CatCompanion>();
            cat.Setup(player, sprite, Resources.Load<Sprite>(WalkSpriteResource));
        }

        private void Setup(TopDownPlayerController target, Sprite sprite, Sprite walk)
        {
            player = target;
            sitSprite = sprite;
            walkSprite = walk;
            viewCamera = Camera.main;

            var child = new GameObject("Visual");
            child.transform.SetParent(transform, false);
            visual = child.transform;
            spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;

            // Look like Wilson: same material, shadows and scale.
            var wilson = player.GetComponentInChildren<TopDownSpriteVisual>();
            var wilsonRenderer = wilson != null ? wilson.GetComponent<SpriteRenderer>() : player.GetComponentInChildren<SpriteRenderer>();
            if (wilsonRenderer != null)
            {
                spriteRenderer.sharedMaterial = wilsonRenderer.sharedMaterial;
                spriteRenderer.shadowCastingMode = wilsonRenderer.shadowCastingMode;
                spriteRenderer.receiveShadows = wilsonRenderer.receiveShadows;
                spriteRenderer.sortingLayerID = wilsonRenderer.sortingLayerID;
                spriteRenderer.sortingOrder = wilsonRenderer.sortingOrder;
                float s = Mathf.Abs(wilsonRenderer.transform.lossyScale.x);
                if (s > 0.0001f) baseScale = Vector3.one * s;
            }
            visual.localScale = baseScale;

            transform.position = TargetPosition();
        }

        private void Update()
        {
            if (player == null) return;

            Vector3 target = TargetPosition();
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance > teleportDistance)
            {
                transform.position = target;
                velocity = Vector3.zero;
                return;
            }

            // Ease in: run when far, slow down near the spot, stop inside stopDistance.
            Vector3 desired = distance > stopDistance
                ? toTarget / distance * Mathf.Min(maxSpeed, distance * 3f)
                : Vector3.zero;
            velocity = Vector3.MoveTowards(velocity, desired, acceleration * Time.deltaTime);

            Vector3 pos = transform.position + velocity * Time.deltaTime;
            pos.y = Mathf.Lerp(pos.y, player.transform.position.y, 10f * Time.deltaTime);
            transform.position = pos;
        }

        private Vector3 TargetPosition()
        {
            Vector3 facing = player.FacingDirection;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.back;
            facing.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, facing);

            // Stay on the side the cat is already on, so it doesn't run across Wilson when he turns.
            float currentSide = Vector3.Dot(transform.position - player.transform.position, side);
            if (Mathf.Abs(currentSide) > 0.3f) sideSign = Mathf.Sign(currentSide);

            return player.transform.position - facing * followOffset.y + side * (followOffset.x * sideSign);
        }

        private void LateUpdate()
        {
            if (visual == null) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            float speed01 = Mathf.Clamp01(velocity.magnitude / maxSpeed);
            walkBlend = Mathf.MoveTowards(walkBlend, speed01 > 0.05f ? 1f : 0f, 8f * Time.deltaTime);

            hopPhase += Time.deltaTime * hopFrequency * Mathf.PI * Mathf.Max(speed01, 0.3f);
            idlePhase += Time.deltaTime * idleFrequency * Mathf.PI * 2f;

            // Standing pose while moving, sitting pose once settled.
            if (speed01 > 0.05f) lastMoveTime = Time.time;
            Sprite pose = Time.time - lastMoveTime < sitDelay && walkSprite != null ? walkSprite : sitSprite;
            if (pose != null && spriteRenderer.sprite != pose) spriteRenderer.sprite = pose;

            float hop = Mathf.Abs(Mathf.Sin(hopPhase));
            float stretch = Mathf.Lerp(Mathf.Sin(idlePhase) * idleSquash, (hop * 2f - 1f) * hopSquash, walkBlend);
            float tilt = Mathf.Cos(hopPhase) * walkTilt * walkBlend;

            // Flip toward movement (screen-space left/right).
            float sideways = Vector3.Dot(velocity, viewCamera.transform.right);
            if (Mathf.Abs(sideways) > 0.2f)
            {
                bool movingLeft = sideways < 0f;
                spriteRenderer.flipX = artFacesLeft ? !movingLeft : movingLeft;
            }

            // Upright, turned to the camera, stretched so it looks camera-facing (same as Wilson in the arenas).
            Vector3 forward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up);
            float cosPitch = forward.magnitude;
            float pitchStretch = Mathf.Min(maxPitchStretch, 1f / Mathf.Max(0.01f, cosPitch));
            Quaternion facing = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward, Vector3.up) : visual.rotation;

            visual.rotation = facing * Quaternion.Euler(0f, 0f, tilt);
            visual.localScale = new Vector3(baseScale.x * (1f - stretch), baseScale.y * (1f + stretch) * pitchStretch, baseScale.z);
        }
    }
}
