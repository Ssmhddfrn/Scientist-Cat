using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScientistCat.Core
{
    /// <summary>
    /// Persistent scene loader with fade transitions. Created automatically on first use;
    /// place one in the first scene to tweak the values in the Inspector.
    /// Scenes must be listed in Build Profiles / Build Settings.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float fadeInDuration = 0.6f;
        [SerializeField] private Color fadeColor = Color.black;

        private static SceneLoader instance;

        public static SceneLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<SceneLoader>();
                    if (instance == null) instance = new GameObject("SceneLoader").AddComponent<SceneLoader>();
                }
                return instance;
            }
        }

        public bool IsLoading { get; private set; }

        private ScreenFader fader;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            fader = ScreenFader.Create(transform, fadeColor);
        }

        public void Load(string sceneName)
        {
            if (IsLoading) return;
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not in the build scene list.");
                return;
            }
            StartCoroutine(LoadRoutine(sceneName));
        }

        public void Reload() => Load(SceneManager.GetActiveScene().name);

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            yield return fader.Fade(1f, fadeOutDuration);

            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;

            yield return fader.Fade(0f, fadeInDuration);
            IsLoading = false;
        }
    }
}
