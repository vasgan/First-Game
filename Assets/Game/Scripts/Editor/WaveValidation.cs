using System;
using FirstGame;
using UnityEditor;
using UnityEngine;

namespace FirstGameEditor
{
    public static class WaveValidation
    {
        [MenuItem("First Game/Validate Waves")]
        public static void Validate()
        {
            WaveConfig normalConfig = AssetDatabase.LoadAssetAtPath<WaveConfig>("Assets/Game/Data/WaveConfig.asset");
            EnemyConfig normalEnemy = AssetDatabase.LoadAssetAtPath<EnemyConfig>("Assets/Game/Data/EnemyConfig.asset");
            GameObject normalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemy.prefab");
            WaveConfig config = ScriptableObject.CreateInstance<WaveConfig>();
            EnemyConfig enemy = ScriptableObject.CreateInstance<EnemyConfig>();
            EnemyConfig alternate = ScriptableObject.CreateInstance<EnemyConfig>();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemy.prefab");
            GameObject alternatePrefab = new("Alternate enemy prefab");
            GameObject routePoint = new("Validation route point");
            GameObject instance = null;
            try
            {
                Require(prefab != null, "Enemy prefab must exist");
                Require(normalConfig != null && normalEnemy != null && normalPrefab != null,
                    "Normal wave assets must exist");
                Require(normalConfig.NormalWaveCount == 10,
                    "Normal mode must define exactly ten waves");
                int[] normalCounts = { 6, 8, 10, 11, 12, 14, 15, 16, 18, 20 };
                int[] normalHealth = { 80, 90, 100, 110, 120, 130, 145, 160, 180, 200 };
                int[] normalRewards = { 15, 18, 20, 22, 24, 26, 28, 30, 32, 35 };
                for (int i = 0; i < normalConfig.NormalWaveCount; i++)
                {
                    WaveSettings normal = normalConfig.GetSettings(i + 1, false, normalEnemy, normalPrefab);
                    Require(normal.EnemyCount == normalCounts[i] && normal.MaxHealth == normalHealth[i] &&
                        normal.CompletionReward == normalRewards[i] && normal.MoveSpeed == 1.5f && normal.SpawnInterval > 0f,
                        "Normal wave " + (i + 1) + " must preserve its configured settings");
                }
                enemy.maxHealth = 100;
                alternate.maxHealth = 200;
                int[] expected = { 100, 107, 114, 123, 131 };
                for (int i = 0; i < expected.Length; i++)
                {
                    Require(config.GetSettings(i + 1, true, enemy, prefab).MaxHealth == expected[i],
                        "Incorrect test health for wave " + (i + 1));
                    Require(config.GetSettings(i + 1, false, enemy, prefab).MaxHealth == 100,
                        "Test progression must not affect normal profile");
                }
                Require(config.GetSettings(100000, true, enemy, prefab).MaxHealth == int.MaxValue,
                    "Long test sessions must not overflow health");
                config.waves = new[]
                {
                    new WaveDefinition { enemyCount = 3, enemyConfig = alternate,
                        enemyPrefab = alternatePrefab, healthMultiplier = 1.5f,
                        spawnInterval = 0.25f, completionReward = 50, moveSpeed = 3.5f },
                    new WaveDefinition { enemyCount = 7, healthMultiplier = 2f }
                };
                WaveSettings first = config.GetSettings(1, true, enemy, prefab);
                Require(first.EnemyCount == 3 && first.EnemyConfig == alternate &&
                    first.EnemyPrefab == alternatePrefab && first.MaxHealth == 300 && first.MoveSpeed == 3.5f &&
                    first.SpawnInterval == 0.25f && first.CompletionReward == 50,
                    "Per-wave settings must select count, type, health, timing and reward");
                WaveSettings second = config.GetSettings(2, true, enemy, prefab);
                Require(second.EnemyCount == 7 && second.EnemyConfig == enemy &&
                    second.EnemyPrefab == prefab && second.MaxHealth == 214,
                    "Default enemy and test growth must combine with per-wave settings");
                Require(config.GetSettings(3, true, enemy, prefab).EnemyCount == 10,
                    "Endless waves must fall back to default settings");
                config.waves = new[]
                {
                    new WaveDefinition
                    {
                        completionReward = 42,
                        groups = new[]
                        {
                            new WaveGroupDefinition { enemyCount = 2, enemyConfig = enemy, enemyPrefab = prefab,
                                maxHealth = 90, moveSpeed = 1.25f, spawnInterval = .2f },
                            new WaveGroupDefinition { enemyCount = 3, enemyConfig = alternate, enemyPrefab = alternatePrefab,
                                maxHealth = 140, moveSpeed = 2f, spawnInterval = .4f }
                        }
                    }
                };
                WaveSettings grouped = config.GetSettings(1, false, enemy, prefab);
                Require(grouped.EnemyCount == 5 && grouped.Groups.Length == 2 && grouped.CompletionReward == 42,
                    "A wave must total its ordered groups and preserve its reward");
                Require(grouped.Groups[0].EnemyCount == 2 && grouped.Groups[0].EnemyPrefab == prefab &&
                    grouped.Groups[0].MaxHealth == 90 && grouped.Groups[1].EnemyCount == 3 &&
                    grouped.Groups[1].EnemyPrefab == alternatePrefab && grouped.Groups[1].MaxHealth == 140,
                    "Each group must keep its prefab, order, count and settings");
                Require(enemy.maxHealth == 100 && alternate.maxHealth == 200,
                    "Wave generation must not modify shared enemy assets");
                instance = UnityEngine.Object.Instantiate(prefab);
                Enemy spawned = instance.GetComponent<Enemy>();
                spawned.Spawn(enemy, new[] { routePoint.transform }, _ => { }, second.MaxHealth);
                Require(spawned.Health == 214 && spawned.MaxHealth == 214 && spawned.IsAlive,
                    "Spawn must apply wave health to the enemy instance");
                Require(enemy.maxHealth == 100, "Spawn must preserve the shared enemy configuration");
                Debug.Log("FIRST_GAME_WAVE_VALIDATION_PASSED");
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                UnityEngine.Object.DestroyImmediate(routePoint);
                UnityEngine.Object.DestroyImmediate(alternatePrefab);
                UnityEngine.Object.DestroyImmediate(config);
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(alternate);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Wave validation failed: " + message);
        }
    }
}
