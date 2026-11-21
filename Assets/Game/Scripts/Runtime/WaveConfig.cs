using System;
using UnityEngine;

namespace FirstGame
{
    [Serializable]
    public sealed class WaveGroupDefinition
    {
        [Min(1)] public int enemyCount = 10;
        [Tooltip("Leave empty to use the wave or controller enemy configuration.")]
        public EnemyConfig enemyConfig;
        [Tooltip("Leave empty to use the wave or controller enemy prefab.")]
        public GameObject enemyPrefab;
        [Min(0.01f)] public float healthMultiplier = 1f;
        [Min(0)] public int maxHealth;
        [Min(0f)] public float moveSpeed;
        [Min(0.01f)] public float spawnInterval = 0.8f;
    }

    [Serializable]
    public sealed class WaveDefinition
    {
        [Min(1)] public int enemyCount = 10;
        [Tooltip("Leave empty to use the controller's default enemy configuration.")]
        public EnemyConfig enemyConfig;
        [Tooltip("Leave empty to use the controller's default enemy prefab.")]
        public GameObject enemyPrefab;
        [Min(0.01f)] public float healthMultiplier = 1f;
        [Tooltip("Base maximum health for this wave. Zero retains legacy multiplier data.")]
        [Min(0)] public int maxHealth;
        [Tooltip("Base movement speed for this wave. Zero retains the enemy default.")]
        [Min(0f)] public float moveSpeed;
        [Min(0.01f)] public float spawnInterval = 0.8f;
        [Min(0)] public int completionReward = 25;
        [Tooltip("Optional ordered groups. Empty keeps the legacy single-group wave fields above.")]
        public WaveGroupDefinition[] groups = Array.Empty<WaveGroupDefinition>();
    }

    public readonly struct WaveSettings
    {
        public readonly int EnemyCount;
        public readonly EnemyConfig EnemyConfig;
        public readonly GameObject EnemyPrefab;
        public readonly int MaxHealth;
        public readonly float MoveSpeed;
        public readonly float SpawnInterval;
        public readonly int CompletionReward;
        public readonly WaveGroupSettings[] Groups;

        public WaveSettings(int enemyCount, EnemyConfig enemyConfig, GameObject enemyPrefab,
            int maxHealth, float moveSpeed, float spawnInterval, int completionReward,
            WaveGroupSettings[] groups = null)
        {
            EnemyCount = enemyCount;
            EnemyConfig = enemyConfig;
            EnemyPrefab = enemyPrefab;
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            SpawnInterval = spawnInterval;
            CompletionReward = completionReward;
            Groups = groups ?? Array.Empty<WaveGroupSettings>();
        }
    }

    public readonly struct WaveGroupSettings
    {
        public readonly int EnemyCount;
        public readonly EnemyConfig EnemyConfig;
        public readonly GameObject EnemyPrefab;
        public readonly int MaxHealth;
        public readonly float MoveSpeed;
        public readonly float SpawnInterval;

        public WaveGroupSettings(int enemyCount, EnemyConfig enemyConfig, GameObject enemyPrefab,
            int maxHealth, float moveSpeed, float spawnInterval)
        {
            EnemyCount = enemyCount;
            EnemyConfig = enemyConfig;
            EnemyPrefab = enemyPrefab;
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            SpawnInterval = spawnInterval;
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
            int baseHealth = definition != null && definition.maxHealth > 0
                ? definition.maxHealth : enemy ? enemy.maxHealth : 1;
            int health = (int)Math.Max(1d, Math.Min(int.MaxValue,
                Math.Round(baseHealth * multiplier, MidpointRounding.AwayFromZero)));
            float speed = definition != null && definition.moveSpeed > 0f
                ? definition.moveSpeed : enemy ? enemy.moveSpeed : 1f;
            WaveGroupSettings legacyGroup = new(Mathf.Max(1, definition?.enemyCount ?? enemyCount), enemy, prefab,
                health, Mathf.Max(.01f, speed), Mathf.Max(0.01f, definition?.spawnInterval ?? spawnInterval));
            WaveGroupDefinition[] definitions = definition?.groups;
            bool hasGroups = definitions != null && definitions.Length > 0;
            WaveGroupSettings[] groups = hasGroups ? new WaveGroupSettings[definitions.Length] : new[] { legacyGroup };
            int total = 0;
            for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
            {
                WaveGroupDefinition group = hasGroups ? definitions[groupIndex] : null;
                EnemyConfig groupEnemy = group?.enemyConfig != null ? group.enemyConfig : enemy;
                GameObject groupPrefab = group?.enemyPrefab != null ? group.enemyPrefab : prefab;
                double groupMultiplier = multiplier * Math.Max(.01d, group?.healthMultiplier ?? 1f);
                int groupBaseHealth = group != null && group.maxHealth > 0 ? group.maxHealth :
                    definition != null && definition.maxHealth > 0 ? definition.maxHealth : groupEnemy ? groupEnemy.maxHealth : 1;
                int groupHealth = (int)Math.Max(1d, Math.Min(int.MaxValue,
                    Math.Round(groupBaseHealth * groupMultiplier, MidpointRounding.AwayFromZero)));
                float groupSpeed = group != null && group.moveSpeed > 0f ? group.moveSpeed : speed;
                float groupInterval = group != null && group.spawnInterval > 0f ? group.spawnInterval : legacyGroup.SpawnInterval;
                int count = Mathf.Max(1, group?.enemyCount ?? legacyGroup.EnemyCount);
                groups[groupIndex] = new WaveGroupSettings(count, groupEnemy, groupPrefab, groupHealth,
                    Mathf.Max(.01f, groupSpeed), Mathf.Max(.01f, groupInterval));
                total += count;
            }
            return new WaveSettings(total, legacyGroup.EnemyConfig, legacyGroup.EnemyPrefab,
                legacyGroup.MaxHealth, legacyGroup.MoveSpeed, legacyGroup.SpawnInterval,
                Mathf.Max(0, definition?.completionReward ?? completionReward), groups);
        }
    }
}
