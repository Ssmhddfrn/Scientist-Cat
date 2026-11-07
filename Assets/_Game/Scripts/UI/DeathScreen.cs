using System.Collections;
using ScientistCat.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ScientistCat.UI
{
    /// <summary>"You died" panel. Retry with the button, R, Enter or gamepad South; reloads the current scene.</summary>
    public class DeathScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private Button retryButton;
        [SerializeField] private float fadeDuration = 0.4f;
        [Tooltip("Gently pulses so the player notices how to respawn.")]
        [SerializeField] private RectTransform pulseTarget;
        [SerializeField] private float pulseAmount = 0.06f;
        [SerializeField] private float pulseSpeed = 4f;
        [Tooltip("Optional UI hidden while the death screen is up (e.g. controls hint).")]
        [SerializeField] private CanvasGroup hideWhileShown;

        public static DeathScreen Instance { get; private set; }
        public bool IsShown { get; private set; }

        private void Awake()
        {
            Instance = this;
            SetVisible(0f);
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show()
        {
            if (IsShown) return;
            IsShown = true;
            if (hideWhileShown != null) hideWhileShown.alpha = 0f;
            StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                SetVisible(t / fadeDuration);
                yield return null;
            }
            SetVisible(1f);
            if (retryButton != null) retryButton.Select();
        }

        private void Update()
        {
            if (!IsShown) return;

            if (pulseTarget != null)
                pulseTarget.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount);

            if (panel.alpha < 0.99f) return;

            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            if ((keyboard != null && (keyboard.rKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) ||
                (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame))
                Retry();
        }

        public void Retry()
        {
            if (!IsShown) return;
            SceneLoader.Instance.Reload();
        }

        private void SetVisible(float alpha)
        {
            if (panel == null) return;
            panel.alpha = alpha;
            panel.interactable = alpha > 0.99f;
            panel.blocksRaycasts = alpha > 0.01f;
        }
    }
}
