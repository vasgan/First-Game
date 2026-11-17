using System;
using UnityEngine;

namespace FirstGame
{
    public sealed class Economy : MonoBehaviour
    {
        [SerializeField] private int startingGold = 100;
        public int Gold { get; private set; }
        public event Action Changed;
        private void Awake() => Gold = startingGold;
        private static bool CanChangeGold => !GameController.Instance || GameController.Instance.CanInteract;

        public bool Spend(int amount)
        {
            if (!CanChangeGold || amount < 0 || Gold < amount) return false;
            Gold -= amount; Changed?.Invoke(); return true;
        }
        public void Add(int amount)
        {
            if (!CanChangeGold) return;
            Gold += Mathf.Max(0, amount);
            Changed?.Invoke();
        }
    }
}
