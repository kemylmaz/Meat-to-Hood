using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>Turns visible locked plots into playable restaurant wings one at a time.</summary>
    public sealed class DioramaExpansion : MonoBehaviour
    {
        private readonly List<GameObject> modules = new();
        private readonly List<GameObject> lockedPlots = new();
        private readonly List<float> maximumXByStep = new();
        private MobilePlayerController player;
        private int unlockedCount;

        public int UnlockedCount => unlockedCount;
        public int Remaining => Mathf.Max(0, modules.Count - unlockedCount);

        public void Configure(
            MobilePlayerController playerController,
            IEnumerable<GameObject> expansionModules,
            IEnumerable<GameObject> previewPlots,
            IEnumerable<float> movementBoundsByStep)
        {
            player = playerController;
            modules.Clear();
            modules.AddRange(expansionModules);
            lockedPlots.Clear();
            if (previewPlots != null) lockedPlots.AddRange(previewPlots);
            maximumXByStep.Clear();
            maximumXByStep.AddRange(movementBoundsByStep);
            unlockedCount = Mathf.Clamp(GameProgress.GetInt("expansion", 0), 0, modules.Count);

            for (int i = 0; i < modules.Count; i++)
            {
                bool unlocked = i < unlockedCount;
                if (modules[i] != null) modules[i].SetActive(unlocked);
                if (i < lockedPlots.Count && lockedPlots[i] != null) lockedPlots[i].SetActive(!unlocked);
            }

            if (player != null && unlockedCount > 0 && unlockedCount - 1 < maximumXByStep.Count)
                player.ExpandMaximumX(maximumXByStep[unlockedCount - 1]);
        }

        public bool UnlockNext()
        {
            if (unlockedCount >= modules.Count) return false;

            GameObject module = modules[unlockedCount];
            if (module != null)
            {
                module.SetActive(true);
                StartCoroutine(AnimateModule(module.transform));
            }
            if (unlockedCount < lockedPlots.Count && lockedPlots[unlockedCount] != null)
                lockedPlots[unlockedCount].SetActive(false);

            if (player != null && unlockedCount < maximumXByStep.Count)
                player.ExpandMaximumX(maximumXByStep[unlockedCount]);

            unlockedCount++;
            GameProgress.SetInt("expansion", unlockedCount);
            GameProgress.RecordUpgrade();
            return true;
        }

        private static IEnumerator AnimateModule(Transform module)
        {
            Vector3 targetScale = module.localScale;
            module.localScale = targetScale * 0.06f;
            float elapsed = 0f;
            const float duration = 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                module.localScale = Vector3.LerpUnclamped(targetScale * 0.06f, targetScale, eased);
                yield return null;
            }

            module.localScale = targetScale;
        }
    }
}
