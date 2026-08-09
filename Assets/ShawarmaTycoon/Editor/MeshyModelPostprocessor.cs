using System;
using UnityEditor;
using UnityEngine;

namespace ShawarmaTycoon.EditorTools
{
    /// <summary>
    /// Applies a deterministic, mobile-oriented import profile to the decimated
    /// Meshy FBX files. Source GLBs are intentionally left untouched.
    /// </summary>
    public sealed class MeshyModelPostprocessor : AssetPostprocessor
    {
        public const string OptimizedModelFolder = "Assets/ShawarmaTycoon/Art/Meshy/Optimized/";

        private bool IsOptimizedMeshyModel =>
            assetPath.StartsWith(OptimizedModelFolder, StringComparison.OrdinalIgnoreCase) &&
            assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);

        private void OnPreprocessModel()
        {
            if (!IsOptimizedMeshyModel || assetImporter is not ModelImporter importer)
                return;

            // Geometry only. These props are static tycoon visuals and do not need
            // animation, avatars, blend shapes, lights, cameras, or material extraction.
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importConstraints = false;
            importer.importAnimatedCustomProperties = false;
            // The optimized FBXs use four material indices as lightweight colour
            // regions. Keep those slots embedded in the model; the prefab builder
            // replaces the placeholder materials with shared cozy URP materials.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;

            // Preserve authored surface normals while avoiding tangent data that the
            // intentionally basic, single-colour URP materials do not use.
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;

            // Mobile memory and rendering profile.
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.generateSecondaryUV = false;
            importer.weldVertices = true;
            importer.optimizeMeshVertices = true;
            importer.optimizeMeshPolygons = true;
            importer.indexFormat = ModelImporterIndexFormat.Auto;
            importer.addCollider = false;
        }

        private void OnPostprocessModel(GameObject importedRoot)
        {
            if (!IsOptimizedMeshyModel || importedRoot == null)
                return;

            // addCollider=false prevents generated colliders. This second guard also
            // strips any colliders authored into an FBX hierarchy.
            Collider[] colliders = importedRoot.GetComponentsInChildren<Collider>(true);
            foreach (Collider modelCollider in colliders)
                UnityEngine.Object.DestroyImmediate(modelCollider);
        }
    }
}
