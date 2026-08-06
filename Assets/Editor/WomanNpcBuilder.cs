using System;
using System.IO;
using System.Linq;
using JoburgRunner.Environment.Decor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JoburgRunner.Editor
{
    public static class WomanNpcBuilder
    {
        const string Folder = "Assets/Characters/WomanNPC";
        const string WaveModelPath = Folder + "/Meshy_AI_Vibrant_Heritage_biped_Animation_Big_Wave_Hello_withSkin.fbx";
        const string BaseColorPath = Folder + "/Meshy_AI_Vibrant_Heritage_biped_texture_0.png";
        const string NormalPath = Folder + "/Meshy_AI_Vibrant_Heritage_biped_texture_0_normal.png";
        const string MaterialPath = "Assets/Materials/WomanNPC_URP.mat";
        const string ControllerPath = "Assets/Animations/WomanNPCAnimator.controller";
        public const string PrefabPath = "Assets/Prefabs/Decor/DecorRoadsideWoman.prefab";

        [MenuItem("Jozi Runner/Assets/Create Roadside Woman")]
        public static GameObject CreatePrefab()
        {
            ConfigureTexture(BaseColorPath, false);
            ConfigureTexture(NormalPath, true);
            ConfigureModel(WaveModelPath, true);

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(WaveModelPath);
            AnimationClip waving = FindClip(WaveModelPath, "Wave");
            if (model == null || waving == null)
            {
                Debug.LogError("Roadside woman assets have not imported correctly.");
                return null;
            }

            Material material = CreateMaterial();
            AnimatorController controller = CreateController(waving);

            GameObject root = new GameObject("DecorRoadsideWoman");
            GameObject visual = UnityEngine.Object.Instantiate(model, root.transform);
            visual.name = "WomanVisual";
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

        static void ConfigureModel(string path, bool loop)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
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
                clip.loopTime = loop;
                clip.loopPose = loop;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        static void ConfigureTexture(string path, bool normal)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                return;
            }
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = true;
            importer.compressionQuality = 55;
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.SaveAndReimport();
        }

        static Material CreateMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
            material.EnableKeyword("_NORMALMAP");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        static AnimatorController CreateController(AnimationClip waving)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState waveState = stateMachine.AddState("BigWave");
            waveState.motion = waving;
            stateMachine.defaultState = waveState;
            return controller;
        }

        static AnimationClip FindClip(string path, string namePart) =>
            AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip =>
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                    clip.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
