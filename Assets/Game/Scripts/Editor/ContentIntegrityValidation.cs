using System;
using FirstGame;
using UnityEditor;
using UnityEngine;

namespace FirstGameEditor
{
    /// <summary>Pre-build validation for authored level, tower and wave data.</summary>
    public static class ContentIntegrityValidation
    {
        private const string LevelCatalogPath = "Assets/Game/Resources/LevelCatalog.asset";
        private const string TowerCatalogPath = "Assets/Game/Data/TowerCatalog.asset";
        private const string DefaultEnemyPath = "Assets/Game/Data/EnemyConfig.asset";
        private const string DefaultEnemyPrefabPath = "Assets/Game/Prefabs/Enemy.prefab";

        [MenuItem("First Game/Validate Content Integrity")]
        public static void Validate()
        {
            LevelCatalog levels = Load<LevelCatalog>(LevelCatalogPath, "catalog");
            TowerCatalog towers = Load<TowerCatalog>(TowerCatalogPath, "catalog");
            EnemyConfig defaultEnemy = Load<EnemyConfig>(DefaultEnemyPath, "enemy config");
            GameObject defaultEnemyPrefab = Load<GameObject>(DefaultEnemyPrefabPath, "enemy prefab");

            ValidateEnemyPrefab(defaultEnemyPrefab, DefaultEnemyPrefabPath);
            Require(levels.levels != null && levels.levels.Length > 0, LevelCatalogPath, "levels", "must contain at least one level");
            for (int index = 0; index < levels.levels.Length; index++)
                ValidateLevel(levels.levels[index], index + 1, defaultEnemy, defaultEnemyPrefab);

            Require(towers.towers != null && towers.towers.Length > 0, TowerCatalogPath, "towers", "must contain at least one tower");
            foreach (TowerConfig tower in towers.towers)
                ValidateTower(tower);

            ValidateDamagedFixtures();
            Debug.Log("FIRST_GAME_CONTENT_INTEGRITY_VALIDATION_PASSED");
        }

        private static void ValidateLevel(LevelDefinition level, int expectedIndex, EnemyConfig defaultEnemy, GameObject defaultPrefab)
        {
            string path = PathOf(level, LevelCatalogPath);
            Require(level != null, LevelCatalogPath, "levels[" + (expectedIndex - 1) + "]", "references a missing level asset");
            Require(level.levelIndex == expectedIndex, path, "levelIndex", "must match its catalog order");
            Require(!string.IsNullOrWhiteSpace(level.sceneName), path, "sceneName", "is required");
            Require(level.route != null && level.route.Length >= 2, path, "route", "requires at least two points");
            Require(level.buildSpots != null && level.buildSpots.Length >= 3, path, "buildSpots", "requires at least three positions");
            Require(HasCompleteRoutePalette(level.routePalette), path, "routePalette", "requires centre, straight-border, and corner sprites");
            Require(level.waveConfig != null, path, "waveConfig", "references a missing WaveConfig");
            ValidateWaves(level.waveConfig, defaultEnemy, defaultPrefab);
        }

        private static bool HasCompleteRoutePalette(RouteTilePalette palette)
        {
            return palette != null && palette.roadCenterHorizontal && palette.roadCenterVertical &&
                   palette.roadBorderTopHorizontal && palette.roadBorderBottomHorizontal &&
                   palette.roadBorderLeftVertical && palette.roadBorderRightVertical &&
                   palette.roadBorderInnerCornerTopLeft && palette.roadBorderInnerCornerTopRight &&
                   palette.roadBorderInnerCornerBottomLeft && palette.roadBorderInnerCornerBottomRight &&
                   palette.roadBorderOuterCornerTopLeft && palette.roadBorderOuterCornerTopRight &&
                   palette.roadBorderOuterCornerBottomLeft && palette.roadBorderOuterCornerBottomRight;
        }

        private static void ValidateWaves(WaveConfig config, EnemyConfig defaultEnemy, GameObject defaultPrefab)
        {
            string path = PathOf(config, "<unsaved WaveConfig>");
            Require(config.waves != null && config.waves.Length > 0, path, "waves", "requires at least one wave");
            for (int waveIndex = 0; waveIndex < config.waves.Length; waveIndex++)
            {
                WaveDefinition wave = config.waves[waveIndex];
                string field = "waves[" + waveIndex + "]";
                Require(wave != null, path, field, "is missing");
                Require(wave.completionReward >= 0, path, field + ".completionReward", "cannot be negative");
                WaveGroupDefinition[] groups = wave.groups;
                if (groups == null || groups.Length == 0)
                    ValidateGroup(path, field, wave.enemyCount, wave.enemyConfig ? wave.enemyConfig : defaultEnemy,
                        wave.enemyPrefab ? wave.enemyPrefab : defaultPrefab, wave.maxHealth, wave.moveSpeed, wave.spawnInterval);
                else
                    for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
                    {
                        WaveGroupDefinition group = groups[groupIndex];
                        string groupField = field + ".groups[" + groupIndex + "]";
                        Require(group != null, path, groupField, "is missing");
                        ValidateGroup(path, groupField, group.enemyCount,
                            group.enemyConfig ? group.enemyConfig : wave.enemyConfig ? wave.enemyConfig : defaultEnemy,
                            group.enemyPrefab ? group.enemyPrefab : wave.enemyPrefab ? wave.enemyPrefab : defaultPrefab,
                            group.maxHealth > 0 ? group.maxHealth : wave.maxHealth,
                            group.moveSpeed > 0f ? group.moveSpeed : wave.moveSpeed, group.spawnInterval);
                    }
            }
        }

