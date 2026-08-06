using UnityEngine;

namespace JoziRunner.AssetPipeline
{
    [DisallowMultipleComponent]
    public sealed class GeneratedAssetMetadata : MonoBehaviour
    {
        public string assetId;
        public string sourceAssetPath;
        public GeneratedAssetCategory category;
        public string intendedDistrict;
        public string spawnRole;
        public int triangleCount;
        public int materialCount;
        public long estimatedTextureMemoryBytes;
        public string approvedUtc;
        public bool spawnMetadataEnabled;
    }
}
