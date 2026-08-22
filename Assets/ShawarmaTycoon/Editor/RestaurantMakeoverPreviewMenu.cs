#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ShawarmaTycoon.EditorTools
{
    /// <summary>
    /// Lets art/design review every restaurant state without buying upgrades or
    /// modifying the current save. The runtime still follows real progression.
    /// </summary>
    public static class RestaurantMakeoverPreviewMenu
    {
        [MenuItem("Shawarma Tycoon/Scene/Makeover Preview/Level 1 - Mahalle", priority = 40)]
        private static void PreviewOne() => Preview(1);

        [MenuItem("Shawarma Tycoon/Scene/Makeover Preview/Level 2 - Bistro", priority = 41)]
        private static void PreviewTwo() => Preview(2);

        [MenuItem("Shawarma Tycoon/Scene/Makeover Preview/Level 3 - Yesil", priority = 42)]
        private static void PreviewThree() => Preview(3);

        [MenuItem("Shawarma Tycoon/Scene/Makeover Preview/Level 4 - Usta", priority = 43)]
        private static void PreviewFour() => Preview(4);

        [MenuItem("Shawarma Tycoon/Scene/Makeover Preview/Level 5 - Imza", priority = 44)]
        private static void PreviewFive() => Preview(5);

        private static void Preview(int tier)
        {
            RestaurantMakeoverSystem system = Object.FindFirstObjectByType<RestaurantMakeoverSystem>();
            if (system == null)
            {
                Debug.LogWarning("[ShawarmaTycoon] Önce Play'e girin veya World Preview oluşturun.");
                return;
            }

            system.PreviewTier(tier);
            SceneView.RepaintAll();
        }
    }
}
#endif
