using System.Collections.Generic;
using System.Linq;
using JoburgRunner.Characters.Pigeon;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// One-shot integrator for the v2 Meshy pigeon: configures the FBX import
    /// (Generic rig + per-clip loop/root settings), makes the single URP material,
    /// builds PigeonAnimator.controller, assembles the pooled Pigeon.prefab
    /// (Animator + PigeonController + LODGroup + collider + audio), and writes the
    /// PigeonAnimationTest scene. Run headless via BuildAll. Nothing here touches
    /// the shipping bird in Assets/Characters/PigeonHD.
    /// </summary>
    public static class PigeonV2Builder
    {
        const string Dir = "Assets/Characters/Pigeon";
        const string FbxPath = Dir + "/Models/Pigeon_Unity.fbx";
        const string AlbedoPath = Dir + "/Textures/Pigeon_Albedo.png";
        const string MatPath = Dir + "/Materials/Pigeon.mat";
        const string ControllerPath = Dir + "/Controllers/PigeonAnimator.controller";
        const string PrefabPath = Dir + "/Prefabs/Pigeon.prefab";
        const string ScenePath = Dir + "/Scenes/PigeonAnimationTest.unity";

        static readonly string[] AllClips =
            { "Idle","Walk","Peck","Alert","Takeoff","Fly","Glide","BankLeft","BankRight","Landing","ShortHop","FrightenedFlutter" };
        static readonly HashSet<string> LoopClips =
            new HashSet<string> { "Idle","Walk","Peck","Alert","Fly","Glide","BankLeft","BankRight" };

        [MenuItem("Joburg Runner/Pigeon v2/Build All")]
        public static void BuildAll()
        {
            ConfigureImport();
            ConfigureAlbedo();
            Material mat = BuildMaterial();
            AnimatorController controller = BuildController();
            BuildPrefab(mat, controller);
            BuildTestScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PigeonV2] Build All complete.");
        }

        // ---------------------------------------------------------------- import
        [MenuItem("Joburg Runner/Pigeon v2/1. Configure Import")]
        public static void ConfigureImport()
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null) { Debug.LogError("[PigeonV2] FBX not found at " + FbxPath); return; }

            // --- Model tab ---
            importer.globalScale = 1f;
            importer.useFileScale = true;              // Convert Units (Blender m -> Unity m)
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.addCollider = false;
            importer.keepQuads = false;
            importer.indexFormat = ModelImporterIndexFormat.Auto;
            importer.weldVertices = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None; // we assign our own

            // --- Rig tab ---
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;      // keep bone hierarchy (Rig child)
            importer.maxBonesPerVertex = 4;
            importer.optimizeBones = true;

            // --- Animation clips: clean names + loop + bake-into-pose root ---
            importer.importAnimation = true;
            ModelImporterClipAnimation[] takes = importer.defaultClipAnimations;
            var configured = new List<ModelImporterClipAnimation>();
            foreach (ModelImporterClipAnimation take in takes)
            {
                string clean = CleanName(take.name);
                if (clean == null) continue;
                take.name = clean;
                bool loop = LoopClips.Contains(clean);
                take.loopTime = loop;
                take.loopPose = loop;                  // cyclic clips loop pose too
                take.cycleOffset = 0f;
                // root motion baked into pose -> plays in place, no drift
                take.lockRootRotation = true;          // Root Transform Rotation: Bake Into Pose
                take.lockRootHeightY = true;           // Root Position Y: Bake Into Pose
                take.lockRootPositionXZ = true;        // Root Position XZ: Bake Into Pose
                take.keepOriginalOrientation = true;   // Based Upon: Original
                take.keepOriginalPositionY = true;
                take.keepOriginalPositionXZ = true;
                configured.Add(take);
            }
            importer.clipAnimations = configured.ToArray();
            importer.SaveAndReimport();
            Debug.Log($"[PigeonV2] Import configured. Clips: {configured.Count}");
        }

        static string CleanName(string takeName)
        {
            // take names come in as "PigeonArmature|Idle"; match the suffix to a clip
            string last = takeName.Contains('|') ? takeName.Substring(takeName.LastIndexOf('|') + 1) : takeName;
            foreach (string c in AllClips) if (last == c) return c;
            foreach (string c in AllClips) if (last.EndsWith(c)) return c;
            return null;
        }

        static void ConfigureAlbedo()
        {
            var ti = AssetImporter.GetAtPath(AlbedoPath) as TextureImporter;
            if (ti == null) return;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.maxTextureSize = 512;                    // single 512 atlas
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
        }

        // ---------------------------------------------------------------- material
        static Material BuildMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, MatPath);
            }
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
            mat.SetTexture("_BaseMap", albedo);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.15f);         // avoid shiny plastic look
            mat.SetFloat("_Surface", 0f);               // Opaque
            mat.enableInstancing = true;                // GPU instancing
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------------------------------------------------------------- controller
        static AnimatorController BuildController()
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("IsAlert", AnimatorControllerParameterType.Bool);
            ac.AddParameter("TakeOff", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Land", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Peck", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("ShortHop", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Frightened", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("FlightMode", AnimatorControllerParameterType.Int);
            ac.AddParameter("GroundState", AnimatorControllerParameterType.Int);

            var sm = ac.layers[0].stateMachine;
            var clips = LoadClips();
            var st = new Dictionary<string, AnimatorState>();
            Vector3 p = new Vector3(300, -80, 0);
            int i = 0;
            foreach (string name in AllClips)
            {
                var s = sm.AddState(name, p + new Vector3((i % 3) * 260, (i / 3) * 90, 0));
                s.motion = clips.TryGetValue(name, out var c) ? c : null;
                s.writeDefaultValues = false;
                st[name] = s; i++;
            }
            sm.defaultState = st["Idle"];

            // ---- ground graph (GroundState int, IsAlert bool) ----
            IntTrans(st["Idle"], st["Walk"], "GroundState", 1);
            IntTrans(st["Walk"], st["Idle"], "GroundState", 0);
            IntTrans(st["Idle"], st["Peck"], "GroundState", 2);
            IntTrans(st["Peck"], st["Idle"], "GroundState", 0);
            IntTrans(st["Walk"], st["Peck"], "GroundState", 2);
            IntTrans(st["Peck"], st["Walk"], "GroundState", 1);
            foreach (string g in new[] { "Idle", "Walk", "Peck" })
                BoolTrans(st[g], st["Alert"], "IsAlert", true);
            BoolTrans(st["Alert"], st["Idle"], "IsAlert", false);

            // ---- takeoff / flight ----
            AnyTrigger(sm, st["Takeoff"], "TakeOff");
            ExitTrans(st["Takeoff"], st["Fly"], 0.82f);
            IntTrans(st["Fly"], st["Glide"], "FlightMode", 1);
            IntTrans(st["Glide"], st["Fly"], "FlightMode", 0);
            foreach (string f in new[] { "Fly", "Glide" })
            {
                IntTrans(st[f], st["BankLeft"], "FlightMode", 2);
                IntTrans(st[f], st["BankRight"], "FlightMode", 3);
            }
            IntTrans(st["BankLeft"], st["Fly"], "FlightMode", 0);
            IntTrans(st["BankRight"], st["Fly"], "FlightMode", 0);
            // let banks answer the other bank directly too (no dead ends)
            IntTrans(st["BankLeft"], st["BankRight"], "FlightMode", 3);
            IntTrans(st["BankRight"], st["BankLeft"], "FlightMode", 2);

            // ---- reactions ----
            AnyTrigger(sm, st["FrightenedFlutter"], "Frightened");
            ExitTrans(st["FrightenedFlutter"], st["Fly"], 0.88f);
            AnyTrigger(sm, st["ShortHop"], "ShortHop");
            ExitTrans(st["ShortHop"], st["Idle"], 0.85f);

            // ---- landing ----
            foreach (string f in new[] { "Fly", "Glide", "BankLeft", "BankRight" })
                TriggerTrans(st[f], st["Landing"], "Land");
            ExitTrans(st["Landing"], st["Idle"], 0.88f);

            EditorUtility.SetDirty(ac);
            Debug.Log("[PigeonV2] Animator controller built.");
            return ac;
        }

        static Dictionary<string, AnimationClip> LoadClips()
        {
            var dict = new Dictionary<string, AnimationClip>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(FbxPath))
            {
                if (o is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    string clean = CleanName(clip.name);
                    if (clean != null && !dict.ContainsKey(clean)) dict[clean] = clip;
                }
            }
            return dict;
        }

        static void SetDur(AnimatorStateTransition t, float dur)
        {
            t.hasFixedDuration = true; t.duration = dur; t.canTransitionToSelf = false;
        }
        static void IntTrans(AnimatorState a, AnimatorState b, string p, int v)
        {
            var t = a.AddTransition(b); t.hasExitTime = false; SetDur(t, 0.12f);
            t.AddCondition(AnimatorConditionMode.Equals, v, p);
        }
        static void BoolTrans(AnimatorState a, AnimatorState b, string p, bool v)
        {
            var t = a.AddTransition(b); t.hasExitTime = false; SetDur(t, 0.10f);
            t.AddCondition(v ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, p);
        }
        static void TriggerTrans(AnimatorState a, AnimatorState b, string p)
        {
            var t = a.AddTransition(b); t.hasExitTime = false; SetDur(t, 0.10f);
            t.AddCondition(AnimatorConditionMode.If, 0, p);
        }
        static void ExitTrans(AnimatorState a, AnimatorState b, float exitTime)
        {
            var t = a.AddTransition(b); t.hasExitTime = true; t.exitTime = exitTime;
            SetDur(t, 0.12f);
        }
        static void AnyTrigger(AnimatorStateMachine sm, AnimatorState to, string p)
        {
            var t = sm.AddAnyStateTransition(to); t.hasExitTime = false;
            t.hasFixedDuration = true; t.duration = 0.08f; t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0, p);
        }

        // ---------------------------------------------------------------- prefab
        static void BuildPrefab(Material mat, AnimatorController controller)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (model == null) { Debug.LogError("[PigeonV2] model missing"); return; }
            GameObject root = Object.Instantiate(model);
            root.name = "Pigeon";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            // remove any auto LODGroup the "_LOD#" naming created; we add a tuned one
            foreach (var stray in root.GetComponentsInChildren<LODGroup>(true))
                Object.DestroyImmediate(stray);

            // assign the single material to every LOD renderer
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }

            // Animator (from the generic import) on the root
            var animator = root.GetComponent<Animator>();
            if (animator == null) animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // LODGroup (screen-relative; ~13/26 m switches, cull ~45 m for a 0.3 m bird)
            var lodGroup = root.AddComponent<LODGroup>();
            var lod0 = FindLod(renderers, "LOD0");
            var lod1 = FindLod(renderers, "LOD1");
            var lod2 = FindLod(renderers, "LOD2");
            var lods = new List<LOD>();
            if (lod0 != null) lods.Add(new LOD(0.020f, new Renderer[] { lod0 }));
            if (lod1 != null) lods.Add(new LOD(0.010f, new Renderer[] { lod1 }));
            if (lod2 != null) lods.Add(new LOD(0.006f, new Renderer[] { lod2 }));
            lodGroup.SetLODs(lods.ToArray());
            lodGroup.RecalculateBounds();

            // body collider (simple capsule around the torso; trigger so it never blocks)
            var cap = root.AddComponent<CapsuleCollider>();
            cap.isTrigger = true;
            cap.direction = 2;                 // along Z (bird's forward/length in Unity)
            cap.center = new Vector3(0f, 0.12f, 0f);
            cap.radius = 0.06f;
            cap.height = 0.22f;

            // audio
            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.dopplerLevel = 0.05f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.minDistance = 2f;
            audio.maxDistance = 22f;

            // ground check empty at the feet
            var ground = new GameObject("GroundCheck");
            ground.transform.SetParent(root.transform);
            ground.transform.localPosition = Vector3.zero;

            // controller + wiring (private serialized fields via SerializedObject)
            var pc = root.AddComponent<PigeonController>();
            var so = new SerializedObject(pc);
            so.FindProperty("animator").objectReferenceValue = animator;
            so.FindProperty("groundCheck").objectReferenceValue = ground.transform;
            so.FindProperty("audioSource").objectReferenceValue = audio;
            so.FindProperty("bodyCollider").objectReferenceValue = cap;
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[PigeonV2] Prefab built at " + PrefabPath);
        }

        static SkinnedMeshRenderer FindLod(SkinnedMeshRenderer[] rs, string key)
        {
            foreach (var r in rs)
                if (r.name.Contains(key) || (r.sharedMesh != null && r.sharedMesh.name.Contains(key)))
                    return r;
            return null;
        }

        // ---------------------------------------------------------------- test scene
        [MenuItem("Joburg Runner/Pigeon v2/Build Test Scene")]
        public static void BuildTestSceneMenu() => BuildTestScene();

        static void BuildTestScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) { Debug.LogError("[PigeonV2] prefab missing for scene"); return; }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ground plane
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground"; plane.transform.localScale = new Vector3(3, 1, 3);

            // light + camera
            var lgo = new GameObject("Sun"); var light = lgo.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.1f;
            lgo.transform.rotation = Quaternion.Euler(50, -30, 0);
            var cgo = new GameObject("Main Camera"); var cam = cgo.AddComponent<Camera>();
            cgo.tag = "MainCamera"; cgo.transform.position = new Vector3(0, 1.2f, -2.6f);
            cgo.transform.rotation = Quaternion.Euler(18, 0, 0);

            PigeonController Spawn(string name, Vector3 pos)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = name; go.transform.position = pos;
                return go.GetComponent<PigeonController>();
            }

            var ground = Spawn("Pigeon_Ground", new Vector3(-1.5f, 0, 0));
            var flyer = Spawn("Pigeon_Flyer", new Vector3(-0.6f, 0, 0.4f));
            var glider = Spawn("Pigeon_Glider", new Vector3(0.3f, 0, 0.8f));
            var banker = Spawn("Pigeon_Banker", new Vector3(1.2f, 0, 0.4f));
            var lander = Spawn("Pigeon_Lander", new Vector3(2.0f, 0, 0f));
            var flock = new PigeonController[4];
            for (int i = 0; i < 4; i++)
                flock[i] = Spawn("Pigeon_Flock" + i, new Vector3(-2.4f + i * 0.5f, 0, -1.2f));

            var driverGo = new GameObject("PigeonTestDriver");
            var driver = driverGo.AddComponent<PigeonTestDriver>();
            var dso = new SerializedObject(driver);
            dso.FindProperty("groundBird").objectReferenceValue = ground;
            dso.FindProperty("flyerBird").objectReferenceValue = flyer;
            dso.FindProperty("gliderBird").objectReferenceValue = glider;
            dso.FindProperty("bankerBird").objectReferenceValue = banker;
            dso.FindProperty("landerBird").objectReferenceValue = lander;
            var arr = dso.FindProperty("flockBirds");
            arr.arraySize = flock.Length;
            for (int i = 0; i < flock.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = flock[i];
            dso.FindProperty("selected").objectReferenceValue = flyer;
            dso.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[PigeonV2] Test scene saved at " + ScenePath);
        }
    }
}
