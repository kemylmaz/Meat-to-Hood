using System.Collections;
using UnityEngine;

namespace ShawarmaTycoon
{
    [DisallowMultipleComponent]
    public sealed class DioramaModule : MonoBehaviour
    {
        [SerializeField] private string moduleId;
        [SerializeField] private bool baseModule;
        [SerializeField] private Transform surfaceRoot;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private GameObject lockedPreview;
        [SerializeField] private DioramaWalkableSurface walkableSurface;

        private Coroutine animationRoutine;

        public string Id => moduleId;
        public bool IsBaseModule => baseModule;
        public bool IsUnlocked { get; private set; }
        public Transform SurfaceRoot => surfaceRoot;
        public Transform VisualRoot => visualRoot;
        public Transform ContentRoot => contentRoot;
        public GameObject LockedPreview => lockedPreview;
        public Bounds WalkableBounds => walkableSurface != null
            ? walkableSurface.Bounds
            : new Bounds(transform.position, Vector3.zero);

        public void Configure(
            string id,
            bool isBase,
            Transform surface,
            Transform visual,
            Transform content,
            GameObject preview,
            DioramaWalkableSurface walkable)
        {
            moduleId = id;
            baseModule = isBase;
            surfaceRoot = surface;
            visualRoot = visual;
            contentRoot = content;
            lockedPreview = preview;
            walkableSurface = walkable;
        }

        public void SetUnlocked(bool unlocked, bool animate)
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            IsUnlocked = unlocked;
            if (surfaceRoot != null)
            {
                surfaceRoot.gameObject.SetActive(unlocked);
                surfaceRoot.localScale = Vector3.one;
            }
            if (contentRoot != null)
            {
                contentRoot.gameObject.SetActive(unlocked);
                contentRoot.localScale = Vector3.one;
            }
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(unlocked);
                visualRoot.localScale = Vector3.one;
            }
            if (lockedPreview != null) lockedPreview.SetActive(!unlocked);

            if (unlocked && animate && visualRoot != null)
                animationRoutine = StartCoroutine(AnimateVisualRoot());
        }

        private IEnumerator AnimateVisualRoot()
        {
            Vector3 target = Vector3.one;
            visualRoot.localScale = target * 0.06f;
            float elapsed = 0f;
            const float duration = 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                visualRoot.localScale = Vector3.LerpUnclamped(target * 0.06f, target, eased);
                yield return null;
            }

            visualRoot.localScale = target;
            animationRoutine = null;
        }
    }
}
