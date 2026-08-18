using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class GameSessionPersistence : MonoBehaviour
    {
        private float saveTimer;
        private EconomyConfig economyConfig;
        private GameConfig gameConfig;

        public void Configure(GameConfig runtimeConfig, EconomyConfig balanceConfig)
        {
            gameConfig = runtimeConfig;
            economyConfig = balanceConfig;
        }

        private void Start()
        {
            long lastSeen = GameProgress.GetLastSeen();
            if (lastSeen > 0 && GameProgress.WorkerCount > 0 && GameEconomy.Instance != null)
            {
                long elapsed = Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastSeen);
                EconomyConfig balance = economyConfig ?? GameCatalogs.Economy;
                int offlineCoins = Mathf.FloorToInt(
                    Mathf.Min(elapsed, balance.OfflineIncomeCapSeconds) * GameProgress.WorkerCount /
                    balance.SecondsPerCoinPerWorker);
                if (offlineCoins > 0) GameEconomy.Instance.AddCoins(offlineCoins);
            }
            GameProgress.SaveLastSeen();
        }

        private void Update()
        {
            saveTimer += Time.unscaledDeltaTime;
            float interval = (gameConfig ?? GameCatalogs.Game).SaveFlushIntervalSeconds;
            if (saveTimer < interval) return;
            saveTimer = 0f;
            SaveNow();
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
        private void OnApplicationFocus(bool focused) { if (!focused) SaveNow(); }
        private void OnApplicationQuit() => SaveNow();

        private static void SaveNow()
        {
            if (GameEconomy.Instance != null) GameProgress.SetInt("coins", GameEconomy.Instance.Coins);
            GameProgress.SaveLastSeen();
            GameProgress.FlushNow();
        }
    }
}
