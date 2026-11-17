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
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseSpeedButton;

        [Header("Audio")]
        [SerializeField] private Button audioSettingsButton;
        [SerializeField] private GameObject audioSettingsPanel;
        [SerializeField] private Text musicVolumeText;
        [SerializeField] private Text effectsVolumeText;
        [SerializeField] private Button musicDownButton;
        [SerializeField] private Button musicUpButton;
        [SerializeField] private Button effectsDownButton;
        [SerializeField] private Button effectsUpButton;
        [SerializeField] private Button closeAudioSettingsButton;

        [Header("Selection")]
        [SerializeField] private GameObject actionPanel;
        [SerializeField] private Text actionTitle;
        [SerializeField] private Text actionDetails;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionButtonText;
        [SerializeField] private Button sellButton;
        [SerializeField] private Text sellButtonText;
        [SerializeField] private Button targetingButton;
        [SerializeField] private Text targetingButtonText;

        [Header("Tower catalog")]
        [SerializeField] private TowerCatalog towerCatalog;
        [SerializeField] private GameObject buildMenu;
        [SerializeField] private Button[] towerButtons;
        [SerializeField] private Text[] towerButtonTexts;

        [Header("Result")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        [Header("Tutorial")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private Text tutorialText;
        [SerializeField] private Button tutorialSkipButton;
        [SerializeField] private Button tutorialRepeatButton;

        private GameController game;
        private WaveController wave;
        private TowerController selectedTower;
        private BuildSpot selectedSpot;
        private int selectionFrame;
        private int tutorialStep;

        public void Initialize(GameController controller, WaveController waveController)
        {
            game = controller;
            wave = waveController;
            EnsureSafeAreaLayout();
            EnsureTutorialUi();
            EnsureTimeControlUi();
            EnsureAudioSettingsUi();
            EnsureMenuButton();
            EnsureTowerActionUi();
            game.Economy.Changed += Refresh;
            game.BaseHealth.Changed += Refresh;
            startWaveButton.onClick.AddListener(game.StartWave);
            testProfileButton.onClick.AddListener(game.ToggleTestProfile);
            pauseButton.onClick.AddListener(game.TogglePause);
            speedButton.onClick.AddListener(game.ToggleSimulationSpeed);
            resumeButton.onClick.AddListener(game.TogglePause);
            pauseSpeedButton.onClick.AddListener(game.ToggleSimulationSpeed);
            audioSettingsButton.onClick.AddListener(ToggleAudioSettings);
            musicDownButton.onClick.AddListener(() => ChangeMusicVolume(-0.1f));
            musicUpButton.onClick.AddListener(() => ChangeMusicVolume(0.1f));
            effectsDownButton.onClick.AddListener(() => ChangeEffectsVolume(-0.1f));
            effectsUpButton.onClick.AddListener(() => ChangeEffectsVolume(0.1f));
            closeAudioSettingsButton.onClick.AddListener(ToggleAudioSettings);
            testProfileButton.image.color = game.IsTestProfile
                ? new Color(0.25f, 0.8f, 0.55f) : new Color(0.65f, 0.65f, 0.7f);
            restartButton.onClick.AddListener(game.Restart);
            menuButton.onClick.AddListener(game.ReturnToMenu);
            actionButton.onClick.AddListener(PerformAction);
            sellButton.onClick.AddListener(SellSelectedTower);
            targetingButton.onClick.AddListener(CycleSelectedTowerTargeting);
            tutorialSkipButton.onClick.AddListener(SkipTutorial);
            tutorialRepeatButton.onClick.AddListener(RestartTutorial);
            actionPanel.SetActive(false);
            buildMenu.SetActive(false);
            resultPanel.SetActive(false);
            RestartTutorial();
            for (int i = 0; i < towerButtons.Length; i++)
            {
                int index = i;
                towerButtons[i].onClick.AddListener(() => BuildTower(index));
            }
            Refresh();
            RefreshTimeControls();
            RefreshAudioSettings();
        }

        private void Update()
        {
            if (!game) return;
            RefreshWaveStatus();
            RefreshTimeControls();
        }

        public void RefreshWaveStatus()
        {
            if (game.IsTestProfile)
            {
                waveText.text = $"Wave: {game.WaveNumber}";
                enemiesText.text = game.IsWaitingForWave
                    ? $"Next wave: {game.NextWaveSeconds:0.0}s"
                    : wave.IsRunning ? $"Remaining: {wave.Remaining}" : "Ready";
            }
            else
            {
                waveText.text = FormatNormalWaveStatus(game.State, game.WaveNumber, wave.NormalWaveCount);
                enemiesText.text = wave.IsRunning
                    ? $"Remaining: {wave.Remaining}"
                    : FormatNormalPreparation(game.WaveNumber, wave.NormalWaveCount);
            }
            if (game.WaveConfigurationError != null)
                enemiesText.text = game.WaveConfigurationError;
            startWaveButton.gameObject.SetActive(!game.IsTestProfile || game.WaveNumber == 0 ||
                game.WaveConfigurationError != null);
            startWaveButton.interactable = game.CanStartWave;
        }

        public void RefreshTimeControls()
        {
            if (!game) return;

            bool canControlTime = game.State == GameState.Build || game.State == GameState.Wave;
            pauseButton.gameObject.SetActive(canControlTime);
            speedButton.gameObject.SetActive(canControlTime);
            pauseButton.GetComponentInChildren<Text>().text = FormatPauseLabel(game.IsPaused);
            speedButton.GetComponentInChildren<Text>().text = FormatSpeedLabel(game.SimulationSpeed);
            pausePanel.SetActive(game.IsPaused);
            pauseSpeedButton.GetComponentInChildren<Text>().text = FormatSpeedLabel(game.SimulationSpeed);
        }

        public static string FormatVolumeLabel(string channel, float value) =>
            $"{channel}: {Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";

        private void ToggleAudioSettings()
        {
            audioSettingsPanel.SetActive(!audioSettingsPanel.activeSelf);
            RefreshAudioSettings();
        }

        private void ChangeMusicVolume(float delta)
        {
            GameAudio.SetMusicVolume(GameAudio.MusicVolume + delta);
            RefreshAudioSettings();
        }

        private void ChangeEffectsVolume(float delta)
        {
            GameAudio.SetEffectsVolume(GameAudio.EffectsVolume + delta);
            RefreshAudioSettings();
        }

        private void RefreshAudioSettings()
        {
            musicVolumeText.text = FormatVolumeLabel("MUSIC", GameAudio.MusicVolume);
            effectsVolumeText.text = FormatVolumeLabel("EFFECTS", GameAudio.EffectsVolume);
        }

        public void NotifyWaveStarted()
        {
            if (tutorialStep == 2) AdvanceTutorial();
        }

        private void Refresh()
        {
            goldText.text = $"Gold: {game.Economy.Gold}";
            healthText.text = game.IsTestProfile ? "Base: unlimited" : $"Base: {game.BaseHealth.Current}";
            RefreshWaveStatus();
            if (selectedTower) ShowTower(selectedTower);
            if (selectedSpot) ShowBuildMenu();
            if (tutorialStep == 3 && game.BaseHealth.Current < game.BaseHealth.StartingHealth)
                AdvanceTutorial();
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
            if (!pressed || UiInputGuard.IsPointerOverUi()) return;
            Canvas canvas = actionPanel.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)actionPanel.transform, position, camera))
                CloseSelection();
        }

        public void SelectBuildSpot(BuildSpot spot)
        {
            if (!game.CanInteract) return;
            CloseSelection();
            selectedSpot = spot;
            ShowBuildMenu();
            if (tutorialStep == 0) AdvanceTutorial();
        }

        private void ShowBuildMenu()
        {
            buildMenu.SetActive(true);
            TowerConfig[] choices = towerCatalog ? towerCatalog.towers : null;
            for (int i = 0; i < towerButtons.Length; i++)
            {
                bool exists = choices != null && i < choices.Length;
                towerButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;

                TowerConfig config = choices[i];
                bool available = TowerFactory.GetConfigurationError(config) == null;
                string title = config ? config.displayName : "Tower";
                towerButtonTexts[i].text = FormatTowerCard(config, game.Economy.Gold, available);
                towerButtons[i].interactable = available && config && game.Economy.Gold >= config.buildCost;
            }
        }

        private void BuildTower(int index)
        {
            if (!game.CanInteract || !selectedSpot || !towerCatalog || towerCatalog.towers == null ||
                index < 0 || index >= towerCatalog.towers.Length) return;
            TowerConfig cfg=towerCatalog.towers[index];
            TowerController tower=selectedSpot.Build(cfg);
            if(tower)
            {
                if (tutorialStep == 1) AdvanceTutorial();
                SelectTower(tower);
            }
        }

        public void SelectTower(TowerController tower)
        {
            if (!game.CanInteract) return;
            CloseSelection();
            selectedTower = tower;
            selectionFrame = Time.frameCount;
            tower.SetSelected(true);
            ShowTower(tower);
        }

        private void ShowTower(TowerController tower)
        {
            actionPanel.SetActive(true);
            actionTitle.text = FormatTowerTitle(tower.Config, tower.Level);
            actionDetails.text = FormatTowerDetails(tower.Config, tower.Stats);
            int cost=tower.CanUpgrade?tower.Stats.upgradeCost:0;
            actionButtonText.text = FormatUpgradeAction(tower.Config, tower.LevelIndex, game.Economy.Gold);
            actionButton.interactable = tower.CanUpgrade && game.Economy.Gold >= cost;
            sellButtonText.text = FormatSellAction(tower.SellValue);
            sellButton.interactable = game.CanInteract;
            targetingButtonText.text = FormatTargetingAction(tower.TargetingRule);
            targetingButton.interactable = game.CanInteract;
        }

        public static string FormatNormalWaveStatus(GameState state, int waveNumber, int totalWaves)
        {
            int displayWave = Mathf.Clamp(waveNumber, 0, totalWaves);
            return state switch
            {
                GameState.Won => $"Victory · wave {totalWaves}/{totalWaves}",
                GameState.Lost => $"Defeat · wave {Mathf.Max(1, displayWave)}/{totalWaves}",
                _ => $"Wave {displayWave}/{totalWaves}"
            };
        }

        public static string FormatNormalPreparation(int waveNumber, int totalWaves)
        {
            int nextWave = Mathf.Min(waveNumber + 1, totalWaves);
            return nextWave > 0 ? $"Next: wave {nextWave}/{totalWaves}" : "No configured waves";
        }

        public static string FormatTowerCard(TowerConfig config, int gold, bool isConfigured)
        {
            if (!config || !isConfigured) return "Tower\nUnavailable: configuration";

            TowerLevelConfig stats = config.levels[0];
            string affordability = gold >= config.buildCost ? "Ready" : $"Need {config.buildCost - gold} more gold";
            return $"{config.displayName} · {config.description}\nCost {config.buildCost} · Rate {stats.attackRate:0.0}/s · Range {stats.range:0.0}\n{affordability}";
        }

        public static string FormatTowerDetails(TowerConfig config, TowerLevelConfig stats)
        {
            return $"Role: {config.description}\nDamage {stats.damage} · Rate {stats.attackRate:0.0}/s · Range {stats.range:0.0}";
        }

        public static string FormatTowerTitle(TowerConfig config, int level)
        {
            string title = config ? config.displayName : "Tower";
            return $"{title} · level {Mathf.Max(1, level)}";
        }

        public static string FormatUpgradeAction(TowerConfig config, int levelIndex, int gold)
        {
            if (!config || config.levels == null || levelIndex + 1 >= config.levels.Length)
                return "Maximum level";

            TowerLevelConfig current = config.levels[levelIndex];
            int cost = current.upgradeCost;
            TowerLevelConfig next = config.levels[levelIndex + 1];
            string affordability = gold >= cost ? "Ready" : $"Need {cost - gold} more gold";
            return $"Upgrade {cost} · Damage {current.damage}→{next.damage} · Rate {current.attackRate:0.0}→{next.attackRate:0.0}/s · Range {current.range:0.0}→{next.range:0.0}\n{affordability}";
        }

        public static string FormatSellAction(int refund) => $"SELL · Return {Mathf.Max(0, refund)} gold";

        public static string FormatTargetingAction(TargetingRule rule) =>
            $"TARGET · {rule switch { TargetingRule.ClosestToBase => "Closest to base", TargetingRule.Nearest => "Nearest", TargetingRule.Strongest => "Strongest", _ => "Closest to base" }}";

        public static string FormatPauseLabel(bool isPaused) => isPaused ? "RESUME" : "PAUSE";
        public static string FormatSpeedLabel(float speed) => $"SPEED ×{speed:0}";

        private void PerformAction()
        {
            if (!game.CanInteract) return;
            if (selectedTower && selectedTower.Upgrade()) CloseSelection();
            Refresh();
        }

        private void SellSelectedTower()
        {
            if (!game.CanInteract || !selectedTower) return;
            if (selectedTower.Sell()) CloseSelection();
            Refresh();
        }

        private void CycleSelectedTowerTargeting()
        {
            if (!game.CanInteract || !selectedTower) return;
            selectedTower.CycleTargetingRule();
            ShowTower(selectedTower);
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
            testProfileButton.interactable = false;
            RefreshWaveStatus();
            RefreshTimeControls();
        }

        public static string GetTutorialText(int step)
        {
            return step switch
            {
                0 => "1/4 Tap a glowing build spot to choose where to place your first tower.",
                1 => "2/4 Choose a tower you can afford. Base is the cheapest long-range start.",
                2 => "3/4 Press START WAVE when your tower is ready.",
                3 => "4/4 Protect the base. If an enemy gets through, the Base value at the top goes down.",
                _ => "Tutorial complete. You can repeat these hints at any time."
            };
        }

        private void RestartTutorial()
        {
            tutorialStep = 0;
            tutorialPanel.SetActive(true);
            tutorialText.text = GetTutorialText(tutorialStep);
        }

        private void SkipTutorial()
        {
            tutorialStep = 4;
            tutorialPanel.SetActive(false);
        }

        private void AdvanceTutorial()
        {
            tutorialStep = Mathf.Min(tutorialStep + 1, 4);
            tutorialPanel.SetActive(true);
            tutorialText.text = GetTutorialText(tutorialStep);
        }

        private void EnsureTutorialUi()
        {
            if (tutorialPanel && tutorialText && tutorialSkipButton && tutorialRepeatButton) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            GameObject panel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(GetUiLayoutParent(canvas), false);
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0.04f, 0.42f);
            panelRect.anchorMax = new Vector2(0.96f, 0.58f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.12f, 0.24f, 0.94f);
            panelImage.raycastTarget = false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tutorialText = CreateTutorialText(panel.transform, font);
            tutorialRepeatButton = CreateTutorialButton(panel.transform, font, "REPEAT HINTS", new Vector2(0.06f, 0.08f), new Vector2(0.49f, 0.32f), new Color(0.25f, 0.55f, 0.9f));
            tutorialSkipButton = CreateTutorialButton(panel.transform, font, "SKIP", new Vector2(0.51f, 0.08f), new Vector2(0.94f, 0.32f), new Color(0.45f, 0.45f, 0.5f));
            tutorialPanel = panel;
        }

        private void EnsureTimeControlUi()
        {
            if (pauseButton && speedButton && pausePanel && resumeButton && pauseSpeedButton) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transform parent = GetUiLayoutParent(canvas);

            pauseButton = CreateTutorialButton(parent, font, "PAUSE", new Vector2(0.65f, 0.785f), new Vector2(0.80f, 0.835f), new Color(0.35f, 0.45f, 0.7f));
            speedButton = CreateTutorialButton(parent, font, "SPEED ×1", new Vector2(0.81f, 0.785f), new Vector2(0.96f, 0.835f), new Color(0.45f, 0.65f, 0.35f));

            pausePanel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
            pausePanel.transform.SetParent(parent, false);
            RectTransform panelRect = (RectTransform)pausePanel.transform;
            panelRect.anchorMin = new Vector2(0.18f, 0.37f);
            panelRect.anchorMax = new Vector2(0.82f, 0.63f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            pausePanel.GetComponent<Image>().color = new Color(0.06f, 0.015f, 0.14f, 0.97f);
            CreateTutorialText(pausePanel.transform, font).text = "PAUSED";
            resumeButton = CreateTutorialButton(pausePanel.transform, font, "RESUME", new Vector2(0.08f, 0.08f), new Vector2(0.47f, 0.30f), new Color(0.25f, 0.55f, 0.9f));
            pauseSpeedButton = CreateTutorialButton(pausePanel.transform, font, "SPEED ×1", new Vector2(0.53f, 0.08f), new Vector2(0.92f, 0.30f), new Color(0.45f, 0.65f, 0.35f));
        }

        private void EnsureAudioSettingsUi()
        {
            if (audioSettingsButton && audioSettingsPanel && musicVolumeText && effectsVolumeText &&
                musicDownButton && musicUpButton && effectsDownButton && effectsUpButton && closeAudioSettingsButton) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transform parent = GetUiLayoutParent(canvas);
            audioSettingsButton = CreateTutorialButton(parent, font, "SOUND", new Vector2(0.04f, 0.785f), new Vector2(0.20f, 0.835f), new Color(0.55f, 0.38f, 0.75f));

            audioSettingsPanel = new GameObject("AudioSettingsPanel", typeof(RectTransform), typeof(Image));
            audioSettingsPanel.transform.SetParent(parent, false);
            RectTransform panelRect = (RectTransform)audioSettingsPanel.transform;
            panelRect.anchorMin = new Vector2(0.12f, 0.36f);
            panelRect.anchorMax = new Vector2(0.88f, 0.65f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            audioSettingsPanel.GetComponent<Image>().color = new Color(0.06f, 0.015f, 0.14f, 0.98f);
            Text title = CreateTutorialText(audioSettingsPanel.transform, font);
            title.text = "SOUND SETTINGS";
            RectTransform titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0.08f, 0.84f);
            titleRect.anchorMax = new Vector2(0.92f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            musicVolumeText = CreateAudioLabel(audioSettingsPanel.transform, font, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.64f));
            effectsVolumeText = CreateAudioLabel(audioSettingsPanel.transform, font, new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.45f));
            musicDownButton = CreateTutorialButton(audioSettingsPanel.transform, font, "MUSIC −", new Vector2(0.08f, 0.12f), new Vector2(0.47f, 0.26f), new Color(0.35f, 0.45f, 0.7f));
            musicUpButton = CreateTutorialButton(audioSettingsPanel.transform, font, "MUSIC +", new Vector2(0.53f, 0.12f), new Vector2(0.92f, 0.26f), new Color(0.35f, 0.45f, 0.7f));
            effectsDownButton = CreateTutorialButton(audioSettingsPanel.transform, font, "EFFECTS −", new Vector2(0.08f, 0.01f), new Vector2(0.47f, 0.11f), new Color(0.45f, 0.65f, 0.35f));
            effectsUpButton = CreateTutorialButton(audioSettingsPanel.transform, font, "EFFECTS +", new Vector2(0.53f, 0.01f), new Vector2(0.92f, 0.11f), new Color(0.45f, 0.65f, 0.35f));
            closeAudioSettingsButton = CreateTutorialButton(audioSettingsPanel.transform, font, "CLOSE", new Vector2(0.32f, 0.67f), new Vector2(0.68f, 0.81f), new Color(0.45f, 0.45f, 0.5f));
            audioSettingsPanel.SetActive(false);
        }

        private void EnsureTowerActionUi()
        {
            if (sellButton && sellButtonText && targetingButton && targetingButtonText) return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform upgradeRect = (RectTransform)actionButton.transform;
            upgradeRect.anchorMin = new Vector2(.06f, .27f);
            upgradeRect.anchorMax = new Vector2(.94f, .43f);
            upgradeRect.offsetMin = Vector2.zero;
            upgradeRect.offsetMax = Vector2.zero;

            sellButton = CreateTutorialButton(actionPanel.transform, font, "SELL", new Vector2(.06f, .06f), new Vector2(.48f, .22f), new Color(.78f, .36f, .32f));
            sellButton.name = "Sell";
            sellButtonText = sellButton.GetComponentInChildren<Text>();
            targetingButton = CreateTutorialButton(actionPanel.transform, font, "TARGET", new Vector2(.52f, .06f), new Vector2(.94f, .22f), new Color(.28f, .58f, .82f));
            targetingButton.name = "Targeting";
            targetingButtonText = targetingButton.GetComponentInChildren<Text>();
            sellButtonText.fontSize = 18;
            targetingButtonText.fontSize = 18;
        }

        private void EnsureMenuButton()
        {
            if (menuButton) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            menuButton = CreateTutorialButton(GetUiLayoutParent(canvas), font, "MENU",
                new Vector2(.21f, .785f), new Vector2(.36f, .835f), new Color(.45f, .45f, .5f));
        }

        private void EnsureSafeAreaLayout()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (!canvas) return;

            Transform safeArea = canvas.transform.Find("SafeArea");
            if (safeArea && !safeArea.GetComponent<SafeAreaLayout>())
                safeArea.gameObject.AddComponent<SafeAreaLayout>();
        }

        private static Transform GetUiLayoutParent(Canvas canvas)
        {
            Transform safeArea = canvas.transform.Find("SafeArea");
            return safeArea ? safeArea : canvas.transform;
        }

        private static Text CreateAudioLabel(Transform parent, Font font, Vector2 anchorMin, Vector2 anchorMax)
        {
            Text text = CreateTutorialText(parent, font);
            RectTransform rect = (RectTransform)text.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            text.fontSize = 25;
            return text;
        }

        private static Text CreateTutorialText(Transform parent, Font font)
        {
            GameObject textObject = new GameObject("TutorialText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)textObject.transform;
            rect.anchorMin = new Vector2(0.06f, 0.38f);
            rect.anchorMax = new Vector2(0.94f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateTutorialButton(Transform parent, Font font, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateTutorialText(buttonObject.transform, font);
            text.name = "Text";
            text.text = label;
            text.fontSize = 23;
            return button;
        }
    }
}
