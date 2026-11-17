using System;
using UnityEngine;

namespace FirstGame
{
    [Serializable]
    public sealed class WaveDefinition
    {
        [Min(1)] public int enemyCount = 10;
        [Tooltip("Leave empty to use the controller's default enemy configuration.")]
        public EnemyConfig enemyConfig;
        [Tooltip("Leave empty to use the controller's default enemy prefab.")]
        public GameObject enemyPrefab;
        [Min(0.01f)] public float healthMultiplier = 1f;
        [Min(0.01f)] public float spawnInterval = 0.8f;
        [Min(0)] public int completionReward = 25;
    }

    public readonly struct WaveSettings
    {
        public readonly int EnemyCount;
        public readonly EnemyConfig EnemyConfig;
        public readonly GameObject EnemyPrefab;
        public readonly int MaxHealth;
        public readonly float SpawnInterval;
        public readonly int CompletionReward;

        public WaveSettings(int enemyCount, EnemyConfig enemyConfig, GameObject enemyPrefab,
            int maxHealth, float spawnInterval, int completionReward)
        {
            EnemyCount = enemyCount;
            EnemyConfig = enemyConfig;
            EnemyPrefab = enemyPrefab;
            MaxHealth = maxHealth;
            SpawnInterval = spawnInterval;
            CompletionReward = completionReward;
        }
    }

    [CreateAssetMenu(menuName = "First Game/Wave Config")]
    public sealed class WaveConfig : ScriptableObject
    {
        [Header("Default wave (also used after the configured sequence)")]
        [Min(1)] public int enemyCount = 10;
        [Min(0.01f)] public float spawnInterval = 0.8f;
        [Min(0)] public int completionReward = 25;
        [Header("Optional settings for waves 1, 2, 3, ...")]
        public WaveDefinition[] waves = Array.Empty<WaveDefinition>();
        [Header("Test profile progression")]
        [Min(0f)] public float testHealthGrowthPercent = 7f;

        public int NormalWaveCount => waves?.Length ?? 0;

        public WaveSettings GetSettings(int waveNumber, bool isTestProfile,
            EnemyConfig defaultEnemy, GameObject defaultPrefab)
        {
            int index = Mathf.Max(1, waveNumber) - 1;
            WaveDefinition definition = waves != null && index < waves.Length ? waves[index] : null;
            EnemyConfig enemy = definition?.enemyConfig != null ? definition.enemyConfig : defaultEnemy;
            GameObject prefab = definition?.enemyPrefab != null ? definition.enemyPrefab : defaultPrefab;
            double multiplier = Math.Max(0.01, definition?.healthMultiplier ?? 1f);
            if (isTestProfile)
                multiplier *= Math.Pow(1d + Math.Max(0d, testHealthGrowthPercent) / 100d, index);
            int health = (int)Math.Max(1d, Math.Min(int.MaxValue,
                Math.Round((enemy ? enemy.maxHealth : 1) * multiplier, MidpointRounding.AwayFromZero)));
            return new WaveSettings(Mathf.Max(1, definition?.enemyCount ?? enemyCount), enemy, prefab,
                health, Mathf.Max(0.01f, definition?.spawnInterval ?? spawnInterval),
                Mathf.Max(0, definition?.completionReward ?? completionReward));
        }
    }
}
