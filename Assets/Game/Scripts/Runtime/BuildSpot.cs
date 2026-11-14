using UnityEngine;

namespace FirstGame
{
    public sealed class BuildSpot : MonoBehaviour
    {
        [SerializeField] private TowerFactory towerFactory;
        [SerializeField] private SpriteRenderer marker;
        public bool IsOccupied { get; private set; }
        private void OnMouseDown()
        {
            if (!IsOccupied && GameController.Instance.CanInteract) GameController.Instance.UI.SelectBuildSpot(this);
        }
        public TowerController Build(TowerConfig config)
        {
            if (IsOccupied || !GameController.Instance.Economy.Spend(config.buildCost)) return null;
            TowerController tower = towerFactory.Create(config, transform.position);
            IsOccupied = true; marker.gameObject.SetActive(false); GameController.Instance.RegisterTower(); return tower;
        }
    }
}
