using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Explicitly marks restaurant deck colliders. Props and locked previews can
    /// no longer be mistaken for ground by the player controller.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DioramaWalkableSurface : MonoBehaviour
    {
        private DioramaWalkableRegistry registry;
        private BoxCollider surfaceCollider;

        public Bounds Bounds => surfaceCollider != null
            ? surfaceCollider.bounds
            : new Bounds(transform.position, Vector3.zero);
        public float TopY => Bounds.max.y;

        public void Configure(DioramaWalkableRegistry owner, BoxCollider collider)
        {
            if (registry != null) registry.Unregister(this);
            registry = owner;
            surfaceCollider = collider;
            if (isActiveAndEnabled && registry != null) registry.Register(this);
        }

        public bool ContainsXZ(Vector3 worldPosition, float inset = 0f)
        {
            Bounds bounds = Bounds;
            return worldPosition.x >= bounds.min.x + inset &&
                   worldPosition.x <= bounds.max.x - inset &&
                   worldPosition.z >= bounds.min.z + inset &&
                   worldPosition.z <= bounds.max.z - inset;
        }

        private void OnEnable()
        {
            if (registry != null) registry.Register(this);
        }

        private void OnDisable()
        {
            if (registry != null) registry.Unregister(this);
        }

        private void OnDestroy()
        {
            if (registry != null) registry.Unregister(this);
        }
    }

    /// <summary>Union of the currently unlocked restaurant deck rectangles.</summary>
    [DisallowMultipleComponent]
    public sealed class DioramaWalkableRegistry : MonoBehaviour
    {
        private readonly List<DioramaWalkableSurface> surfaces = new();
        private readonly Vector3[] footprintSamples = new Vector3[9];

        public IReadOnlyList<DioramaWalkableSurface> Surfaces => surfaces;
        public int ActiveSurfaceCount => surfaces.Count;

        public Bounds ActiveBounds
        {
            get
            {
                bool found = false;
                Bounds result = default;
                for (int i = surfaces.Count - 1; i >= 0; i--)
                {
                    DioramaWalkableSurface surface = surfaces[i];
                    if (surface == null || !surface.isActiveAndEnabled)
                    {
                        surfaces.RemoveAt(i);
                        continue;
                    }

                    if (!found)
                    {
                        result = surface.Bounds;
                        found = true;
                    }
                    else result.Encapsulate(surface.Bounds);
                }
                return result;
            }
        }

        internal void Register(DioramaWalkableSurface surface)
        {
            if (surface != null && !surfaces.Contains(surface)) surfaces.Add(surface);
        }

        internal void Unregister(DioramaWalkableSurface surface)
        {
            if (surface != null) surfaces.Remove(surface);
        }

        public bool TryGetGroundHeight(Vector3 worldPosition, out float height)
        {
            height = float.NegativeInfinity;
            bool found = false;
            for (int i = 0; i < surfaces.Count; i++)
            {
                DioramaWalkableSurface surface = surfaces[i];
                if (surface == null || !surface.isActiveAndEnabled || !surface.ContainsXZ(worldPosition))
                    continue;
                height = Mathf.Max(height, surface.TopY);
                found = true;
            }
            return found;
        }

        public bool ContainsFootprint(Vector3 center, float radius)
        {
            float diagonal = radius * 0.70710678f;
            footprintSamples[0] = center;
            footprintSamples[1] = center + new Vector3(radius, 0f, 0f);
            footprintSamples[2] = center + new Vector3(-radius, 0f, 0f);
            footprintSamples[3] = center + new Vector3(0f, 0f, radius);
            footprintSamples[4] = center + new Vector3(0f, 0f, -radius);
            footprintSamples[5] = center + new Vector3(diagonal, 0f, diagonal);
            footprintSamples[6] = center + new Vector3(diagonal, 0f, -diagonal);
            footprintSamples[7] = center + new Vector3(-diagonal, 0f, diagonal);
            footprintSamples[8] = center + new Vector3(-diagonal, 0f, -diagonal);

            for (int sample = 0; sample < footprintSamples.Length; sample++)
            {
                bool contained = false;
                for (int i = 0; i < surfaces.Count; i++)
                {
                    DioramaWalkableSurface surface = surfaces[i];
                    if (surface != null && surface.isActiveAndEnabled &&
                        surface.ContainsXZ(footprintSamples[sample]))
                    {
                        contained = true;
                        break;
                    }
                }
                if (!contained) return false;
            }
            return true;
        }
    }
}
