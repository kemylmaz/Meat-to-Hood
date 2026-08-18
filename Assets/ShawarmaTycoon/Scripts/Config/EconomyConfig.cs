using UnityEngine;

namespace ShawarmaTycoon
{
    [CreateAssetMenu(menuName = "Shawarma Tycoon/Configuration/Economy Config", fileName = "EconomyConfig")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [Header("Starting State")]
        [SerializeField, Min(0)] private int startingCoins;

        [Header("Offline Income")]
        [SerializeField, Min(0f)] private float offlineIncomeCapHours = 8f;
        [SerializeField, Min(1f)] private float secondsPerCoinPerWorker = 30f;

        public int StartingCoins => Mathf.Max(0, startingCoins);
        public float OfflineIncomeCapSeconds => Mathf.Max(0f, offlineIncomeCapHours) * 60f * 60f;
        public float SecondsPerCoinPerWorker => Mathf.Max(1f, secondsPerCoinPerWorker);

        public static EconomyConfig CreateRuntimeDefaults()
        {
            EconomyConfig config = CreateInstance<EconomyConfig>();
            config.name = "Runtime Economy Config";
            return config;
        }
    }
}
