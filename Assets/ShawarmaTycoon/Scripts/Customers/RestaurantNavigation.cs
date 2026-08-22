using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Builds a runtime navigation mesh from the restaurant's colliders. The
    /// restaurant is assembled (and later rearranged) at runtime, so an authored
    /// NavMesh asset would immediately become stale.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RestaurantNavigation : MonoBehaviour
    {
        private static readonly Vector2[] SampleOffsets =
        {
            Vector2.zero,
            new(0.45f, 0f), new(-0.45f, 0f), new(0f, 0.45f), new(0f, -0.45f),
            new(0.32f, 0.32f), new(0.32f, -0.32f),
            new(-0.32f, 0.32f), new(-0.32f, -0.32f),
            new(0.9f, 0f), new(-0.9f, 0f), new(0f, 0.9f), new(0f, -0.9f),
            new(0.64f, 0.64f), new(0.64f, -0.64f),
            new(-0.64f, 0.64f), new(-0.64f, -0.64f)
        };

        private NavMeshPath scratchPath;
        private NavMeshSurface surface;

        public static RestaurantNavigation Instance { get; private set; }
        public int Version { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            Instance = this;
            if (scratchPath == null) scratchPath = new NavMeshPath();
            if (surface == null) surface = GetComponent<NavMeshSurface>();
            if (surface == null) surface = gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        }

        public void Rebuild()
        {
            if (surface == null) return;

            int notWalkable = NavMesh.GetAreaFromName("Not Walkable");
            PlaceableObject[] furniture = FindObjectsByType<PlaceableObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < furniture.Length; i++)
            {
                NavMeshModifier modifier = GetOrAddModifier(furniture[i].gameObject);
                modifier.ignoreFromBuild = false;
                modifier.overrideArea = true;
                modifier.area = notWalkable;
                modifier.applyToChildren = true;

                // A modifier changes the area of the furniture meshes themselves,
                // but it does not cut their footprint out of the floor below.
                // This volume does, so path corners cannot run through a chair or
                // counter merely because the floor collider continues under it.
                Bounds bounds = furniture[i].FootprintBounds;
                Transform blocker = furniture[i].transform.Find("Navigation Footprint");
                if (blocker == null)
                {
                    blocker = new GameObject("Navigation Footprint").transform;
                    blocker.SetParent(furniture[i].transform, true);
                }
                blocker.SetPositionAndRotation(bounds.center, Quaternion.identity);
                NavMeshModifierVolume volume = blocker.GetComponent<NavMeshModifierVolume>();
                if (volume == null) volume = blocker.gameObject.AddComponent<NavMeshModifierVolume>();
                volume.center = Vector3.zero;
                volume.size = new Vector3(
                    Mathf.Max(0.2f, bounds.size.x - 0.12f),
                    Mathf.Max(2f, bounds.size.y + 0.5f),
                    Mathf.Max(0.2f, bounds.size.z - 0.12f));
                volume.area = notWalkable;
            }

            // Upgrade pads are floor markings, not walls. Their shallow visual
            // colliders should neither block a route nor create walkable islands
            // on top of their signs.
            PurchasePad[] pads = FindObjectsByType<PurchasePad>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < pads.Length; i++)
            {
                NavMeshModifier modifier = GetOrAddModifier(pads[i].gameObject);
                modifier.ignoreFromBuild = true;
                modifier.applyToChildren = true;
            }

            // Script-driven characters are temporary obstacles. Baking any one
            // of them into the floor would leave a person-shaped hole after they
            // walked away, especially when build mode refreshes the mesh.
            CharacterController[] characters = FindObjectsByType<CharacterController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < characters.Length; i++)
            {
                NavMeshModifier modifier = GetOrAddModifier(characters[i].gameObject);
                modifier.ignoreFromBuild = true;
                modifier.applyToChildren = true;
            }

            surface.BuildNavMesh();
            Version++;
        }

        public bool TryCalculatePath(Vector3 from, Vector3 to, List<Vector3> corners)
        {
            if (corners == null) return false;
            corners.Clear();

            if (scratchPath == null) scratchPath = new NavMeshPath();
            // Search horizontally at floor height. A broad single SamplePosition
            // can choose the navmesh on a tabletop because it is geometrically
            // closer than the aisle around that table.
            from.y = to.y;
            for (int startIndex = 0; startIndex < SampleOffsets.Length; startIndex++)
            {
                if (!TrySampleFloor(from, SampleOffsets[startIndex], out NavMeshHit start)) continue;
                for (int endIndex = 0; endIndex < SampleOffsets.Length; endIndex++)
                {
                    if (!TrySampleFloor(to, SampleOffsets[endIndex], out NavMeshHit end)) continue;
                    if (!NavMesh.CalculatePath(
                            start.position, end.position, NavMesh.AllAreas, scratchPath) ||
                        scratchPath.status != NavMeshPathStatus.PathComplete)
                        continue;

                    Vector3[] pathCorners = scratchPath.corners;
                    for (int i = 0; i < pathCorners.Length; i++) corners.Add(pathCorners[i]);
                    return corners.Count > 0;
                }
            }
            return false;
        }

        private static bool TrySampleFloor(
            Vector3 origin, Vector2 offset, out NavMeshHit hit)
        {
            Vector3 probe = origin + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(probe, out hit, 0.3f, NavMesh.AllAreas) &&
                Mathf.Abs(hit.position.y - origin.y) <= 0.2f)
                return true;
            hit = default;
            return false;
        }

        private static NavMeshModifier GetOrAddModifier(GameObject target)
        {
            NavMeshModifier modifier = target.GetComponent<NavMeshModifier>();
            return modifier != null ? modifier : target.AddComponent<NavMeshModifier>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
