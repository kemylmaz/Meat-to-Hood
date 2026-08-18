using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class GameEconomy : MonoBehaviour
    {
        public static GameEconomy Instance { get; private set; }

        [SerializeField, Min(0)] private int startingCoins;
        public int Coins { get; private set; }
        public event Action<int> CoinsChanged;

        private void Awake()
        {
            Instance = this;
            Coins = Mathf.Max(0, startingCoins);
            CoinsChanged?.Invoke(Coins);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Configure(int initialCoins)
        {
            startingCoins = Mathf.Max(0, initialCoins);
            Coins = GameProgress.GetInt("coins", startingCoins);
            CoinsChanged?.Invoke(Coins);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            GameProgress.SetInt("coins", Coins);
            CoinsChanged?.Invoke(Coins);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || Coins < amount)
            {
                AudioDirector.Play(GameSfx.Error, 0.6f);
                return false;
            }

            Coins -= amount;
            GameProgress.SetInt("coins", Coins);
            CoinsChanged?.Invoke(Coins);
            AudioDirector.Play(GameSfx.Coin);
            return true;
        }
    }
}
