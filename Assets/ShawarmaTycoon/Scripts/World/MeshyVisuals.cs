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
        private const string Phase1ResourceFolder = "Phase1Prefabs/";
        private const string LegacyResourceFolder = "MeshyPrefabs/";
        private static readonly Dictionary<string, GameObject> Prefabs = new();
        private static readonly Dictionary<string, string> Phase1Aliases = new()
        {
            { "01_player_character", "01_player_character" },
            { "02_customer_character", "02_customer_character" },
            { "06_shawarma_rotisserie", "06_rotisserie_station" },
            { "15_dining_table_clean", "15_dining_table" },
            { "17_trash_bin", "17_trash_bin" }
        };

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
            CozyVisualMetadata metadata = prefab.GetComponent<CozyVisualMetadata>();
            anchor.transform.localEulerAngles = localEulerAngles +
                (metadata != null ? metadata.RuntimeEulerOffset : Vector3.zero);

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

            // The legacy Meshy customer has no facial geometry. The approved
            // Blender character already has a complete face and must not get
            // this compatibility overlay.
            if (assetName == "02_customer_character" &&
                (metadata == null || !metadata.HasAuthoredFace))
                AddCustomerFace(anchor.transform, targetSize);

            return anchor;
        }

        private static void AddCustomerFace(Transform parent, Vector3 targetSize)
        {
            GameObject face = new("Customer Face");
            face.transform.SetParent(parent, false);

            float eyeX = targetSize.x * 0.11f;
            float eyeY = targetSize.y * 0.895f;
            float mouthY = targetSize.y * 0.835f;
            float front = -Mathf.Min(targetSize.x, targetSize.z) * 0.315f;
            Color featureColor = new(0.18f, 0.10f, 0.08f);

            PrototypeVisuals.CreatePrimitive("Eye Left", PrimitiveType.Sphere, face.transform,
                new Vector3(-eyeX, eyeY, front), new Vector3(0.055f, 0.075f, 0.025f), featureColor);
            PrototypeVisuals.CreatePrimitive("Eye Right", PrimitiveType.Sphere, face.transform,
                new Vector3(eyeX, eyeY, front), new Vector3(0.055f, 0.075f, 0.025f), featureColor);
            PrototypeVisuals.CreatePrimitive("Mouth", PrimitiveType.Cube, face.transform,
                new Vector3(0f, mouthY, front - 0.002f), new Vector3(0.13f, 0.025f, 0.018f),
                new Color(0.48f, 0.16f, 0.14f));

            foreach (Renderer renderer in face.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
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

        public static bool TryFindAnchor(Transform root, string anchorName, out Transform anchor)
        {
            anchor = null;
            if (root == null || string.IsNullOrWhiteSpace(anchorName))
                return false;

            Stack<Transform> pending = new();
            pending.Push(root);
            while (pending.Count > 0)
            {
                Transform current = pending.Pop();
                if (current.name == anchorName)
                {
                    anchor = current;
                    return true;
                }

                for (int i = current.childCount - 1; i >= 0; i--)
                    pending.Push(current.GetChild(i));
            }

            return false;
        }

        private static GameObject Load(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
                return null;
            if (Prefabs.TryGetValue(assetName, out GameObject cached))
                return cached;

            GameObject prefab = null;
            // Legacy gameplay names are aliased onto the approved pack; anything
            // authored later (the city kit) already uses its own name.
            if (Phase1Aliases.TryGetValue(assetName, out string phase1Name))
                prefab = Resources.Load<GameObject>(Phase1ResourceFolder + phase1Name);
            if (prefab == null)
                prefab = Resources.Load<GameObject>(Phase1ResourceFolder + assetName);
            if (prefab == null)
                prefab = Resources.Load<GameObject>(LegacyResourceFolder + assetName);
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

                // FBX skinned renderers can expose a tiny bind-pose localBounds
                // while their evaluated bone bounds are correctly sized in world
                // space. Using that tiny box would scale authored characters up by
                // roughly 100x. Static meshes still use their local mesh bounds so
                // rotated gameplay wrappers do not inflate the fitted size.
                Bounds source = renderer is SkinnedMeshRenderer
                    ? renderer.bounds
                    : renderer.localBounds;
                Matrix4x4 matrix = renderer is SkinnedMeshRenderer
                    ? root.worldToLocalMatrix
                    : root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                Vector3 min = source.min;
                Vector3 max = source.max;

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
