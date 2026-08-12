using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShawarmaTycoon.EditorTools
{
    /// <summary>
    /// Imports the approved Blender Phase 1 pack without passing it through the
    /// geometry-only Meshy pipeline. Each source FBX already contains all LODs,
    /// authored material slots, anchors and (for characters) one shared rig.
    /// </summary>
    [InitializeOnLoad]
    public static class CozyPackPhase1Builder
    {
        public const string ModelFolder = "Assets/ShawarmaTycoon/Art/BlenderPhase1/Models";
        public const string MaterialFolder = "Assets/ShawarmaTycoon/Art/BlenderPhase1/Materials";
        public const string AnimationFolder = "Assets/ShawarmaTycoon/Art/BlenderPhase1/Animation";
        public const string PrefabFolder = "Assets/ShawarmaTycoon/Resources/Phase1Prefabs";

        private enum AssetProfile
        {
            Character,
            Station,
            Prop
        }

        private readonly struct AssetSpec
        {
            public readonly string SourceId;
            public readonly string RuntimeId;
            public readonly AssetProfile Profile;
            public readonly bool HasAuthoredFace;
            public readonly float ExistingCallerYaw;

            public bool IsCharacter => Profile == AssetProfile.Character;
            public string ModelPath => $"{ModelFolder}/{SourceId}.fbx";
            public string PrefabPath => $"{PrefabFolder}/{SourceId}.prefab";

            public AssetSpec(
                string sourceId,
                string runtimeId,
                AssetProfile profile,
                bool hasAuthoredFace,
                float existingCallerYaw)
            {
                SourceId = sourceId;
                RuntimeId = runtimeId;
                Profile = profile;
                HasAuthoredFace = hasAuthoredFace;
                ExistingCallerYaw = existingCallerYaw;
            }
        }

        private readonly struct PaletteEntry
        {
            public readonly string Name;
            public readonly uint Rgb;
            public readonly float Metallic;
            public readonly float Smoothness;

            public PaletteEntry(string name, uint rgb, float metallic = 0f, float smoothness = 0.25f)
            {
                Name = name;
                Rgb = rgb;
                Metallic = metallic;
                Smoothness = smoothness;
            }
        }

        private static readonly AssetSpec[] Specs =
        {
            new("01_player_character", "01_player_character", AssetProfile.Character, true, 0f),
            new("02_customer_character", "02_customer_character", AssetProfile.Character, true, 0f),
            new("06_rotisserie_station", "06_shawarma_rotisserie", AssetProfile.Station, false, 180f),
            new("15_dining_table", "15_dining_table_clean", AssetProfile.Prop, false, 0f),
            new("17_trash_bin", "17_trash_bin", AssetProfile.Prop, false, 0f)
        };

        private static readonly PaletteEntry[] Palette =
        {
            new("MAT_Cream", 0xF4DFC0),
            new("MAT_SkinWarm", 0xE8B98C),
            new("MAT_Terracotta", 0xD97A55),
            new("MAT_SkinWarmLight", 0xF2D2B0),
            new("MAT_MeatBrown", 0xA9583B),
            new("MAT_SkinWarmDeep", 0xC98C63),
            new("MAT_DarkCookedMeat", 0x743D2B),
            new("MAT_HairDarkBrown", 0x3A2A22),
            new("MAT_Teal", 0x55B7AD),
            new("MAT_HairBrown", 0x6B4230),
            new("MAT_Mustard", 0xF1C557),
            new("MAT_HairAuburn", 0x8C4A2F),
            new("MAT_WarmRed", 0xD9564A),
            new("MAT_HairBlack", 0x241E1E),
            new("MAT_DarkBlueGray", 0x44515B),
            new("MAT_DarkNavy", 0x2E3944),
            new("MAT_WarmWood", 0x9A5A3A),
            new("MAT_HeatOrange", 0xF08A3C),
            new("MAT_WoodLight", 0xBE8354),
            new("MAT_BinGreen", 0x4C6B58),
            new("MAT_Steel", 0xC9CDD2, 1f, 0.65f)
        };

        private static bool building;

        static CozyPackPhase1Builder()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Shawarma Tycoon/CozyPack/Rebuild Phase 1 Prefabs", priority = 110)]
        public static void RebuildMenu()
        {
            BuildAll(force: true);
        }

        [MenuItem("Shawarma Tycoon/CozyPack/Rebuild Phase 1 Prefabs", true)]
        private static bool ValidateRebuildMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   !EditorApplication.isCompiling;
        }

        private static void BuildIfNeeded()
        {
            if (building || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;
            if (Specs.Any(spec => AssetDatabase.LoadAssetAtPath<GameObject>(spec.ModelPath) == null))
                return;
            if (Specs.All(spec => AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath) != null))
                return;

            BuildAll(force: false);
        }

        public static int BuildAll(bool force)
        {
            if (building)
                return 0;

            building = true;
            try
            {
                EnsureAssetFolder(MaterialFolder);
                EnsureAssetFolder(AnimationFolder);
                EnsureAssetFolder(PrefabFolder);

                List<string> missing = Specs
                    .Where(spec => AssetDatabase.LoadAssetAtPath<GameObject>(spec.ModelPath) == null)
                    .Select(spec => spec.ModelPath)
                    .ToList();
                if (missing.Count > 0)
                {
                    Debug.LogError("[CozyPack Phase 1] Missing FBX files:\n" + string.Join("\n", missing));
                    return 0;
                }

                foreach (AssetSpec spec in Specs)
                    ConfigureImporter(spec);

                Dictionary<string, Material> materials = CreateOrUpdatePalette();
                int built = 0;
                foreach (AssetSpec spec in Specs)
                {
                    RuntimeAnimatorController controller = spec.IsCharacter
                        ? CreateOrUpdateController(spec)
                        : null;
                    if (BuildPrefab(spec, materials, controller))
                        built++;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                if (built == Specs.Length)
                    Debug.Log($"[CozyPack Phase 1] Built {built}/{Specs.Length} approved prefabs.");
                else
                    Debug.LogError($"[CozyPack Phase 1] Built only {built}/{Specs.Length} prefabs.");
                return built;
            }
            finally
            {
                building = false;
            }
        }

        private static void ConfigureImporter(AssetSpec spec)
        {
            ModelImporter importer = AssetImporter.GetAtPath(spec.ModelPath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException($"No ModelImporter found for {spec.ModelPath}.");

            importer.globalScale = 1f;
            importer.bakeAxisConversion = false;
            importer.preserveHierarchy = true;
            importer.optimizeGameObjects = false;
            importer.addCollider = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importConstraints = false;
            importer.importAnimatedCustomProperties = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.generateSecondaryUV = false;
            importer.weldVertices = true;
            importer.optimizeMeshVertices = true;
            importer.optimizeMeshPolygons = true;
            importer.indexFormat = ModelImporterIndexFormat.Auto;

            if (spec.IsCharacter)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.resampleCurves = true;
                importer.animationCompression = ModelImporterAnimationCompression.Optimal;

                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                    clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    foreach (ModelImporterClipAnimation clip in clips)
                    {
                        clip.name = ShortClipName(string.IsNullOrEmpty(clip.takeName) ? clip.name : clip.takeName);
                        clip.loop = true;
                        clip.loopTime = true;
                        clip.loopPose = true;
                        clip.keepOriginalOrientation = true;
                        clip.keepOriginalPositionY = true;
                        clip.keepOriginalPositionXZ = true;
                        clip.lockRootRotation = true;
                        clip.lockRootHeightY = true;
                        clip.lockRootPositionXZ = true;
                    }
                    importer.clipAnimations = clips;
                }
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
            }

            importer.SaveAndReimport();
        }

        private static Dictionary<string, Material> CreateOrUpdatePalette()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException("URP/Lit and Standard shaders are both unavailable.");

            Dictionary<string, Material> materials = new(StringComparer.OrdinalIgnoreCase);
            foreach (PaletteEntry entry in Palette)
            {
                string path = $"{MaterialFolder}/{entry.Name}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = entry.Name };
                    AssetDatabase.CreateAsset(material, path);
                }
                else if (material.shader != shader)
                {
                    material.shader = shader;
                }

                Color color = FromRgb(entry.Rgb);
                material.color = color;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", entry.Metallic);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", entry.Smoothness);
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
                materials[entry.Name] = material;
            }

            return materials;
        }

        private static RuntimeAnimatorController CreateOrUpdateController(AssetSpec spec)
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(spec.ModelPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            AnimationClip idle = FindClip(clips, "Idle");
            AnimationClip walk = FindClip(clips, "Walk");
            AnimationClip carryWalk = FindClip(clips, "CarryWalk");
            if (idle == null || walk == null || carryWalk == null)
            {
                Debug.LogError(
                    $"[CozyPack Phase 1] {spec.SourceId} animation clips incomplete. " +
                    $"Found: {string.Join(", ", clips.Select(clip => clip.name))}");
                return null;
            }

            string path = $"{AnimationFolder}/{spec.SourceId}.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states.ToArray())
                stateMachine.RemoveState(child.state);
            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
                stateMachine.RemoveAnyStateTransition(transition);

            AnimatorState idleState = stateMachine.AddState("Idle");
            idleState.motion = idle;
            stateMachine.defaultState = idleState;
            stateMachine.AddState("Walk").motion = walk;
            stateMachine.AddState("CarryWalk").motion = carryWalk;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static bool BuildPrefab(
            AssetSpec spec,
            IReadOnlyDictionary<string, Material> materials,
            RuntimeAnimatorController controller)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(spec.ModelPath);
            GameObject prefabRoot = new(spec.SourceId);
            try
            {
                GameObject model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                if (model == null)
                    model = UnityEngine.Object.Instantiate(modelAsset);
                model.name = "Model";
                model.transform.SetParent(prefabRoot.transform, false);
                model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                model.transform.localScale = Vector3.one;

                foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);
                foreach (LODGroup importedLodGroup in model.GetComponentsInChildren<LODGroup>(true))
                    UnityEngine.Object.DestroyImmediate(importedLodGroup);

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    Debug.LogError($"[CozyPack Phase 1] {spec.SourceId} contains no renderers.");
                    return false;
                }

                foreach (Renderer renderer in renderers)
                {
                    Material[] slots = renderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        string materialName = NormalizeMaterialName(slots[i] != null ? slots[i].name : string.Empty);
                        if (materials.TryGetValue(materialName, out Material material))
                            slots[i] = material;
                        else
                            Debug.LogWarning($"[CozyPack Phase 1] Unmapped material '{materialName}' on {renderer.name}.");
                    }
                    renderer.sharedMaterials = slots;
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    renderer.allowOcclusionWhenDynamic = true;
                    if (renderer is SkinnedMeshRenderer skinned)
                        skinned.updateWhenOffscreen = false;
                }

                Renderer[] lod0 = RenderersForLod(renderers, 0);
                Renderer[] lod1 = RenderersForLod(renderers, 1);
                Renderer[] lod2 = RenderersForLod(renderers, 2);
                if (lod0.Length == 0 || lod1.Length == 0 || lod2.Length == 0)
                {
                    Debug.LogError(
                        $"[CozyPack Phase 1] {spec.SourceId} LOD renderers incomplete: " +
                        $"{lod0.Length}/{lod1.Length}/{lod2.Length}.");
                    return false;
                }

                float[] transitions = spec.Profile switch
                {
                    AssetProfile.Character => new[] { 0.18f, 0.08f, 0.025f },
                    AssetProfile.Station => new[] { 0.20f, 0.09f, 0.025f },
                    _ => new[] { 0.16f, 0.07f, 0.020f }
                };
                LODGroup lodGroup = prefabRoot.AddComponent<LODGroup>();
                lodGroup.fadeMode = LODFadeMode.None;
                lodGroup.animateCrossFading = false;
                lodGroup.SetLODs(new[]
                {
                    new LOD(transitions[0], lod0),
                    new LOD(transitions[1], lod1),
                    new LOD(transitions[2], lod2)
                });
                lodGroup.RecalculateBounds();

                Vector3 importedFront = FindImportedFront(prefabRoot.transform);
                float importedYaw = Mathf.Atan2(importedFront.x, importedFront.z) * Mathf.Rad2Deg;
                float correctionYaw = importedFront.sqrMagnitude > 0.25f
                    ? Mathf.DeltaAngle(importedYaw, 180f)
                    : 180f;
                float runtimeOffset = Mathf.DeltaAngle(spec.ExistingCallerYaw, correctionYaw);
                CozyVisualMetadata metadata = prefabRoot.AddComponent<CozyVisualMetadata>();
                metadata.Configure(
                    spec.SourceId,
                    new Vector3(0f, runtimeOffset, 0f),
                    spec.HasAuthoredFace);

                if (spec.IsCharacter)
                {
                    Animator animator = model.GetComponentInChildren<Animator>(true);
                    if (animator == null)
                        animator = model.AddComponent<Animator>();
                    if (animator.avatar == null)
                    {
                        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(spec.ModelPath)
                            .OfType<Avatar>()
                            .FirstOrDefault();
                        animator.avatar = avatar;
                    }
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                    prefabRoot.AddComponent<CozyAnimationDriver>();
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, spec.PrefabPath, out bool saved);
                if (!saved)
                {
                    Debug.LogError($"[CozyPack Phase 1] Failed to save {spec.PrefabPath}.");
                    return false;
                }

                Debug.Log(
                    $"[CozyPack Phase 1] {spec.SourceId} -> {spec.RuntimeId}; " +
                    $"front {importedFront:F2}, runtime yaw offset {runtimeOffset:0.#}°.");
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefabRoot);
            }
        }

        private static Renderer[] RenderersForLod(IEnumerable<Renderer> renderers, int lod)
        {
            string token = $"LOD{lod}";
            return renderers
                .Where(renderer => renderer.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
        }

        private static Vector3 FindImportedFront(Transform root)
        {
            Transform front = FindDeepChild(root, "FRONT_DIRECTION");
            if (front == null)
            {
                Debug.LogWarning($"[CozyPack Phase 1] FRONT_DIRECTION missing under {root.name}.");
                return Vector3.forward;
            }

            Vector3 direction = root.InverseTransformPoint(front.position);
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            Stack<Transform> pending = new();
            pending.Push(root);
            while (pending.Count > 0)
            {
                Transform current = pending.Pop();
                if (current.name == name || current.name.EndsWith("|" + name, StringComparison.Ordinal))
                    return current;
                for (int i = current.childCount - 1; i >= 0; i--)
                    pending.Push(current.GetChild(i));
            }
            return null;
        }

        private static AnimationClip FindClip(IEnumerable<AnimationClip> clips, string name)
        {
            return clips.FirstOrDefault(clip =>
                string.Equals(ShortClipName(clip.name), name, StringComparison.OrdinalIgnoreCase));
        }

        private static string ShortClipName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;
            string[] parts = name.Split('|');
            return parts[parts.Length - 1].Trim();
        }

        private static string NormalizeMaterialName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            string normalized = name;
            int namespaceSeparator = normalized.LastIndexOf(':');
            if (namespaceSeparator >= 0)
                normalized = normalized[(namespaceSeparator + 1)..];
            int dot = normalized.LastIndexOf('.');
            if (dot > 0 && int.TryParse(normalized[(dot + 1)..], out _))
                normalized = normalized[..dot];
            return normalized;
        }

        private static Color FromRgb(uint rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                1f);
        }

        private static void EnsureAssetFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
