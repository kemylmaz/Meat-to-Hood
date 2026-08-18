using UnityEngine;

namespace ShawarmaTycoon
{
    public readonly struct PrototypeRuntimeServices
    {
        public PrototypeRuntimeServices(
            GameEconomy economy, GameplayObjectPool objectPool,
            GameSessionPersistence persistence)
        {
            Economy = economy;
            ObjectPool = objectPool;
            Persistence = persistence;
        }

        public GameEconomy Economy { get; }
        public GameplayObjectPool ObjectPool { get; }
        public GameSessionPersistence Persistence { get; }
    }

    /// <summary>
    /// Installs runtime-wide services before the prototype creates gameplay
    /// objects. The bootstrap can now concentrate on layout and Phase 3 can
    /// replace that layout without rebuilding persistence, pooling or platform
    /// setup again.
    /// </summary>
    public static class PrototypeRuntimeInstaller
    {
        public static void ConfigureApplication(GameConfig config)
        {
            config ??= GameConfig.CreateRuntimeDefaults();
            Application.targetFrameRate = config.TargetFrameRate;
            QualitySettings.vSyncCount = 0;

#if !UNITY_EDITOR
            // Player-only. In the Editor this flag decides whether an unfocused
            // Editor keeps ticking, and forcing it off froze play mode the moment
            // the window lost focus - which is also what stopped the play mode
            // test runs from ever starting. It also writes itself into
            // ProjectSettings, so every play session dirtied the file.
            Application.runInBackground = config.RunInBackground;
            if (config.PortraitOnly) Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = config.KeepScreenAwake
                ? SleepTimeout.NeverSleep
                : SleepTimeout.SystemSetting;
#endif
        }

        public static PrototypeRuntimeServices Install(
            GameObject root, GameConfig gameConfig, EconomyConfig economyConfig,
            int sceneStartingCoins)
        {
            GameCatalogs.Initialize();
            gameConfig ??= GameCatalogs.Game;
            economyConfig ??= GameCatalogs.Economy;

            GameplayObjectPool objectPool = root.AddComponent<GameplayObjectPool>();
            GameEconomy economy = root.AddComponent<GameEconomy>();
            int initialCoins = sceneStartingCoins > 0
                ? sceneStartingCoins
                : economyConfig.StartingCoins;
            economy.Configure(initialCoins);

            if (gameConfig.Features.RushHour) root.AddComponent<RushHourSystem>();
            if (gameConfig.Features.Combo) root.AddComponent<ComboSystem>();
            root.AddComponent<ReputationSystem>();
            AudioDirector.Ensure(root.transform);

            GameSessionPersistence persistence = root.AddComponent<GameSessionPersistence>();
            persistence.Configure(gameConfig, economyConfig);
            return new PrototypeRuntimeServices(economy, objectPool, persistence);
        }
    }
}
