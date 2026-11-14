using UnityEngine;
namespace FirstGame
{
    public sealed class TowerFactory:MonoBehaviour
    {
        public TowerController Create(TowerConfig config,Vector3 position){if(!config||!config.towerPrefab){Debug.LogError("TowerConfig has no prefab",config);return null;}GameObject instance=Instantiate(config.towerPrefab,position,Quaternion.identity);TowerController tower=instance.GetComponent<TowerController>();tower.Initialize(config);return tower;}
    }
}
