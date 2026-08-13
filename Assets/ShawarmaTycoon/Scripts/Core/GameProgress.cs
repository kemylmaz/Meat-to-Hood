using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    public static class GameProgress
    {
        // Shared local-save prefix for mobile and WebGL builds.
        private const string Prefix = "shawarma.tycoon.";

        public static int ServedToday => GetDaily("served");
        public static int RevenueToday => GetDaily("revenue");
        public static int TrashToday => GetDaily("trash");
        public static int UpgradesToday => GetDaily("upgrades");
        public static int WorkerCount => PlayerPrefs.GetInt(Prefix + "workers", 0);
        public static int BestDailyRevenue => GetInt("records.best_daily_revenue");
        public static int BestDailyServed => GetInt("records.best_daily_served");
        public static int BestCombo => GetInt("records.best_combo");
        public static int VipServedTotal => GetInt("records.vip_served");
        public static int TakeawayServedTotal => GetInt("records.takeaway_served");
        public static bool DailyRewardClaimed => PlayerPrefs.GetInt(DailyKey("claimed"), 0) == 1;

        public static void RecordServed(bool vip = false, bool takeaway = false)
        {
            int served = ServedToday + 1;
            PlayerPrefs.SetInt(DailyKey("served"), served);
            SetRecordIfHigher("records.best_daily_served", served);
            if (vip) PlayerPrefs.SetInt(Prefix + "records.vip_served", VipServedTotal + 1);
            if (takeaway) PlayerPrefs.SetInt(Prefix + "records.takeaway_served", TakeawayServedTotal + 1);
            PlayerPrefs.Save();
        }

        public static void RecordRevenue(int amount)
        {
            if (amount <= 0) return;
            int revenue = RevenueToday + amount;
            PlayerPrefs.SetInt(DailyKey("revenue"), revenue);
            SetRecordIfHigher("records.best_daily_revenue", revenue);
            PlayerPrefs.Save();
        }

        public static void RecordBestCombo(int combo)
        {
            if (combo <= BestCombo) return;
            PlayerPrefs.SetInt(Prefix + "records.best_combo", combo);
            PlayerPrefs.Save();
        }

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

        /// <summary>
        /// Wipes every saved value: coins, records, daily counters, unlocks and
        /// hired staff. Keys are generated (per worker, per calendar day) so they
        /// cannot be enumerated - and this project stores nothing else in
        /// PlayerPrefs, so clearing the lot is the only complete reset.
        /// </summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
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

        private static void SetRecordIfHigher(string key, int value)
        {
            string fullKey = Prefix + key;
            if (value > PlayerPrefs.GetInt(fullKey, 0))
                PlayerPrefs.SetInt(fullKey, value);
        }

        private static string DailyKey(string name) => Prefix + "daily." + DateTime.UtcNow.ToString("yyyyMMdd") + "." + name;
    }
}
