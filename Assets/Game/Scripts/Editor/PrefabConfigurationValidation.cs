using System;
using FirstGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstGameEditor
{
    public static class PrefabConfigurationValidation
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Game/Scenes/Bootstrap.unity",
            "Assets/Game/Scenes/Level01.unity"
        };

        private static readonly string[] PrefabPaths =
        {
            "Assets/Game/Prefabs/BaseTower.prefab",
            "Assets/Game/Prefabs/RapidTower.prefab",
            "Assets/Game/Prefabs/CannonTower.prefab",
            "Assets/Game/Prefabs/BeamTower.prefab",
            "Assets/Game/Prefabs/Enemy.prefab",
            "Assets/Game/Prefabs/Projectile.prefab"
        };

        [MenuItem("First Game/Validate Prefabs And Configurations")]
        public static void Validate()
        {
            try
            {
                Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Tower.prefab") == null,
                    "The obsolete Tower prefab must not remain in the project");
                Require(AssetDatabase.LoadAssetAtPath<TowerConfig>("Assets/Game/Data/TowerConfig.asset") == null,
                    "The obsolete Tower configuration must not remain in the project");

                TowerCatalog catalog = AssetDatabase.LoadAssetAtPath<TowerCatalog>("Assets/Game/Data/TowerCatalog.asset");
                Require(catalog != null && catalog.towers != null && catalog.towers.Length > 0,
                    "Tower catalog must contain active configurations");
                foreach (TowerConfig config in catalog.towers)
                    Require(TowerFactory.GetConfigurationError(config) == null,
                        "Active tower configuration is invalid: " + (config ? config.name : "<missing>"));

                foreach (string prefabPath in PrefabPaths) ValidatePrefab(prefabPath);
                foreach (string scenePath in ScenePaths) ValidateScene(scenePath);
                Debug.Log("FIRST_GAME_PREFAB_CONFIGURATION_VALIDATION_PASSED");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void ValidatePrefab(string path)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Require(prefab != null, "Required prefab is missing: " + path);
                ValidateComponents(prefab, path);
            }
            finally
            {
                if (prefab != null) PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static void ValidateScene(string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects()) ValidateComponents(root, path);
        }

        private static void ValidateComponents(GameObject root, string owner)
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
                Require(component != null, "Missing Script in " + owner);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Prefab and configuration validation failed: " + message);
        }
    }
}
