using System;
using System.Collections.Generic;

namespace ShawarmaTycoon
{
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public long revision;
        public long savedAtUtc;
        public List<SaveIntEntry> ints = new();
        public List<SaveLongEntry> longs = new();

        public bool TryGetInt(string key, out int value)
        {
            EnsureCollections();
            for (int i = 0; i < ints.Count; i++)
            {
                if (!string.Equals(ints[i].key, key, StringComparison.Ordinal)) continue;
                value = ints[i].value;
                return true;
            }
            value = default;
            return false;
        }

        public void SetInt(string key, int value)
        {
            EnsureCollections();
            for (int i = 0; i < ints.Count; i++)
            {
                if (!string.Equals(ints[i].key, key, StringComparison.Ordinal)) continue;
                SaveIntEntry entry = ints[i];
                entry.value = value;
                ints[i] = entry;
                return;
            }
            ints.Add(new SaveIntEntry { key = key, value = value });
        }

        public bool TryGetLong(string key, out long value)
        {
            EnsureCollections();
            for (int i = 0; i < longs.Count; i++)
            {
                if (!string.Equals(longs[i].key, key, StringComparison.Ordinal)) continue;
                value = longs[i].value;
                return true;
            }
            value = default;
            return false;
        }

        public void SetLong(string key, long value)
        {
            EnsureCollections();
            for (int i = 0; i < longs.Count; i++)
            {
                if (!string.Equals(longs[i].key, key, StringComparison.Ordinal)) continue;
                SaveLongEntry entry = longs[i];
                entry.value = value;
                longs[i] = entry;
                return;
            }
            longs.Add(new SaveLongEntry { key = key, value = value });
        }

        public void Normalize()
        {
            EnsureCollections();
            if (schemaVersion <= 0) schemaVersion = 1;
        }

        private void EnsureCollections()
        {
            ints ??= new List<SaveIntEntry>();
            longs ??= new List<SaveLongEntry>();
        }
    }

    [Serializable]
    public struct SaveIntEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    public struct SaveLongEntry
    {
        public string key;
        public long value;
    }

    [Serializable]
    internal sealed class SaveEnvelope
    {
        public int formatVersion = 1;
        public string payload;
        public string checksum;
    }
}
