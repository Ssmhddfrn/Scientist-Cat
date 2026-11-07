using ScientistCat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static ScientistCat.EditorTools.GamePrefabBuilder;

namespace ScientistCat.EditorTools
{
    /// <summary>
    /// Builds the opening menu scene: title, controls table, Start / Quit.
    /// Menu: Scientist Cat > Rebuild Main Menu
    /// </summary>
    public static class MenuBuilder
    {
        public const string MenuScene = "Assets/_Game/Scenes/00_MainMenu.unity";
        private const string SquareSprite = "Assets/_Game/Art/Placeholder/PH_Square.png";

        private static readonly Color Background = new Color(0.04f, 0.05f, 0.12f);
        private static readonly Color Accent = new Color(0.3f, 0.85f, 1f);
        private static readonly Color Ink = new Color(0.04f, 0.05f, 0.12f);

        private static readonly string[,] Controls =
        {
            { "W A S D", "Yürü" },
            { "FARE", "Nişan al" },
            { "SOL TIK", "Foton ateşle" },
            { "TEKERLEK", "Kamerayı yaklaştır / uzaklaştır" },
            { "R", "Ölünce yeniden doğ" },
        };

        [MenuItem("Scientist Cat/Rebuild Main Menu")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSprite);

            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;

            var canvasGo = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.GetComponent<RectTransform>();

            // Title + story line
            var title = Label("Title", root, font, "SCIENTIST CAT", 130, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0, 390), new Vector2(1600, 160));
            title.color = Accent;

            // Controls panel
            var panel = Rect("Controls", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(980, 470));
            Img("Frame", panel, square, new Color(1f, 1f, 1f, 0.15f), true);
            var inner = Img("Bg", panel, square, new Color(0f, 0f, 0f, 0.45f), true);
            inner.rectTransform.offsetMin = new Vector2(3, 3);
            inner.rectTransform.offsetMax = new Vector2(-3, -3);
            var header = Label("Header", panel, font, "KONTROLLER", 40, TextAnchor.MiddleCenter);
            Place(header.rectTransform, new Vector2(0, 190), new Vector2(900, 60));
            header.color = Accent;

            int rows = Controls.GetLength(0);
            for (int i = 0; i < rows; i++)
            {
                float y = 115 - i * 72;
                var key = Rect("Key_" + i, panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-250, y), new Vector2(300, 58));
                Img("Face", key, square, Color.white, true);
                var keyText = Label("Text", key, font, Controls[i, 0], 30, TextAnchor.MiddleCenter);
                keyText.color = Ink;
                Object.DestroyImmediate(keyText.GetComponent<Outline>());

                var desc = Label("Desc_" + i, panel, font, Controls[i, 1], 32, TextAnchor.MiddleLeft);
                desc.rectTransform.anchorMin = desc.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                desc.rectTransform.pivot = new Vector2(0f, 0.5f);
                desc.rectTransform.anchoredPosition = new Vector2(-70, y);
                desc.rectTransform.sizeDelta = new Vector2(540, 58);
            }

            // Buttons
            var start = MakeButton(root, font, square, "StartButton", "BAŞLA", new Vector2(0, -340), new Vector2(420, 100), Accent, Ink, 54);
            var quit = MakeButton(root, font, square, "QuitButton", "ÇIKIŞ", new Vector2(0, -455), new Vector2(260, 64), new Color(1f, 1f, 1f, 0.2f), Color.white, 30);

            var menu = canvasGo.AddComponent<MainMenu>();
            Set(menu, "startButton", start);
            Set(menu, "quitButton", quit);
            Set(menu, "pulseTarget", start.GetComponent<RectTransform>());

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            EditorSceneManager.SaveScene(scene, MenuScene);
            GameFlow.ApplyBuildOrder();
            Debug.Log("[MenuBuilder] 00_MainMenu rebuilt.");
        }

        private static Button MakeButton(RectTransform parent, Font font, Sprite sprite, string name, string text, Vector2 pos, Vector2 size, Color bg, Color fg, int fontSize)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            var img = Img("Bg", rt, sprite, bg, true);
            var label = Label("Text", rt, font, text, fontSize, TextAnchor.MiddleCenter);
            label.color = fg;
            if (fg != Color.white) Object.DestroyImmediate(label.GetComponent<Outline>());
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            img.raycastTarget = true;
            return button;
        }
    }
}
