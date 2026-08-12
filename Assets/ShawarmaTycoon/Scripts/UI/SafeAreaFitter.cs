using UnityEngine;

namespace ShawarmaTycoon.UI
{
    /// <summary>Keeps a RectTransform inside the device safe area (notches, home bar).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rect;
        private Rect appliedArea;
        private Vector2Int appliedResolution;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != appliedArea ||
                appliedResolution.x != Screen.width ||
                appliedResolution.y != Screen.height)
                Apply();
        }

        private void Apply()
        {
            if (rect == null) return;
            Rect area = Screen.safeArea;
            appliedArea = area;
            appliedResolution = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            if (area.width <= 0f || area.height <= 0f)
                area = new Rect(0f, 0f, Screen.width, Screen.height);

            Vector2 min = area.position;
            Vector2 max = area.position + area.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
