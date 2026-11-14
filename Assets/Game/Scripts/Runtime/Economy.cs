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
        public bool Spend(int amount)
        {
            if (amount < 0 || Gold < amount) return false;
            Gold -= amount; Changed?.Invoke(); return true;
        }
        public void Add(int amount) { Gold += Mathf.Max(0, amount); Changed?.Invoke(); }
    }
}
