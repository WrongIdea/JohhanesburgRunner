// One-shot headless importer for the two JHB building asset sets.
// Run via: Unity -batchmode -quit -executeMethod JoburgRunner.Editor.BuildingImporter.ImportAll
// Idempotent: re-running updates importers/materials/prefabs in place (no duplicate materials).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace JoburgRunner.Editor
{
    public static class BuildingImporter
    {
        const string Root = "Assets/Environment/Buildings";
        const string BuildingA = Root + "/JHB_Building02";
        const string BuildingB = Root + "/JHB_SkylinePop_02";
        const string CbdTower = Root + "/JHB_CBDTower";
        const string MidRiseRoot = Root + "/MidRise";
        const string ImportedHeroRoot = Root + "/ImportedHeroes";

        // LOD0 = highest detail (shown closest). Value = screen-relative height BELOW which Unity
        // switches to the next, less-detailed LOD. Last entry doubles as the cull threshold.
        static readonly float[] LodTransitions = { 0.55f, 0.25f, 0.08f };

        static readonly StaticEditorFlags StaticFlags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;
        // Deliberately NOT ContributeGI / lightmap-static: project uses no baked lighting.

        static readonly StringBuilder Log = new StringBuilder();
        static readonly List<string> Problems = new List<string>();

        static void L(string s) { Log.AppendLine(s); Debug.Log("[BuildingImporter] " + s); }
        static void Warn(string s) { Problems.Add(s); Log.AppendLine("WARN: " + s); Debug.LogWarning("[BuildingImporter] " + s); }

        // CI/device deployment entry point. Builds the scenes already enabled in Build
        // Settings without regenerating or modifying them.
        public static void BuildCurrentAndroidApk()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No enabled build scenes.");
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/JoburgEndlessRunner.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + report.summary.result);
        }

        public static void NarrowRoadAndPavements()
        {
            const string path = "Assets/Prefabs/RoadSegment.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SetChild(root, "Road/ThreeLaneAsphalt", 0f, RoadMetrics.RoadWidth);
                SetChild(root, "Road/LeftYellowShoulderLine", -4.0f);
                SetChild(root, "Road/RightYellowShoulderLine", 4.0f);
                SetChild(root, "Sidewalks/LeftConcreteSidewalk", -5.82f, 2.95f);
                SetChild(root, "Sidewalks/RightConcreteSidewalk", 5.82f, 2.95f);
                SetChild(root, "Sidewalks/LeftCurb", -4.38f, 0.30f);
                SetChild(root, "Sidewalks/RightCurb", 4.38f, 0.30f);
                SetChild(root, "Sidewalks/LeftGrassStrip", -7.5f, 0.45f);
                SetChild(root, "Sidewalks/RightGrassStrip", 7.5f, 0.45f);

                foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                {
                    if (tr.name == "PavementExpansionJoint_Left") SetTransformX(tr, -5.82f, 2.7f);
                    else if (tr.name == "PavementExpansionJoint_Right") SetTransformX(tr, 5.82f, 2.7f);
                }

                SetChild(root, "DecorSockets/HeroBuildingSocket_L", -11f);
                SetChild(root, "DecorSockets/HeroBuildingSocket_R", 11f);
                foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                {
                    if (tr.name.StartsWith("TreeSocket_L")) SetTransformX(tr, -6.55f);
                    else if (tr.name.StartsWith("TreeSocket_R")) SetTransformX(tr, 6.55f);
                    else if (tr.name.StartsWith("LampSocket_L") || tr.name.StartsWith("TrafficLightSocket_S") && tr.name.EndsWith("W") ||
                             tr.name.StartsWith("TrafficLightSocket_N") && tr.name.EndsWith("W")) SetTransformX(tr, -4.8f);
                    else if (tr.name.StartsWith("LampSocket_R") || tr.name.StartsWith("TrafficLightSocket_S") && tr.name.EndsWith("E") ||
                             tr.name.StartsWith("TrafficLightSocket_N") && tr.name.EndsWith("E")) SetTransformX(tr, 4.8f);
                    else if (tr.name == "BenchSocket_L1") SetTransformX(tr, -5.65f);
                    else if (tr.name == "BinSocket_R1") SetTransformX(tr, 5.25f);
                    else if (tr.name == "BillboardSocket_R1" || tr.name == "BusStopSocket_R1") SetTransformX(tr, 5.65f);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Narrowed road/pavements and moved hero building sockets inward.");
        }

        public static void AddActiveBuildingsToBackdrop()
        {
            const string scenePath = "Assets/Scenes/JoburgEndlessRunner.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject skyline = GameObject.Find("JoburgSkylineBackdrop");
            if (skyline == null) throw new InvalidOperationException("JoburgSkylineBackdrop not found.");

            AddBackdropPrefab(
                skyline.transform,
                "Assets/Environment/Buildings/JHB_Building02/Prefabs/PF_JHB_Building02.prefab",
                "Backdrop_JHB_Building02",
                new Vector3(-27f, 0f, 13f),
                1.25f);
            AddBackdropPrefab(
                skyline.transform,
                "Assets/Environment/Buildings/JHB_SkylinePop_02/Prefabs/PF_JHB_SkylinePop_02.prefab",
                "Backdrop_JHB_SkylinePop_02",
                new Vector3(27f, 0f, 19f),
                1.15f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Added active JHB buildings to the moving skyline backdrop.");
        }

        public static void ConfigureTwoBuildingOnlyScene()
        {
            const string scenePath = "Assets/Scenes/JoburgEndlessRunner.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var director = UnityEngine.Object.FindFirstObjectByType<
                JoburgRunner.Environment.Decor.EnvironmentDecorDirector>();
            if (director == null) throw new InvalidOperationException("EnvironmentDecorDirector not found.");
            var directorSo = new SerializedObject(director);
            directorSo.FindProperty("useSocketDecoration").boolValue = true;
            directorSo.FindProperty("heroOnlyMode").boolValue = true;
            directorSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject skyline = GameObject.Find("JoburgSkylineBackdrop");
            if (skyline == null) throw new InvalidOperationException("JoburgSkylineBackdrop not found.");
            for (int i = skyline.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = skyline.transform.GetChild(i);
                if (child.name != "Backdrop_JHB_Building02" &&
                    child.name != "Backdrop_JHB_SkylinePop_02")
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            const string roadPath = "Assets/Prefabs/RoadSegment.prefab";
            GameObject road = PrefabUtility.LoadPrefabContents(roadPath);
            try
            {
                var visuals = road.GetComponent<JoburgRunner.RoadSegmentVisuals>();
                if (visuals == null) throw new InvalidOperationException("RoadSegmentVisuals not found.");
                var visualsSo = new SerializedObject(visuals);
                visualsSo.FindProperty("suppressDistrictBuildings").boolValue = true;
                visualsSo.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(road, roadPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(road); }

            AssetDatabase.SaveAssets();
            Debug.Log("Configured strict two-building-only environment.");
        }

        public static void RestoreSkylineAhead()
        {
            const string scenePath = "Assets/Scenes/JoburgEndlessRunner.unity";
            const string backupPath = "_Backups/TwoBuildingOnly_20260724/JoburgEndlessRunner.unity.bak";
            const string sourcePath = "Assets/__SkylineRestoreSource.unity";
            if (!File.Exists(backupPath))
                throw new FileNotFoundException("Skyline source backup is missing.", backupPath);

            File.Copy(backupPath, sourcePath, true);
            AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport);
            var targetScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var sourceScene = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);
            GameObject targetSkyline = FindSceneObject(targetScene, "JoburgSkylineBackdrop");
            GameObject sourceSkyline = FindSceneObject(sourceScene, "JoburgSkylineBackdrop");
            if (targetSkyline == null || sourceSkyline == null)
                throw new InvalidOperationException("Skyline root missing from target or restore source.");

            for (int i = targetSkyline.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(targetSkyline.transform.GetChild(i).gameObject);

            var sourceChildren = new List<GameObject>();
            foreach (Transform child in sourceSkyline.transform) sourceChildren.Add(child.gameObject);
            foreach (GameObject sourceChild in sourceChildren)
            {
                GameObject clone = UnityEngine.Object.Instantiate(sourceChild, targetSkyline.transform);
                clone.name = sourceChild.name;
                clone.transform.localPosition = sourceChild.transform.localPosition;
                clone.transform.localRotation = sourceChild.transform.localRotation;
                clone.transform.localScale = sourceChild.transform.localScale;
                foreach (Collider c in clone.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(c);
            }

            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
            EditorSceneManager.CloseScene(sourceScene, true);
            AssetDatabase.DeleteAsset(sourcePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Restored full player-following Johannesburg skyline.");
        }

        static GameObject FindSceneObject(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform tr in transforms)
                    if (tr.name == name) return tr.gameObject;
            }
            return null;
        }

        static void AddBackdropPrefab(
            Transform parent, string prefabPath, string instanceName,
            Vector3 localPosition, float uniformScale)
        {
            Transform old = parent.Find(instanceName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new InvalidOperationException("Backdrop prefab missing: " + prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = instanceName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            instance.transform.localScale = Vector3.one * uniformScale;
            foreach (Collider c in instance.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            foreach (Transform tr in instance.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(tr.gameObject, 0);
        }

        static void SetChild(GameObject root, string path, float x, float width = -1f)
        {
            Transform tr = root.transform.Find(path);
            if (tr == null) throw new InvalidOperationException("Road prefab child missing: " + path);
            SetTransformX(tr, x, width);
        }

        static void SetTransformX(Transform tr, float x, float width = -1f)
        {
            Vector3 position = tr.localPosition;
            position.x = x;
            tr.localPosition = position;
            if (width > 0f)
            {
                Vector3 scale = tr.localScale;
                scale.x = width;
                tr.localScale = scale;
            }
        }

        [MenuItem("Joburg Runner/Import JHB Mid-Rise Buildings")]
        public static void ImportMidRiseBuildings()
        {
            Log.Clear(); Problems.Clear();
            try
            {
                AssetDatabase.Refresh();
                var prefabs = new List<GameObject>();
                for (int i = 1; i <= 5; i++)
                {
                    string name = "JHB_MidRise" + i.ToString("00");
                    string dir = MidRiseRoot + "/" + name;
                    string models = dir + "/Models/";
                    string textures = dir + "/Textures/";

                    foreach (string suffix in new[] { "_LOD0.fbx", "_LOD1.fbx", "_LOD2.fbx" })
                        ConfigureModel(models + name + suffix, readable: false, preserveHierarchy: true);
                    // Project convention: imported collision meshes remain readable for reliable
                    // MeshCollider cooking on mobile; visual meshes remain non-readable.
                    ConfigureModel(models + name + "_Collision.fbx", readable: true, preserveHierarchy: true);

                    Texture2D baseColor = ImportTexture(textures + "base_color.png", true, false);
                    Texture2D normal = ImportTexture(textures + "normal.png", false, true);
                    Texture2D metalSmooth = GenerateMetalSmooth(
                        textures + "metallic_roughness.png",
                        textures + name + "_metallic_smoothness.png");
                    Texture2D emissive = i == 4
                        ? ImportTexture(textures + "emissive.png", true, false)
                        : null;

                    var materials = BuildMidRiseMaterials(name, dir, baseColor, normal, metalSmooth, emissive);
                    for (int lod = 0; lod < 3; lod++)
                    {
                        string fbx = models + name + "_LOD" + lod + ".fbx";
                        RemapMaterials(fbx, materials, (slot, unused) => materials[slot], null);
                    }

                    string prefabPath = dir + "/Prefabs/" + name + ".prefab";
                    BuildMidRisePrefab(name, prefabPath, models);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null) prefabs.Add(prefab);
                    Validate(prefabPath);
                }

                UpdateHeroBuildingSet(prefabs);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e) { Warn("EXCEPTION: " + e); }
            finally
            {
                L("\n===== MID-RISE SUMMARY =====");
                L(Problems.Count == 0 ? "No problems reported." : Problems.Count + " problem(s).");
                foreach (string p in Problems) L("  - " + p);
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Builds"));
                File.WriteAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Builds/midrise_import_report.txt"),
                    Log.ToString());
            }
        }

        [MenuItem("Joburg Runner/Import Prepared Hero Buildings")]
        public static void ImportPreparedHeroBuildings()
        {
            Log.Clear(); Problems.Clear();
            try
            {
                AssetDatabase.Refresh();
                var prefabs = new List<GameObject>();
                for (int i = 3; i <= 7; i++)
                {
                    string name = "JHB_HeroBuilding" + i.ToString("00");
                    string dir = ImportedHeroRoot + "/" + name;
                    string models = dir + "/Models/";
                    string textures = dir + "/Textures/";
                    for (int lod = 0; lod < 3; lod++)
                        ConfigureModel(models + name + "_LOD" + lod + ".fbx", false, true);
                    ConfigureModel(models + name + "_Collision.fbx", true, true);

                    Texture2D baseColor = ImportTexture(textures + "base_color.png", true, false);
                    Texture2D normal = ImportTexture(textures + "normal.png", false, true);
                    Texture2D mask = GenerateMetalSmooth(
                        textures + "metallic_roughness.png",
                        textures + name + "_metallic_smoothness.png");
                    Texture2D emissive = ImportTexture(textures + "emissive.png", true, false);
                    var materials = BuildMidRiseMaterials(
                        name, dir, baseColor, normal, mask, emissive);
                    for (int lod = 0; lod < 3; lod++)
                    {
                        string fbx = models + name + "_LOD" + lod + ".fbx";
                        RemapMaterials(fbx, materials, (slot, unused) => materials[slot], null);
                    }

                    string prefabPath = dir + "/Prefabs/" + name + ".prefab";
                    BuildMidRisePrefab(name, prefabPath, models);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null) prefabs.Add(prefab);
                    Validate(prefabPath);
                }
                AddPreparedHeroesToSet(prefabs);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e) { Warn("EXCEPTION: " + e); }
            finally
            {
                Directory.CreateDirectory("Builds");
                File.WriteAllText("Builds/prepared_hero_import_report.txt", Log.ToString());
            }
        }

        static void AddPreparedHeroesToSet(List<GameObject> prefabs)
        {
            const string path = "Assets/Environment/Decor/HeroBuildingSet.asset";
            var set = AssetDatabase.LoadAssetAtPath<JoburgRunner.Environment.Decor.HeroBuildingSet>(path);
            if (set == null) { Warn("Hero set missing."); return; }
            var entries = (set.entries ?? Array.Empty<JoburgRunner.Environment.Decor.HeroEntry>()).ToList();
            foreach (GameObject prefab in prefabs)
            {
                var entry = entries.FirstOrDefault(e => e != null && e.label == prefab.name);
                if (entry == null)
                {
                    entry = new JoburgRunner.Environment.Decor.HeroEntry();
                    entries.Add(entry);
                }
                entry.label = prefab.name;
                entry.prefab = prefab;
                entry.weight = 1f;
                entry.allowedSides = JoburgRunner.Environment.Decor.HeroSide.Both;
                entry.localPositionOffset = new Vector3(0f, 0f, -1.5f);
                entry.eulerOffset = Vector3.zero;
                entry.facePlayerDegrees = 40f;
                entry.uniformScale = 1f;
                entry.footprintRadius = 8f;
                entry.minRepeatDistance = 0f;
                entry.enabled = true;
            }
            set.entries = entries.ToArray();
            EditorUtility.SetDirty(set);
            L("Added " + prefabs.Count + " prepared hero buildings to the active pool.");
        }

        [MenuItem("Joburg Runner/Import JHB Buildings")]
        public static void ImportAll()
        {
            Log.Clear(); Problems.Clear();
            try
            {
                AssetDatabase.Refresh();

                // ---- Building A: textured atlas building ----
                ConfigureModel(BuildingA + "/FBX/JHB_Building02_LOD0.fbx", readable: false, preserveHierarchy: true);
                ConfigureModel(BuildingA + "/FBX/JHB_Building02_LOD1.fbx", readable: false, preserveHierarchy: true);
                ConfigureModel(BuildingA + "/FBX/JHB_Building02_LOD2.fbx", readable: false, preserveHierarchy: true);
                // Collision mesh: readable so the MeshCollider cooks cleanly on all platforms.
                ConfigureModel(BuildingA + "/FBX/JHB_Building02_Collision.fbx", readable: true, preserveHierarchy: true);

                var texA = ConfigureTexturesA();
                var matsA = BuildMaterialsA(texA);
                RemapMaterials(BuildingA + "/FBX/JHB_Building02_LOD0.fbx", matsA, MaterialForA, texA);
                RemapMaterials(BuildingA + "/FBX/JHB_Building02_LOD1.fbx", matsA, MaterialForA, texA);
                RemapMaterials(BuildingA + "/FBX/JHB_Building02_LOD2.fbx", matsA, MaterialForA, texA);

                BuildPrefab(
                    name: "PF_JHB_Building02",
                    prefabPath: BuildingA + "/Prefabs/PF_JHB_Building02.prefab",
                    lodFbx: new[]{
                        BuildingA + "/FBX/JHB_Building02_LOD0.fbx",
                        BuildingA + "/FBX/JHB_Building02_LOD1.fbx",
                        BuildingA + "/FBX/JHB_Building02_LOD2.fbx" },
                    collisionFbx: BuildingA + "/FBX/JHB_Building02_Collision.fbx",
                    boxColliders: false);

                // ---- Building B: solid-colour skyline pop (no textures by design) ----
                ConfigureModel(BuildingB + "/FBX/JHB_SkylinePop_02_LOD0.fbx", readable: false, preserveHierarchy: true);
                ConfigureModel(BuildingB + "/FBX/JHB_SkylinePop_02_LOD1.fbx", readable: false, preserveHierarchy: true);
                ConfigureModel(BuildingB + "/FBX/JHB_SkylinePop_02_LOD2.fbx", readable: false, preserveHierarchy: true);

                var matsB = BuildMaterialsB();
                RemapMaterials(BuildingB + "/FBX/JHB_SkylinePop_02_LOD0.fbx", matsB, MaterialForB, null);
                RemapMaterials(BuildingB + "/FBX/JHB_SkylinePop_02_LOD1.fbx", matsB, MaterialForB, null);
                RemapMaterials(BuildingB + "/FBX/JHB_SkylinePop_02_LOD2.fbx", matsB, MaterialForB, null);

                BuildPrefab(
                    name: "PF_JHB_SkylinePop_02",
                    prefabPath: BuildingB + "/Prefabs/PF_JHB_SkylinePop_02.prefab",
                    lodFbx: new[]{
                        BuildingB + "/FBX/JHB_SkylinePop_02_LOD0.fbx",
                        BuildingB + "/FBX/JHB_SkylinePop_02_LOD1.fbx",
                        BuildingB + "/FBX/JHB_SkylinePop_02_LOD2.fbx" },
                    collisionFbx: null,
                    boxColliders: true);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Validate(BuildingA + "/Prefabs/PF_JHB_Building02.prefab");
                Validate(BuildingB + "/Prefabs/PF_JHB_SkylinePop_02.prefab");
            }
            catch (Exception e)
            {
                Warn("EXCEPTION: " + e);
            }
            finally
            {
                L("\n===== SUMMARY =====");
                L(Problems.Count == 0 ? "No problems reported." : (Problems.Count + " problem(s):"));
                foreach (var p in Problems) L("  - " + p);
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Builds/building_import_report.txt"), Log.ToString());
                L("Report written to Builds/building_import_report.txt");
            }
        }

        // ================================================================ JHB_CBDTower
        // Brutalist CBD tower: one packed atlas material + one blank red crown band.
        [MenuItem("Joburg Runner/Import JHB CBDTower")]
        public static void ImportCbdTower()
        {
            Log.Clear(); Problems.Clear();
            try
            {
                AssetDatabase.Refresh();

                ConfigureModel(CbdTower + "/FBX/JHB_CBDTower_LOD0.fbx", readable: false, preserveHierarchy: true);
                ConfigureModel(CbdTower + "/FBX/JHB_CBDTower_LOD1.fbx", readable: false, preserveHierarchy: true);
                ConfigureModel(CbdTower + "/FBX/JHB_CBDTower_LOD2.fbx", readable: false, preserveHierarchy: true);
                ConfigureModel(CbdTower + "/FBX/JHB_CBDTower_Collision.fbx", readable: true, preserveHierarchy: true);

                Texture2D baseColor = ImportTexture(CbdTower + "/Textures/base_color.png", srgb: true, normal: false);
                Texture2D normal = ImportTexture(CbdTower + "/Textures/normal.png", srgb: false, normal: true);
                ImportTexture(CbdTower + "/Textures/emissive.png", srgb: true, normal: false); // kept as asset; emission stays off
                Texture2D metalSmooth = GenerateMetalSmooth(
                    CbdTower + "/Textures/metallic_roughness.png",
                    CbdTower + "/Textures/JHB_CBDTower_metallic_smoothness.png");

                // Atlas material (concrete shell + window grid + lobby + rooftop).
                Material atlas = LoadOrCreateURPLit(CbdTower + "/Materials/M_JHB_CBDTower_Atlas.mat");
                SetBaseColor(atlas, Color.white);
                SetMap(atlas, "_BaseMap", baseColor); SetMap(atlas, "_MainTex", baseColor);
                if (normal != null) { SetMap(atlas, "_BumpMap", normal); EnableNormal(atlas); } else DisableNormal(atlas);
                if (metalSmooth != null) EnableMetallicMap(atlas, metalSmooth); else DisableMetallicMap(atlas);
                DisableEmission(atlas); // emissive.png peaks at ~0.03 (near-black) -> no visible contribution
                atlas.SetFloat("_Smoothness", 0.5f);
                atlas.SetFloat("_Metallic", 1f); // map-driven; value is the fallback
                atlas.enableInstancing = true;
                EditorUtility.SetDirty(atlas);

                // Blank red crown band: flat dark architectural red, no textures, no text/logo.
                Material red = LoadOrCreateURPLit(CbdTower + "/Materials/M_JHB_CBDTower_BlankRedBand.mat");
                SetBaseColor(red, new Color(0.50f, 0.08f, 0.08f, 1f));
                SetMap(red, "_BaseMap", null); SetMap(red, "_MainTex", null);
                DisableNormal(red); DisableMetallicMap(red); DisableEmission(red);
                red.SetFloat("_Smoothness", 0.25f);
                red.SetFloat("_Metallic", 0f);
                red.enableInstancing = true;
                EditorUtility.SetDirty(red);

                foreach (string lod in new[] { "LOD0", "LOD1", "LOD2" })
                    RemapCbdMaterials(CbdTower + "/FBX/JHB_CBDTower_" + lod + ".fbx", atlas, red);

                BuildCbdTowerPrefab(CbdTower + "/Prefabs/PF_JHB_CBDTower.prefab");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Validate(CbdTower + "/Prefabs/PF_JHB_CBDTower.prefab");
            }
            catch (Exception e) { Warn("EXCEPTION: " + e); }
            finally
            {
                L("\n===== SUMMARY =====");
                L(Problems.Count == 0 ? "No problems reported." : (Problems.Count + " problem(s):"));
                foreach (var p in Problems) L("  - " + p);
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Builds/cbdtower_import_report.txt"), Log.ToString());
                L("Report written to Builds/cbdtower_import_report.txt");
            }
        }

        // glTF metallic-roughness (G=roughness, B=metallic) -> Unity metallic/smoothness
        // (R=metallic, A=1-roughness), linear. Writes a NEW asset; never touches the source.
        static Texture2D GenerateMetalSmooth(string mrPath, string outPath)
        {
            try
            {
                var raw = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), mrPath));
                var src = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                src.LoadImage(raw);
                var px = src.GetPixels32();
                for (int i = 0; i < px.Length; i++)
                {
                    byte metallic = px[i].b;
                    byte smoothness = (byte)(255 - px[i].g);
                    px[i] = new Color32(metallic, 0, 0, smoothness);
                }
                var dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, true);
                dst.SetPixels32(px); dst.Apply();
                File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), outPath), dst.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(src);
                UnityEngine.Object.DestroyImmediate(dst);
                AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
                Texture2D tex = ImportTexture(outPath, srgb: false, normal: false);
                L("Generated metallic/smoothness map: " + Path.GetFileName(outPath));
                return tex;
            }
            catch (Exception e) { Warn("metallic/smoothness conversion failed: " + e.Message); return null; }
        }

        static Dictionary<string, Material> BuildMidRiseMaterials(
            string buildingName, string dir, Texture2D baseColor, Texture2D normal,
            Texture2D metalSmooth, Texture2D emissive)
        {
            string[] fbxs = Enumerable.Range(0, 3)
                .Select(i => dir + "/Models/" + buildingName + "_LOD" + i + ".fbx").ToArray();
            var result = new Dictionary<string, Material>();
            foreach (string slot in DiscoverMaterialNames(fbxs))
            {
                string safe = new string(slot.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
                Material m = LoadOrCreateURPLit(dir + "/Materials/M_" + buildingName + "_" + safe + ".mat");
                SetBaseColor(m, Color.white);
                SetMap(m, "_BaseMap", baseColor); SetMap(m, "_MainTex", baseColor);
                if (normal != null) { SetMap(m, "_BumpMap", normal); EnableNormal(m); }
                else DisableNormal(m);
                if (metalSmooth != null) EnableMetallicMap(m, metalSmooth);
                else DisableMetallicMap(m);
                if (emissive != null) EnableEmission(m, emissive);
                else DisableEmission(m);
                m.SetFloat("_Metallic", 1f);
                m.SetFloat("_Smoothness", 0.5f);
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
                result[slot] = m;
            }
            return result;
        }

        static void BuildMidRisePrefab(string name, string prefabPath, string models)
        {
            var root = new GameObject(name);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var group = root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            var lods = new LOD[3];

            for (int i = 0; i < 3; i++)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                    models + name + "_LOD" + i + ".fbx");
                if (model == null) { Warn("Missing model for " + name + " LOD" + i); continue; }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = "LOD" + i;
                instance.transform.SetParent(visual.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    r.receiveShadows = true;
                    r.shadowCastingMode = i == 2 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                }
                lods[i] = new LOD(LodTransitions[i], renderers);
            }
            group.SetLODs(lods);
            group.RecalculateBounds();

            var collisionModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                models + name + "_Collision.fbx");
            if (collisionModel != null)
            {
                var collision = (GameObject)PrefabUtility.InstantiatePrefab(collisionModel);
                collision.name = "Collision";
                collision.transform.SetParent(root.transform, false);
                collision.transform.localPosition = Vector3.zero;
                collision.transform.localRotation = Quaternion.identity;
                collision.transform.localScale = Vector3.one;
                foreach (Renderer r in collision.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                foreach (MeshFilter mf in collision.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    var mc = mf.gameObject.GetComponent<MeshCollider>();
                    // Unity's imported-model placeholder can compare equal to null while still
                    // being a non-null CLR reference, so avoid the null-coalescing operator here.
                    if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                }
            }
            else Warn("Missing collision model for " + name);

            foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(tr.gameObject, StaticFlags);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool ok);
            if (!ok || saved == null) Warn("Prefab save failed: " + prefabPath);
            else L("Saved prefab: " + prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        static void UpdateHeroBuildingSet(List<GameObject> prefabs)
        {
            const string setPath = "Assets/Environment/Decor/HeroBuildingSet.asset";
            var set = AssetDatabase.LoadAssetAtPath<JoburgRunner.Environment.Decor.HeroBuildingSet>(setPath);
            if (set == null) { Warn("Hero building set not found: " + setPath); return; }
            var existing = (set.entries ?? Array.Empty<JoburgRunner.Environment.Decor.HeroEntry>()).ToList();
            foreach (GameObject prefab in prefabs)
            {
                var entry = existing.FirstOrDefault(e => e != null && e.label == prefab.name);
                if (entry == null)
                {
                    entry = new JoburgRunner.Environment.Decor.HeroEntry();
                    existing.Add(entry);
                }
                entry.label = prefab.name;
                entry.prefab = prefab;
                // New production mid-rises intentionally dominate the legacy hero variants
                // while the director's recent-history queue keeps consecutive picks distinct.
                entry.weight = 4f;
                entry.allowedSides = JoburgRunner.Environment.Decor.HeroSide.Both;
                entry.localPositionOffset = new Vector3(0f, 0f, -2f);
                entry.eulerOffset = Vector3.zero;
                entry.facePlayerDegrees = 40f;
                entry.uniformScale = 1f;
                entry.footprintRadius = 8.5f;
                entry.minRepeatDistance = 0f;
                entry.enabled = true;
            }
            set.entries = existing.ToArray();
            EditorUtility.SetDirty(set);
            L("Updated weighted no-repeat hero pool with " + prefabs.Count + " mid-rise variants.");
        }

        static void RemapCbdMaterials(string fbxPath, Material atlas, Material red)
        {
            var mi = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (mi == null) { Warn("Remap: model not found " + fbxPath); return; }
            var names = new List<string>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                if (o is Material mat) names.Add(mat.name);

            foreach (var n in names)
            {
                string lo = n.ToLowerInvariant();
                bool isBand = lo.Contains("red") || lo.Contains("band") || lo.Contains("crown") || lo.Contains("sign");
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), isBand ? red : atlas);
            }
            mi.SaveAndReimport();
            L("Remapped " + names.Count + " slot(s) on " + Path.GetFileName(fbxPath) + ": " + string.Join(", ", names));
        }

        static void BuildCbdTowerPrefab(string prefabPath)
        {
            var root = new GameObject("PF_JHB_CBDTower");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var lodGroup = root.AddComponent<LODGroup>();
            lodGroup.fadeMode = LODFadeMode.None;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            string[] lodFbx =
            {
                CbdTower + "/FBX/JHB_CBDTower_LOD0.fbx",
                CbdTower + "/FBX/JHB_CBDTower_LOD1.fbx",
                CbdTower + "/FBX/JHB_CBDTower_LOD2.fbx",
            };
            var lods = new LOD[lodFbx.Length];
            for (int i = 0; i < lodFbx.Length; i++)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(lodFbx[i]);
                if (fbx == null) { Warn("LOD FBX missing: " + lodFbx[i]); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                inst.name = "LOD" + i;
                inst.transform.SetParent(visual.transform, false);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;
                var rends = inst.GetComponentsInChildren<Renderer>(true);
                float t = i < LodTransitions.Length ? LodTransitions[i] : 0.02f;
                lods[i] = new LOD(t, rends);
            }
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            var colFbx = AssetDatabase.LoadAssetAtPath<GameObject>(CbdTower + "/FBX/JHB_CBDTower_Collision.fbx");
            if (colFbx != null)
            {
                var colInst = (GameObject)PrefabUtility.InstantiatePrefab(colFbx);
                colInst.name = "Collision";
                colInst.transform.SetParent(root.transform, false);
                colInst.transform.localPosition = Vector3.zero;
                colInst.transform.localRotation = Quaternion.identity;
                colInst.transform.localScale = Vector3.one;
                foreach (var r in colInst.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                var mf = colInst.GetComponentInChildren<MeshFilter>(true);
                if (mf != null && mf.sharedMesh != null)
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                    L("Collision: MeshCollider from " + mf.sharedMesh.name + " (" + mf.sharedMesh.triangles.Length / 3 + " tris)");
                }
                else Warn("Collision mesh not found in collision FBX");
            }
            else Warn("Collision FBX missing");

            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(tr.gameObject, StaticFlags);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), prefabPath)));
            var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool ok);
            if (!ok || saved == null) Warn("Prefab save failed: " + prefabPath);
            else L("Saved prefab: " + prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- model import
        static void ConfigureModel(string path, bool readable, bool preserveHierarchy)
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { Warn("Model not found: " + path); return; }

            mi.globalScale = 1f;
            mi.useFileScale = true;              // "Convert Units"
            mi.importCameras = false;
            mi.importLights = false;
            mi.importVisibility = false;
            mi.importConstraints = false;
            mi.importBlendShapes = false;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.isReadable = readable;
            mi.meshCompression = ModelImporterMeshCompression.Low;
            mi.optimizeMeshPolygons = true;
            mi.optimizeMeshVertices = true;
            mi.preserveHierarchy = preserveHierarchy;
            mi.addCollider = false;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.indexFormat = ModelImporterIndexFormat.Auto;
            mi.weldVertices = true;
#if UNITY_2020_1_OR_NEWER
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
#endif
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.SaveAndReimport();
            L("Configured model: " + Path.GetFileName(path) + " (readable=" + readable + ")");
        }

        // ---------------------------------------------------------------- textures (Building A)
        class TexA { public Texture2D baseColor, normal, emissive, metalSmooth; }

        static TexA ConfigureTexturesA()
        {
            var t = new TexA();
            t.baseColor = ImportTexture(BuildingA + "/Textures/base_color.png", srgb: true, normal: false);
            t.emissive  = ImportTexture(BuildingA + "/Textures/emissive.png",   srgb: true, normal: false);
            t.normal    = ImportTexture(BuildingA + "/Textures/normal.png",      srgb: false, normal: true);

            // Convert glTF metallic-roughness (G=roughness, B=metallic) -> Unity metallic/smoothness
            // packed map (R=metallic, A=smoothness=1-roughness). Linear.
            string mrPath = BuildingA + "/Textures/metallic_roughness.png";
            string outPath = BuildingA + "/Textures/JHB_Building02_metallic_smoothness.png";
            try
            {
                var raw = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), mrPath));
                var src = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                src.LoadImage(raw);
                var px = src.GetPixels32();
                for (int i = 0; i < px.Length; i++)
                {
                    byte metallic = px[i].b;                 // glTF metalness
                    byte smoothness = (byte)(255 - px[i].g); // 1 - roughness
                    px[i] = new Color32(metallic, 0, 0, smoothness);
                }
                var dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, true);
                dst.SetPixels32(px); dst.Apply();
                File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), outPath), dst.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(src);
                UnityEngine.Object.DestroyImmediate(dst);
                AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
                t.metalSmooth = ImportTexture(outPath, srgb: false, normal: false);
                L("Generated metallic/smoothness map: " + Path.GetFileName(outPath));
            }
            catch (Exception e) { Warn("metallic/smoothness conversion failed: " + e.Message); }
            return t;
        }

        static Texture2D ImportTexture(string path, bool srgb, bool normal)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Warn("Texture not found: " + path); return null; }
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = srgb;
            ti.mipmapEnabled = true;
            ti.streamingMipmaps = true;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Bilinear;
            ti.anisoLevel = 1;
            ti.isReadable = false;
            ti.alphaIsTransparency = false;
            var ap = ti.GetDefaultPlatformTextureSettings();
            ap.maxTextureSize = 2048;
            ti.SetPlatformTextureSettings(ap);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------------------------------------------------------------- materials (Building A)
        static Dictionary<string, Material> BuildMaterialsA(TexA tex)
        {
            // Discover the material slot names the FBX actually exposes, then build one shared
            // URP material per name (tuned by semantic keyword). Shared across all three LODs.
            var names = DiscoverMaterialNames(new[]{
                BuildingA + "/FBX/JHB_Building02_LOD0.fbx",
                BuildingA + "/FBX/JHB_Building02_LOD1.fbx",
                BuildingA + "/FBX/JHB_Building02_LOD2.fbx" });
            L("Building A material slots: " + string.Join(", ", names));

            var map = new Dictionary<string, Material>();
            foreach (var n in names)
                map[n] = MaterialForA(n, tex);
            return map;
        }

        static Material MaterialForA(string slotName, TexA tex)
        {
            string lower = slotName.ToLowerInvariant();
            string matPath = BuildingA + "/Materials/M_" + Sanitize(slotName) + ".mat";
            var m = LoadOrCreateURPLit(matPath);

            bool isBillboard = lower.Contains("billboard");
            bool isGlass = lower.Contains("glass");
            bool isMetal = lower.Contains("metal");
            bool isEmissive = lower.Contains("emiss");

            if (isBillboard)
            {
                // Blank, text-free charcoal-green surface. No atlas albedo (keeps it readable-text-free).
                SetBaseColor(m, new Color(0.09f, 0.12f, 0.10f, 1f));
                SetMap(m, "_BaseMap", null); SetMap(m, "_BumpMap", null);
                DisableNormal(m); DisableMetallicMap(m); DisableEmission(m);
                m.SetFloat("_Smoothness", 0.15f);
                m.SetFloat("_Metallic", 0f);
            }
            else
            {
                SetBaseColor(m, Color.white);
                SetMap(m, "_BaseMap", tex?.baseColor);
                SetMap(m, "_MainTex", tex?.baseColor);
                if (tex?.normal != null) { SetMap(m, "_BumpMap", tex.normal); EnableNormal(m); } else DisableNormal(m);
                if (tex?.metalSmooth != null) EnableMetallicMap(m, tex.metalSmooth); else DisableMetallicMap(m);

                // Emissive window/sign glow from the shared emissive atlas (black elsewhere).
                if (tex?.emissive != null) EnableEmission(m, tex.emissive); else DisableEmission(m);

                if (isGlass)
                {
                    // Opaque glass for mobile: controlled smoothness + slight metallic, no transparency.
                    m.SetFloat("_Smoothness", 0.85f);
                    m.SetFloat("_Metallic", 0.1f);
                }
                else if (isMetal)
                {
                    m.SetFloat("_Smoothness", 0.6f);
                    m.SetFloat("_Metallic", 1f);
                }
                else // facade / mural / emissive / generic atlas
                {
                    m.SetFloat("_Smoothness", 0.5f);
                    m.SetFloat("_Metallic", 1f); // map-driven; value acts as fallback
                }
            }

            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------------------------------------------------------------- materials (Building B)
        static Dictionary<string, Material> BuildMaterialsB()
        {
            var jsonPath = BuildingB + "/Materials/JHB_SkylinePop_02_URP_Materials.json";
            var text = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), jsonPath));
            var defs = ParseSolidMaterials(text);
            L("Building B JSON material defs: " + string.Join(", ", defs.Keys));

            var map = new Dictionary<string, Material>();
            foreach (var kv in defs)
            {
                string matPath = BuildingB + "/Materials/M_" + Sanitize(kv.Key) + ".mat";
                var m = LoadOrCreateURPLit(matPath);
                SetBaseColor(m, kv.Value.color);
                SetMap(m, "_BaseMap", null); SetMap(m, "_MainTex", null);
                DisableNormal(m); DisableMetallicMap(m); DisableEmission(m);
                m.SetFloat("_Metallic", kv.Value.metallic);
                m.SetFloat("_Smoothness", Mathf.Clamp01(1f - kv.Value.roughness));
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
                map[kv.Key] = m;
            }
            return map;
        }

        static Material MaterialForB(string slotName, TexA _)
        {
            // Match FBX slot -> JSON material by name; fall back to loading the already-built asset.
            string matPath = BuildingB + "/Materials/M_" + Sanitize(slotName) + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null) return existing;
            Warn("Building B slot '" + slotName + "' has no matching JSON material; using neutral grey.");
            var m = LoadOrCreateURPLit(matPath);
            SetBaseColor(m, new Color(0.5f, 0.5f, 0.5f, 1f));
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        struct SolidDef { public Color color; public float metallic; public float roughness; }

        static Dictionary<string, SolidDef> ParseSolidMaterials(string json)
        {
            // Minimal hand parser for the flat, known schema (avoids adding a JSON dependency).
            var result = new Dictionary<string, SolidDef>();
            int mi = json.IndexOf("\"materials\"", StringComparison.Ordinal);
            if (mi < 0) return result;
            int i = json.IndexOf('{', mi) + 1;
            while (true)
            {
                int keyStart = json.IndexOf('"', i);
                if (keyStart < 0) break;
                int keyEnd = json.IndexOf('"', keyStart + 1);
                string key = json.Substring(keyStart + 1, keyEnd - keyStart - 1);
                int objStart = json.IndexOf('{', keyEnd);
                int objEnd = json.IndexOf('}', objStart);
                if (objStart < 0 || objEnd < 0) break;
                string body = json.Substring(objStart, objEnd - objStart + 1);

                var col = ParseFloatArray(body, "base_color");
                float rough = ParseFloat(body, "roughness", 0.5f);
                float metal = ParseFloat(body, "metallic", 0f);
                var def = new SolidDef {
                    color = col.Length >= 3 ? new Color(col[0], col[1], col[2], col.Length >= 4 ? col[3] : 1f) : Color.grey,
                    metallic = metal, roughness = rough };
                result[key] = def;

                i = objEnd + 1;
                int nextComma = json.IndexOf(',', i);
                int closeAll = json.IndexOf('}', i); // end of materials object
                if (nextComma < 0 || (closeAll >= 0 && closeAll < nextComma)) break;
                i = nextComma + 1;
            }
            return result;
        }

        static float[] ParseFloatArray(string body, string key)
        {
            int k = body.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (k < 0) return new float[0];
            int a = body.IndexOf('[', k), b = body.IndexOf(']', a);
            if (a < 0 || b < 0) return new float[0];
            return body.Substring(a + 1, b - a - 1)
                       .Split(',')
                       .Select(s => s.Trim())
                       .Where(s => s.Length > 0)
                       .Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture))
                       .ToArray();
        }

        static float ParseFloat(string body, string key, float dflt)
        {
            int k = body.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (k < 0) return dflt;
            int c = body.IndexOf(':', k) + 1;
            int end = body.IndexOfAny(new[] { ',', '}', '\n' }, c);
            var s = body.Substring(c, end - c).Trim();
            return float.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : dflt;
        }

        // ---------------------------------------------------------------- URP Lit helpers
        static Material LoadOrCreateURPLit(string path)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) { Warn("URP Lit shader not found!"); sh = Shader.Find("Standard"); }
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
                L("Created material: " + path);
            }
            else if (m.shader == null || !m.shader.name.Contains("Universal Render Pipeline/Lit"))
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh != null) m.shader = sh;
            }
            return m;
        }

        static void SetBaseColor(Material m, Color c) { m.SetColor("_BaseColor", c); m.SetColor("_Color", c); }
        static void SetMap(Material m, string prop, Texture t) { if (m.HasProperty(prop)) m.SetTexture(prop, t); }
        static void EnableNormal(Material m) { m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
        static void DisableNormal(Material m) { m.DisableKeyword("_NORMALMAP"); SetMap(m, "_BumpMap", null); }
        static void EnableMetallicMap(Material m, Texture t)
        {
            SetMap(m, "_MetallicGlossMap", t);
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_SmoothnessTextureChannel", 0f); // 0 = metallic alpha
            m.SetFloat("_GlossMapScale", 1f);
            m.SetFloat("_WorkflowMode", 1f);             // metallic
        }
        static void DisableMetallicMap(Material m)
        {
            SetMap(m, "_MetallicGlossMap", null);
            m.DisableKeyword("_METALLICSPECGLOSSMAP");
        }
        static void EnableEmission(Material m, Texture t)
        {
            m.EnableKeyword("_EMISSION");
            SetMap(m, "_EmissionMap", t);
            m.SetColor("_EmissionColor", Color.white);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        static void DisableEmission(Material m)
        {
            m.DisableKeyword("_EMISSION");
            SetMap(m, "_EmissionMap", null);
            m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        // ---------------------------------------------------------------- material remap
        static List<string> DiscoverMaterialNames(string[] fbxPaths)
        {
            var set = new List<string>();
            foreach (var p in fbxPaths)
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
                    if (o is Material mat && !set.Contains(mat.name))
                        set.Add(mat.name);
            return set;
        }

        static void RemapMaterials(string fbxPath, Dictionary<string, Material> shared,
                                   Func<string, TexA, Material> factory, TexA tex)
        {
            var mi = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (mi == null) { Warn("Remap: model not found " + fbxPath); return; }
            var names = new List<string>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                if (o is Material mat) names.Add(mat.name);

            foreach (var n in names)
            {
                if (!shared.TryGetValue(n, out var target))
                {
                    target = factory(n, tex);
                    shared[n] = target;
                }
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), target);
            }
            mi.SaveAndReimport();
            L("Remapped " + names.Count + " material(s) on " + Path.GetFileName(fbxPath) + ": " + string.Join(", ", names));
        }

        // ---------------------------------------------------------------- prefab build
        static void BuildPrefab(string name, string prefabPath, string[] lodFbx, string collisionFbx, bool boxColliders)
        {
            var root = new GameObject(name);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var lodGroup = root.AddComponent<LODGroup>();
            lodGroup.fadeMode = LODFadeMode.None; // no cross-fade: cheapest for mobile
            var lods = new LOD[lodFbx.Length];

            for (int i = 0; i < lodFbx.Length; i++)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(lodFbx[i]);
                if (fbx == null) { Warn("LOD FBX missing: " + lodFbx[i]); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                inst.name = "LOD" + i;
                inst.transform.SetParent(root.transform, false);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;
                var rends = inst.GetComponentsInChildren<Renderer>(true);
                float t = i < LodTransitions.Length ? LodTransitions[i] : 0.02f;
                lods[i] = new LOD(t, rends);
            }
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            // Collision
            var combined = CombinedBounds(root);
            if (!boxColliders && !string.IsNullOrEmpty(collisionFbx))
            {
                var colFbx = AssetDatabase.LoadAssetAtPath<GameObject>(collisionFbx);
                if (colFbx != null)
                {
                    var colInst = (GameObject)PrefabUtility.InstantiatePrefab(colFbx);
                    colInst.name = "Collision";
                    colInst.transform.SetParent(root.transform, false);
                    colInst.transform.localPosition = Vector3.zero;
                    colInst.transform.localRotation = Quaternion.identity;
                    colInst.transform.localScale = Vector3.one;
                    foreach (var r in colInst.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                    var mf = colInst.GetComponentInChildren<MeshFilter>(true);
                    if (mf != null && mf.sharedMesh != null)
                    {
                        var mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        mc.convex = false;
                        L("Collision: MeshCollider from " + mf.sharedMesh.name + " (" + mf.sharedMesh.triangles.Length / 3 + " tris)");
                    }
                    else Warn("Collision mesh not found in " + collisionFbx);
                }
            }
            else if (boxColliders)
            {
                // Podium + tower box masses derived from the combined bounds. Local space (root at origin).
                var b = combined;
                float podiumH = b.size.y * 0.30f;
                var podium = new GameObject("Collision_Podium");
                podium.transform.SetParent(root.transform, false);
                var pbc = podium.AddComponent<BoxCollider>();
                pbc.center = new Vector3(b.center.x, b.min.y + podiumH * 0.5f, b.center.z);
                pbc.size = new Vector3(b.size.x, podiumH, b.size.z);

                var tower = new GameObject("Collision_Tower");
                tower.transform.SetParent(root.transform, false);
                var tbc = tower.AddComponent<BoxCollider>();
                float towerH = b.size.y - podiumH;
                // Tower slightly inset from the podium footprint.
                tbc.center = new Vector3(b.center.x, b.min.y + podiumH + towerH * 0.5f, b.center.z);
                tbc.size = new Vector3(b.size.x * 0.72f, towerH, b.size.z * 0.72f);
                L("Collision: 2 BoxColliders (podium " + pbc.size + ", tower " + tbc.size + ")");
            }

            // Static flags on everything.
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(tr.gameObject, StaticFlags);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), prefabPath)));
            var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool ok);
            if (!ok || saved == null) Warn("Prefab save failed: " + prefabPath);
            else L("Saved prefab: " + prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        static Bounds CombinedBounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
            if (rends.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            return b;
        }

        // ---------------------------------------------------------------- validation
        static void Validate(string prefabPath)
        {
            L("\n----- Validate " + prefabPath + " -----");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Warn("Prefab not loadable: " + prefabPath); return; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            var lodGroup = inst.GetComponent<LODGroup>();
            if (lodGroup == null) Warn("No LODGroup on " + prefabPath);
            else
            {
                var lods = lodGroup.GetLODs();
                L("LODGroup: " + lods.Length + " LODs, fadeMode=" + lodGroup.fadeMode);
                var seen = new HashSet<Renderer>();
                for (int i = 0; i < lods.Length; i++)
                {
                    L("  LOD" + i + " transition=" + lods[i].screenRelativeTransitionHeight + " renderers=" + lods[i].renderers.Length);
                    foreach (var r in lods[i].renderers)
                    {
                        if (r == null) { Warn("LOD" + i + " has a null renderer"); continue; }
                        if (!seen.Add(r)) Warn("Renderer shared across multiple LODs: " + r.name);
                    }
                }
            }

            // Bounds / ground / upright
            var b = CombinedBounds(inst);
            L("Bounds size = " + b.size + "  min.y = " + b.min.y.ToString("F3"));
            if (Mathf.Abs(b.min.y) > 0.25f) Warn("Ground not at Y=0 (min.y=" + b.min.y.ToString("F3") + ")");
            if (b.size.y < b.size.x && b.size.y < b.size.z) Warn("Building may not be upright (Y is smallest axis)");

            // Materials / shaders / textures / negative scale
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var ls = r.transform.lossyScale;
                if (ls.x < 0 || ls.y < 0 || ls.z < 0) Warn("Negative scale on " + r.name);
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) { Warn("Null material slot on " + r.name); continue; }
                    if (m.shader == null || m.shader.name.Contains("Hidden/InternalErrorShader"))
                        Warn("Bad/pink shader on material " + m.name);
                    if (!m.shader.name.StartsWith("Universal Render Pipeline"))
                        Warn("Non-URP shader '" + m.shader.name + "' on material " + m.name);
                    if (m.HasProperty("_BaseMap"))
                    {
                        var bm = m.GetTexture("_BaseMap");
                        // Building B is intentionally textureless; only warn if a base map is expected.
                    }
                }
            }

            // Material inventory
            var mats = inst.GetComponentsInChildren<Renderer>(true)
                           .SelectMany(r => r.sharedMaterials).Where(m => m != null)
                           .Select(m => m.name).Distinct().OrderBy(s => s);
            L("Materials in use: " + string.Join(", ", mats));

            var cols = inst.GetComponentsInChildren<Collider>(true);
            L("Colliders: " + cols.Length + " (" + string.Join(", ", cols.Select(c => c.GetType().Name)) + ")");

            UnityEngine.Object.DestroyImmediate(inst);
        }

        // ---------------------------------------------------------------- visual preview
        [MenuItem("Joburg Runner/Capture Building Previews")]
        public static void CaptureBuildingPreviews()
        {
            EditorSettings.asyncShaderCompilation = false; // avoid salmon-red placeholder in batch capture
            CaptureOne(BuildingA + "/Prefabs/PF_JHB_Building02.prefab", "Builds/preview_JHB_Building02.png");
            CaptureOne(BuildingB + "/Prefabs/PF_JHB_SkylinePop_02.prefab", "Builds/preview_JHB_SkylinePop_02.png");
        }

        [MenuItem("Joburg Runner/Capture CBD Tile With Landmarks")]
        public static void CaptureCbdTile()
        {
            EditorSettings.asyncShaderCompilation = false;
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var road = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RoadSegment.prefab");
            var tile = (GameObject)PrefabUtility.InstantiatePrefab(road);
            tile.transform.position = Vector3.zero;
            var rv = tile.GetComponent<JoburgRunner.RoadSegmentVisuals>();
            if (rv != null) rv.SetDistrict(0);
            foreach (var lg in tile.GetComponentsInChildren<LODGroup>(true)) lg.ForceLOD(0);

            var lightGO = new GameObject("Sun"); var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.15f;
            lightGO.transform.rotation = Quaternion.Euler(42f, 25f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.66f);

            var camGO = new GameObject("Cam"); var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.7f, 0.8f, 0.9f);
            cam.fieldOfView = 55f; cam.farClipPlane = 300f;

            // Elevated 3/4 from the near end of the tile, looking down the street.
            cam.transform.position = new Vector3(-6f, 14f, -14f);
            cam.transform.LookAt(new Vector3(-6f, 8f, 15f));
            var rt = new RenderTexture(900, 720, 24); cam.targetTexture = rt;
            cam.Render(); cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(900, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0,0,900,720),0,0); tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Builds/preview_cbd_landmarks.png"), tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null;
            L("Captured Builds/preview_cbd_landmarks.png");
        }

        [MenuItem("Joburg Runner/Capture CBDTower Previews")]
        public static void CaptureCbdTowerPreviews()
        {
            EditorSettings.asyncShaderCompilation = false;
            CaptureOne(CbdTower + "/Prefabs/PF_JHB_CBDTower.prefab", "Builds/preview_JHB_CBDTower.png");
            CaptureSides(CbdTower + "/Prefabs/PF_JHB_CBDTower.prefab", "Builds/sides_CBDTower");
        }

        [MenuItem("Joburg Runner/Capture Building Sides")]
        public static void CaptureBuildingSides()
        {
            EditorSettings.asyncShaderCompilation = false;
            CaptureSides(BuildingA + "/Prefabs/PF_JHB_Building02.prefab", "Builds/sides_A");
            CaptureSides(BuildingB + "/Prefabs/PF_JHB_SkylinePop_02.prefab", "Builds/sides_B");
        }

        static void CaptureSides(string prefabPath, string outPrefix)
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = Vector3.zero;
            var lg = go.GetComponent<LODGroup>(); if (lg != null) lg.ForceLOD(0);
            var b = CombinedBounds(go);

            var lightGO = new GameObject("Sun"); var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.1f;
            lightGO.transform.rotation = Quaternion.Euler(40f, 20f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.6f);

            var camGO = new GameObject("Cam"); var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.7f, 0.8f, 0.9f);
            cam.orthographic = true; cam.orthographicSize = b.size.y * 0.55f;
            float dist = b.size.magnitude * 2f;
            cam.nearClipPlane = 0.01f; cam.farClipPlane = dist * 3f;

            (string tag, Vector3 dir)[] views = {
                ("posZ", new Vector3(0,0,1)), ("negZ", new Vector3(0,0,-1)),
                ("posX", new Vector3(1,0,0)), ("negX", new Vector3(-1,0,0)) };
            int W = 480, H = 600;
            foreach (var v in views)
            {
                cam.transform.position = b.center + v.dir * dist;
                cam.transform.LookAt(b.center);
                var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt;
                cam.Render(); cam.Render(); RenderTexture.active = rt;
                var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0,0,W,H),0,0); tex.Apply();
                File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), outPrefix + "_" + v.tag + ".png"), tex.EncodeToPNG());
                RenderTexture.active = null; cam.targetTexture = null;
            }
            L("Captured 4 sides -> " + outPrefix + "_{posZ,negZ,posX,negX}.png");
        }

        static void CaptureOne(string prefabPath, string outPng)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = Vector3.zero;
            var lg = go.GetComponent<LODGroup>(); if (lg != null) lg.ForceLOD(0);

            var b = CombinedBounds(go);

            var lightGO = new GameObject("Sun");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.1f; light.color = Color.white;
            lightGO.transform.rotation = Quaternion.Euler(45f, 30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.6f);

            var camGO = new GameObject("Cam");
            var cam = camGO.AddComponent<Camera>();
            cam.backgroundColor = new Color(0.7f, 0.8f, 0.9f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            float r = b.size.magnitude;
            Vector3 dir = new Vector3(0.8f, 0.35f, -1f).normalized;
            cam.transform.position = b.center + dir * r * 1.15f;
            cam.transform.LookAt(b.center);
            cam.nearClipPlane = 0.1f; cam.farClipPlane = r * 5f;

            int W = 720, H = 900;
            var rt = new RenderTexture(W, H, 24);
            cam.targetTexture = rt;
            cam.Render(); cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), outPng), tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null;
            L("Captured " + outPng);

            // Also log collision renderer disabled state for Building A
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (t.name == "Collision")
                {
                    var mr = t.GetComponentInChildren<MeshRenderer>(true);
                    L("Collision MeshRenderer.enabled = " + (mr != null ? mr.enabled.ToString() : "none"));
                }
        }

        static string Sanitize(string s)
        {
            var sb = new StringBuilder();
            foreach (var ch in s) sb.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
            return sb.ToString();
        }
    }
}
