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
        private const string PolyResourceFolder = "PolyPrefabs/";
        private const string LegacyResourceFolder = "MeshyPrefabs/";
        private static readonly Dictionary<string, GameObject> Prefabs = new();
        /// <summary>
        /// Maps the gameplay-facing asset ids the bootstrap asks for onto the
        /// approved art. Phase 3 (the soft "cozy" set built from the reference
        /// renders) supersedes the earlier packs wherever it has a match.
        /// </summary>
        private static readonly Dictionary<string, string> Phase1Aliases = new()
        {
            // characters
            { "01_player_character", "54_worker_red" },
            { "02_customer_character", "52_customer_sweater" },
            { "03_cashier_worker", "53_worker_teal" },
            // stations
            { "06_shawarma_rotisserie", "06_rotisserie_station" },
            { "08_cutting_station", "60_cutting_station" },
            { "10_wrap_preparation_station", "61_wrap_station" },
            { "12_service_cashier_counter", "62_cashier_counter" },
            { "13_conveyor_straight", "63_conveyor_straight" },
            { "14_conveyor_corner", "64_conveyor_corner" },
            // furniture and props
            { "15_dining_table_clean", "70_dining_table" },
            { "16_dirty_table_props", "71_dining_table_dirty" },
            { "17_trash_bin", "72_trash_bin" },
            { "33_decorative_plant", "73_planter" },
            // pads and shell
            { "18_money_collection_pad", "82_money_pad" },
            { "19_upgrade_pad", "83_upgrade_pad" },
            { "20_locked_expansion_pad", "81_lock_pad" },
            { "21_entrance_door", "80_entrance" },
            { "22_modular_floor_tile", "78_floor_tiled" },
            { "23_modular_wall_straight", "77_wall_straight" },
            { "24_modular_wall_corner", "76_wall_corner" },
            { "34_floating_diorama_island", "79_floor_plot" },
            // offices
            { "25_hr_manager_desk", "65_manager_desk_stamp" },
            { "26_recruitment_desk", "66_manager_desk_pencils" },
            { "27_general_manager_desk", "67_manager_desk_plant" }
        };

        /// <summary>
        /// Customer body variants, picked per spawn for visual variety. Eight
        /// bodies rather than three, so a full queue is no longer two of everyone.
        /// The staff and the player deliberately stay on the authored pack: they
        /// are the shop's own people and read as a uniformed set against a crowd
        /// that is dressed at random.
        /// </summary>
        public static readonly string[] CustomerVariants =
        {
            "140_customer_male_casual", "145_customer_female_casual",
            "142_customer_male_longsleeve", "146_customer_female_dress",
            "141_customer_male_shirt", "147_customer_female_tanktop",
            "143_customer_male_suit", "144_customer_female_alt"
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

        /// <summary>
        /// Places an approved CozyPack prefab at its authored metre scale. These
        /// prefabs already use a bottom-centre pivot and +Z forward; fitting them
        /// into arbitrary boxes made counters undersized and table chairs drift.
        /// </summary>
        public static GameObject TryAttachAuthored(
            Transform parent,
            string assetName,
            Vector3 localPosition,
            Vector3 localEulerAngles)
        {
            GameObject prefab = Load(assetName);
            if (prefab == null || parent == null) return null;

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
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
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

        public static bool TryReplaceDirectAuthored(
            Transform parent,
            string assetName,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            params string[] placeholderNames)
        {
            GameObject visual = TryAttachAuthored(parent, assetName, localPosition, localEulerAngles);
            if (visual == null) return false;
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
            if (GameCatalogs.TryGetArt(assetName, out GameObject catalogPrefab))
                prefab = catalogPrefab;
            // Legacy gameplay names are aliased onto the approved pack; anything
            // authored later (the city kit) already uses its own name.
            if (prefab == null && Phase1Aliases.TryGetValue(assetName, out string phase1Name))
                prefab = Resources.Load<GameObject>(Phase1ResourceFolder + phase1Name);
            if (prefab == null)
                prefab = Resources.Load<GameObject>(Phase1ResourceFolder + assetName);
            // The Poly Pizza bundles. Searched after the authored pack so an alias
            // above still wins, and before the legacy Meshy geometry it replaces.
            if (prefab == null)
                prefab = Resources.Load<GameObject>(PolyResourceFolder + assetName);
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
