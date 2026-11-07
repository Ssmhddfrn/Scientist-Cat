using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ScientistCat.Core
{
    /// <summary>Full-screen color overlay used for scene transitions. Uses unscaled time.</summary>
    public class ScreenFader : MonoBehaviour
    {
        private CanvasGroup group;

        public float Alpha => group.alpha;

        public static ScreenFader Create(Transform parent, Color color)
        {
            var go = new GameObject("ScreenFader", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var image = new GameObject("Overlay", typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(go.transform, false);
            image.color = color;
            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var fader = go.AddComponent<ScreenFader>();
            fader.group = go.AddComponent<CanvasGroup>();
            fader.SetAlpha(0f);
            return fader;
        }

        public void SetAlpha(float alpha)
        {
            group.alpha = alpha;
            group.blocksRaycasts = alpha > 0.01f;
        }

        public IEnumerator Fade(float target, float duration)
        {
            float start = group.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(start, target, t / duration));
                yield return null;
            }
            SetAlpha(target);
        }
    }
}
