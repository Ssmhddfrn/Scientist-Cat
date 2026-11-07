using System;
using ScientistCat.Combat;
using ScientistCat.TopDown;
using UnityEngine;

namespace ScientistCat.Core
{
    /// <summary>Dimension gate. Locked until Unlock() is called (e.g. when the boss dies); walking in loads the target scene.</summary>
    [RequireComponent(typeof(Collider))]
    public class Portal : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private string targetScene;
        [SerializeField] private bool startsLocked = true;
        [Tooltip("Optional: unlocks automatically when this Health dies (boss, dummy...).")]
        [SerializeField] private Health unlockWhenDead;

        [Header("Visuals")]
        [SerializeField] private Renderer[] tintRenderers;
        [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.4f);
        [SerializeField] private Color openColor = new Color(0.3f, 0.9f, 1f);
        [SerializeField] private Transform spinner;
        [SerializeField] private float spinSpeed = 60f;

        public bool IsOpen { get; private set; }
        public event Action Opened;

        private MaterialPropertyBlock block;
        private bool used;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            block = new MaterialPropertyBlock();
            IsOpen = !startsLocked;
            ApplyColor();
        }

        private void OnEnable()
        {
            if (unlockWhenDead != null) unlockWhenDead.Died += Unlock;
        }

        private void OnDisable()
        {
            if (unlockWhenDead != null) unlockWhenDead.Died -= Unlock;
        }

        public void Unlock()
        {
            if (IsOpen) return;
            IsOpen = true;
            ApplyColor();
            Opened?.Invoke();
        }

        private void Update()
        {
            if (spinner != null) spinner.Rotate(0f, 0f, spinSpeed * (IsOpen ? 4f : 1f) * Time.deltaTime, Space.Self);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsOpen || used || !other.CompareTag("Player")) return;
            used = true;

            var mover = other.GetComponent<TopDownPlayerController>();
            if (mover != null)
            {
                mover.ControlEnabled = false;
                mover.Stop();
            }
            SceneLoader.Instance.Load(targetScene);
        }

        private void ApplyColor()
        {
            if (tintRenderers == null) return;
            Color c = IsOpen ? openColor : lockedColor;
            foreach (var r in tintRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, c);
                r.SetPropertyBlock(block);
            }
        }
    }
}
