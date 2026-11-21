using System;
using System.Diagnostics;
using FirstGame;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace FirstGameEditor
{
    public static class PerformanceValidation
    {
        private const string LevelPath = "Assets/Game/Data/Level02.asset";
        private const string EnemyPath = "Assets/Game/Data/EnemyConfig.asset";
        private const string EnemyPrefabPath = "Assets/Game/Prefabs/Enemy.prefab";
        private const int TowerCount = 6;
        private const int Iterations = 10000;

        [MenuItem("First Game/Validate Performance")]
        public static void Validate()
        {
            LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);
            EnemyConfig enemyConfig = AssetDatabase.LoadAssetAtPath<EnemyConfig>(EnemyPath);
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            Require(level && level.waveConfig && level.waveConfig.NormalWaveCount > 0,
                "Level 2 must define normal waves");
            Require(enemyConfig && enemyPrefab && enemyPrefab.GetComponent<Enemy>(),
                "Enemy assets must exist");

            WaveSettings maximumWave = level.waveConfig.GetSettings(
                level.waveConfig.NormalWaveCount, false, enemyConfig, enemyPrefab);
            Require(maximumWave.EnemyCount == 24,
                "Benchmark must track the current maximum configured wave");

            GameObject routeRoot = new("Performance validation route");
            GameObject enemyRoot = new("Performance validation enemies");
            GameObject targetingRoot = new("Performance validation targeting");
            Enemy[] enemies = new Enemy[maximumWave.EnemyCount];
            Transform[] route = new Transform[level.route.Length];
            try
            {
                Enemy.Active.Clear();
                for (int index = 0; index < route.Length; index++)
                {
                    GameObject point = new($"Route {index}");
                    point.transform.SetParent(routeRoot.transform);
                    point.transform.position = level.route[index];
                    route[index] = point.transform;
                }

                GC.Collect();
                long memoryBefore = Profiler.GetTotalAllocatedMemoryLong();
                float[] routeDistances = CacheRouteDistances(route);
                for (int index = 0; index < enemies.Length; index++)
                {
                    GameObject instance = UnityEngine.Object.Instantiate(enemyPrefab, enemyRoot.transform);
                    Enemy enemy = instance.GetComponent<Enemy>();
                    enemy.Spawn(maximumWave.EnemyConfig, route, _ => { }, maximumWave.MaxHealth,
                        maximumWave.MoveSpeed, routeDistances);
                    enemy.transform.position = Vector3.Lerp(route[0].position, route[1].position,
                        (index + 1f) / (enemies.Length + 1f));
                    enemies[index] = enemy;
                }
                long memoryWithMaximumWave = Profiler.GetTotalAllocatedMemoryLong();

                TowerTargeting targeting = targetingRoot.AddComponent<TowerTargeting>();
                Vector3[] towerPositions = new Vector3[TowerCount];
                for (int index = 0; index < towerPositions.Length; index++)
                    towerPositions[index] = level.buildSpots[index % level.buildSpots.Length];

                RunTargeting(targeting, towerPositions, 100);
                RunLegacyTargeting(enemies, route, towerPositions, 100);
                Stopwatch legacyTimer = Stopwatch.StartNew();
                int legacyChecksum = RunLegacyTargeting(enemies, route, towerPositions, Iterations);
                legacyTimer.Stop();
                Stopwatch optimizedTimer = Stopwatch.StartNew();
                int checksum = RunTargeting(targeting, towerPositions, Iterations);
                optimizedTimer.Stop();

                double calls = (double)Iterations * towerPositions.Length;
                double legacyMillisecondsPerSweep = legacyTimer.Elapsed.TotalMilliseconds / Iterations;
                double millisecondsPerSweep = optimizedTimer.Elapsed.TotalMilliseconds / Iterations;
                double millisecondsPerLookup = optimizedTimer.Elapsed.TotalMilliseconds / calls;
                long waveMemory = Math.Max(0L, memoryWithMaximumWave - memoryBefore);
                Require(checksum > 0 && legacyChecksum > 0,
                    "Targeting benchmark must select live enemies");
                Require(millisecondsPerSweep < legacyMillisecondsPerSweep,
                    "Cached route distances must outperform the legacy route traversal");
                Require(millisecondsPerSweep < 1d,
                    $"Targeting sweep budget exceeded: {millisecondsPerSweep:F4} ms");

                UnityEngine.Debug.Log(
                    "FIRST_GAME_PERFORMANCE_VALIDATION_PASSED " +
                    $"environment=UnityEditor; os={SystemInfo.operatingSystem}; cpu={SystemInfo.processorType}; " +
                    $"scenario=Level02Wave10; enemies={maximumWave.EnemyCount}; " +
                    $"towerSweeps={Iterations}; towers={towerPositions.Length}; " +
                    $"legacyElapsedMs={legacyTimer.Elapsed.TotalMilliseconds:F3}; " +
                    $"legacyMsPerSixTowerSweep={legacyMillisecondsPerSweep:F6}; " +
                    $"optimizedElapsedMs={optimizedTimer.Elapsed.TotalMilliseconds:F3}; " +
                    $"msPerSixTowerSweep={millisecondsPerSweep:F6}; " +
                    $"msPerLookup={millisecondsPerLookup:F6}; " +
                    $"allocatedMemoryDeltaBytes={waveMemory}; checksum={checksum}");
            }
            finally
            {
                Enemy.Active.Clear();
                UnityEngine.Object.DestroyImmediate(targetingRoot);
                UnityEngine.Object.DestroyImmediate(enemyRoot);
                UnityEngine.Object.DestroyImmediate(routeRoot);
            }
        }

        private static int RunTargeting(TowerTargeting targeting, Vector3[] positions, int iterations)
        {
            int checksum = 0;
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                foreach (Vector3 position in positions)
                {
                    Enemy target = targeting.FindTarget(position, 100f, TargetingRule.ClosestToBase);
                    if (target) checksum += target.SpawnVersion;
                }
            }
            return checksum;
        }

        private static int RunLegacyTargeting(Enemy[] enemies, Transform[] route,
            Vector3[] positions, int iterations)
        {
            int checksum = 0;
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                foreach (Vector3 origin in positions)
                {
                    Enemy best = null;
                    float bestScore = float.MinValue;
                    foreach (Enemy enemy in enemies)
                    {
                        float squaredDistance = (enemy.transform.position - origin).sqrMagnitude;
                        if (squaredDistance > 10000f) continue;
                        float remainingDistance = Vector3.Distance(enemy.transform.position,
                            route[1].position);
                        for (int index = 1; index < route.Length - 1; index++)
                            remainingDistance += Vector3.Distance(route[index].position,
                                route[index + 1].position);
                        float score = -remainingDistance;
                        if (score <= bestScore) continue;
                        best = enemy;
                        bestScore = score;
                    }
                    if (best) checksum += best.SpawnVersion;
                }
            }
            return checksum;
        }

        private static float[] CacheRouteDistances(Transform[] route)
        {
            float[] distances = new float[route.Length];
            for (int index = route.Length - 2; index >= 0; index--)
                distances[index] = distances[index + 1] +
                    Vector3.Distance(route[index].position, route[index + 1].position);
            return distances;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Performance validation failed: " + message);
        }
    }
}
