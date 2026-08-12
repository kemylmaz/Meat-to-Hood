using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Import-time information carried by authored CozyPack prefabs. Runtime
    /// attachment code uses it without making gameplay objects depend on FBX
    /// hierarchy details.
    /// </summary>
    public sealed class CozyVisualMetadata : MonoBehaviour
    {
        [SerializeField] private string sourceAssetId;
        [SerializeField] private Vector3 runtimeEulerOffset;
        [SerializeField] private bool hasAuthoredFace;

        public string SourceAssetId => sourceAssetId;
        public Vector3 RuntimeEulerOffset => runtimeEulerOffset;
        public bool HasAuthoredFace => hasAuthoredFace;

        public void Configure(string assetId, Vector3 eulerOffset, bool authoredFace)
        {
            sourceAssetId = assetId;
            runtimeEulerOffset = eulerOffset;
            hasAuthoredFace = authoredFace;
        }
    }
}
