using System;
using UnityEngine;

namespace FirstGame
{
    public sealed class BaseHealth : MonoBehaviour
    {
        [SerializeField] private int startingHealth = 5;
        public int Current { get; private set; }
        public int StartingHealth => startingHealth;
        public event Action Changed;
        public event Action Depleted;
        private void Awake() => Current = startingHealth;
        public void TakeDamage(int amount = 1)
        {
            GameController game = GameController.Instance;
            if (game && (game.IsTestProfile || !game.CanInteract)) return;
            if (Current <= 0) return;
            Current = Mathf.Max(0, Current - amount); Changed?.Invoke();
            if (Current == 0) Depleted?.Invoke();
        }
    }
}
