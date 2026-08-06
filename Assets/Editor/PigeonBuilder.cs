using System.Collections.Generic;
using System.Linq;
using JoburgRunner.Environment.Pigeons;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Imports the Blender pigeon FBX + atlas and assembles the pooled
    /// <c>Pigeon.prefab</c> (Animator + 3-level LODGroup + trigger CapsuleCollider
    /// + PigeonController + AudioSource) and the empty <c>PigeonFlock.prefab</c>.
    /// One shared URP material / atlas, GPU instancing on, animator culling on.
    /// </summary>
    public static class PigeonBuilder
    {
        // HD Meshy pigeon (rigged/animated in Blender 5.2). Replaces the old
        // scratch-authored Pigeon.fbx + flat atlas.
        const string Folder = "Assets/Characters/PigeonHD";
        const string ModelPath = Folder + "/PigeonHD.fbx";
        const string AlbedoPath = Folder + "/Pigeon_Albedo.png";
        const string NormalPath = Folder + "/Pigeon_Normal.png";
        const string MaterialPath = "Assets/Materials/Pigeon_URP.mat";
        const string ControllerPath = "Assets/Animations/PigeonAnimator.controller";
        public const string PigeonPrefabPath = "Assets/Prefabs/Pigeons/Pigeon.prefab";
        public const string FlockPrefabPath = "Assets/Prefabs/Pigeons/PigeonFlock.prefab";

        // FBX clip names (authored in Blender). Canonicalised on import.
        static readonly string[] AllClips = { "Idle", "Walk", "Peck", "Hop", "Takeoff", "Glide", "Landing" };
        // Clips that should loop (endpoints authored to match).
        static readonly string[] LoopClips = { "Idle", "Walk", "Glide" };
        // Animator state name -> clip name. PigeonController plays the state
        // names on the left (Idle/Walk/Peck/TakeOff/Fly/Land); we route them to
        // the new clips so the controller code needs no changes.
        static readonly (string state, string clip)[] StateMap =
        {
            ("Idle", "Idle"), ("Walk", "Walk"), ("Peck", "Peck"),
            ("TakeOff", "Takeoff"), ("Fly", "Glide"), ("Land", "Landing"),
            ("Hop", "Hop"),
        };

        [MenuItem("Joburg Runner/Assets/Build Pigeon Prefabs")]
        public static void BuildAll()
        {
            BuildPigeonPrefab();
            BuildFlockPrefab();
            AssetDatabase.SaveAssets();
        }

        public static GameObject BuildPigeonPrefab()
        {
            ConfigureTextures();
            ConfigureModel();

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError("Pigeon FBX has not imported; run the Blender build first.");
                return null;
            }

            Material material = CreateMaterial();
            AnimatorController controller = CreateController();

            GameObject root = new GameObject("Pigeon");
            GameObject visual = Object.Instantiate(model, root.transform);
            visual.name = "PigeonModel";
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;

            // The FBX auto-generates a LODGroup from the Pigeon_LOD0/1/2 mesh
            // naming convention. Remove it so it doesn't conflict with the tuned
            // root LODGroup added below (double-registration spams warnings and
            // makes the pigeon renderers cull/flicker unpredictably).
            foreach (LODGroup stray in visual.GetComponentsInChildren<LODGroup>(true))
            {
                Object.DestroyImmediate(stray);
            }

            // Shared material on every LOD renderer.
            var renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = material;
                }
                r.sharedMaterials = mats;
            }

            // Animator on the model root.
            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullCompletely;

            // LODGroup (cull far — screen-relative thresholds approximate ~40 m).
            LODGroup lodGroup = root.AddComponent<LODGroup>();
            SkinnedMeshRenderer lod0 = FindLod(renderers, "LOD0");
            SkinnedMeshRenderer lod1 = FindLod(renderers, "LOD1");
            SkinnedMeshRenderer lod2 = FindLod(renderers, "LOD2");
            // Screen-relative transition heights are tiny because the pigeon is only
            // ~0.3 m tall: at 40 m it fills well under 1% of the viewport, so these
            // must be small or it culls a few metres away (which is why it was
            // invisible). Tuned to stay visible out to ~40 m, then cull.
            var lods = new List<LOD>();
            if (lod0 != null) lods.Add(new LOD(0.03f, new Renderer[] { lod0 }));
            if (lod1 != null) lods.Add(new LOD(0.012f, new Renderer[] { lod1 }));
            if (lod2 != null) lods.Add(new LOD(0.004f, new Renderer[] { lod2 }));
            if (lods.Count > 0)
            {
                lodGroup.SetLODs(lods.ToArray());
                lodGroup.RecalculateBounds();
            }

            // Small trigger capsule (never blocks the runner).
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.center = new Vector3(0f, 0.15f, 0f);
            capsule.radius = 0.12f;
            capsule.height = 0.32f;

            AudioSource audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.minDistance = 3f;
            audio.maxDistance = 40f;

            root.AddComponent<PigeonController>();

            // Real pigeons are ~0.3 m, which reads too small/subtle in motion at
            // running speed on a phone. Scale up slightly (~0.5 m) so they register
            // without looking cartoonishly large. LOD thresholds are screen-relative
            // so they still cull correctly at the larger size.
            root.transform.localScale = Vector3.one * 1.7f;

            EnsureFolder("Assets/Prefabs", "Pigeons");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PigeonPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static GameObject BuildFlockPrefab()
        {
            EnsureFolder("Assets/Prefabs", "Pigeons");
            GameObject root = new GameObject("PigeonFlock");
            root.AddComponent<PigeonFlock>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, FlockPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static SkinnedMeshRenderer FindLod(SkinnedMeshRenderer[] renderers, string key)
        {
            foreach (SkinnedMeshRenderer r in renderers)
            {
                if (r.name.Contains(key) || (r.sharedMesh != null && r.sharedMesh.name.Contains(key)))
                {
                    return r;
                }
            }
            return null;
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
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;

            // Canonicalise clip names (strip any "Armature|" prefix the FBX take
            // carries) and set loop flags. Result: Idle/Walk/Peck/Hop/Takeoff/
            // Glide/Landing exactly.
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                string canonical = AllClips.FirstOrDefault(n => clip.name.Contains(n));
                if (canonical != null)
                {
                    clip.name = canonical;
                }
                bool loop = canonical != null && LoopClips.Contains(canonical);
                clip.loopTime = loop;
                clip.loopPose = loop;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        static void ConfigureTextures()
        {
            if (AssetImporter.GetAtPath(AlbedoPath) is TextureImporter albedo)
            {
                albedo.textureType = TextureImporterType.Default;
                albedo.sRGBTexture = true;
                albedo.filterMode = FilterMode.Bilinear;
                albedo.maxTextureSize = 512; // hundreds of instances -> keep small
                albedo.textureCompression = TextureImporterCompression.Compressed;
                albedo.SaveAndReimport();
            }
            if (AssetImporter.GetAtPath(NormalPath) is TextureImporter normal)
            {
                normal.textureType = TextureImporterType.NormalMap;
                normal.maxTextureSize = 512;
                normal.textureCompression = TextureImporterCompression.Compressed;
                normal.SaveAndReimport();
            }
        }

        static Material CreateMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath));
            Texture2D nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
            if (nrm != null)
            {
                material.SetTexture("_BumpMap", nrm);
                material.EnableKeyword("_NORMALMAP");
            }
            material.SetFloat("_Smoothness", 0.25f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        static AnimatorController CreateController()
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var sm = controller.layers[0].stateMachine;

            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToArray();

            AnimatorState defaultState = null;
            foreach ((string stateName, string clipName) in StateMap)
            {
                AnimationClip clip = clips.FirstOrDefault(c => c.name == clipName)
                                     ?? clips.FirstOrDefault(c => c.name.Contains(clipName));
                AnimatorState state = sm.AddState(stateName);
                state.motion = clip;
                if (stateName == "Idle")
                {
                    defaultState = state;
                }
            }
            if (defaultState != null)
            {
                sm.defaultState = defaultState;
            }
            return controller;
        }

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
