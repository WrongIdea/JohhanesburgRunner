using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Auto-configures the HD Meshy pigeon FBX on import so its seven clips come
    /// in ready to use — no menu step required. Sets a Generic rig, caps bone
    /// influences at 4 (mobile GPU skinning), ignores embedded materials (the
    /// builder makes the shared URP material), and canonicalises clip names to
    /// Idle/Walk/Peck/Hop/Takeoff/Glide/Landing with correct loop flags.
    /// </summary>
    public class PigeonModelPostprocessor : AssetPostprocessor
    {
        const string TargetPath = "Assets/Characters/PigeonHD/PigeonHD.fbx";
        static readonly string[] AllClips = { "Idle", "Walk", "Peck", "Hop", "Takeoff", "Glide", "Landing" };
        static readonly string[] LoopClips = { "Idle", "Walk", "Glide" };

        bool IsTarget => assetPath.Replace('\\', '/') == TargetPath;

        void OnPreprocessModel()
        {
            if (!IsTarget) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.maxBonesPerVertex = 4;
            importer.optimizeBones = true;
        }

        void OnPreprocessAnimation()
        {
            if (!IsTarget) return;
            var importer = (ModelImporter)assetImporter;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                string canonical = AllClips.FirstOrDefault(n => clip.name.Contains(n));
                if (canonical != null) clip.name = canonical;
                bool loop = canonical != null && LoopClips.Contains(canonical);
                clip.loopTime = loop;
                clip.loopPose = loop;
            }
            importer.clipAnimations = clips;
        }
    }
}
