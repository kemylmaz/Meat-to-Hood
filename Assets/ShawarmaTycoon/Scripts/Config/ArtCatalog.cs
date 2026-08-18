using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    [CreateAssetMenu(menuName = "Shawarma Tycoon/Configuration/Art Catalog", fileName = "ArtCatalog")]
    public sealed class ArtCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public GameObject prefab;
        }

        [SerializeField] private List<Entry> entries = new();
        private Dictionary<string, GameObject> lookup;

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGetPrefab(string id, out GameObject prefab)
        {
            EnsureLookup();
            return lookup.TryGetValue(id, out prefab) && prefab != null;
        }

#if UNITY_EDITOR
        public void ReplaceEntries(IEnumerable<Entry> replacement)
        {
            entries.Clear();
            entries.AddRange(replacement);
            lookup = null;
        }
#endif

        private void OnEnable() => lookup = null;

        private void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry.id) || entry.prefab == null) continue;
                lookup[entry.id] = entry.prefab;
            }
        }
    }
}
