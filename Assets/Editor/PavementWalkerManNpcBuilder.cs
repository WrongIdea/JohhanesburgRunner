using System;
using System.Linq;
using JoburgRunner.Environment.Decor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Builds the pooled "walking man" pedestrian prefab (the male counterpart to
    /// <see cref="PavementWalkerNpcBuilder"/>). Same setup — pooled, off-lane, no
    /// collider, PavementWalker mover — but characterId = 1 so the one-of-each
    /// in-view rule tracks men and women independently. Grounding is measured from
    /// the model's bounds (this rig's foot-to-pivot offset differs from the woman).
    /// </summary>
    public static class PavementWalkerManNpcBuilder
    {
        const string Folder = "Assets/Characters/PavementWalkerMan";
        const string ModelPath = Folder + "/Meshy_AI_Low_poly_Johannesburg_biped_Animation_Casual_Walk_withSkin.fbx";
        const string BaseColorPath = Folder + "/Meshy_AI_Low_poly_Johannesburg_biped_texture_0.png";
        const string NormalPath = Folder + "/Meshy_AI_Low_poly_Johannesburg_biped_texture_0_normal.png";
        const string MaterialPath = "Assets/Materials/PavementWalkerMan_URP.mat";
        const string ControllerPath = "Assets/Animations/PavementWalkerManAnimator.controller";
        public const string PrefabPath = "Assets/Prefabs/Decor/DecorPavementWalkerMan.prefab";

        [MenuItem("Jozi Runner/Assets/Create Pavement Walker Man")]
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
                Debug.LogError("Pavement walker man assets have not imported correctly.");
                return null;
            }

            Material material = CreateMaterial();
            AnimatorController controller = CreateController(walking);

            GameObject root = new GameObject("DecorPavementWalkerMan");
            GameObject visual = UnityEngine.Object.Instantiate(model, root.transform);
            visual.name = "WalkerVisual";
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;

            // Ground the feet at the prefab root by measuring the rig's lowest point.
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    b.Encapsulate(renderers[i].bounds);
                }
                visual.transform.localPosition = new Vector3(0f, -b.min.y, 0f);
                Debug.Log($"WALKERMAN-DEBUG footOffset={-b.min.y:0.###} height={b.size.y:0.###}");
            }

            foreach (Renderer renderer in renderers)
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
            serialized.FindProperty("characterId").intValue = 1; // 1 = man
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
