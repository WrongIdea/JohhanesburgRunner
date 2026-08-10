using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoziRunner.AssetPipeline
{
    public enum GeneratedAssetCategory { HeroBuilding, StandardBuilding, Vehicle, Obstacle, SmallProp }
    public enum GeneratedColliderType { None, Box, Capsule, Sphere, Mesh }
    public enum AssetRequestStatus { Pending, Generated, InReview, Approved, Rejected, Completed }
    public enum AssetApprovalState { Unreviewed, Approved, Rejected }
    public enum GenerationTaskState { AwaitingApproval, Queued, Processing, Succeeded, Failed, Cancelled }

    [Serializable]
    public sealed class AssetDimensions
    {
        public float width = 2f;
        public float height = 2f;
        public float depth = 4f;
    }

    [Serializable]
    public sealed class GeneratedAssetRequest
    {
        public string assetId;
        public string assetName;
        public GeneratedAssetCategory assetCategory;
        [TextArea] public string description;
        public string intendedDistrict;
        public AssetDimensions targetDimensionsMetres = new AssetDimensions();
        public int targetTriangleCount;
        public int maximumTriangleCount;
        public int textureResolution = 1024;
        public List<string> requiredMaterials = new List<string>();
        public GeneratedColliderType colliderType = GeneratedColliderType.Box;
        public bool movingPartsRequired;
        public bool lodsRequired;
        public string spawnRole;
        [TextArea] public string gameplayCameraVisibilityRequirements;
        public List<string> referenceImagePaths = new List<string>();
        [TextArea] public string meshyGenerationPrompt;
        public AssetRequestStatus status = AssetRequestStatus.Pending;
        public AssetApprovalState approvalState = AssetApprovalState.Unreviewed;
        [TextArea] public string reviewNotes;
    }

    [Serializable]
    public sealed class AssetValidationWarning
    {
        public string code;
        public string message;
    }

    [Serializable]
    public sealed class AssetValidationReport
    {
        public string sourcePath;
        public string generatedUtc;
        public List<string> meshNames = new List<string>();
        public int vertexCount;
        public int triangleCount;
        public int materialCount;
        public List<string> materialNames = new List<string>();
        public int textureCount;
        public List<string> textureDimensions = new List<string>();
        public long estimatedTextureMemoryBytes;
        public AssetDimensions boundsMetres = new AssetDimensions();
        public int rootObjectCount;
        public List<AssetValidationWarning> warnings = new List<AssetValidationWarning>();
    }

    [Serializable]
    public sealed class GenerationTaskInfo
    {
        public string taskId;
        public GenerationTaskState state;
        public float progress;
        public string message;
        public string resultPath;
    }
}
