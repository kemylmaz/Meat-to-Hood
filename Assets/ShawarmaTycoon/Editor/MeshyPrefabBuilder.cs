using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShawarmaTycoon.EditorTools
{
    /// <summary>
    /// Builds the canonical Meshy art library as Resources-loadable LOD prefabs.
    /// Expected model names are: &lt;asset-id&gt;_LOD0.fbx, _LOD1.fbx and _LOD2.fbx.
    /// </summary>
    public static class MeshyPrefabBuilder
    {
        public const string MaterialFolder = "Assets/ShawarmaTycoon/Art/Meshy/Materials";
        public const string PrefabFolder = "Assets/ShawarmaTycoon/Resources/MeshyPrefabs";

        private enum AssetProfile
        {
            Character,
            Station,
            Prop,
            Architecture,
            Island
        }

        private readonly struct AssetSpec
        {
            public readonly string Id;
            public readonly Color[] Palette;
            public readonly AssetProfile Profile;

            public AssetSpec(
                string id,
                AssetProfile profile,
                uint baseColor,
                uint lightColor,
                uint accentColor,
                uint darkColor)
            {
                Id = id;
                Profile = profile;
                Palette = new[]
                {
                    Cozy(baseColor),
                    Cozy(lightColor),
                    Cozy(accentColor),
                    Cozy(darkColor)
                };
            }
        }

        private readonly struct GeometryStats
        {
            public readonly int Meshes;
            public readonly long Vertices;
            public readonly long Triangles;

            public GeometryStats(int meshes, long vertices, long triangles)
            {
                Meshes = meshes;
                Vertices = vertices;
                Triangles = triangles;
            }

            public override string ToString() =>
                $"{Meshes} mesh, {Vertices:N0} verts, {Triangles:N0} tris";
        }

        // Soft, warm colours shared with the project's cozy cartoon direction.
        private static readonly AssetSpec[] AssetSpecs =
        {
            new("01_player_character",         AssetProfile.Character,    0xFFF1CF, 0xEFA77D, 0xD95B4D, 0x34495E),
            new("02_customer_character",       AssetProfile.Character,    0xF2C879, 0xE9A47C, 0x6B4936, 0x4D698C),
            new("03_cashier_worker",           AssetProfile.Character,    0xFFF1CF, 0xEFA77D, 0xD95B4D, 0x44596B),
            new("04_meat_storage_rack",        AssetProfile.Station,      0x60757B, 0xF4D6A2, 0xD56F43, 0x61473D),
            new("06_shawarma_rotisserie",      AssetProfile.Station,      0x687C80, 0xF4D6A2, 0xC65D3F, 0x343A3C),
            new("08_cutting_station",          AssetProfile.Station,      0x7FA69A, 0xFFF1CF, 0xC98255, 0x53656A),
            new("10_wrap_preparation_station", AssetProfile.Station,      0xD8A84E, 0xFFF1CF, 0x6EAE68, 0x69543A),
            new("12_service_cashier_counter",  AssetProfile.Station,      0x439C98, 0xFFF1CF, 0xE9825B, 0x385B5A),
            new("13_conveyor_straight",        AssetProfile.Prop,         0x6C8797, 0xAFC6CE, 0xE9C46A, 0x44555E),
            new("14_conveyor_corner",          AssetProfile.Prop,         0x6C8797, 0xAFC6CE, 0xE9C46A, 0x44555E),
            new("15_dining_table_clean",       AssetProfile.Prop,         0xC98255, 0xF4D6A2, 0x4FA3A0, 0x6B4936),
            new("17_trash_bin",                AssetProfile.Prop,         0x66866A, 0x97B89B, 0xE9C46A, 0x334A3A),
            new("18_money_collection_pad",     AssetProfile.Prop,         0x51B96D, 0xDDF3C4, 0xF0CA55, 0x2E6C43),
            new("19_upgrade_pad",              AssetProfile.Prop,         0x5DADE2, 0xBBDDF2, 0xF0CA55, 0x356987),
            new("21_entrance_door",            AssetProfile.Architecture, 0xC96A50, 0xFFF1CF, 0x4FA3A0, 0x714535),
            new("22_modular_floor_tile",       AssetProfile.Architecture, 0xE8B98B, 0xF8DEC0, 0xC98765, 0x8B5E48),
            new("23_modular_wall_straight",    AssetProfile.Architecture, 0xF1DFC4, 0xFFF6E4, 0xC96A50, 0x8B5E48),
            new("24_modular_wall_corner",      AssetProfile.Architecture, 0xE9D4B4, 0xFFF6E4, 0xC96A50, 0x8B5E48),
            new("34_floating_diorama_island",  AssetProfile.Island,       0xB87956, 0xE8B98B, 0x7FA06B, 0x694735)
        };

        [MenuItem("Shawarma Tycoon/Meshy/Build All LOD Prefabs", priority = 100)]
        public static void BuildAllPrefabsMenu()
        {
            BuildAllPrefabs();
        }

        [MenuItem("Shawarma Tycoon/Meshy/Build All LOD Prefabs", true)]
        private static bool ValidateBuildAllPrefabsMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   !EditorApplication.isCompiling;
        }

        /// <summary>
        /// Public build API for Unity command line, editor tests, or other tooling.
        /// Returns the number of successfully generated prefabs.
        /// </summary>
        public static int BuildAllPrefabs()
        {
            EnsureAssetFolder(MaterialFolder);
            EnsureAssetFolder(PrefabFolder);

            Dictionary<string, string> modelPaths = IndexOptimizedModels();
            List<string> validationErrors = ValidateInputs(modelPaths);
            if (validationErrors.Count > 0)
            {
                string details = string.Join("\n", validationErrors.Select(error => $"  - {error}"));
                Debug.LogError(
                    $"[Meshy] LOD prefab build cancelled. All three LOD FBXs are required for every asset.\n{details}");
                return 0;
            }

            int builtCount = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (AssetSpec spec in AssetSpecs)
                {
                    Material[] palette = CreateOrUpdateMaterials(spec);
                    if (BuildPrefab(spec, palette, modelPaths))
                        builtCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (builtCount == AssetSpecs.Length)
            {
                Debug.Log(
                    $"[Meshy] Built {builtCount}/{AssetSpecs.Length} mobile LOD prefabs in {PrefabFolder}.");
            }
            else
            {
                Debug.LogError(
                    $"[Meshy] Built {builtCount}/{AssetSpecs.Length} prefabs. Check the preceding errors.");
            }

            return builtCount;
        }

        private static bool BuildPrefab(
            AssetSpec spec,
            Material[] palette,
            IReadOnlyDictionary<string, string> modelPaths)
        {
            GameObject prefabRoot = new(spec.Id);

            try
            {
                LODGroup lodGroup = prefabRoot.AddComponent<LODGroup>();
                float[] transitions = GetTransitions(spec.Profile);
                LOD[] lods = new LOD[3];
                GeometryStats[] stats = new GeometryStats[3];

                for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
                {
                    string lookupKey = MakeLookupKey(spec.Id, lodIndex);
                    string modelPath = modelPaths[lookupKey];
                    GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                    if (modelAsset == null)
                    {
                        Debug.LogError($"[Meshy] Could not load {modelPath} as a model GameObject.");
                        return false;
                    }

                    GameObject lodObject = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                    if (lodObject == null)
                        lodObject = UnityEngine.Object.Instantiate(modelAsset);

                    lodObject.name = $"LOD{lodIndex}";
                    Transform lodTransform = lodObject.transform;
                    lodTransform.SetParent(prefabRoot.transform, false);
                    lodTransform.localPosition = Vector3.zero;
                    lodTransform.localRotation = Quaternion.identity;
                    lodTransform.localScale = Vector3.one;

                    RemoveColliders(lodObject);
                    Renderer[] renderers = GetMeshRenderers(lodObject);
                    AssignMaterials(renderers, palette);
                    stats[lodIndex] = GetGeometryStats(lodObject);

                    if (renderers.Length == 0)
                    {
                        Debug.LogError($"[Meshy] {modelPath} has no MeshRenderer or SkinnedMeshRenderer.");
                        return false;
                    }

                    lods[lodIndex] = new LOD(transitions[lodIndex], renderers)
                    {
                        fadeTransitionWidth = 0f
                    };
                }

                lodGroup.fadeMode = LODFadeMode.None;
                lodGroup.animateCrossFading = false;
                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();

                string prefabPath = $"{PrefabFolder}/{spec.Id}.prefab";
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath, out bool savedSuccessfully);
                if (!savedSuccessfully)
                {
                    Debug.LogError($"[Meshy] Unity failed to save prefab: {prefabPath}");
                    return false;
                }

                Debug.Log(
                    $"[Meshy] {spec.Id}: " +
                    $"LOD0 [{stats[0]}], LOD1 [{stats[1]}], LOD2 [{stats[2]}].");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(new InvalidOperationException(
                    $"Failed while building Meshy prefab '{spec.Id}'.", exception));
                return false;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefabRoot);
            }
        }

        private static Material[] CreateOrUpdateMaterials(AssetSpec spec)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException("Neither URP/Lit nor Standard shader could be found.");

            Material[] materials = new Material[spec.Palette.Length];
            string[] slotNames = { "Base", "Light", "Accent", "Dark" };
            for (int index = 0; index < materials.Length; index++)
            {
                string suffix = index == 0 ? string.Empty : $"_{index}_{slotNames[index]}";
                string materialPath = $"{MaterialFolder}/{spec.Id}_Cozy{suffix}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader)
                    {
                        name = $"{spec.Id}_Cozy{suffix}"
                    };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                else if (material.shader != shader)
                {
                    material.shader = shader;
                }

                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", spec.Palette[index]);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", spec.Palette[index]);
                if (material.HasProperty("_Metallic"))
                    material.SetFloat("_Metallic", 0f);
                if (material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness", 0.12f);
                if (material.HasProperty("_SpecularHighlights"))
                    material.SetFloat("_SpecularHighlights", 0f);

                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
                materials[index] = material;
            }

            return materials;
        }

        private static Dictionary<string, string> IndexOptimizedModels()
        {
            Dictionary<string, string> modelPaths =
                new(StringComparer.OrdinalIgnoreCase);

            if (!AssetDatabase.IsValidFolder(MeshyModelPostprocessor.OptimizedModelFolder.TrimEnd('/')))
                return modelPaths;

            string[] guids = AssetDatabase.FindAssets(
                "t:GameObject",
                new[] { MeshyModelPostprocessor.OptimizedModelFolder.TrimEnd('/') });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    continue;

                string key = Path.GetFileNameWithoutExtension(path);
                if (!modelPaths.TryAdd(key, path))
                    Debug.LogWarning($"[Meshy] Duplicate optimized model name '{key}': {path}");
            }

            return modelPaths;
        }

        private static List<string> ValidateInputs(IReadOnlyDictionary<string, string> modelPaths)
        {
            List<string> errors = new();

            foreach (AssetSpec spec in AssetSpecs)
            {
                for (int lodIndex = 0; lodIndex < 3; lodIndex++)
                {
                    string key = MakeLookupKey(spec.Id, lodIndex);
                    if (!modelPaths.ContainsKey(key))
                    {
                        errors.Add(
                            $"Missing {key}.fbx in {MeshyModelPostprocessor.OptimizedModelFolder.TrimEnd('/')}");
                    }
                }
            }

            return errors;
        }

        private static string MakeLookupKey(string assetId, int lodIndex) =>
            $"{assetId}_LOD{lodIndex}";

        private static Renderer[] GetMeshRenderers(GameObject root)
        {
            return root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                .ToArray();
        }

        private static void AssignMaterials(IEnumerable<Renderer> renderers, IReadOnlyList<Material> palette)
        {
            foreach (Renderer meshRenderer in renderers)
            {
                Material[] importedSlots = meshRenderer.sharedMaterials;
                int materialSlotCount = Math.Max(1, importedSlots.Length);
                Material[] materials = new Material[materialSlotCount];
                for (int index = 0; index < materialSlotCount; index++)
                {
                    int paletteIndex = Math.Min(index, palette.Count - 1);
                    string importedName = index < importedSlots.Length && importedSlots[index] != null
                        ? importedSlots[index].name
                        : string.Empty;

                    for (int slot = 0; slot < palette.Count; slot++)
                    {
                        if (!importedName.Contains($"MeshyPaletteSlot{slot}", StringComparison.Ordinal))
                            continue;
                        paletteIndex = slot;
                        break;
                    }

                    materials[index] = palette[paletteIndex];
                }
                meshRenderer.sharedMaterials = materials;
            }
        }

        private static void RemoveColliders(GameObject root)
        {
            foreach (Collider modelCollider in root.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(modelCollider);
        }

        private static GeometryStats GetGeometryStats(GameObject root)
        {
            HashSet<Mesh> meshes = new();

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                    meshes.Add(filter.sharedMesh);
            }

            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh != null)
                    meshes.Add(renderer.sharedMesh);
            }

            long vertices = 0;
            long triangles = 0;
            foreach (Mesh mesh in meshes)
            {
                vertices += mesh.vertexCount;
                for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    ulong indexCount = mesh.GetIndexCount(subMesh);
                    MeshTopology topology = mesh.GetTopology(subMesh);
                    if (topology == MeshTopology.Triangles)
                        triangles += (long)(indexCount / 3UL);
                    else if (topology == MeshTopology.Quads)
                        triangles += (long)(indexCount / 2UL);
                }
            }

            return new GeometryStats(meshes.Count, vertices, triangles);
        }

        private static float[] GetTransitions(AssetProfile profile)
        {
            return profile switch
            {
                AssetProfile.Character => new[] { 0.18f, 0.08f, 0.02f },
                AssetProfile.Station => new[] { 0.16f, 0.07f, 0.018f },
                AssetProfile.Architecture => new[] { 0.12f, 0.05f, 0.012f },
                AssetProfile.Island => new[] { 0.18f, 0.08f, 0.02f },
                _ => new[] { 0.14f, 0.06f, 0.015f }
            };
        }

        private static Color Cozy(uint rgb)
        {
            float red = ((rgb >> 16) & 0xFF) / 255f;
            float green = ((rgb >> 8) & 0xFF) / 255f;
            float blue = (rgb & 0xFF) / 255f;
            return new Color(red, green, blue, 1f);
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            string normalized = folderPath.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(normalized))
                return;

            string[] segments = normalized.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }
    }
}
