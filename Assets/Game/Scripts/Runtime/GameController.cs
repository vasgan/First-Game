using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstGame
{
    public enum GameState { Build, Wave, Won, Lost }

    public sealed class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }
        private static bool selectedTestProfile;
        [SerializeField] private Economy economy;
        [SerializeField] private BaseHealth baseHealth;
        [SerializeField] private WaveController waveController;
        [SerializeField] private GameUI gameUI;
        public Economy Economy => economy;
        public BaseHealth BaseHealth => baseHealth;
        public GameUI UI => gameUI;
        public GameState State { get; private set; } = GameState.Build;
        public int BuiltTowerCount { get; private set; }
        public bool IsTestProfile { get; private set; }
        public int WaveNumber { get; private set; }
        public string WaveConfigurationError { get; private set; }
        public float NextWaveSeconds => Mathf.Max(0f, nextWaveAt - Time.time);
        public bool IsWaitingForWave { get; private set; }
        public bool IsPaused { get; private set; }
        public float SimulationSpeed { get; private set; } = 1f;
        private float nextWaveAt;
        private void Awake()
        {
            Instance = this;
            GameAudio.EnsureInstance();
            IsTestProfile = selectedTestProfile;
            ResetSimulationSpeed();
            Application.targetFrameRate = 60;
        }
        private void Start()
        {
            baseHealth.Depleted += Lose; waveController.Completed += Win;
            gameUI.Initialize(this, waveController);
        }
        public void RegisterTower() => BuiltTowerCount++;
        public void UnregisterTower() => BuiltTowerCount = Mathf.Max(0, BuiltTowerCount - 1);
        public bool CanInteract => !IsPaused && (State == GameState.Build || State == GameState.Wave);
        public bool CanStartWave => !IsPaused && State == GameState.Build && !IsWaitingForWave &&
            (IsTestProfile || BuiltTowerCount > 0);
        public void ToggleTestProfile()
        {
            if (!CanInteract) return;
            selectedTestProfile = !IsTestProfile;
            Restart();
        }
        public void StartWave()
        {
            if (!CanStartWave) return;

            WaveConfigurationError = waveController.PrepareWave(WaveNumber + 1, IsTestProfile, out WaveSettings settings);
            if (WaveConfigurationError != null)
            {
                Debug.LogError(WaveConfigurationError, waveController);
                gameUI.RefreshWaveStatus();
                return;
            }

            State = GameState.Wave;
            WaveNumber++;
            gameUI.NotifyWaveStarted();
            if (!IsTestProfile) gameUI.CloseSelection();
            waveController.StartWave(settings);
            gameUI.RefreshWaveStatus();
        }
        public void TogglePause()
        {
            if (State != GameState.Build && State != GameState.Wave) return;

            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0f : SimulationSpeed;
            if (gameUI) gameUI.RefreshTimeControls();
        }

        public void ToggleSimulationSpeed()
        {
            if (State != GameState.Build && State != GameState.Wave) return;

            SimulationSpeed = SimulationSpeed == 1f ? 2f : 1f;
            if (!IsPaused) Time.timeScale = SimulationSpeed;
            if (gameUI) gameUI.RefreshTimeControls();
        }
        private void Win()
        {
            if (State != GameState.Wave) return;
            if (IsTestProfile)
            {
                State = GameState.Build;
                StartCoroutine(WaitForNextWave());
                return;
            }

            if (waveController.IsFinalNormalWave(WaveNumber))
            {
                FinishBattle(baseHealth.Current > 0);
                return;
            }

            // Normal mode deliberately pauses here: the player chooses when to launch
            // the next configured wave after receiving its completion reward.
            State = GameState.Build;
            gameUI.RefreshWaveStatus();
        }
        private IEnumerator WaitForNextWave()
        {
            IsWaitingForWave = true;
            nextWaveAt = Time.time + 5f;
            gameUI.RefreshWaveStatus();
            yield return new WaitForSeconds(5f);
            IsWaitingForWave = false;
            StartWave();
        }
        private void Lose()
        {
            if (!IsTestProfile) FinishBattle(false);
        }

        private void FinishBattle(bool won)
        {
            if (!CanInteract) return;

            ResetSimulationSpeed();
            State = won ? GameState.Won : GameState.Lost;
            GameAudio.PlayResult(won);
            waveController.StopWave();
            foreach (TowerController tower in FindObjectsByType<TowerController>(FindObjectsSortMode.None))
                tower.StopAttacking();
            foreach (Projectile projectile in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                projectile.Return();

            gameUI.ShowResult(won);
        }
        public void Restart()
        {
            ResetSimulationSpeed();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ResetSimulationSpeed()
        {
            IsPaused = false;
            SimulationSpeed = 1f;
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Time.timeScale = 1f;
        }
    }
}
