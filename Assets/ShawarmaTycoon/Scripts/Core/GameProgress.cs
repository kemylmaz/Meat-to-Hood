using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    public static class GameProgress
    {
        public static int ServedToday => GetDaily("served");
        public static int RevenueToday => GetDaily("revenue");
        public static int TrashToday => GetDaily("trash");
        public static int LostToday => GetDaily("lost");
        public static int UpgradesToday => GetDaily("upgrades");
        public static int WorkerCount => GetInt("workers");
        public static int BestDailyRevenue => GetInt("records.best_daily_revenue");
        public static int BestDailyServed => GetInt("records.best_daily_served");
        public static int BestCombo => GetInt("records.best_combo");
        public static int VipServedTotal => GetInt("records.vip_served");
        public static int TakeawayServedTotal => GetInt("records.takeaway_served");
        public static bool DailyRewardClaimed => GetInt(DailyKey("claimed")) == 1;

        public static void RecordServed(bool vip = false, bool takeaway = false)
        {
            int served = ServedToday + 1;
            SetInt(DailyKey("served"), served);
            SetRecordIfHigher("records.best_daily_served", served);
            if (vip) SetInt("records.vip_served", VipServedTotal + 1);
            if (takeaway) SetInt("records.takeaway_served", TakeawayServedTotal + 1);
        }

        public static void RecordRevenue(int amount)
        {
            if (amount <= 0) return;
            int revenue = RevenueToday + amount;
            SetInt(DailyKey("revenue"), revenue);
            SetRecordIfHigher("records.best_daily_revenue", revenue);
        }

        public static void RecordBestCombo(int combo)
        {
            if (combo <= BestCombo) return;
            SetInt("records.best_combo", combo);
        }

        public static void RecordTrash(int count) => SetDaily("trash", TrashToday + Mathf.Max(0, count));

        /// <summary>A customer who ran out of patience and left without buying.</summary>
        public static void RecordLostCustomer() => SetDaily("lost", LostToday + 1);

        public static void RecordUpgrade() => SetDaily("upgrades", UpgradesToday + 1);

        public static void RegisterWorker(string uniqueName)
        {
            string key = "worker." + uniqueName;
            if (GetInt(key) == 1) return;
            SetInt(key, 1);
            SetInt("workers", WorkerCount + 1);
        }

        public static void ClaimDailyReward()
        {
            SetInt(DailyKey("claimed"), 1);
        }

        /// <summary>
        /// Wipes this game's versioned save without deleting preferences owned
        /// by another SDK or subsystem.
        /// </summary>
        public static void ResetAll()
        {
            SaveRepository.ResetAll();
        }

        public static int GetInt(string key, int fallback = 0) => SaveRepository.GetInt(key, fallback);
        public static void SetInt(string key, int value) => SaveRepository.SetInt(key, value);

        public static long GetLastSeen()
        {
            return SaveRepository.GetLong("last_seen");
        }

        public static void SaveLastSeen()
        {
            SaveRepository.SetLong("last_seen", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        public static void FlushNow() => SaveRepository.FlushNow();

        private static int GetDaily(string name) => GetInt(DailyKey(name));
        private static void SetDaily(string name, int value) => SetInt(DailyKey(name), value);

        private static void SetRecordIfHigher(string key, int value)
        {
            if (value > GetInt(key)) SetInt(key, value);
        }

        private static string DailyKey(string name) => "daily." + DateTime.UtcNow.ToString("yyyyMMdd") + "." + name;
    }
}
