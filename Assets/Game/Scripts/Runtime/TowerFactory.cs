using System;
using UnityEngine;

namespace FirstGame
{
    public sealed class TowerFactory : MonoBehaviour
    {
        public static string GetConfigurationError(TowerConfig config)
        {
            if (!config) return "Missing tower configuration.";
            if (config.buildCost < 0) return "Tower price cannot be negative.";
            if (config.levels == null || config.levels.Length == 0 ||
                Array.Exists(config.levels, level => level == null))
                return "Tower configuration requires valid levels.";
            if (!config.towerPrefab || !config.towerPrefab.TryGetComponent(out TowerController tower))
                return "Tower prefab requires a TowerController on its root.";
            return tower.HasRequiredReferences ? null : "Tower prefab has missing component references.";
        }

        public TowerController Create(TowerConfig config, Vector3 position)
        {
            string error = GetConfigurationError(config);
            if (error != null)
            {
                Debug.LogError(error, config ? (UnityEngine.Object)config : this);
                return null;
            }

            GameObject instance = null;
            try
            {
                instance = Instantiate(config.towerPrefab, position, Quaternion.identity);
                TowerController tower = instance.GetComponent<TowerController>();
                tower.Initialize(config);
                return tower;
            }
            catch (Exception exception)
            {
                if (instance)
                {
                    instance.SetActive(false);
                    Destroy(instance);
                }
                Debug.LogException(exception, config);
                return null;
            }
        }
    }
}
