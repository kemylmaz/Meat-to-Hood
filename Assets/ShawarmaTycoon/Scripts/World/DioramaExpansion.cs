using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class DioramaExpansion : MonoBehaviour
    {
        private readonly List<GameObject> modules = new();
        private readonly List<float> maximumXByStep = new();
        private MobilePlayerController player;
        private Camera worldCamera;
        private Vector3 baseCameraPosition;
        private float baseCameraSize;
        private int unlockedCount;

        public int UnlockedCount => unlockedCount;
        public int Remaining => Mathf.Max(0, modules.Count - unlockedCount);

        public void Configure(
            MobilePlayerController playerController,
            IEnumerable<GameObject> expansionModules,
            IEnumerable<float> movementBoundsByStep)
        {
            player = playerController;
            modules.Clear();
            modules.AddRange(expansionModules);
            maximumXByStep.Clear();
            maximumXByStep.AddRange(movementBoundsByStep);

            worldCamera = Camera.main;
            if (worldCamera != null)
            {
                baseCameraPosition = worldCamera.transform.position;
                baseCameraSize = worldCamera.orthographicSize;
            }

            for (int i = 0; i < modules.Count; i++)
                if (modules[i] != null) modules[i].SetActive(i < unlockedCount);
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

            if (player != null && unlockedCount < maximumXByStep.Count)
                player.ExpandMaximumX(maximumXByStep[unlockedCount]);

            if (worldCamera != null)
            {
                float cameraShift = unlockedCount == 0 ? 2.2f : 4.2f;
                float targetSize = baseCameraSize + (unlockedCount + 1) * 3.5f;
                StartCoroutine(AnimateCamera(baseCameraPosition + Vector3.right * cameraShift, targetSize));
            }

            unlockedCount++;
            return true;
        }

        private static IEnumerator AnimateModule(Transform module)
        {
            Vector3 targetScale = module.localScale;
            module.localScale = targetScale * 0.05f;
            float elapsed = 0f;
            const float duration = 0.45f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                module.localScale = Vector3.LerpUnclamped(targetScale * 0.05f, targetScale, eased);
                yield return null;
            }

            module.localScale = targetScale;
        }

        private IEnumerator AnimateCamera(Vector3 targetPosition, float targetSize)
        {
            Vector3 startPosition = worldCamera.transform.position;
            float startSize = worldCamera.orthographicSize;
            float elapsed = 0f;
            const float duration = 0.65f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                worldCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                worldCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
                yield return null;
            }

            worldCamera.transform.position = targetPosition;
            worldCamera.orthographicSize = targetSize;
        }
    }
}
