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
        public bool IsRunning { get; private set; }
        public event Action Completed;

        public void StartWave(int waveNumber, bool isTestProfile)
        {
            if (IsRunning) return;
            currentWave = config.GetSettings(waveNumber, isTestProfile, enemyConfig, enemyPrefab);
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
            IsRunning = false;
            GameController.Instance.Economy.Add(currentWave.CompletionReward);
            Completed?.Invoke();
        }

        public void StopWave() { IsRunning = false; if (routine != null) StopCoroutine(routine); }
    }
}
