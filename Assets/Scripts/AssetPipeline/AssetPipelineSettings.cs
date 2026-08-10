using UnityEngine;

namespace JoziRunner.AssetPipeline
{
    [CreateAssetMenu(menuName = "Jozi Runner/Asset Pipeline Settings", fileName = "AssetPipelineSettings")]
    public sealed class AssetPipelineSettings : ScriptableObject
    {
        public int heroBuildingMaxTriangles = 25000;
        public int standardBuildingMaxTriangles = 12000;
        public int vehicleMaxTriangles = 15000;
        public int obstacleMaxTriangles = 8000;
        public int smallPropMaxTriangles = 3000;
        public int defaultTextureResolution = 1024;
        public int ordinaryPropMaxMaterials = 2;
        public int vehicleMaxMaterials = 4;
        public float minimumReasonableDimension = 0.05f;
        public float maximumReasonableDimension = 200f;

        public int MaximumTriangles(GeneratedAssetCategory category)
        {
            switch (category)
            {
                case GeneratedAssetCategory.HeroBuilding: return heroBuildingMaxTriangles;
                case GeneratedAssetCategory.StandardBuilding: return standardBuildingMaxTriangles;
                case GeneratedAssetCategory.Vehicle: return vehicleMaxTriangles;
                case GeneratedAssetCategory.Obstacle: return obstacleMaxTriangles;
                default: return smallPropMaxTriangles;
            }
        }
    }
}
