using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    public static class GameProgress
    {
        // Shared local-save prefix for mobile and WebGL builds.
        private const string Prefix = "shawarma.tycoon.";

        public static int ServedToday => GetDaily("served");
        public static int TrashToday => GetDaily("trash");
        public static int UpgradesToday => GetDaily("upgrades");
        public static int WorkerCount => PlayerPrefs.GetInt(Prefix + "workers", 0);
        public static bool DailyRewardClaimed => PlayerPrefs.GetInt(DailyKey("claimed"), 0) == 1;

        public static void RecordServed() => SetDaily("served", ServedToday + 1);
        public static void RecordTrash(int count) => SetDaily("trash", TrashToday + Mathf.Max(0, count));
        public static void RecordUpgrade() => SetDaily("upgrades", UpgradesToday + 1);

        public static void RegisterWorker(string uniqueName)
        {
            string key = Prefix + "worker." + uniqueName;
            if (PlayerPrefs.GetInt(key, 0) == 1) return;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.SetInt(Prefix + "workers", WorkerCount + 1);
            PlayerPrefs.Save();
        }

        public static void ClaimDailyReward()
        {
            PlayerPrefs.SetInt(DailyKey("claimed"), 1);
            PlayerPrefs.Save();
        }

        public static int GetInt(string key, int fallback = 0) => PlayerPrefs.GetInt(Prefix + key, fallback);
        public static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(Prefix + key, value);
            PlayerPrefs.Save();
        }

        public static long GetLastSeen()
        {
            return long.TryParse(PlayerPrefs.GetString(Prefix + "last_seen", "0"), out long value) ? value : 0L;
        }

        public static void SaveLastSeen()
        {
            PlayerPrefs.SetString(Prefix + "last_seen", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            PlayerPrefs.Save();
        }

        private static int GetDaily(string name) => PlayerPrefs.GetInt(DailyKey(name), 0);
        private static void SetDaily(string name, int value)
        {
            PlayerPrefs.SetInt(DailyKey(name), value);
            PlayerPrefs.Save();
        }

        private static string DailyKey(string name) => Prefix + "daily." + DateTime.UtcNow.ToString("yyyyMMdd") + "." + name;
    }
}
