using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JoburgRunner.Editor
{
    public static class GentlemanWaveNpcBuilder
    {
        const string Folder = "Assets/Characters/GentlemanNPC";
        const string ModelPath = Folder + "/Meshy_AI_Stylised_adult_male_p_biped_Animation_Big_Wave_Hello_withSkin.fbx";
        const string MaterialPath = "Assets/Materials/GentlemanNPC_URP.mat";
        const string ControllerPath = "Assets/Animations/GentlemanWaveNPCAnimator.controller";
        public const string PrefabPath = "Assets/Prefabs/Decor/DecorWavingGentleman.prefab";

        public static GameObject CreatePrefab()
        {
            ConfigureModel();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            AnimationClip wave = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip =>
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                    clip.name.IndexOf("wave", StringComparison.OrdinalIgnoreCase) >= 0);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (model == null || wave == null || material == null)
            {
                Debug.LogError("Waving gentleman assets have not imported correctly.");
                return null;
            }

            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorState state = controller.layers[0].stateMachine.AddState("BigWave");
            state.motion = wave;
            controller.layers[0].stateMachine.defaultState = state;

            GameObject root = new GameObject("DecorWavingGentleman");
            GameObject visual = UnityEngine.Object.Instantiate(model, root.transform);
            visual.name = "WavingGentlemanVisual";
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        static void ConfigureModel()
        {
            if (!(AssetImporter.GetAtPath(ModelPath) is ModelImporter importer))
            {
                return;
            }
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.loopTime = true;
                clip.loopPose = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }
}
