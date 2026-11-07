using System.Linq;
using ScientistCat.Combat;
using ScientistCat.Core;
using ScientistCat.Player;
using ScientistCat.TopDown;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScientistCat.EditorTools
{
    /// <summary>
    /// Builds TestArena as a copy of SampleScene (SampleScene itself is only read, never modified):
    /// combat, collision fixes, a training dummy, a hazard pad, HUD and a gate that opens when the dummy dies
    /// and leads to the first boss dimension.
    /// Menu: Scientist Cat > Rebuild Test Arena
    /// </summary>
    public static class TestArenaBuilder
    {
        internal const string SourceScene = "Assets/Scenes/SampleScene.unity";
        internal const string ArenaScene = "Assets/_Game/Scenes/TestArena.unity";
        private const string OldDimensionScene = "Assets/_Game/Scenes/TestDimension.unity"; // removed, was confusing
        private const string ArenaPortalTarget = "01_CrystalDimension";
        private static readonly Vector3 PortalPosition = new Vector3(-9f, 0f, -9f);

        internal const string ProjectilePrefabPath = "Assets/_Game/Prefabs/Projectiles/PlayerPhoton.prefab";
        internal const string UnlitSpriteMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        private const float ArenaHalfSize = 30f; // SampleScene ground: Plane (10x10) scaled 6
        private const float PlayerRadius = 0.65f; // ~ half the visible sprite width, so it never pokes into boxes

        [MenuItem("Scientist Cat/Rebuild Test Arena")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int groundLayer = LayerMask.NameToLayer("Ground");
            BuildProjectilePrefab(enemyLayer, groundLayer);
            GamePrefabBuilder.BuildAll();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OldDimensionScene) != null) AssetDatabase.DeleteAsset(OldDimensionScene);
            if (!BuildScene(ArenaScene, ArenaPortalTarget)) return;

            GameFlow.ApplyBuildOrder();
            Debug.Log("[TestArenaBuilder] TestArena rebuilt from SampleScene.");
        }

        private static bool BuildScene(string path, string portalTarget)
        {
            AssetDatabase.DeleteAsset(path);
            if (!AssetDatabase.CopyAsset(SourceScene, path))
            {
                Debug.LogError($"[TestArenaBuilder] Could not copy {SourceScene} to {path}.");
                return false;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer = LayerMask.NameToLayer("Enemy");

            // Load prefabs AFTER opening the scene: OpenScene unloads unused assets, which would
            // leave a reference loaded earlier pointing at a destroyed object (saved as "None").
            var projectile = AssetDatabase.LoadAssetAtPath<Projectile>(ProjectilePrefabPath);
            if (projectile == null)
            {
                Debug.LogError($"[TestArenaBuilder] Missing projectile prefab at {ProjectilePrefabPath}.");
                return false;
            }

            SetupPlayer(projectile, playerLayer);
            BuildBounds();
            var dummy = BuildDummy(enemyLayer);
            BuildHazard(playerLayer);
            BuildHud(dummy);
            BuildPortal(dummy, portalTarget);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return true;
        }

        private static void BuildHud(Health dummy)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GamePrefabBuilder.HudPath);
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var bossBar = hud.GetComponentInChildren<ScientistCat.UI.BossHealthBar>();
            var so = new SerializedObject(bossBar);
            so.FindProperty("autoShowTarget").objectReferenceValue = dummy;
            so.FindProperty("autoShowName").stringValue = "EĞİTİM KUKLASI";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Locked gate that opens when the dummy dies and loads the other test scene.</summary>
        private static void BuildPortal(Health dummy, string targetScene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GamePrefabBuilder.PortalPath);
            var portal = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            portal.transform.position = PortalPosition;
            var so = new SerializedObject(portal.GetComponent<Portal>());
            so.FindProperty("targetScene").stringValue = targetScene;
            so.FindProperty("unlockWhenDead").objectReferenceValue = dummy;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void EnsureInBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        internal static void SetupPlayer(Projectile projectilePrefab, int playerLayer)
        {
            var mover = Object.FindAnyObjectByType<TopDownPlayerController>();
            if (mover == null)
            {
                Debug.LogError("[TestArenaBuilder] No TopDownPlayerController found in the scene.");
                return;
            }

            var player = mover.gameObject;
            player.tag = "Player";
            foreach (var t in player.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = playerLayer;

            // Collision: wide enough that the sprite never visually enters boxes/walls.
            var cc = player.GetComponent<CharacterController>();
            cc.radius = PlayerRadius;

            // Upright sprite (no backward lean into objects), stretched to look camera-facing.
            var visual = player.GetComponentInChildren<TopDownSpriteVisual>();
            if (visual != null)
            {
                var so = new SerializedObject(visual);
                so.FindProperty("matchCameraPitch").boolValue = false;
                so.FindProperty("compensateUprightPitch").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var health = player.AddComponent<Health>();
            var hso = new SerializedObject(health);
            hso.FindProperty("maxHealth").intValue = 5;
            hso.FindProperty("invulnerabilityTime").floatValue = 1f;
            hso.ApplyModifiedPropertiesWithoutUndo();

            player.AddComponent<DamageFlash>();
            player.AddComponent<PlayerHitReaction>();
            player.AddComponent<PlayerDeath>();

            var firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(player.transform, false);
            firePoint.localPosition = new Vector3(0f, 1f, 0f);
            firePoint.gameObject.layer = playerLayer;

            var shooter = player.AddComponent<PlayerShooter>();
            var sso = new SerializedObject(shooter);
            sso.FindProperty("fireAction").objectReferenceValue = FindAction("Player/Attack");
            sso.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            sso.FindProperty("firePoint").objectReferenceValue = firePoint;
            sso.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Projectile BuildProjectilePrefab(int enemyLayer, int groundLayer)
        {
            var root = new GameObject("PlayerPhoton");
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Placeholder/PH_Glow.png");
            sr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitSpriteMaterial);
            sr.color = new Color(0.4f, 1f, 1f);
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            root.transform.localScale = Vector3.one * 0.6f;
            root.AddComponent<FaceCamera>();

            var projectile = root.AddComponent<Projectile>();
            var so = new SerializedObject(projectile);
            so.FindProperty("damageLayers").intValue = 1 << enemyLayer;
            so.FindProperty("obstacleLayers").intValue = (1 << 0) | (1 << groundLayer); // Default + Ground
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Projectile>();
        }

        /// <summary>Invisible walls on the ground edges so nothing can walk off the map.</summary>
        internal static void BuildBounds()
        {
            var parent = new GameObject("ArenaBounds").transform;
            const float height = 4f, thickness = 1f;
            float s = ArenaHalfSize + thickness * 0.5f;
            CreateWall(parent, "North", new Vector3(0, height / 2, s), new Vector3(ArenaHalfSize * 2 + 2, height, thickness));
            CreateWall(parent, "South", new Vector3(0, height / 2, -s), new Vector3(ArenaHalfSize * 2 + 2, height, thickness));
            CreateWall(parent, "East", new Vector3(s, height / 2, 0), new Vector3(thickness, height, ArenaHalfSize * 2));
            CreateWall(parent, "West", new Vector3(-s, height / 2, 0), new Vector3(thickness, height, ArenaHalfSize * 2));
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;
            wall.AddComponent<BoxCollider>().size = size;
        }

        private static Health BuildDummy(int enemyLayer)
        {
            var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = "TrainingDummy";
            dummy.layer = enemyLayer;
            dummy.transform.position = new Vector3(8f, 1f, 0f);
            dummy.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMaterial("M_Dummy", new Color(0.85f, 0.35f, 0.3f));

            var health = dummy.AddComponent<Health>();
            var so = new SerializedObject(health);
            so.FindProperty("maxHealth").intValue = 30;
            so.ApplyModifiedPropertiesWithoutUndo();

            var flash = dummy.AddComponent<DamageFlash>();
            var fso = new SerializedObject(flash);
            fso.FindProperty("blinkWhileInvulnerable").boolValue = false;
            fso.ApplyModifiedPropertiesWithoutUndo();
            return health;
        }

        private static void BuildHazard(int playerLayer)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "TestHazardPad";
            pad.transform.position = new Vector3(-8f, 0.05f, 4f);
            pad.transform.localScale = new Vector3(2f, 0.1f, 2f);
            pad.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMaterial("M_Hazard", new Color(1f, 0.55f, 0.1f));
            pad.GetComponent<BoxCollider>().isTrigger = true;

            var damage = pad.AddComponent<ContactDamage>();
            var so = new SerializedObject(damage);
            so.FindProperty("targetLayers").intValue = 1 << playerLayer;
            so.ApplyModifiedPropertiesWithoutUndo();

            pad.AddComponent<HazardPulse>();
        }

        internal static Material GetOrCreateMaterial(string name, Color color) => GamePrefabBuilder.GetLitMaterial(name, color);

        private static InputActionReference FindAction(string name)
        {
            return AssetDatabase.LoadAllAssetsAtPath(InputActionsPath)
                .OfType<InputActionReference>()
                .FirstOrDefault(r => r.name == name);
        }
    }
}
