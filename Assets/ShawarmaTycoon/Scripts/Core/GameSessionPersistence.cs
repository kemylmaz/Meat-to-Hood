using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class GameSessionPersistence : MonoBehaviour
    {
        private float saveTimer;

        private void Start()
        {
            long lastSeen = GameProgress.GetLastSeen();
            if (lastSeen > 0 && GameProgress.WorkerCount > 0 && GameEconomy.Instance != null)
            {
                long elapsed = Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastSeen);
                int offlineCoins = Mathf.FloorToInt(Mathf.Min(elapsed, 8 * 60 * 60) * GameProgress.WorkerCount / 30f);
                if (offlineCoins > 0) GameEconomy.Instance.AddCoins(offlineCoins);
            }
            GameProgress.SaveLastSeen();
        }

        private void Update()
        {
            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer < 15f) return;
            saveTimer = 0f;
            SaveNow();
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
        private void OnApplicationQuit() => SaveNow();

        private static void SaveNow()
        {
            if (GameEconomy.Instance != null) GameProgress.SetInt("coins", GameEconomy.Instance.Coins);
            GameProgress.SaveLastSeen();
        }
    }
}
