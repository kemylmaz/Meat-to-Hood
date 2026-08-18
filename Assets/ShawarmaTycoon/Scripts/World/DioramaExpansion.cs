using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>Turns visible locked plots into playable restaurant wings one at a time.</summary>
    public sealed class DioramaExpansion : MonoBehaviour
    {
        private readonly List<DioramaModule> modules = new();
        private int unlockedCount;

        public int UnlockedCount => unlockedCount;
        public int Remaining => Mathf.Max(0, modules.Count - unlockedCount);
        public IReadOnlyList<DioramaModule> Modules => modules;
        public event Action<DioramaModule> ModuleUnlocked;

        public void Configure(IEnumerable<DioramaModule> expansionModules)
        {
            modules.Clear();
            modules.AddRange(expansionModules);
            unlockedCount = Mathf.Clamp(GameProgress.GetInt("expansion", 0), 0, modules.Count);

            for (int i = 0; i < modules.Count; i++)
                modules[i]?.SetUnlocked(i < unlockedCount, false);
        }

        public bool UnlockNext()
        {
            if (unlockedCount >= modules.Count) return false;

            DioramaModule module = modules[unlockedCount];
            module?.SetUnlocked(true, true);

            unlockedCount++;
            GameProgress.SetInt("expansion", unlockedCount);
            GameProgress.RecordUpgrade();
            ModuleUnlocked?.Invoke(module);
            return true;
        }
    }
}
