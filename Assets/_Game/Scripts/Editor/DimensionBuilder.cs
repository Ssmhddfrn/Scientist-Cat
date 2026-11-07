using ScientistCat.Bosses;
using ScientistCat.Combat;
using ScientistCat.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ScientistCat.EditorTools
{
    /// <summary>
    /// Builds the dimension scenes as copies of SampleScene (SampleScene itself is only read).
    /// Everything here is placeholder art: swap meshes/materials in the generated prefabs later.
    /// Menu: Scientist Cat > Rebuild Dimension 1 (Crystal)
    /// </summary>
    public static class DimensionBuilder
    {
        public const string CrystalScene = "Assets/_Game/Scenes/01_CrystalDimension.unity";
        private const string GolemPrefabPath = "Assets/_Game/Prefabs/Bosses/CrystalGolem.prefab";
        private const string ShardPrefabPath = "Assets/_Game/Prefabs/Projectiles/CrystalShard.prefab";
        private const string FallingCrystalPath = "Assets/_Game/Prefabs/Bosses/FallingCrystal.prefab";
        private const string GolemSpritePath = "Assets/_Game/Art/Bosses/CrystalGolem.png";
        private const string TelegraphMaterialPath = "Assets/_Game/Materials/M_Telegraph.mat";

        // Until Dimension 2 exists, the crystal gate leads back to the test arena.
        private const string CrystalNextScene = "TestArena";

        [MenuItem("Scientist Cat/Rebuild Dimension 1 (Crystal)")]
        public static void BuildCrystalDimension()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int groundLayer = LayerMask.NameToLayer("Ground");

            // 1) Prefabs first (opening a scene unloads unused assets, so we reload them by path afterwards).
            GamePrefabBuilder.BuildAll();
            BuildShardPrefab(playerLayer, groundLayer);
            BuildFallingCrystalPrefab();
            GetTelegraphMaterial();
            BuildGolemPrefab(enemyLayer, playerLayer);
            AssetDatabase.SaveAssets();

            // 2) Scene
            AssetDatabase.DeleteAsset(CrystalScene);
            if (!AssetDatabase.CopyAsset(TestArenaBuilder.SourceScene, CrystalScene))
            {
                Debug.LogError("[DimensionBuilder] Could not copy SampleScene.");
                return;
            }
            var scene = EditorSceneManager.OpenScene(CrystalScene, OpenSceneMode.Single);

            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "Box") Object.DestroyImmediate(go);

            ApplyCrystalAtmosphere();

            var photon = AssetDatabase.LoadAssetAtPath<Projectile>(TestArenaBuilder.ProjectilePrefabPath);
            TestArenaBuilder.SetupPlayer(photon, playerLayer);
            var player = GameObject.FindWithTag("Player");
            player.transform.position = new Vector3(0f, 0.05f, -5f); // boss is fully on screen from the start

            TestArenaBuilder.BuildBounds();
            BuildPillars();
            BuildDecor();

            var golem = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GolemPrefabPath));
            golem.transform.SetPositionAndRotation(new Vector3(0f, 0f, 3f), Quaternion.Euler(0f, 180f, 0f));
            var gso = new SerializedObject(golem.GetComponent<CrystalGolemBoss>());
            gso.FindProperty("activationRange").floatValue = 6.5f; // wakes when the player steps a little closer
            gso.ApplyModifiedPropertiesWithoutUndo();

            var portal = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GamePrefabBuilder.PortalPath));
            portal.transform.position = new Vector3(0f, 0f, 18f);
            var pso = new SerializedObject(portal.GetComponent<Portal>());
            pso.FindProperty("targetScene").stringValue = CrystalNextScene;
            pso.FindProperty("unlockWhenDead").objectReferenceValue = golem.GetComponent<Health>();
            pso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GamePrefabBuilder.HudPath));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            GameFlow.ApplyBuildOrder();
            Debug.Log("[DimensionBuilder] 01_CrystalDimension rebuilt.");
        }

        // ---------------- Atmosphere ----------------

        private static void ApplyCrystalAtmosphere()
        {
            var ground = GameObject.Find("Ground");
            if (ground != null)
                ground.GetComponent<Renderer>().sharedMaterial = GamePrefabBuilder.GetLitMaterial("M_CrystalGround", new Color(0.55f, 0.66f, 0.82f));

            foreach (var light in Object.FindObjectsByType<Light>())
            {
                if (light.type != LightType.Directional) continue;
                light.color = new Color(0.75f, 0.87f, 1f);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.74f, 0.92f);
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 70f;
        }

        /// <summary>Crystal pillars: solid cover that blocks the player, the boss and shards.</summary>
        private static void BuildPillars()
        {
            var parent = new GameObject("CrystalPillars").transform;
            var mat = GamePrefabBuilder.GetLitMaterial("M_CrystalPillar", new Color(0.6f, 0.85f, 1f));
            Vector3[] spots =
            {
                new Vector3(-10, 0, 0), new Vector3(10, 0, 0),
                new Vector3(-7, 0, 13), new Vector3(7, 0, 13),
                new Vector3(-13, 0, -9), new Vector3(13, 0, -9),
            };
            foreach (var s in spots)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "CrystalPillar";
                pillar.transform.SetParent(parent, false);
                pillar.transform.SetPositionAndRotation(s + Vector3.up * 2f, Quaternion.Euler(0f, 45f, 0f));
                pillar.transform.localScale = new Vector3(1.6f, 4f, 1.6f);
                pillar.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        /// <summary>Small floor crystals for atmosphere (no colliders).</summary>
        private static void BuildDecor()
        {
            var parent = new GameObject("CrystalDecor").transform;
            var mat = GamePrefabBuilder.GetLitMaterial("M_CrystalDecor", new Color(0.75f, 0.9f, 1f));
            var rng = new System.Random(7);
            for (int i = 0; i < 40; i++)
            {
                float x = (float)(rng.NextDouble() * 56 - 28), z = (float)(rng.NextDouble() * 56 - 28);
                if (new Vector2(x, z - 4f).magnitude < 12f) continue; // keep the fight area clear
                float size = 0.3f + (float)rng.NextDouble() * 0.6f;
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = "Shard";
                Object.DestroyImmediate(c.GetComponent<Collider>());
                c.transform.SetParent(parent, false);
                c.transform.SetPositionAndRotation(new Vector3(x, size * 0.6f, z), Quaternion.Euler(45f, (float)rng.NextDouble() * 360f, 45f));
                c.transform.localScale = new Vector3(size, size * 1.8f, size);
                c.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        // ---------------- Prefabs ----------------

        private static Material GetTelegraphMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(TelegraphMaterialPath);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", new Color(1f, 0.25f, 0.2f));
            AssetDatabase.CreateAsset(mat, TelegraphMaterialPath);
            return mat;
        }

        private static void BuildShardPrefab(int playerLayer, int groundLayer)
        {
            var root = new GameObject("CrystalShard");
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Placeholder/PH_Diamond.png");
            sr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(TestArenaBuilder.UnlitSpriteMaterial);
            sr.color = new Color(0.55f, 0.9f, 1f);
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            root.transform.localScale = Vector3.one * 0.8f;
            root.AddComponent<FaceCamera>();

            var p = root.AddComponent<Projectile>();
            var so = new SerializedObject(p);
            so.FindProperty("speed").floatValue = 9f;
            so.FindProperty("lifetime").floatValue = 4f;
            so.FindProperty("radius").floatValue = 0.3f;
            so.FindProperty("damageLayers").intValue = 1 << playerLayer;
            so.FindProperty("obstacleLayers").intValue = (1 << 0) | (1 << groundLayer);
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ShardPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildFallingCrystalPrefab()
        {
            var root = new GameObject("FallingCrystal");
            var c = Visual(PrimitiveType.Cube, "Crystal", root.transform, new Vector3(0, 0.9f, 0), Quaternion.Euler(45f, 0f, 45f),
                new Vector3(0.9f, 1.6f, 0.9f), GamePrefabBuilder.GetLitMaterial("M_CrystalFalling", new Color(0.7f, 0.95f, 1f)));
            c.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            PrefabUtility.SaveAsPrefabAsset(root, FallingCrystalPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildGolemPrefab(int enemyLayer, int playerLayer)
        {
            var root = new GameObject("CrystalGolem");
            root.layer = enemyLayer;

            var cc = root.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 1.6f, 0f);
            cc.height = 3.2f;
            cc.radius = 1.3f;
            cc.stepOffset = 0.3f;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            // Hand-drawn sprite (pivot at the feet), upright and turned to the camera like Wilson.
            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(visual, false);
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GolemSpritePath);
            sr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(TestArenaBuilder.UnlitSpriteMaterial);
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            var walk = spriteGo.AddComponent<SpriteWalkVisual>();
            var walkSo = new SerializedObject(walk);
            walkSo.FindProperty("body").objectReferenceValue = root.transform;
            walkSo.ApplyModifiedPropertiesWithoutUndo();
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = enemyLayer;

            var health = root.AddComponent<Health>();
            var hso = new SerializedObject(health);
            hso.FindProperty("maxHealth").intValue = 70;
            hso.ApplyModifiedPropertiesWithoutUndo();

            var flash = root.AddComponent<DamageFlash>();
            var fso = new SerializedObject(flash);
            fso.FindProperty("blinkWhileInvulnerable").boolValue = false;
            fso.FindProperty("flashCount").intValue = 1;
            fso.ApplyModifiedPropertiesWithoutUndo();

            var contact = root.AddComponent<ContactDamage>();
            var cso = new SerializedObject(contact);
            cso.FindProperty("targetLayers").intValue = 1 << playerLayer;
            cso.ApplyModifiedPropertiesWithoutUndo();

            var boss = root.AddComponent<CrystalGolemBoss>();
            var bso = new SerializedObject(boss);
            bso.FindProperty("bossName").stringValue = "KRİSTAL GOLEM";
            bso.FindProperty("shardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Projectile>(ShardPrefabPath);
            bso.FindProperty("fallingCrystalPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(FallingCrystalPath);
            bso.FindProperty("telegraphMaterial").objectReferenceValue = GetTelegraphMaterial();
            bso.FindProperty("visual").objectReferenceValue = visual;
            bso.FindProperty("playerLayers").intValue = 1 << playerLayer;
            bso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, GolemPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static GameObject Visual(PrimitiveType type, string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
