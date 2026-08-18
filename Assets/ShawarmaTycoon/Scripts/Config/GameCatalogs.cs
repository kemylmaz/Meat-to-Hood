using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Central asset/config entry point. Gameplay code no longer needs to know
    /// where a Resources asset lives, and tests can run with safe in-memory
    /// defaults when the authored catalog has not been created yet.
    /// </summary>
    public static class GameCatalogs
    {
        private const string Root = "Config/";
        private static bool initialized;

        public static GameConfig Game { get; private set; }
        public static EconomyConfig Economy { get; private set; }
        public static RestaurantLayoutConfig Layout { get; private set; }
        public static DioramaWorldConfig World { get; private set; }
        public static ArtCatalog Art { get; private set; }

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            Game = Resources.Load<GameConfig>(Root + "GameConfig") ?? GameConfig.CreateRuntimeDefaults();
            Economy = Resources.Load<EconomyConfig>(Root + "EconomyConfig") ?? EconomyConfig.CreateRuntimeDefaults();
            Layout = Resources.Load<RestaurantLayoutConfig>(Root + "RestaurantLayoutConfig")
                ?? RestaurantLayoutConfig.CreateRuntimeDefaults();
            World = Resources.Load<DioramaWorldConfig>(Root + "DioramaWorldConfig")
                ?? DioramaWorldConfig.CreateRuntimeDefaults();
            Art = Resources.Load<ArtCatalog>(Root + "ArtCatalog");
        }

        public static bool TryGetArt(string id, out GameObject prefab)
        {
            Initialize();
            if (Art != null) return Art.TryGetPrefab(id, out prefab);
            prefab = null;
            return false;
        }

#if UNITY_EDITOR
        public static void ResetForEditor()
        {
            initialized = false;
            Game = null;
            Economy = null;
            Layout = null;
            World = null;
            Art = null;
        }
#endif
    }
}
