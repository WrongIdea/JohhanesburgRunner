using System.Collections.Generic;
using System.Linq;
using JoburgRunner.Environment.Pigeons;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Imports the stylized, pre-rigged/animated pigeon FBX and assembles it into
    /// the pooled flock prefab, replacing the previous bird in place. The FBX ships
    /// 12 takes; this canonicalises the six the flock's <see cref="PigeonController"/>
    /// drives (Idle / Walk / Peck / TakeOff / Fly / Land) and builds a matching
    /// six-state Animator controller. One shared URP/Lit material (diffuse only),
    /// GPU instancing on, animator culling on.
    ///
    /// Writes the prefab to the <b>live</b> path so both scenes' PigeonSpawner
    /// reference resolves to the new bird with no scene edits. Run via the menu
    /// item, or headless: <c>-executeMethod JoburgRunner.Editor.PigeonStylizedBuilder.BuildAll</c>.
    /// </summary>
    public static class PigeonStylizedBuilder
    {
        const string Folder = "Assets/Characters/PigeonStylized";
        const string ModelPath = Folder + "/PigeonStylized.fbx";
        const string DiffusePath = Folder + "/Pigeon_Diffuse.png";
        const string MaterialPath = "Assets/Materials/PigeonStylized_URP.mat";
        const string ControllerPath = "Assets/Animations/PigeonStylizedAnimator.controller";

        // Overwrite the LIVE prefab in place: keeps its GUID so both scenes'
        // PigeonSpawner.pigeonPrefab auto-use the new bird (no scene surgery).
        public const string PigeonPrefabPath = "Assets/Prefabs/Pigeons/Pigeon.prefab";

        // Approved real-world standing height for the replacement pigeon.
        const float TargetHeightMetres = 0.30f;

        // FBX take suffix (the part after "PigeonALL_") -> the canonical Unity clip
        // name the flock plays. Only these six are wired; the rest (Left/Right/
        // Cooing/Circle/TPOSE and the short one-shot Idle) import under "Extra_*"
        // names, kept for future use but out of the flock's way.
        static readonly Dictionary<string, string> ClipMap = new Dictionary<string, string>
        {
            { "IdleLoop", "Idle" },
            { "Walk",     "Walk" },
            { "Peck",     "Peck" },
            { "TakeOff",  "TakeOff" },
            { "FlyLoop",  "Fly" },
            { "Land",     "Land" },
        };
        static readonly HashSet<string> LoopClips = new HashSet<string> { "Idle", "Walk", "Fly" };
        // Animator states the controller drives (state name == clip name), in order.
        static readonly string[] StateNames = { "Idle", "Walk", "Peck", "TakeOff", "Fly", "Land" };

        [MenuItem("Joburg Runner/Assets/Build Stylized Pigeon (replaces flock bird)")]
        public static void BuildAll()
        {
            ConfigureTexture();
            ConfigureModel();

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError("[PigeonStylized] FBX not imported at " + ModelPath);
                return;
            }

            Material material = CreateMaterial();
            AnimatorController controller = CreateController();
            BuildPrefab(model, material, controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PigeonStylized] Build complete -> " + PigeonPrefabPath);
        }

        // ------------------------------------------------------------- import
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
            importer.useFileScale = true;

            // Rename takes to canonical clip names + set loop / bake-in-place. The
            // controller moves the transform itself, so every clip plays in place.
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.clipAnimations;
            }
            foreach (ModelImporterClipAnimation clip in clips)
            {
                string suffix = Suffix(clip.name);
                string canonical = ClipMap.TryGetValue(suffix, out string mapped) ? mapped : "Extra_" + suffix;
                clip.name = canonical;
                bool loop = LoopClips.Contains(canonical);
                clip.loopTime = loop;
                clip.loopPose = loop;
                clip.lockRootRotation = true;    // Root Transform Rotation: Bake Into Pose
                clip.lockRootHeightY = true;      // Root Position Y: Bake Into Pose
                clip.lockRootPositionXZ = true;   // Root Position XZ: Bake Into Pose
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        /// <summary>Extract the take suffix, e.g. "Armature|PigeonALL_FlyLoop" -> "FlyLoop".</summary>
        static string Suffix(string takeName)
        {
            string s = takeName;
            int bar = s.LastIndexOf('|');
            if (bar >= 0)
            {
                s = s.Substring(bar + 1);
            }
            const string prefix = "PigeonALL_";
            int idx = s.IndexOf(prefix, System.StringComparison.Ordinal);
            if (idx >= 0)
            {
                s = s.Substring(idx + prefix.Length);
            }
            return s;
        }

        static void ConfigureTexture()
        {
            if (AssetImporter.GetAtPath(DiffusePath) is TextureImporter tex)
            {
                tex.textureType = TextureImporterType.Default;
                tex.sRGBTexture = true;
                tex.maxTextureSize = 512;   // many instances -> keep small
                tex.textureCompression = TextureImporterCompression.Compressed;
                tex.mipmapEnabled = true;
                tex.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------- material
        static Material CreateMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DiffusePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.1f);   // stylized/flat: avoid plastic sheen
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------- controller
        static AnimatorController CreateController()
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToArray();

            AnimatorState defaultState = null;
            foreach (string stateName in StateNames)
            {
                AnimationClip clip = clips.FirstOrDefault(c => c.name == stateName);
                if (clip == null)
                {
                    Debug.LogWarning("[PigeonStylized] no clip found for state '" + stateName + "'");
                }
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
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // ------------------------------------------------------------- prefab
        static void BuildPrefab(GameObject model, Material material, AnimatorController controller)
        {
            GameObject root = new GameObject("Pigeon");
            GameObject visual = Object.Instantiate(model, root.transform);
            visual.name = "PigeonModel";
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Drop any auto LODGroup the FBX naming created; we add our own.
            foreach (LODGroup stray in visual.GetComponentsInChildren<LODGroup>(true))
            {
                Object.DestroyImmediate(stray);
            }

            SkinnedMeshRenderer[] renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = material;
                }
                r.sharedMaterials = mats;
            }

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullCompletely;

            // Enforce the approved standing height even if the FBX import scale changes.
            float meshHeight = 0f;
            foreach (SkinnedMeshRenderer r in renderers)
            {
                if (r.sharedMesh != null)
                {
                    meshHeight = Mathf.Max(meshHeight, r.sharedMesh.bounds.size.y);
                }
            }
            Vector3 ls = visual.transform.localScale;
            meshHeight *= Mathf.Abs(ls.y);
            float scale = meshHeight > 1e-4f ? TargetHeightMetres / meshHeight : 1f;
            root.transform.localScale = Vector3.one * scale;
            Debug.Log($"[PigeonStylized] mesh height={meshHeight:F3} m -> 0.300 m, root scale={scale:F3}");

            // Single-LOD group purely for the far-cull distance (626-tri bird needs
            // no decimation LODs). Screen-relative, so it culls correctly at any scale.
            LODGroup lodGroup = root.AddComponent<LODGroup>();
            if (renderers.Length > 0)
            {
                lodGroup.SetLODs(new[] { new LOD(0.01f, renderers.Cast<Renderer>().ToArray()) });
                lodGroup.RecalculateBounds();
            }

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;   // never blocks the runner
            capsule.center = new Vector3(0f, 0.15f, 0f);
            capsule.radius = 0.12f;
            capsule.height = 0.32f;

            AudioSource audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.minDistance = 3f;
            audio.maxDistance = 40f;

            root.AddComponent<PigeonController>();

            EnsureFolder("Assets/Prefabs", "Pigeons");
            PrefabUtility.SaveAsPrefabAsset(root, PigeonPrefabPath);
            Object.DestroyImmediate(root);
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
