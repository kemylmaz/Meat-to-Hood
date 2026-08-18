using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// In-memory, versioned save facade. Mutations only mark the save dirty;
    /// GameSessionPersistence flushes at checkpoints instead of blocking the
    /// main thread on every collected banknote.
    /// </summary>
    public static class SaveRepository
    {
        private const string LegacyPrefix = "shawarma.tycoon.";
        private const string LegacyDisabledKey = LegacyPrefix + "legacy.disabled";

        private static ISaveProvider provider;
        private static SaveData data;
        private static bool initialized;
        private static bool dirty;
        private static bool allowLegacyFallback;

        public static bool IsDirty => dirty;
        public static long Revision
        {
            get { EnsureInitialized(); return data.revision; }
        }

        public static int GetInt(string key, int fallback = 0)
        {
            EnsureInitialized();
            if (data.TryGetInt(key, out int value)) return value;

            string legacyKey = LegacyPrefix + key;
            if (allowLegacyFallback && PlayerPrefs.HasKey(legacyKey))
            {
                value = PlayerPrefs.GetInt(legacyKey, fallback);
                data.SetInt(key, value);
                dirty = true;
                return value;
            }
            return fallback;
        }

        public static void SetInt(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Save key is required.", nameof(key));
            EnsureInitialized();
            if (data.TryGetInt(key, out int current) && current == value) return;
            data.SetInt(key, value);
            dirty = true;
        }

        public static long GetLong(string key, long fallback = 0L)
        {
            EnsureInitialized();
            if (data.TryGetLong(key, out long value)) return value;

            string legacyKey = LegacyPrefix + key;
            if (allowLegacyFallback && PlayerPrefs.HasKey(legacyKey) &&
                long.TryParse(PlayerPrefs.GetString(legacyKey, fallback.ToString()), out value))
            {
                data.SetLong(key, value);
                dirty = true;
                return value;
            }
            return fallback;
        }

        public static void SetLong(string key, long value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Save key is required.", nameof(key));
            EnsureInitialized();
            if (data.TryGetLong(key, out long current) && current == value) return;
            data.SetLong(key, value);
            dirty = true;
        }

        public static void FlushNow()
        {
            EnsureInitialized();
            if (!dirty) return;
            data.schemaVersion = SaveData.CurrentSchemaVersion;
            data.revision++;
            data.savedAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            provider.Save(data);
            dirty = false;
        }

        public static void ResetAll()
        {
            EnsureInitialized();
            provider.Delete();
            data = new SaveData();
            allowLegacyFallback = false;
            PlayerPrefs.SetInt(LegacyDisabledKey, 1);
            PlayerPrefs.Save();
            dirty = true;
            FlushNow();
        }

        private static void EnsureInitialized()
        {
            if (initialized) return;
            Initialize(SaveProviderFactory.CreateDefault());
        }

        internal static void Initialize(ISaveProvider saveProvider)
        {
            provider = saveProvider ?? throw new ArgumentNullException(nameof(saveProvider));
            initialized = true;
            allowLegacyFallback = PlayerPrefs.GetInt(LegacyDisabledKey, 0) == 0;

            if (!provider.TryLoad(out data) || data == null)
            {
                data = new SaveData();
                dirty = false;
                return;
            }

            data.Normalize();
            MigrateToCurrentSchema();
        }

        private static void MigrateToCurrentSchema()
        {
            if (data.schemaVersion >= SaveData.CurrentSchemaVersion) return;

            // Schema 2 introduces a long timestamp field. Older saves lazily
            // migrate last_seen from PlayerPrefs when it is first requested.
            data.schemaVersion = SaveData.CurrentSchemaVersion;
            dirty = true;
        }

#if UNITY_EDITOR
        public static void InitializeForTests(ISaveProvider saveProvider) => Initialize(saveProvider);

        public static void ResetStateForTests()
        {
            provider = null;
            data = null;
            initialized = false;
            dirty = false;
            allowLegacyFallback = false;
        }
#endif
    }
}
