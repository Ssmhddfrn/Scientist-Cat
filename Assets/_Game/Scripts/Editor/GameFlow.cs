using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ScientistCat.EditorTools
{
    /// <summary>
    /// Single place that defines the game's scene order, plus a menu to play from the first scene.
    /// Order: Main Menu -> TestArena -> Dimension 1 (the game starts at index 0), then SampleScene.
    /// </summary>
    public static class GameFlow
    {
        /// <summary>Game scenes in play order. Add Lab / later dimensions here as they are built.</summary>
        public static readonly string[] GameScenes =
        {
            MenuBuilder.MenuScene,          // opening menu with controls
            TestArenaBuilder.ArenaScene,    // warm-up arena; its gate opens when the dummy dies
            DimensionBuilder.CrystalScene,  // Dimension 1 boss
        };

        private static readonly string[] ExtraScenes =
        {
            TestArenaBuilder.SourceScene,
        };

        [MenuItem("Scientist Cat/▶ Play Game (from first scene)", priority = 0)]
        public static void PlayFromStart()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyBuildOrder();
            string first = GameScenes.FirstOrDefault(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p) != null);
            if (first == null)
            {
                Debug.LogError("[GameFlow] No game scene built yet. Run the Scientist Cat > Rebuild menus first.");
                return;
            }
            EditorSceneManager.OpenScene(first, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        /// <summary>Rewrites Build Settings so the game scenes come first. Other existing entries are kept at the end.</summary>
        public static void ApplyBuildOrder()
        {
            var ordered = new List<EditorBuildSettingsScene>();
            foreach (var path in GameScenes.Concat(ExtraScenes))
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null && ordered.All(s => s.path != path))
                    ordered.Add(new EditorBuildSettingsScene(path, true));

            foreach (var existing in EditorBuildSettings.scenes)
                if (ordered.All(s => s.path != existing.path) && AssetDatabase.LoadAssetAtPath<SceneAsset>(existing.path) != null)
                    ordered.Add(existing);

            EditorBuildSettings.scenes = ordered.ToArray();
            AssetDatabase.SaveAssets();
        }
    }
}
