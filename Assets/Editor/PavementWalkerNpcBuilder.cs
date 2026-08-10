using System;
using System.Linq;
using JoburgRunner.Environment.Decor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Builds the pooled "walking woman" pedestrian prefab: a Meshy biped with a
    /// looping walk clip and a <see cref="PavementWalker"/> mover. Placed on the
    /// pavement and yawed to travel opposite the player, so it strolls past them.
    /// Purely visual (off-lane, no collider), mirroring
    /// <see cref="GentlemanNpcBuilder"/>'s import/material/animator setup.
    /// </summary>
    public static class PavementWalkerNpcBuilder
    {
        const string Folder = "Assets/Characters/Meshy_AI_Low_poly_Johannesburg_biped";
        const string ModelPath = Folder + "/Meshy_AI_Low_poly_Johannesburg_biped_Animation_Walking_Woman_withSkin.fbx";
        const string BaseColorPath = Folder + "/Meshy_AI_Low_poly_Johannesburg_biped_texture_0.png";
        const string NormalPath = Folder + "/Meshy_AI_Low_poly_Johannesburg_biped_texture_0_normal.png";
        const string MaterialPath = "Assets/Materials/PavementWalkerWoman_URP.mat";
        const string ControllerPath = "Assets/Animations/PavementWalkerWomanAnimator.controller";
        public const string PrefabPath = "Assets/Prefabs/Decor/DecorPavementWalkerWoman.prefab";

        [MenuItem("Jozi Runner/Assets/Create Pavement Walker Woman")]
        public static GameObject CreatePrefab()
        {
            ConfigureTexture(BaseColorPath, false);
            ConfigureTexture(NormalPath, true);
            ConfigureModel();

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            AnimationClip walking = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip =>
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                    clip.name.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0);
            if (model == null || walking == null)
            {
                Debug.LogError("Pavement walker woman assets have not imported correctly.");
                return null;
            }

            Material material = CreateMaterial();
            AnimatorController controller = CreateController(walking);

            GameObject root = new GameObject("DecorPavementWalkerWoman");
            GameObject visual = UnityEngine.Object.Instantiate(model, root.transform);
            visual.name = "WalkerVisual";
            // This rig's pivot sits ~0.245 m above the soles, so lift the visual by
            // that much to put her feet at the prefab root. Sockets can then be
            // placed at pavement height like every other prop. PavementWalker moves
            // the visual horizontally, preserving this Y offset.
            visual.transform.SetLocalPositionAndRotation(
                new Vector3(0f, 0.245f, 0f), Quaternion.identity);
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

            PavementWalker walker = root.AddComponent<PavementWalker>();
            SerializedObject serialized = new SerializedObject(walker);
            serialized.FindProperty("actor").objectReferenceValue = visual.transform;
            serialized.FindProperty("characterId").intValue = 0; // 0 = woman
            serialized.ApplyModifiedPropertiesWithoutUndo();

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

        static AnimatorController CreateController(AnimationClip walking)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorState state = controller.layers[0].stateMachine.AddState("Walking");
            state.motion = walking;
            controller.layers[0].stateMachine.defaultState = state;
            return controller;
        }
    }
}
