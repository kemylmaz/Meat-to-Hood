using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    [CreateAssetMenu(menuName = "Shawarma Tycoon/Configuration/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Mobile Runtime")]
        [SerializeField, Range(30, 120)] private int targetFrameRate = 60;
        [SerializeField] private bool portraitOnly = true;
        [SerializeField] private bool runInBackground;
        [SerializeField] private bool keepScreenAwake;

        [Header("Persistence")]
        [SerializeField, Min(2f)] private float saveFlushIntervalSeconds = 15f;

        [Header("Feature Flags")]
        [SerializeField] private GameFeatureFlags features = new();

        public int TargetFrameRate => Mathf.Clamp(targetFrameRate, 30, 120);
        public bool PortraitOnly => portraitOnly;
        public bool RunInBackground => runInBackground;
        public bool KeepScreenAwake => keepScreenAwake;
        public float SaveFlushIntervalSeconds => Mathf.Max(2f, saveFlushIntervalSeconds);
        public GameFeatureFlags Features => features ??= new GameFeatureFlags();

        public static GameConfig CreateRuntimeDefaults()
        {
            GameConfig config = CreateInstance<GameConfig>();
            config.name = "Runtime Game Config";
            return config;
        }
    }

    [Serializable]
    public sealed class GameFeatureFlags
    {
        // The shop stands on a street now rather than floating, so the block and
        // its traffic are part of the world instead of optional decor.
        [SerializeField] private bool cityDecor = true;
        [SerializeField] private bool traffic = true;
        [SerializeField] private bool rushHour = true;
        [SerializeField] private bool combo = true;
        [SerializeField] private bool vipCustomers = true;
        [SerializeField] private bool takeaway = true;
        [SerializeField] private bool floorSpills = true;

        public bool CityDecor => cityDecor;
        public bool Traffic => traffic;
        public bool RushHour => rushHour;
        public bool Combo => combo;
        public bool VipCustomers => vipCustomers;
        public bool Takeaway => takeaway;
        public bool FloorSpills => floorSpills;
    }
}
