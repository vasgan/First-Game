using UnityEngine;

namespace FirstGame
{
    public sealed class BuildSpot : MonoBehaviour
    {
        [SerializeField] private TowerFactory towerFactory;
        [SerializeField] private SpriteRenderer marker;
        private bool isBuilding;
        public bool IsOccupied { get; private set; }
        private void OnMouseDown()
        {
            if (!UiInputGuard.IsPointerOverUi() && !IsOccupied && GameController.Instance.CanInteract)
                GameController.Instance.UI.SelectBuildSpot(this);
        }
        public TowerController Build(TowerConfig config)
        {
            GameController game = GameController.Instance;
            if (IsOccupied || isBuilding || !game || !game.CanInteract) return null;

            if (!towerFactory || !marker)
            {
                Debug.LogError("Build spot requires a tower factory and a marker.", this);
                return null;
            }

            if (config && game.Economy.Gold < config.buildCost) return null;

            isBuilding = true;
            try
            {
                TowerController tower = towerFactory.Create(config, transform.position);
                if (!tower) return null;
                if (!game.Economy.Spend(config.buildCost))
                {
                    tower.gameObject.SetActive(false);
                    Destroy(tower.gameObject);
                    return null;
                }

                IsOccupied = true;
                marker.gameObject.SetActive(false);
                tower.SetBuildSpot(this);
                game.RegisterTower();
                GameAudio.PlayBuild();
                return tower;
            }
            finally
            {
                isBuilding = false;
            }
        }

        public bool Sell(TowerController tower)
        {
            GameController game = GameController.Instance;
            if (!IsOccupied || !tower || !game || !game.CanInteract) return false;

            int refund = tower.SellValue;
            tower.StopAttacking();
            IsOccupied = false;
            marker.gameObject.SetActive(true);
            game.UnregisterTower();
            game.Economy.Add(refund);
            Destroy(tower.gameObject);
            return true;
        }
    }
}
