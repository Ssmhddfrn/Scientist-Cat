using ScientistCat.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ScientistCat.UI
{
    /// <summary>Opening menu: shows the controls, starts the game (button / Enter / Space / gamepad) or quits.</summary>
    public class MainMenu : MonoBehaviour
    {
        [Tooltip("Scene loaded when the player presses Start.")]
        [SerializeField] private string firstScene = "TestArena";
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [Tooltip("Optional: gently pulses to draw attention to Start.")]
        [SerializeField] private RectTransform pulseTarget;

        private bool started;

        private void Start()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(StartGame);
                startButton.Select();
            }
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
        }

        private void Update()
        {
            if (pulseTarget != null)
                pulseTarget.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.04f);

            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            if ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) ||
                (gamepad != null && (gamepad.startButton.wasPressedThisFrame || gamepad.buttonSouth.wasPressedThisFrame)))
                StartGame();
        }

        public void StartGame()
        {
            if (started) return;
            started = true;
            SceneLoader.Instance.Load(firstScene);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
