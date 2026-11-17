using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstGame
{
    public sealed class WaveController : MonoBehaviour
    {
        [SerializeField] private WaveConfig config;
        [SerializeField] private EnemyConfig enemyConfig;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform[] route;
        private readonly Dictionary<GameObject, Queue<Enemy>> pools = new();
        private WaveSettings currentWave;
        private int spawned;
        private int remaining;
        private Coroutine routine;
        public int Remaining => remaining;
        public int NormalWaveCount => config ? config.NormalWaveCount : 0;
        public bool IsRunning { get; private set; }
        internal bool IsFinalNormalWave(int waveNumber) => config && waveNumber >= config.NormalWaveCount;
        public event Action Completed;

        internal string PrepareWave(int waveNumber, bool isTestProfile, out WaveSettings settings)
        {
            settings = default;
            if (!config) return "Missing wave configuration.";
            if (route == null || route.Length == 0 || Array.Exists(route, point => !point))
                return "Enemy route requires valid waypoints.";
            if (!isTestProfile && (config.NormalWaveCount == 0 || waveNumber > config.NormalWaveCount))
                return "Normal mode requires a configured finite wave sequence.";

            settings = config.GetSettings(waveNumber, isTestProfile, enemyConfig, enemyPrefab);
            if (!settings.EnemyConfig) return "Missing enemy configuration.";
            if (!settings.EnemyPrefab || !settings.EnemyPrefab.TryGetComponent(out Enemy enemy)
                || !enemy.HasRequiredReferences)
                return "Enemy prefab requires an Enemy with valid component references.";

            return null;
        }

        internal void StartWave(WaveSettings settings)
        {
            if (IsRunning) return;
            currentWave = settings;
            IsRunning = true;
            spawned = 0;
            remaining = currentWave.EnemyCount;
            routine = StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            // Keep separate pools so a wave cannot reuse a different enemy type's prefab.
            if (!pools.TryGetValue(currentWave.EnemyPrefab, out Queue<Enemy> pool))
            {
                pool = new Queue<Enemy>();
                pools.Add(currentWave.EnemyPrefab, pool);
            }
            Action<Enemy> onFinished = enemy => OnEnemyFinished(enemy, pool);
            while (spawned < currentWave.EnemyCount)
            {
                Enemy enemy = pool.Count > 0 ? pool.Dequeue() : Instantiate(currentWave.EnemyPrefab).GetComponent<Enemy>();
                spawned++;
                enemy.Spawn(currentWave.EnemyConfig, route, onFinished, currentWave.MaxHealth);
                yield return new WaitForSeconds(currentWave.SpawnInterval);
            }
        }

        private void OnEnemyFinished(Enemy enemy, Queue<Enemy> pool)
        {
            pool.Enqueue(enemy);
            remaining--;
            if (remaining > 0 || spawned < currentWave.EnemyCount || !IsRunning) return;
            StopWave();
            GameController.Instance.Economy.Add(currentWave.CompletionReward);
            Completed?.Invoke();
        }

        public void StopWave()
        {
            IsRunning = false;
            if (routine == null) return;
            StopCoroutine(routine);
            routine = null;
        }
    }
}
