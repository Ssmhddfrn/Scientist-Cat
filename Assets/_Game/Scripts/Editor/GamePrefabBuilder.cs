using ScientistCat.Core;
using ScientistCat.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ScientistCat.EditorTools
{
    /// <summary>
    /// Generates the shared placeholder prefabs (HUD, Portal). Re-run after layout changes;
    /// once real art exists, edit the prefabs directly instead and stop regenerating them.
    /// Menu: Scientist Cat > Rebuild Shared Prefabs
    /// </summary>
    public static class GamePrefabBuilder
    {
        public const string HudPath = "Assets/_Game/Prefabs/UI/HUD.prefab";
        public const string PortalPath = "Assets/_Game/Prefabs/Environment/Portal.prefab";
        private const string SquareSprite = "Assets/_Game/Art/Placeholder/PH_Square.png";
        /// <summary>Russo One: bold, readable techno font with full Turkish support (SIL OFL).</summary>
        public const string FontPath = "Assets/_Game/Art/Fonts/RussoOne.ttf";

        [MenuItem("Scientist Cat/Rebuild Shared Prefabs")]
        public static void BuildAll()
        {
            BuildHud();
            BuildPortal();
            AssetDatabase.SaveAssets();
            Debug.Log("[GamePrefabBuilder] HUD and Portal prefabs rebuilt.");
        }

        // ---------------- HUD ----------------

        public static GameObject BuildHud()
        {
            var heavy = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSprite);

            var frameColor = new Color(1f, 1f, 1f, 0.85f);
            var trackColor = new Color(0.06f, 0.06f, 0.1f, 0.85f);

            var hud = new GameObject("HUD", typeof(RectTransform));
            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            hud.AddComponent<GraphicRaycaster>();

            // ---- Player health (top-left): name above, framed bar, value inside ----
            var playerBar = Rect("PlayerHealth", hud.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -70), new Vector2(420, 34));
            var pName = Label("Name", playerBar, heavy, "BİLİM İNSANI", 24, TextAnchor.LowerLeft);
            AboveBar(pName.rectTransform);
            Frame(playerBar, square, frameColor);
            Img("Track", playerBar, square, trackColor, true);
            var pDelayed = Img("Delayed", playerBar, square, new Color(1f, 0.85f, 0.4f), true, filled: true);
            var pFill = Img("Fill", playerBar, square, new Color(0.3f, 0.9f, 0.45f), true, filled: true);
            var pLabel = Label("Value", playerBar, heavy, "5 / 5", 22, TextAnchor.MiddleCenter);
            var phb = playerBar.gameObject.AddComponent<PlayerHealthBar>();
            Set(phb, "fill", pFill); Set(phb, "delayedFill", pDelayed); Set(phb, "label", pLabel);
            SetString(phb, "labelFormat", "{0} / {1}");

            // ---- Boss health (top-center): name above, framed bar ----
            var bossHolder = Rect("BossHealth", hud.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(820, 30));
            var bossRoot = Rect("Root", bossHolder, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bName = Label("Name", bossRoot, heavy, "BOSS", 30, TextAnchor.LowerCenter);
            AboveBar(bName.rectTransform);
            bName.color = new Color(1f, 0.85f, 0.85f);
            Frame(bossRoot, square, frameColor);
            Img("Track", bossRoot, square, trackColor, true);
            var bDelayed = Img("Delayed", bossRoot, square, new Color(1f, 0.9f, 0.6f), true, filled: true);
            var bFill = Img("Fill", bossRoot, square, new Color(0.9f, 0.2f, 0.3f), true, filled: true);
            var bhb = bossHolder.gameObject.AddComponent<BossHealthBar>();
            Set(bhb, "root", bossRoot.gameObject); Set(bhb, "fill", bFill); Set(bhb, "delayedFill", bDelayed); Set(bhb, "nameLabel", bName);

            // ---- Death screen ----
            var death = Rect("DeathScreen", hud.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var panel = Rect("Panel", death, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            Img("Dim", panel, square, new Color(0.04f, 0f, 0.06f, 0.85f), true);

            // Just one big pulsing "R" key in the middle (also clickable).
            var buttonRt = Rect("RetryKey", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 220));
            var buttonImg = Img("Border", buttonRt, square, new Color(0.3f, 0.85f, 1f), true);
            buttonImg.raycastTarget = true; // so clicking the key works, not only pressing R
            var keyFace = Img("Face", buttonRt, square, Color.white, true);
            keyFace.rectTransform.offsetMin = new Vector2(12, 12);
            keyFace.rectTransform.offsetMax = new Vector2(-12, -12);
            var keyText = Label("Letter", buttonRt, heavy, "R", 140, TextAnchor.MiddleCenter);
            keyText.color = new Color(0.05f, 0.05f, 0.12f);
            Object.DestroyImmediate(keyText.GetComponent<Outline>());
            var button = buttonRt.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImg;

            var ds = death.gameObject.AddComponent<DeathScreen>();
            Set(ds, "panel", group); Set(ds, "retryButton", button); Set(ds, "pulseTarget", buttonRt);

            // EventSystem for the retry button (new Input System module)
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(hud.transform, false);

            var prefab = PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
            Object.DestroyImmediate(hud);
            return prefab;
        }

        /// <summary>Places a label just above its parent bar.</summary>
        private static void AboveBar(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, 40);
            rt.anchoredPosition = new Vector2(0, 6);
        }

        /// <summary>Thin border: an image slightly larger than the bar, drawn behind it.</summary>
        private static void Frame(RectTransform bar, Sprite sprite, Color color)
        {
            var frame = Img("Frame", bar, sprite, color, true);
            frame.rectTransform.offsetMin = new Vector2(-3, -3);
            frame.rectTransform.offsetMax = new Vector2(3, 3);
        }

        // ---------------- Portal ----------------

        public static GameObject BuildPortal()
        {
            var portal = new GameObject("Portal");
            var trigger = portal.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 1.5f, 0);
            trigger.size = new Vector3(2.4f, 3f, 1.2f);

            var mat = GetLitMaterial("M_Portal", new Color(0.3f, 0.9f, 1f));

            var pad = Primitive(PrimitiveType.Cylinder, "Pad", portal.transform, new Vector3(0, 0.03f, 0), Quaternion.identity, new Vector3(2.6f, 0.03f, 2.6f), mat);
            var ring = Primitive(PrimitiveType.Cylinder, "Ring", portal.transform, new Vector3(0, 1.6f, 0), Quaternion.Euler(90, 0, 0), new Vector3(2.4f, 0.05f, 2.4f), mat);
            // A bar spinning around Z (toward the camera) in front of the ring.
            var spinner = Primitive(PrimitiveType.Cube, "Spinner", portal.transform, new Vector3(0, 1.6f, -0.1f), Quaternion.identity, new Vector3(0.25f, 2.1f, 0.25f), mat);

            var p = portal.AddComponent<Portal>();
            var so = new SerializedObject(p);
            var tints = so.FindProperty("tintRenderers");
            tints.arraySize = 3;
            tints.GetArrayElementAtIndex(0).objectReferenceValue = pad.GetComponent<Renderer>();
            tints.GetArrayElementAtIndex(1).objectReferenceValue = ring.GetComponent<Renderer>();
            tints.GetArrayElementAtIndex(2).objectReferenceValue = spinner.GetComponent<Renderer>();
            so.FindProperty("spinner").objectReferenceValue = spinner.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(portal, PortalPath);
            Object.DestroyImmediate(portal);
            return prefab;
        }

        // ---------------- helpers ----------------

        public static Material GetLitMaterial(string name, Color color)
        {
            string path = $"Assets/_Game/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // visuals only; the portal root has the trigger
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        internal static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin == anchorMax ? anchorMin.x : 0.5f, anchorMin == anchorMax ? anchorMin.y : 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        internal static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        internal static Image Img(string name, RectTransform parent, Sprite sprite, Color color, bool stretch, bool filled = false)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (filled)
            {
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = 0;
            }
            return img;
        }

        internal static Text Label(string name, RectTransform parent, Font font, string text, int size, TextAnchor anchor)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            return t;
        }

        internal static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
