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
                        spawnInterval = 0.25f, completionReward = 50 },
                    new WaveDefinition { enemyCount = 7, healthMultiplier = 2f }
                };
                WaveSettings first = config.GetSettings(1, true, enemy, prefab);
                Require(first.EnemyCount == 3 && first.EnemyConfig == alternate &&
                    first.EnemyPrefab == alternatePrefab && first.MaxHealth == 300 &&
                    first.SpawnInterval == 0.25f && first.CompletionReward == 50,
                    "Per-wave settings must select count, type, health, timing and reward");
                WaveSettings second = config.GetSettings(2, true, enemy, prefab);
                Require(second.EnemyCount == 7 && second.EnemyConfig == enemy &&
                    second.EnemyPrefab == prefab && second.MaxHealth == 214,
                    "Default enemy and test growth must combine with per-wave settings");
                Require(config.GetSettings(3, true, enemy, prefab).EnemyCount == 10,
                    "Endless waves must fall back to default settings");
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
