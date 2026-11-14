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
        public float NextWaveSeconds => Mathf.Max(0f, nextWaveAt - Time.time);
        public bool IsWaitingForWave { get; private set; }
        private float nextWaveAt;
        private void Awake() { Instance = this; IsTestProfile = selectedTestProfile; Application.targetFrameRate = 60; }
        private void Start()
        {
            baseHealth.Depleted += Lose; waveController.Completed += Win;
            gameUI.Initialize(this, waveController);
        }
        public void RegisterTower() => BuiltTowerCount++;
        public bool CanInteract => State == GameState.Build || State == GameState.Wave;
        public bool CanStartWave => State == GameState.Build && !IsWaitingForWave && (IsTestProfile || BuiltTowerCount > 0);
        public void ToggleTestProfile()
        {
            selectedTestProfile = !IsTestProfile;
            Restart();
        }
        public void StartWave()
        {
            if (!CanStartWave) return;
            State = GameState.Wave;
            WaveNumber++;
            if (!IsTestProfile) gameUI.CloseSelection();
            waveController.StartWave(WaveNumber, IsTestProfile);
        }
        private void Win()
        {
            if (IsTestProfile)
            {
                State = GameState.Build;
                StartCoroutine(WaitForNextWave());
                return;
            }
            if (State == GameState.Lost) return;
            State = GameState.Won; gameUI.ShowResult(true);
        }
        private IEnumerator WaitForNextWave()
        {
            IsWaitingForWave = true;
            nextWaveAt = Time.time + 5f;
            yield return new WaitForSeconds(5f);
            IsWaitingForWave = false;
            StartWave();
        }
        private void Lose()
        {
            if (IsTestProfile) return;
            State = GameState.Lost; waveController.StopWave(); gameUI.ShowResult(false);
        }
        public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