        private static void ValidateGroup(string path, string field, int count, EnemyConfig enemy, GameObject prefab,
            int health, float speed, float interval)
        {
            Require(count > 0, path, field + ".enemyCount", "must be greater than zero");
            Require(enemy != null, path, field + ".enemyConfig", "references a missing EnemyConfig");
            Require(prefab != null, path, field + ".enemyPrefab", "references a missing prefab");
            Require(prefab.GetComponent<Enemy>() != null, path, field + ".enemyPrefab", "must contain Enemy on its root");
            Require(health > 0 || enemy.maxHealth > 0, path, field + ".maxHealth", "requires positive health from the group, wave, or enemy config");
            Require(speed > 0f || enemy.moveSpeed > 0f, path, field + ".moveSpeed", "requires positive speed from the group, wave, or enemy config");
            Require(interval > 0f, path, field + ".spawnInterval", "must be greater than zero");
        }

        private static void ValidateTower(TowerConfig tower)
        {
            string path = PathOf(tower, TowerCatalogPath);
            Require(tower != null, TowerCatalogPath, "towers", "references a missing TowerConfig");
            Require(!string.IsNullOrWhiteSpace(tower.id), path, "id", "is required");
            Require(tower.buildCost >= 0, path, "buildCost", "cannot be negative");
            string error = TowerFactory.GetConfigurationError(tower);
            Require(error == null, path, "towerPrefab/levels", error);
            if (tower.specializations == null) return;
            for (int specializationIndex = 0; specializationIndex < tower.specializations.Length; specializationIndex++)
            {
                TowerSpecializationConfig specialization = tower.specializations[specializationIndex];
                string field = "specializations[" + specializationIndex + "]";
                Require(specialization != null, path, field, "is missing");
                Require(!string.IsNullOrWhiteSpace(specialization.displayName), path, field + ".displayName", "is required");
                Require(!string.IsNullOrWhiteSpace(specialization.description), path, field + ".description", "is required");
                Require(specialization.cost >= 0, path, field + ".cost", "cannot be negative");
                Require(specialization.stats != null, path, field + ".stats", "is required");
                Require(specialization.stats.damage > 0, path, field + ".stats.damage", "must be greater than zero");
                Require(specialization.stats.attackRate > 0f, path, field + ".stats.attackRate", "must be greater than zero");
                Require(specialization.stats.range > 0f, path, field + ".stats.range", "must be greater than zero");
            }
        }

        private static void ValidateEnemyPrefab(GameObject prefab, string path)
        {
            Enemy enemy = prefab ? prefab.GetComponent<Enemy>() : null;
            Require(enemy != null, path, "Enemy", "is required on the prefab root");
            SerializedObject serialized = new(enemy);
            Require(serialized.FindProperty("body")?.objectReferenceValue != null, path, "Enemy.body", "is required for the enemy visual");
            Require(serialized.FindProperty("healthFill")?.objectReferenceValue != null, path, "Enemy.healthFill", "is required for the enemy health behaviour");
        }

        private static void ValidateDamagedFixtures()
        {
            WaveConfig damaged = ScriptableObject.CreateInstance<WaveConfig>();
            try
            {
                damaged.waves = new[] { new WaveDefinition { enemyCount = 0 } };
                bool reported = false;
                try { ValidateWaves(damaged, null, null); }
                catch (Exception exception) { reported = exception.Message.Contains("<unsaved WaveConfig>") && exception.Message.Contains("enemyCount"); }
                Require(reported, "ContentIntegrityValidation", "damaged fixture", "must report the resource and invalid field");
            }
            finally { UnityEngine.Object.DestroyImmediate(damaged); }
        }

        private static T Load<T>(string path, string label) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Require(asset != null, path, label, "asset is missing");
            return asset;
        }

        private static string PathOf(UnityEngine.Object asset, string fallback)
        {
            string path = asset ? AssetDatabase.GetAssetPath(asset) : null;
            return string.IsNullOrEmpty(path) ? fallback : path;
        }

        private static void Require(bool condition, string path, string field, string message)
        {
            if (!condition) throw new Exception("Content validation failed: " + path + " :: " + field + " — " + message + ".");
        }
    }
}
