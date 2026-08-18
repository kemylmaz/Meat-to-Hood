using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Places authored CozyPack street pieces at 1:1 scale.
    ///
    /// <see cref="MeshyVisuals"/> fits a model into a target box, which is right
    /// for gameplay wrappers but wrong for modular tiles: a road tile has to keep
    /// its exact 4 m pitch or the street stops lining up. So the kit instantiates
    /// prefabs directly and only applies the authored orientation offset, which
    /// leaves every piece facing its authored Unity-forward direction (+Z).
    /// </summary>
    public static class CityKit
    {
        private const string Folder = "Phase1Prefabs/";
        private const string PolyFolder = "PolyPrefabs/";
        private static readonly Dictionary<string, GameObject> Cache = new();

        public static bool Has(string assetName) => Load(assetName) != null;

        /// <summary>Tile pitch along X, read from the model's own bounds.</summary>
        public static float TileWidth(string assetName, float fallback)
        {
            GameObject prefab = Load(assetName);
            if (prefab == null) return fallback;
            Bounds bounds = MeasureBounds(prefab);
            return bounds.size.x > 0.01f ? bounds.size.x : fallback;
        }

        public static float TileDepth(string assetName, float fallback)
        {
            GameObject prefab = Load(assetName);
            if (prefab == null) return fallback;
            Bounds bounds = MeasureBounds(prefab);
            return bounds.size.z > 0.01f ? bounds.size.z : fallback;
        }

        /// <summary>
        /// How tall a piece stands. These models have their pivot at the base, so
        /// this is what a caller subtracts to land a tile's top on a chosen height.
        /// </summary>
        public static float TileHeight(string assetName, float fallback)
        {
            GameObject prefab = Load(assetName);
            if (prefab == null) return fallback;
            Bounds bounds = MeasureBounds(prefab);
            return bounds.size.y > 0.01f ? bounds.size.y : fallback;
        }

        /// <summary>
        /// Spawns a piece with its authored +Z front, then turns it by
        /// <paramref name="extraYaw"/>. Returns null when the model is absent so
        /// callers can fall back to primitives.
        /// </summary>
        public static GameObject Spawn(
            string assetName, Transform parent, Vector3 position, float extraYaw = 0f)
        {
            GameObject prefab = Load(assetName);
            if (prefab == null) return null;

            GameObject instance = Object.Instantiate(prefab, parent, false);
            instance.name = assetName;
            instance.transform.localPosition = position;

            CozyVisualMetadata metadata = prefab.GetComponent<CozyVisualMetadata>();
            float authoredYaw = metadata != null ? metadata.RuntimeEulerOffset.y : 0f;
            instance.transform.localRotation = Quaternion.Euler(0f, authoredYaw + extraYaw, 0f);

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            return instance;
        }

        /// <summary>Repeats a tile along X, centred on <paramref name="centerX"/>.</summary>
        public static int Tile(
            string assetName, Transform parent, float centerX, float z, float span,
            float extraYaw = 0f, float y = 0f)
        {
            float pitch = TileWidth(assetName, 4f);
            if (pitch <= 0.01f) return 0;

            int count = Mathf.Max(1, Mathf.CeilToInt(span / pitch));
            float start = centerX - (count - 1) * pitch * 0.5f;
            for (int i = 0; i < count; i++)
                Spawn(assetName, parent, new Vector3(start + i * pitch, y, z), extraYaw);
            return count;
        }

        /// <summary>
        /// Repeats a tile along Z, centred on <paramref name="centerZ"/>. The
        /// piece is turned a quarter turn, so its authored length runs along Z.
        /// </summary>
        public static int TileAlongZ(
            string assetName, Transform parent, float x, float centerZ, float span, float yaw,
            float y = 0f)
        {
            float pitch = TileWidth(assetName, 4f);
            if (pitch <= 0.01f) return 0;

            int count = Mathf.Max(1, Mathf.CeilToInt(span / pitch));
            float start = centerZ - (count - 1) * pitch * 0.5f;
            for (int i = 0; i < count; i++)
                Spawn(assetName, parent, new Vector3(x, y, start + i * pitch), yaw);
            return count;
        }

        private static GameObject Load(string assetName)
        {
            if (Cache.TryGetValue(assetName, out GameObject cached))
                return cached;
            GameObject prefab = GameCatalogs.TryGetArt(assetName, out GameObject catalogPrefab)
                ? catalogPrefab
                : Resources.Load<GameObject>(Folder + assetName);
            if (prefab == null)
                prefab = Resources.Load<GameObject>(PolyFolder + assetName);
            Cache[assetName] = prefab;
            return prefab;
        }

        private static Bounds MeasureBounds(GameObject prefab)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            // LOD1/LOD2 sit at the same place, so on the authored pack LOD0 alone
            // is enough and avoids counting decimated duplicates. The Poly Pizza
            // pieces are single-mesh, so a pass that finds no LOD0 measures the
            // whole model rather than reporting a zero-size tile.
            Bounds bounds = Measure(renderers, lod0Only: true);
            return bounds.size.sqrMagnitude > 0f ? bounds : Measure(renderers, lod0Only: false);
        }

        private static Bounds Measure(Renderer[] renderers, bool lod0Only)
        {
            bool found = false;
            Bounds bounds = default;
            foreach (Renderer renderer in renderers)
            {
                if (lod0Only && !renderer.name.EndsWith("LOD0")) continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found ? bounds : new Bounds(Vector3.zero, Vector3.zero);
        }
    }
}
