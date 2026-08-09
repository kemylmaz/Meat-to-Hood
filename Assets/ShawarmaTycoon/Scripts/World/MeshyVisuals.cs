using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Loads the optimized Meshy LOD prefabs as visual-only children. Gameplay
    /// wrappers, primitive colliders and interaction components stay untouched.
    /// Missing assets cleanly fall back to the prototype primitives.
    /// </summary>
    public static class MeshyVisuals
    {
        private const string ResourceFolder = "MeshyPrefabs/";
        private static readonly Dictionary<string, GameObject> Prefabs = new();

        public static bool IsAvailable(string assetName) => Load(assetName) != null;

        public static GameObject TryAttach(
            Transform parent,
            string assetName,
            Vector3 targetSize,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            bool nonUniformScale = false)
        {
            GameObject prefab = Load(assetName);
            if (prefab == null || parent == null)
                return null;

            GameObject anchor = new(assetName + " Visual");
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = localPosition;
            anchor.transform.localEulerAngles = localEulerAngles;

            GameObject visual = Object.Instantiate(prefab, anchor.transform, false);
            visual.name = assetName;
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            if (TryCalculateLocalBounds(anchor.transform, renderers, out Bounds bounds))
            {
                Vector3 scale = CalculateScale(bounds.size, targetSize, nonUniformScale);
                visual.transform.localScale = scale;
                visual.transform.localPosition = new Vector3(
                    -bounds.center.x * scale.x,
                    -bounds.min.y * scale.y,
                    -bounds.center.z * scale.z);
            }

            return anchor;
        }

        public static bool TryReplaceDirect(
            Transform parent,
            string assetName,
            Vector3 targetSize,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            bool nonUniformScale,
            params string[] placeholderNames)
        {
            GameObject visual = TryAttach(parent, assetName, targetSize, localPosition, localEulerAngles, nonUniformScale);
            if (visual == null)
                return false;

            HideDirectRenderers(parent, placeholderNames);
            return true;
        }

        public static void HideDirectRenderers(Transform parent, params string[] childNames)
        {
            if (parent == null || childNames == null)
                return;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                for (int j = 0; j < childNames.Length; j++)
                {
                    if (child.name != childNames[j])
                        continue;

                    foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>(true))
                        renderer.enabled = false;
                    break;
                }
            }
        }

        private static GameObject Load(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
                return null;
            if (Prefabs.TryGetValue(assetName, out GameObject cached))
                return cached;

            GameObject prefab = Resources.Load<GameObject>(ResourceFolder + assetName);
            Prefabs[assetName] = prefab;
            return prefab;
        }

        private static Vector3 CalculateScale(Vector3 source, Vector3 target, bool nonUniform)
        {
            const float epsilon = 0.0001f;
            if (nonUniform)
            {
                return new Vector3(
                    target.x > epsilon && source.x > epsilon ? target.x / source.x : 1f,
                    target.y > epsilon && source.y > epsilon ? target.y / source.y : 1f,
                    target.z > epsilon && source.z > epsilon ? target.z / source.z : 1f);
            }

            float factor = float.PositiveInfinity;
            if (target.x > epsilon && source.x > epsilon) factor = Mathf.Min(factor, target.x / source.x);
            if (target.y > epsilon && source.y > epsilon) factor = Mathf.Min(factor, target.y / source.y);
            if (target.z > epsilon && source.z > epsilon) factor = Mathf.Min(factor, target.z / source.z);
            if (float.IsInfinity(factor) || factor <= epsilon) factor = 1f;
            return Vector3.one * factor;
        }

        private static bool TryCalculateLocalBounds(Transform root, Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            if (root == null || renderers == null)
                return false;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Bounds local = renderer.localBounds;
                Matrix4x4 matrix = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                Vector3 min = local.min;
                Vector3 max = local.max;

                for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    Vector3 corner = matrix.MultiplyPoint3x4(new Vector3(
                        x == 0 ? min.x : max.x,
                        y == 0 ? min.y : max.y,
                        z == 0 ? min.z : max.z));
                    if (!found)
                    {
                        bounds = new Bounds(corner, Vector3.zero);
                        found = true;
                    }
                    else bounds.Encapsulate(corner);
                }
            }

            return found;
        }
    }
}
