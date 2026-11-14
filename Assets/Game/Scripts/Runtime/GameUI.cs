using UnityEngine;
using UnityEngine.UI;

namespace FirstGame
{
    public sealed class GameUI : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private Text goldText;
        [SerializeField] private Text healthText;
        [SerializeField] private Text waveText;
        [SerializeField] private Text enemiesText;
        [SerializeField] private Button startWaveButton;
        [SerializeField] private Button testProfileButton;

        [Header("Selection")]
        [SerializeField] private GameObject actionPanel;
        [SerializeField] private Text actionTitle;
        [SerializeField] private Text actionDetails;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionButtonText;

        [Header("Tower catalog")]
        [SerializeField] private TowerCatalog towerCatalog;
        [SerializeField] private GameObject buildMenu;
        [SerializeField] private Button[] towerButtons;
        [SerializeField] private Text[] towerButtonTexts;

        [Header("Result")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultText;
        [SerializeField] private Button restartButton;

        private GameController game;
        private WaveController wave;
        private TowerController selectedTower;
        private BuildSpot selectedSpot;
        private int selectionFrame;

        public void Initialize(GameController controller, WaveController waveController)
        {
            game = controller;
            wave = waveController;
            game.Economy.Changed += Refresh;
            game.BaseHealth.Changed += Refresh;
            startWaveButton.onClick.AddListener(game.StartWave);
            testProfileButton.onClick.AddListener(game.ToggleTestProfile);
            testProfileButton.image.color = game.IsTestProfile
                ? new Color(0.25f, 0.8f, 0.55f) : new Color(0.65f, 0.65f, 0.7f);
            restartButton.onClick.AddListener(game.Restart);
            actionButton.onClick.AddListener(PerformAction);
            actionPanel.SetActive(false);
            buildMenu.SetActive(false);
            resultPanel.SetActive(false);
            for (int i = 0; i < towerButtons.Length; i++)
            {
                int index = i;
                towerButtons[i].onClick.AddListener(() => BuildTower(index));
            }
            Refresh();
        }

        private void Update()
        {
            if (!game) return;
            if (game.IsTestProfile)
            {
                waveText.text = $"Wave: {game.WaveNumber}";
                healthText.text = "Base: unlimited";
                enemiesText.text = game.IsWaitingForWave
                    ? $"Next wave: {game.NextWaveSeconds:0.0}s"
                    : wave.IsRunning ? $"Remaining: {wave.Remaining}" : "Ready";
            }
            else
            {
                enemiesText.text = wave.IsRunning ? $"Remaining: {wave.Remaining}" : "Wave 1/1";
            }
            startWaveButton.gameObject.SetActive(!game.IsTestProfile || game.WaveNumber == 0);
            startWaveButton.interactable = game.CanStartWave;
        }

        private void Refresh()
        {
            goldText.text = $"Gold: {game.Economy.Gold}";
            healthText.text = $"Base: {game.BaseHealth.Current}";
            waveText.text = game.State == GameState.Wave ? "Wave active" : "Build phase";
            if (selectedTower) ShowTower(selectedTower);
            if (selectedSpot) ShowBuildMenu();
        }

        private void LateUpdate()
        {
            if (!selectedTower || !actionPanel.activeSelf || selectionFrame == Time.frameCount) return;
            bool pressed = Input.GetMouseButtonDown(0);
            Vector2 position = Input.mousePosition;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                pressed = touch.phase == TouchPhase.Began;
                position = touch.position;
            }
            if (!pressed) return;
            Canvas canvas = actionPanel.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)actionPanel.transform, position, camera))
                CloseSelection();
        }

        public void SelectBuildSpot(BuildSpot spot)
        {
            CloseSelection();
            selectedSpot = spot;
            ShowBuildMenu();
        }

        private void ShowBuildMenu()
        {
            buildMenu.SetActive(true);
            for(int i=0;i<towerButtons.Length;i++)
            {
                bool exists=i<towerCatalog.towers.Length;
                towerButtons[i].gameObject.SetActive(exists);
                if(!exists)continue;
                TowerConfig cfg=towerCatalog.towers[i];
                towerButtonTexts[i].text=$"{cfg.displayName}\n{cfg.buildCost} gold";
                towerButtons[i].interactable=game.Economy.Gold>=cfg.buildCost;
            }
        }

        private void BuildTower(int index)
        {
            if(!selectedSpot||index>=towerCatalog.towers.Length)return;
            TowerConfig cfg=towerCatalog.towers[index];
            TowerController tower=selectedSpot.Build(cfg);
            if(tower)SelectTower(tower);
        }

        public void SelectTower(TowerController tower)
        {
            CloseSelection();
            selectedTower = tower;
            selectionFrame = Time.frameCount;
            tower.SetSelected(true);
            ShowTower(tower);
        }

        private void ShowTower(TowerController tower)
        {
            actionPanel.SetActive(true);
            actionTitle.text = $"{tower.Config.displayName} · level {tower.Level}";
            actionDetails.text = $"Damage {tower.Stats.damage}  Range {tower.Stats.range:0.0}";
            int cost=tower.CanUpgrade?tower.Stats.upgradeCost:0;
            actionButtonText.text = tower.CanUpgrade ? $"Upgrade · {cost}" : "Maximum level";
            actionButton.interactable = tower.CanUpgrade && game.Economy.Gold >= cost;
        }

        private void PerformAction()
        {
            if (selectedTower && selectedTower.Upgrade()) CloseSelection();
            Refresh();
        }

        public void CloseSelection()
        {
            if (selectedTower) selectedTower.SetSelected(false);
            selectedTower = null;
            selectedSpot = null;
            actionPanel.SetActive(false);
            buildMenu.SetActive(false);
        }

        public void ShowResult(bool won)
        {
            if (game.IsTestProfile) return;
            CloseSelection();
            resultPanel.SetActive(true);
            resultText.text = won ? "VICTORY" : "DEFEAT";
            startWaveButton.interactable = false;
        }
    }
}
