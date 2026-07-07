using System.Collections.Generic;
using System.IO;
using JoburgRunner;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Builds the Johannesburg Street Run scene to match the reference footage:
    /// a sunny two-way suburban street with yellow lane markings, red-brick
    /// boundary walls, terracotta-roof houses, vendor stalls under rainbow
    /// umbrellas, minibus taxis, and the Hillbrow Tower skyline on the horizon.
    /// </summary>
    public static class EndlessRunnerSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/JoburgEndlessRunner.unity";
        const string RoadPrefabPath = "Assets/Prefabs/RoadSegment.prefab";
        const string TaxiPrefabPath = "Assets/Prefabs/MinibusTaxiObstacle.prefab";
        const string PotholePrefabPath = "Assets/Prefabs/PotholeObstacle.prefab";
        const string BarrierPrefabPath = "Assets/Prefabs/ConstructionBarrier.prefab";
        const string CoinPrefabPath = "Assets/Prefabs/GoldCoin.prefab";
        const string RareCoinPrefabPath = "Assets/Prefabs/RareCoinR5.prefab";
        const string CoinParticlePrefabPath = "Assets/Prefabs/CoinCollectParticles.prefab";
        const string CoinTailsModelPath = "Assets/Models/R1Tails/R1Tails.fbx";
        const string CoinHeadsModelPath = "Assets/Models/R1Heads/R1Heads.fbx";
        const string CoinTailsMaterialPath = "Assets/Models/R1Tails/R1Tails_URP.mat";
        const string CoinHeadsMaterialPath = "Assets/Models/R1Heads/R1Heads_URP.mat";
        const string SkyboxPath = "Assets/Materials/JoburgSkybox.mat";
        const string RunnerAnimatorPath = "Assets/Animations/RunnerAnimator.controller";
        const string RunnerRollClipPath = "Assets/Animations/RunnerRoll.anim";
        const string RunnerIdleClipPath = "Assets/Animations/RunnerIdle.anim";
        const string RunnerJumpClipPath = "Assets/Animations/RunnerJump.anim";
        const string PlayableRunnerModelPath = "Assets/Characters/Meshy_AI_Beaded_Warrior_biped/Meshy_AI_Beaded_Warrior_biped_Animation_Running_withSkin.fbx";
        // Extra clips on the same rig: retargeted onto the runner's avatar.
        const string RunnerJumpModelPath = "Assets/Characters/Meshy_AI_Beaded_Warrior_biped/Meshy_AI_Beaded_Warrior_biped_Animation_Jump_Over_Obstacle_2_withSkin.fbx";
        const string RunnerAirRollModelPath = "Assets/Characters/Meshy_AI_Beaded_Warrior_biped/Meshy_AI_Beaded_Warrior_biped_Animation_Run_Jump_and_Roll_withSkin.fbx";
        const string RunnerIdleModelPath = "Assets/Characters/Meshy_AI_Beaded_Warrior_biped/Meshy_AI_Beaded_Warrior_biped_Animation_Hip_Hop_Dance_2_withSkin.fbx";
        const string VideoSkylineTexturePath = "Assets/Textures/VideoSkyline.png";
        const string VideoSkylineMaterialPath = "Assets/Materials/VideoSkylinePhoto.mat";
        const string TelkomTowerModelPath = "Assets/Environment/TelkomTower/Meshy_AI_Telkom_Tower_Skyline_0705203244_texture_fbx/Meshy_AI_Telkom_Tower_Skyline_0705203244_texture.fbx";
        const string TelkomTowerAlbedoPath = "Assets/Environment/TelkomTower/Meshy_AI_Telkom_Tower_Skyline_0705203244_texture_fbx/Meshy_AI_Telkom_Tower_Skyline_0705203244_texture.png";
        const string TelkomTowerNormalPath = "Assets/Environment/TelkomTower/Meshy_AI_Telkom_Tower_Skyline_0705203244_texture_fbx/Meshy_AI_Telkom_Tower_Skyline_0705203244_texture_normal.png";
        const string TelkomTowerMaterialPath = "Assets/Environment/TelkomTower/TelkomTower_URP.mat";
        const string ApkPath = "Builds/JoburgEndlessRunner.apk";
        const string PreviewPath = "Builds/preview.png";
        const string RunnerPbrMaterialPath = "Assets/Materials/RunnerExternalPbr.mat";
        // Runner is measured at its real extents now, so this is a true world
        // height: ~1.8 units reads as human next to the 2.3-unit taxis and
        // 2.7-unit lane spacing. (It was 6.0 to counter the old inflated-bounds
        // measurement, which never produced a 6-unit runner on screen.)
        const float RunnerVisualTargetHeight = 1.85f;
        const float RunnerGroundSink = 0.35f;
        const float CoinTrailHeight = 1.25f;
        const float CoinArcHeight = 1.7f;
        const float CoinPrefabScale = 1f;
        const float RareCoinPrefabScale = 1.25f;
        const float CoinTriggerRadius = 0.65f;

        static readonly Dictionary<string, Material> Palette = new Dictionary<string, Material>();

        [MenuItem("Joburg Runner/Build Minimum Playable Scene")]
        public static void BuildMinimumPlayableScene()
        {
            EnsureFolders();
            CreatePalette();
            CreateEnvironmentLibraryPrefabs();
            TuneRenderPipelineForMobile();
            CompressTextures();

            GameObject roadPrefab = CreateRoadSegmentPrefab();
            GameObject taxiPrefab = CreateTaxiObstaclePrefab();
            GameObject coinPopPrefab = CreateCoinPopPrefab();
            GameObject coinPrefab = CreateCoinPrefab(coinPopPrefab);
            GameObject rareCoinPrefab = CreateRareCoinPrefab(coinPopPrefab);
            GameObject[] powerUpPrefabs = CreatePowerUpPrefabs();
            TrackChunk[] chunkPrefabs = CreateTrackChunkPrefabs(taxiPrefab, coinPrefab, rareCoinPrefab, powerUpPrefabs);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "JoburgEndlessRunner";

            CreateLighting();
            GameObject player = CreatePlayer();
            CreateCamera(player.transform);
            CreateSkyline(player.transform);
            // Taxis are the only obstacle: they come head-on across the three lanes.
            CreateSystems(player.transform, roadPrefab, chunkPrefabs);
            CreateUi(player.GetComponent<PlayerController>());

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Joburg Runner/Build Scene And Capture Preview")]
        public static void BuildSceneAndCapturePreview()
        {
            BuildMinimumPlayableScene();
            CapturePreviewScreenshot();
        }

        [MenuItem("Joburg Runner/Capture Preview Screenshot")]
        public static void CapturePreviewScreenshot()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("No main camera found for preview capture.");
                return;
            }

            bool previousAsyncCompilation = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;

            // Overlay canvases composite straight to the screen and are
            // invisible to camera.Render(); point them at the camera while
            // capturing so the UI lands in the screenshot.
            List<Canvas> overlayCanvases = new List<Canvas>();
            foreach (Canvas sceneCanvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (sceneCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    sceneCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    sceneCanvas.worldCamera = camera;
                    sceneCanvas.planeDistance = 1f;
                    overlayCanvases.Add(sceneCanvas);
                }
            }

            Canvas.ForceUpdateCanvases();

            const int width = 720;
            const int height = 1280;
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            camera.targetTexture = renderTexture;
            camera.aspect = (float)width / height;
            camera.Render();
            camera.Render();

            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();

            camera.targetTexture = null;
            camera.ResetAspect();
            RenderTexture.active = null;
            EditorSettings.asyncShaderCompilation = previousAsyncCompilation;

            foreach (Canvas sceneCanvas in overlayCanvases)
            {
                sceneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            Directory.CreateDirectory("Builds");
            File.WriteAllBytes(PreviewPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(renderTexture);
            Debug.Log($"Preview written to {PreviewPath}");
        }

        [MenuItem("Joburg Runner/Debug Runner Model Assets")]
        public static void DebugRunnerModelAssets()
        {
            string path = FindRunnerModelPath();
            Debug.Log($"Runner model path: {path}");
            if (path == null)
            {
                return;
            }

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                string detail = asset is Material material ? $" shader={material.shader.name} mainTex={(material.mainTexture ? material.mainTexture.name : "none")}" : "";
                Debug.Log($"ModelSubAsset: {asset.GetType().Name} '{asset.name}'{detail}");
            }
        }

        [MenuItem("Joburg Runner/Capture Taxi Closeup")]
        public static void CaptureTaxiCloseup()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            CreatePalette();
            GameObject holder = new GameObject("TaxiCloseupHolder");
            GameObject taxi = FbxVehicleInstance("taxi", holder.transform, new Vector3(0f, 0f, 7f), 180f, 2.3f);
            if (taxi == null)
            {
                Debug.LogError("taxi.fbx not found for closeup.");
                Object.DestroyImmediate(holder);
                return;
            }

            CapturePreviewScreenshot();
            Object.DestroyImmediate(holder);
        }

        [MenuItem("Joburg Runner/Capture Coin Closeup")]
        public static void CaptureCoinCloseup()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Coin prefab not found at {CoinPrefabPath}.");
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("No main camera found for coin closeup.");
                return;
            }

            GameObject holder = new GameObject("CoinCloseupHolder");
            // Left coin shows the camera-facing side, right coin is spun
            // half a turn to show the far side.
            GameObject front = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            GameObject back = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            Vector3 anchor = camera.transform.position + camera.transform.forward * 3.4f;
            front.transform.position = anchor + camera.transform.right * -0.55f;
            back.transform.position = anchor + camera.transform.right * 0.55f;
            back.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            CapturePreviewScreenshot();
            Object.DestroyImmediate(holder);
        }

        public static void BuildSceneAndCaptureCoinCloseup()
        {
            BuildMinimumPlayableScene();
            CapturePreviewScreenshot();
            File.Copy(PreviewPath, "Builds/preview_scene.png", true);
            CaptureCoinCloseup();
        }

        [MenuItem("Joburg Runner/Capture Game Over Preview")]
        public static void CaptureGameOverPreview()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject canvas = GameObject.Find("RunnerCanvas");
            Transform panel = canvas != null ? canvas.transform.Find("GameOverPanel") : null;
            if (panel == null)
            {
                Debug.LogError("GameOverPanel not found for preview.");
                return;
            }

            TextMeshProUGUI finalScore = panel.GetComponentInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI label in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label.name == "FinalScoreText")
                {
                    finalScore = label;
                }
            }

            finalScore.text =
                "<size=44><color=#AEB4C2>FINAL SCORE</color></size>\n" +
                "<size=130><b>4820</b></size>\n" +
                "<size=46><color=#FFC845>New best score!</color></size>\n" +
                "<size=38><color=#AEB4C2>Distance 0.96 km      Coins 32  (total 118)</color></size>";

            panel.gameObject.SetActive(true);
            CapturePreviewScreenshot();
            panel.gameObject.SetActive(false);
        }

        public static void BuildSceneAndCaptureUiPreviews()
        {
            BuildMinimumPlayableScene();
            CapturePreviewScreenshot();
            File.Copy(PreviewPath, "Builds/preview_scene.png", true);
            CaptureGameOverPreview();
            File.Copy(PreviewPath, "Builds/preview_gameover.png", true);
        }

        [MenuItem("Joburg Runner/Capture Menu Preview")]
        public static void CaptureMenuPreview() => CaptureCanvasPanel("MenuPanel");

        [MenuItem("Joburg Runner/Capture Store Preview")]
        public static void CaptureStorePreview() => CaptureCanvasPanel("StorePanel");

        [MenuItem("Joburg Runner/Capture Collectables Preview")]
        public static void CaptureCollectablesPreview() => CaptureCanvasPanel("CollectablesPanel");

        static void CaptureCanvasPanel(string panelName)
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject canvas = GameObject.Find("RunnerCanvas");
            Transform panel = canvas != null ? canvas.transform.Find(panelName) : null;
            if (panel == null)
            {
                Debug.LogError($"{panelName} not found for preview.");
                return;
            }

            panel.gameObject.SetActive(true);
            CapturePreviewScreenshot();
            panel.gameObject.SetActive(false);
        }

        [MenuItem("Joburg Runner/Capture Pickups Closeup")]
        public static void CapturePickupsCloseup()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("No main camera found for pickups closeup.");
                return;
            }

            string[] paths =
            {
                "Assets/Prefabs/PowerUpTaxiMagnet.prefab",
                "Assets/Prefabs/PowerUpJoziSneakers.prefab",
                "Assets/Prefabs/PowerUpDroneBoost.prefab",
                "Assets/Prefabs/PowerUpUbuntuMultiplier.prefab",
                "Assets/Prefabs/PowerUpHoverboard.prefab",
                RareCoinPrefabPath,
            };

            GameObject holder = new GameObject("PickupsCloseupHolder");
            for (int i = 0; i < paths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                if (prefab == null)
                {
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
                Vector3 anchor = camera.transform.position + camera.transform.forward * 4.5f;
                float column = i % 2 == 0 ? -1.1f : 1.1f;
                float rowOffset = 0.7f - i / 2 * 1.1f;
                instance.transform.position = anchor + camera.transform.right * column + camera.transform.up * rowOffset;
            }

            CapturePreviewScreenshot();
            Object.DestroyImmediate(holder);
        }

        public static void BuildSceneAndCaptureAllPreviews()
        {
            BuildMinimumPlayableScene();
            CapturePreviewScreenshot();
            File.Copy(PreviewPath, "Builds/preview_scene.png", true);
            CaptureGameOverPreview();
            File.Copy(PreviewPath, "Builds/preview_gameover.png", true);
            CaptureMenuPreview();
            File.Copy(PreviewPath, "Builds/preview_menu.png", true);
            CaptureStorePreview();
            File.Copy(PreviewPath, "Builds/preview_store.png", true);
            CaptureCollectablesPreview();
            File.Copy(PreviewPath, "Builds/preview_collectables.png", true);
            CapturePickupsCloseup();
            File.Copy(PreviewPath, "Builds/preview_pickups.png", true);
        }

        [MenuItem("Joburg Runner/Build Android APK")]
        public static void BuildAndroidApk()
        {
            BuildMinimumPlayableScene();
            Directory.CreateDirectory("Builds");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
        }

        static void TuneRenderPipelineForMobile()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (pipeline == null)
                {
                    continue;
                }

                pipeline.shadowDistance = 40f;
                pipeline.renderScale = 0.8f;
                pipeline.supportsHDR = false;
                pipeline.msaaSampleCount = 2;
                EditorUtility.SetDirty(pipeline);
            }
        }

        static void CompressTextures()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            {
                return;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                if (textureImporter == null)
                {
                    continue;
                }

                bool dirty = false;
                if (textureImporter.maxTextureSize > 1024)
                {
                    textureImporter.maxTextureSize = 1024;
                    dirty = true;
                }

                if (textureImporter.textureCompression != TextureImporterCompression.Compressed)
                {
                    textureImporter.textureCompression = TextureImporterCompression.Compressed;
                    dirty = true;
                }

                if (dirty)
                {
                    textureImporter.SaveAndReimport();
                }
            }
        }

        /// <summary>
        /// Decimates all meshes under the root to a shared triangle budget so
        /// high-poly source models run smoothly on mobile. Simplified meshes are
        /// cached as assets, so repeat builds are fast.
        /// </summary>
        static void SimplifyMeshes(GameObject root, int targetTriangles)
        {
            int total = CountTriangles(root);
            if (total <= targetTriangles)
            {
                return;
            }

            float ratio = (float)targetTriangles / total;
            foreach (MeshFilter meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh != null)
                {
                    meshFilter.sharedMesh = GetSimplifiedMesh(meshFilter.sharedMesh, ratio);
                }
            }

            foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned.sharedMesh != null)
                {
                    skinned.sharedMesh = GetSimplifiedMesh(skinned.sharedMesh, ratio);
                }
            }

            Debug.Log($"SimplifyMeshes {root.name}: {total} -> {CountTriangles(root)} triangles");
        }

        static Mesh GetSimplifiedMesh(Mesh source, float ratio)
        {
            int sourceTriangles = (int)(source.triangles.Length / 3);
            int target = Mathf.Max(64, (int)(sourceTriangles * ratio));
            if (sourceTriangles <= target)
            {
                return source;
            }

            Directory.CreateDirectory("Assets/Meshes");
            string path = $"Assets/Meshes/{source.name}_{source.vertexCount}_{target}.asset";
            Mesh cached = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (cached != null)
            {
                return cached;
            }

            var simplifier = new UnityMeshSimplifier.MeshSimplifier();
            simplifier.Initialize(source);
            simplifier.SimplifyMesh(Mathf.Clamp01(ratio));
            Mesh simplified = simplifier.ToMesh();
            simplified.name = $"{source.name}_simplified";
            simplified.bindposes = source.bindposes;

            // Collapsed normals shade badly; rebuild them from the new topology.
            simplified.RecalculateNormals();
            simplified.RecalculateTangents();
            AssetDatabase.CreateAsset(simplified, path);
            return simplified;
        }

        static int CountTriangles(GameObject root)
        {
            int triangles = 0;
            foreach (MeshFilter meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh != null)
                {
                    triangles += (int)(meshFilter.sharedMesh.triangles.Length / 3);
                }
            }

            foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned.sharedMesh != null)
                {
                    triangles += (int)(skinned.sharedMesh.triangles.Length / 3);
                }
            }

            return triangles;
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Scripts");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Prefabs/Environment");
            Directory.CreateDirectory("Assets/Materials");
            Directory.CreateDirectory("Assets/Scenes");
        }

        static void CreatePalette()
        {
            Palette.Clear();

            // Street surfaces
            Pal("RoadAsphalt", new Color(0.24f, 0.24f, 0.245f));
            Pal("Asphalt", new Color(0.17f, 0.17f, 0.16f));
            Pal("AsphaltCrack", new Color(0.1f, 0.1f, 0.105f));
            Pal("PaintYellow", new Color(0.95f, 0.72f, 0.1f));
            Pal("PaintWhite", new Color(0.92f, 0.92f, 0.88f));
            Pal("RoadMarkingWhite", new Color(0.94f, 0.94f, 0.9f));
            Pal("SidewalkConcrete", new Color(0.63f, 0.61f, 0.56f));
            Pal("Concrete", new Color(0.58f, 0.58f, 0.54f));
            Pal("Pavement", new Color(0.52f, 0.5f, 0.45f));
            Pal("BrickPaving", new Color(0.6f, 0.29f, 0.19f));
            Pal("KerbStone", new Color(0.56f, 0.56f, 0.53f));

            // Walls, houses, street furniture
            Pal("BrickWallRed", new Color(0.55f, 0.22f, 0.15f));
            Pal("Brick", new Color(0.48f, 0.2f, 0.14f));
            Pal("BrickWallTan", new Color(0.73f, 0.47f, 0.25f));
            Pal("PlasterCream", new Color(0.85f, 0.77f, 0.6f));
            Pal("PlasterOrange", new Color(0.8f, 0.55f, 0.3f));
            Pal("RoofTerracotta", new Color(0.67f, 0.28f, 0.14f));
            Pal("PoleGrey", new Color(0.6f, 0.62f, 0.64f));
            Pal("Metal", new Color(0.38f, 0.39f, 0.38f));
            Pal("MetalDark", new Color(0.08f, 0.09f, 0.09f));
            Pal("WoodPole", new Color(0.3f, 0.2f, 0.12f));
            Pal("WireBlack", new Color(0.03f, 0.03f, 0.03f));
            Pal("TreeGreen", new Color(0.3f, 0.5f, 0.25f));
            Pal("Leaves", new Color(0.2f, 0.42f, 0.16f));
            Pal("TreeTrunk", new Color(0.32f, 0.2f, 0.1f));
            Pal("TreeBark", new Color(0.31f, 0.2f, 0.11f));
            Pal("ShrubGreen", new Color(0.15f, 0.38f, 0.12f));
            Pal("Grass", new Color(0.24f, 0.42f, 0.19f));

            // Skyline
            Pal("TowerConcrete", new Color(0.74f, 0.75f, 0.77f));
            Pal("AntennaRed", new Color(0.8f, 0.15f, 0.1f));
            Pal("SkyscraperGlass", new Color(0.45f, 0.6f, 0.72f));
            Pal("Glass", new Color(0.26f, 0.46f, 0.58f));
            Pal("GlassDark", new Color(0.12f, 0.23f, 0.3f));
            Pal("SkyscraperConcrete", new Color(0.63f, 0.65f, 0.68f));
            Pal("SkylineHaze", new Color(0.7f, 0.76f, 0.83f));

            // Vehicles
            Pal("TaxiYellow", new Color(1f, 0.75f, 0.05f));
            Pal("TaxiWhite", new Color(0.92f, 0.92f, 0.9f));
            Pal("CheckerBlack", new Color(0.05f, 0.05f, 0.05f));
            Pal("HazardRed", new Color(0.85f, 0.12f, 0.08f));
            Pal("GlassWindow", new Color(0.35f, 0.5f, 0.6f));
            Pal("WheelBlack", new Color(0.06f, 0.06f, 0.06f));

            // Vendors and signage
            Pal("UmbrellaRed", new Color(0.85f, 0.15f, 0.12f));
            Pal("UmbrellaYellow", new Color(0.95f, 0.8f, 0.15f));
            Pal("UmbrellaBlue", new Color(0.2f, 0.35f, 0.75f));
            Pal("CrateWood", new Color(0.6f, 0.42f, 0.22f));
            Pal("FruitOrange", new Color(0.95f, 0.55f, 0.1f));
            Pal("SignRed", new Color(0.8f, 0.1f, 0.08f));
            Pal("SignGreen", new Color(0.02f, 0.24f, 0.16f));
            Pal("SignWhite", new Color(0.95f, 0.95f, 0.95f));
            Pal("SignYellow", new Color(0.92f, 0.62f, 0.05f));
            Pal("SignBlack", new Color(0.05f, 0.05f, 0.05f));
            Pal("ShopAwningRed", new Color(0.66f, 0.08f, 0.06f));
            Pal("ShopAwningGreen", new Color(0.02f, 0.33f, 0.22f));
            Pal("ShopAwningBlue", new Color(0.08f, 0.18f, 0.48f));
            Pal("MuralBlue", new Color(0.02f, 0.38f, 0.62f));
            Pal("MuralMagenta", new Color(0.72f, 0.08f, 0.45f));
            Pal("MuralOrange", new Color(0.9f, 0.35f, 0.06f));
            Pal("CloudWhite", new Color(1f, 1f, 0.96f));

            // Obstacles and pickups
            Pal("PotholeDarkAsphalt", new Color(0.025f, 0.02f, 0.018f));
            Pal("ConstructionBarrierOrange", new Color(1f, 0.38f, 0.05f));
            Pal("CoinGold", new Color(1f, 0.78f, 0.08f), true);
            Pal("PowerRed", new Color(0.92f, 0.2f, 0.15f), true);
            Pal("PowerBlue", new Color(0.25f, 0.65f, 1f), true);
            Pal("PowerPurple", new Color(0.62f, 0.42f, 1f), true);
            Pal("PowerGreen", new Color(0.25f, 0.85f, 0.5f), true);
            Pal("SneakerWhite", new Color(0.95f, 0.95f, 0.95f));

            // Runner
            Pal("RunnerSkin", new Color(0.85f, 0.6f, 0.44f));
            Pal("HairBrown", new Color(0.2f, 0.12f, 0.06f));
            Pal("VestGreen", new Color(0.09f, 0.5f, 0.28f));
            Pal("VestWhite", new Color(0.92f, 0.92f, 0.9f));
            Pal("ShortsBlack", new Color(0.05f, 0.05f, 0.06f));
            Pal("ShoeOrange", new Color(0.95f, 0.4f, 0.08f));
            Pal("ShoeWhite", new Color(0.93f, 0.93f, 0.9f));

            // Pedestrians
            Pal("ShirtBlue", new Color(0.25f, 0.4f, 0.7f));
            Pal("ShirtRed", new Color(0.75f, 0.2f, 0.15f));
            Pal("ShirtYellow", new Color(0.9f, 0.75f, 0.2f));
            Pal("DenimBlue", new Color(0.2f, 0.25f, 0.4f));
        }

        static void Pal(string name, Color color, bool emissive = false)
        {
            Palette[name] = CreateMaterial(name, color, emissive);
        }

        static Material Mat(string name) => Palette[name];

        static Material CreateMaterial(string name, Color color, bool emissive = false)
        {
            string path = $"Assets/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = name;
            material.color = color;
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.4f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        static void CreateEnvironmentLibraryPrefabs()
        {
            SaveEnvironmentPrefab("BuildingSmall", root => Building(root, Vector3.zero, new Vector3(4.5f, 6f, 5f), Mat("Brick"), BuildingStyle.BrickApartment, "FLATS"));
            SaveEnvironmentPrefab("BuildingMedium", root => Building(root, Vector3.zero, new Vector3(5.5f, 10f, 5.5f), Mat("Concrete"), BuildingStyle.Office, "OFFICES"));
            SaveEnvironmentPrefab("BuildingTall", root => Building(root, Vector3.zero, new Vector3(6f, 18f, 6f), Mat("Glass"), BuildingStyle.GlassTower, "TOWER"));
            SaveEnvironmentPrefab("Shop", root => Shopfront(root, Vector3.zero, 0f, "CAFE", Mat("ShopAwningGreen")));
            SaveEnvironmentPrefab("Tree01", root => PavementTree(root, Vector3.zero, 1f, 0));
            SaveEnvironmentPrefab("Tree02", root => PavementTree(root, Vector3.zero, 0.85f, 1));
            SaveEnvironmentPrefab("StreetLight", root => StreetLight(root, Vector3.zero, 1f));
            SaveEnvironmentPrefab("Bench", root => Bench(root, Vector3.zero, 0f));
            SaveEnvironmentPrefab("Bin", root => PublicBin(root, Vector3.zero));
            SaveEnvironmentPrefab("BusStop", root => BusStop(root, Vector3.zero, 0f));
            SaveEnvironmentPrefab("RoadSign", root => JohannesburgRoadSign(root, new Vector3(0f, 0f, 0f), 0f, "JOHANNESBURG", "M1 City", "M2 Newtown"));
            SaveEnvironmentPrefab("TrafficLight", root => TrafficLight(root, Vector3.zero, 0f));
            SaveEnvironmentPrefab("Pavement", root => PavementSection(root, Vector3.zero, 0f));
            SaveEnvironmentPrefab("SidewalkSection", root => PavementSection(root, Vector3.zero, 0f));
        }

        static void SaveEnvironmentPrefab(string name, System.Action<Transform> build)
        {
            string path = $"Assets/Prefabs/Environment/{name}.prefab";
            GameObject root = new GameObject(name);
            build(root.transform);
            StripColliders(root);
            SavePrefab(root, path);
        }

        // ------------------------------------------------------------------
        // Road segment
        // ------------------------------------------------------------------

        static GameObject CreateRoadSegmentPrefab()
        {
            GameObject root = new GameObject("RoadSegment");
            RoadSegmentVisuals visuals = root.AddComponent<RoadSegmentVisuals>();
            Transform t = root.transform;

            Transform road = Category("Road", t);
            Transform sidewalks = Category("Sidewalks", t);
            Transform buildings = Category("Buildings", t);
            Transform trees = Category("Trees", t);
            Transform props = Category("Props", t);
            Transform signs = Category("Signs", t);

            BuildRoadSurface(road);
            BuildSidewalks(sidewalks);
            BuildStreetFurniture(props, signs);
            BuildVegetation(trees);

            GameObject[] districts =
            {
                District("CBDOfficeStreet", buildings),
                District("RetailCafeStreet", buildings),
                District("BrickMuralStreet", buildings),
                District("ApartmentMixedUseStreet", buildings)
            };

            BuildSuburbanStreet(districts[0].transform);
            BuildVendorMarketStreet(districts[1].transform);
            BuildTaxiStreet(districts[2].transform);
            BuildMixedCommercialStreet(districts[3].transform);

            foreach (GameObject district in districts)
            {
                AddDistrictLod(district);
            }

            StripColliders(buildings.gameObject);
            StripColliders(trees.gameObject);
            StripColliders(props.gameObject);
            StripColliders(signs.gameObject);

            SetField(visuals, "districtRoots", districts);
            visuals.SetDistrict(0);

            return SavePrefab(root, RoadPrefabPath);
        }

        static Transform Category(string name, Transform parent)
        {
            GameObject category = new GameObject(name);
            category.transform.SetParent(parent);
            category.transform.localPosition = Vector3.zero;
            return category.transform;
        }

        static void BuildRoadSurface(Transform road)
        {
            Cube("ThreeLaneAsphalt", road, new Vector3(0f, -0.08f, 15f), new Vector3(10.8f, 0.16f, 30f), Mat("Asphalt"));
            Cube("LeftYellowShoulderLine", road, new Vector3(-5.18f, 0.055f, 15f), new Vector3(0.12f, 0.045f, 30f), Mat("PaintYellow"));
            Cube("RightYellowShoulderLine", road, new Vector3(5.18f, 0.055f, 15f), new Vector3(0.12f, 0.045f, 30f), Mat("PaintYellow"));

            for (int z = 2; z < 30; z += 6)
            {
                Cube("WhiteDashedLaneMark_Left", road, new Vector3(-1.35f, 0.06f, z), new Vector3(0.12f, 0.045f, 2.45f), Mat("RoadMarkingWhite"));
                Cube("WhiteDashedLaneMark_Right", road, new Vector3(1.35f, 0.06f, z + 3f), new Vector3(0.12f, 0.045f, 2.45f), Mat("RoadMarkingWhite"));
            }

            for (int z = 4; z < 30; z += 10)
            {
                StormDrain(road, new Vector3(-5.05f, 0.075f, z), 0f);
                StormDrain(road, new Vector3(5.05f, 0.075f, z + 4f), 180f);
            }

            for (int z = 8; z < 30; z += 12)
            {
                Cylinder("RoundDrainCover", road, new Vector3(3.4f, 0.062f, z), new Vector3(0.55f, 0.014f, 0.55f), Mat("MetalDark"));
            }

            AsphaltCracks(road);
        }

        static void BuildSidewalks(Transform sidewalks)
        {
            Cube("LeftConcreteSidewalk", sidewalks, new Vector3(-7.35f, 0f, 15f), new Vector3(3.5f, 0.22f, 30f), Mat("Pavement"));
            Cube("RightConcreteSidewalk", sidewalks, new Vector3(7.35f, 0f, 15f), new Vector3(3.5f, 0.22f, 30f), Mat("SidewalkConcrete"));
            Cube("LeftCurb", sidewalks, new Vector3(-5.48f, 0.12f, 15f), new Vector3(0.38f, 0.28f, 30f), Mat("KerbStone"));
            Cube("RightCurb", sidewalks, new Vector3(5.48f, 0.12f, 15f), new Vector3(0.38f, 0.28f, 30f), Mat("KerbStone"));
            Cube("LeftGrassStrip", sidewalks, new Vector3(-9.15f, 0.03f, 15f), new Vector3(0.55f, 0.08f, 30f), Mat("Grass"));
            Cube("RightGrassStrip", sidewalks, new Vector3(9.15f, 0.03f, 15f), new Vector3(0.55f, 0.08f, 30f), Mat("Grass"));

            for (int z = 2; z < 30; z += 4)
            {
                Cube("PavementExpansionJoint_Left", sidewalks, new Vector3(-7.35f, 0.13f, z), new Vector3(3.25f, 0.02f, 0.05f), Mat("Concrete"));
                Cube("PavementExpansionJoint_Right", sidewalks, new Vector3(7.35f, 0.13f, z + 1f), new Vector3(3.25f, 0.02f, 0.05f), Mat("Concrete"));
            }
        }

        static void BuildStreetFurniture(Transform props, Transform signs)
        {
            for (int z = 4; z < 30; z += 9)
            {
                StreetLight(props, new Vector3(-5.95f, 0f, z), 1f);
                StreetLight(props, new Vector3(5.95f, 0f, z + 4.5f), -1f);
                BollardRow(props, -5.85f, z + 2f);
                BollardRow(props, 5.85f, z + 5f);
            }

            Bench(props, new Vector3(-7.5f, 0.12f, 7f), 90f);
            Bench(props, new Vector3(7.45f, 0.12f, 22f), -90f);
            PublicBin(props, new Vector3(-6.25f, 0.15f, 11f));
            PublicBin(props, new Vector3(6.25f, 0.15f, 25f));
            NewspaperBox(props, new Vector3(-6.8f, 0.1f, 18f));
            Mailbox(props, new Vector3(6.7f, 0.1f, 12f));
            ElectricalCabinet(props, new Vector3(-8.6f, 0.1f, 23f));
            FireHydrant(props, new Vector3(6.2f, 0.1f, 6f));
            ParkingMeter(props, new Vector3(-5.95f, 0.1f, 26f));
            BusStop(props, new Vector3(7.4f, 0.1f, 14f), 180f);
            TrafficLight(signs, new Vector3(-5.9f, 0f, 2.5f), 90f);
            TrafficLight(signs, new Vector3(5.9f, 0f, 27f), -90f);
            JohannesburgRoadSign(signs, new Vector3(-7.4f, 0f, 5f), 8f, "JOHANNESBURG", "M1 City", "M2 Newtown");
            JohannesburgRoadSign(signs, new Vector3(7.35f, 0f, 23f), -8f, "JEPPE ST", "M31 East", "M2 Newtown");
            StreetNameSign(signs, new Vector3(-6.0f, 0f, 21f), "JOUBERT ST");
        }

        static void BuildVegetation(Transform trees)
        {
            for (int i = 0; i < 4; i++)
            {
                float z = 5f + i * 7f;
                PavementTree(trees, new Vector3(-8.7f, 0f, z), 0.85f + (i % 2) * 0.18f, i);
                PavementTree(trees, new Vector3(8.7f, 0f, z + 3.5f), 0.9f + (i % 3) * 0.12f, i + 1);
                ShrubCluster(trees, new Vector3(-9.2f, 0.18f, z + 2f), i);
                Planter(trees, new Vector3(9.1f, 0.22f, z - 1f));
            }
        }

        static void AsphaltCracks(Transform parent)
        {
            Cube("AsphaltCrack", parent, new Vector3(-2.1f, 0.045f, 6f), new Vector3(0.18f, 0.02f, 3.2f), Mat("AsphaltCrack"), new Vector3(0f, 12f, 0f));
            Cube("AsphaltCrack", parent, new Vector3(1.7f, 0.045f, 13f), new Vector3(0.14f, 0.02f, 2.6f), Mat("AsphaltCrack"), new Vector3(0f, -18f, 0f));
            Cube("AsphaltCrack", parent, new Vector3(-0.6f, 0.045f, 21f), new Vector3(0.2f, 0.02f, 4f), Mat("AsphaltCrack"), new Vector3(0f, 7f, 0f));
            Cube("AsphaltCrack", parent, new Vector3(3.2f, 0.045f, 26f), new Vector3(0.12f, 0.02f, 2.2f), Mat("AsphaltCrack"), new Vector3(0f, -9f, 0f));
            Cube("AsphaltCrack", parent, new Vector3(-4f, 0.045f, 17f), new Vector3(0.16f, 0.02f, 2.8f), Mat("AsphaltCrack"), new Vector3(0f, 15f, 0f));

            GameObject patchA = Cylinder("TarRepairPatch", parent, new Vector3(2.6f, 0.043f, 9f), new Vector3(1.6f, 0.012f, 1.1f), Mat("AsphaltCrack"));
            GameObject patchB = Cylinder("TarRepairPatch", parent, new Vector3(-3.1f, 0.043f, 24f), new Vector3(1.2f, 0.012f, 0.9f), Mat("AsphaltCrack"));
            patchA.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            patchB.transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
        }

        static void SuburbanBoundaryWalls(Transform parent)
        {
            Cube("LeftRedBrickBoundaryWall", parent, new Vector3(-9.2f, 1.05f, 15f), new Vector3(0.34f, 2.1f, 30f), Mat("BrickWallRed"));
            Cube("RightTanBrickBoundaryWall", parent, new Vector3(9.2f, 1.05f, 15f), new Vector3(0.34f, 2.1f, 30f), Mat("BrickWallTan"));
            for (int z = 3; z < 30; z += 6)
            {
                Cube("BoundaryWallPillar_Left", parent, new Vector3(-9.2f, 1.25f, z), new Vector3(0.52f, 2.5f, 0.52f), Mat("BrickWallRed"));
                Cube("BoundaryWallPillar_Right", parent, new Vector3(9.2f, 1.25f, z + 2f), new Vector3(0.52f, 2.5f, 0.52f), Mat("BrickWallTan"));
            }
        }

        static void PowerPoleLine(Transform parent)
        {
            foreach (float z in new[] { 5f, 23f })
            {
                Cylinder("WoodenPowerPole", parent, new Vector3(-6.45f, 2.6f, z), new Vector3(0.14f, 2.6f, 0.14f), Mat("WoodPole"));
                Cube("PowerPoleCrossarm_Upper", parent, new Vector3(-6.45f, 4.9f, z), new Vector3(1.5f, 0.09f, 0.09f), Mat("WoodPole"));
                Cube("PowerPoleCrossarm_Lower", parent, new Vector3(-6.45f, 4.5f, z), new Vector3(1.1f, 0.08f, 0.08f), Mat("WoodPole"));
            }

            Cube("PowerLine_A", parent, new Vector3(-7f, 4.88f, 15f), new Vector3(0.02f, 0.02f, 30f), Mat("WireBlack"));
            Cube("PowerLine_B", parent, new Vector3(-5.9f, 4.88f, 15f), new Vector3(0.02f, 0.02f, 30f), Mat("WireBlack"));
            Cube("PowerLine_C", parent, new Vector3(-6.85f, 4.48f, 15f), new Vector3(0.02f, 0.02f, 30f), Mat("WireBlack"));
            Cube("PowerLine_D", parent, new Vector3(-6.05f, 4.48f, 15f), new Vector3(0.02f, 0.02f, 30f), Mat("WireBlack"));

            // Service wires crossing above the road like the reference footage
            Cube("RoadCrossingWire_A", parent, new Vector3(0f, 5.05f, 8f), new Vector3(13f, 0.02f, 0.02f), Mat("WireBlack"), new Vector3(0f, 6f, 2f));
            Cube("RoadCrossingWire_B", parent, new Vector3(0f, 4.85f, 22f), new Vector3(13f, 0.02f, 0.02f), Mat("WireBlack"), new Vector3(0f, -5f, -2f));
        }

        static void StreetLight(Transform parent, Vector3 position, float sideTowardsRoad)
        {
            Cylinder("MunicipalStreetLight_Pole", parent, position + new Vector3(0f, 2.3f, 0f), new Vector3(0.08f, 2.3f, 0.08f), Mat("PoleGrey"));
            Cube("MunicipalStreetLight_Arm", parent, position + new Vector3(sideTowardsRoad * 0.5f, 4.55f, 0f), new Vector3(1f, 0.07f, 0.07f), Mat("PoleGrey"));
            Cube("MunicipalStreetLight_Lamp", parent, position + new Vector3(sideTowardsRoad * 0.95f, 4.5f, 0f), new Vector3(0.45f, 0.14f, 0.22f), Mat("PaintWhite"));
        }

        static void TreeCanopy(Transform parent, Vector3 position)
        {
            Cylinder("StreetTree_Trunk", parent, position + new Vector3(0f, 1.4f, 0f), new Vector3(0.18f, 1.4f, 0.18f), Mat("TreeTrunk"));
            Sphere("StreetTree_Canopy", parent, position + new Vector3(0f, 3.2f, 0f), new Vector3(2.2f, 1.7f, 2.2f), Mat("TreeGreen"));
        }

        // ------------------------------------------------------------------
        // Districts — all variants of the same sunny Joburg street
        // ------------------------------------------------------------------

        static GameObject District(string name, Transform parent)
        {
            GameObject district = new GameObject(name);
            district.transform.SetParent(parent);
            district.transform.localPosition = Vector3.zero;
            return district;
        }

        static void AddDistrictLod(GameObject district)
        {
            Renderer[] renderers = district.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            LODGroup lodGroup = district.AddComponent<LODGroup>();
            lodGroup.SetLODs(new[]
            {
                new LOD(0.35f, renderers),
                new LOD(0.08f, renderers)
            });
            lodGroup.RecalculateBounds();
        }

        static void BuildSuburbanStreet(Transform parent)
        {
            Building(parent, new Vector3(-12.4f, 0f, 5f), new Vector3(5.5f, 12f, 6f), Mat("Concrete"), BuildingStyle.Office, "CITY OFFICES");
            Building(parent, new Vector3(-12.2f, 0f, 14f), new Vector3(5f, 8.5f, 5f), Mat("Brick"), BuildingStyle.BrickApartment, "LOFTS");
            Building(parent, new Vector3(-12.5f, 0f, 24f), new Vector3(6f, 16f, 6f), Mat("Glass"), BuildingStyle.GlassTower, "BANK");
            Building(parent, new Vector3(12.3f, 0f, 4f), new Vector3(5f, 9f, 5f), Mat("BrickWallTan"), BuildingStyle.Office, "WORKHUB");
            Building(parent, new Vector3(12.5f, 0f, 13f), new Vector3(5.8f, 14f, 6f), Mat("GlassDark"), BuildingStyle.GlassTower, "TOWER");
            Building(parent, new Vector3(12.1f, 0f, 24f), new Vector3(5f, 11f, 5f), Mat("SkyscraperConcrete"), BuildingStyle.Office, "CITY");
            JohannesburgRoadSign(parent, new Vector3(-8.0f, 0f, 17f), 0f, "JOHANNESBURG", "M1 City", "M2 Newtown");
            RoadsideBillboard(parent, new Vector3(7.0f, 0f, 15f), -20f, "HF_Taxi");
            RoadsideBillboard(parent, new Vector3(-7.0f, 0f, 11f), 20f, "HF_Skyline");
        }

        static void BuildVendorMarketStreet(Transform parent)
        {
            Shopfront(parent, new Vector3(-11.7f, 0f, 4f), 90f, "CAFE", Mat("ShopAwningGreen"));
            Shopfront(parent, new Vector3(-11.7f, 0f, 11f), 90f, "BARBER", Mat("ShopAwningBlue"));
            Shopfront(parent, new Vector3(-11.7f, 0f, 18f), 90f, "CORNER STORE", Mat("ShopAwningRed"));
            Shopfront(parent, new Vector3(11.7f, 0f, 6f), -90f, "RESTAURANT", Mat("ShopAwningRed"));
            Shopfront(parent, new Vector3(11.7f, 0f, 15f), -90f, "CAFE", Mat("ShopAwningGreen"));
            Shopfront(parent, new Vector3(11.7f, 0f, 24f), -90f, "HAIR SALON", Mat("ShopAwningBlue"));
            VendorStall(parent, new Vector3(-7.1f, 0f, 22f));
            VendorStall(parent, new Vector3(7.1f, 0f, 10f));
            BusStop(parent, new Vector3(7.2f, 0.1f, 20f), 180f);
            RoadsideBillboard(parent, new Vector3(-7.0f, 0f, 12f), 20f, "HF_Street");
        }

        static void BuildTaxiStreet(Transform parent)
        {
            MuralWall(parent, new Vector3(-10.9f, 0f, 8f), 90f, "JOZI RUNNER");
            MuralWall(parent, new Vector3(10.9f, 0f, 20f), -90f, "JOZI MY HOME");
            Building(parent, new Vector3(-12.4f, 0f, 22f), new Vector3(5f, 8f, 5f), Mat("Brick"), BuildingStyle.BrickApartment, "SOWETO IS US");
            Building(parent, new Vector3(12.4f, 0f, 7f), new Vector3(5.6f, 10f, 5.5f), Mat("BrickWallRed"), BuildingStyle.BrickApartment, "LOFTS");
            Building(parent, new Vector3(12.2f, 0f, 26f), new Vector3(5f, 7f, 5f), Mat("PlasterCream"), BuildingStyle.Office, "STUDIO");
            ZebraCrossing(parent, 6f);
            PedestrianCrossingSign(parent, new Vector3(-5.9f, 0f, 4f));
            PedestrianCrossingSign(parent, new Vector3(5.9f, 0f, 8f));
            RoadsideBillboard(parent, new Vector3(7.0f, 0f, 22f), -20f, "HF_Runner");
        }

        static void BuildMixedCommercialStreet(Transform parent)
        {
            Building(parent, new Vector3(-12.3f, 0f, 4f), new Vector3(5f, 7f, 5f), Mat("BrickWallTan"), BuildingStyle.MixedUse, "APARTMENTS");
            Shopfront(parent, new Vector3(-11.7f, 0f, 12f), 90f, "RETAIL", Mat("ShopAwningGreen"));
            Building(parent, new Vector3(-12.2f, 0f, 22f), new Vector3(5.8f, 13f, 5.5f), Mat("Concrete"), BuildingStyle.Office, "MEDIA");
            Shopfront(parent, new Vector3(11.7f, 0f, 5f), -90f, "FOOD", Mat("ShopAwningRed"));
            Building(parent, new Vector3(12.5f, 0f, 14f), new Vector3(5f, 9.5f, 5f), Mat("Brick"), BuildingStyle.MixedUse, "ROOMS");
            Building(parent, new Vector3(12.3f, 0f, 24f), new Vector3(5.8f, 16f, 5.5f), Mat("Glass"), BuildingStyle.GlassTower, "OFFICES");
            JohannesburgRoadSign(parent, new Vector3(7.5f, 0f, 17f), -8f, "M31 JEPPE ST", "M2 Newtown", "CITY");
            ZebraCrossing(parent, 26f);
        }

        /// <summary>
        /// Roadside advertising billboard showing a still cropped from the
        /// reference video: two poles, a dark frame, and an unlit photo panel.
        /// </summary>
        static void RoadsideBillboard(Transform parent, Vector3 position, float yRotation, string textureName)
        {
            string texturePath = $"Assets/Textures/{textureName}.png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                return;
            }

            string materialPath = $"Assets/Materials/{textureName}.mat";
            Material photo = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (photo == null)
            {
                photo = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(photo, materialPath);
            }

            photo.mainTexture = texture;
            photo.color = Color.white;
            EditorUtility.SetDirty(photo);

            GameObject root = new GameObject($"RoadsideBillboard_{textureName}");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            Transform t = root.transform;

            Cylinder("BillboardPole", t, new Vector3(-1.7f, 1.5f, 0f), new Vector3(0.09f, 1.5f, 0.09f), Mat("PoleGrey"));
            Cylinder("BillboardPole", t, new Vector3(1.7f, 1.5f, 0f), new Vector3(0.09f, 1.5f, 0.09f), Mat("PoleGrey"));
            Cube("BillboardFrame", t, new Vector3(0f, 4.15f, 0.05f), new Vector3(4.6f, 2.8f, 0.12f), Mat("SignBlack"));

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = "BillboardPhoto";
            panel.transform.SetParent(t);
            panel.transform.localPosition = new Vector3(0f, 4.15f, -0.03f);
            panel.transform.localScale = new Vector3(4.3f, 2.5f, 1f);
            Object.DestroyImmediate(panel.GetComponent<Collider>());
            panel.GetComponent<Renderer>().sharedMaterial = photo;
        }

        static void SuburbanHouse(Transform parent, Vector3 position, Material wall)
        {
            Cube("SuburbanHouse_Wall", parent, position + new Vector3(0f, 1.3f, 0f), new Vector3(3.8f, 2.6f, 4.4f), wall);
            Cube("SuburbanHouse_TerracottaPitchedRoof", parent, position + new Vector3(0f, 2.55f, 0f), new Vector3(2.9f, 2.9f, 4.8f), Mat("RoofTerracotta"), new Vector3(0f, 0f, 45f));
        }

        static void VendorStall(Transform parent, Vector3 position)
        {
            Cube("VendorStall_Table", parent, position + new Vector3(0f, 0.42f, 0f), new Vector3(1.5f, 0.75f, 1f), Mat("CrateWood"));
            Cube("VendorStall_FruitCrate", parent, position + new Vector3(-0.35f, 0.97f, 0f), new Vector3(0.55f, 0.35f, 0.55f), Mat("CrateWood"));
            Cube("VendorStall_FruitCrate", parent, position + new Vector3(0.35f, 0.97f, 0.1f), new Vector3(0.55f, 0.35f, 0.55f), Mat("CrateWood"));

            for (int i = 0; i < 3; i++)
            {
                Sphere("VendorStall_Oranges", parent, position + new Vector3(-0.45f + i * 0.18f, 1.2f, -0.08f + i * 0.07f), new Vector3(0.16f, 0.16f, 0.16f), Mat("FruitOrange"));
                Sphere("VendorStall_Oranges", parent, position + new Vector3(0.2f + i * 0.16f, 1.2f, 0.12f - i * 0.06f), new Vector3(0.16f, 0.16f, 0.16f), Mat("FruitOrange"));
            }

            // Rainbow beach umbrella over the stall
            Cylinder("VendorUmbrella_Pole", parent, position + new Vector3(0.3f, 1.3f, -0.2f), new Vector3(0.04f, 1.3f, 0.04f), Mat("PoleGrey"));
            Sphere("VendorUmbrella_RedTier", parent, position + new Vector3(0.3f, 2.35f, -0.2f), new Vector3(2f, 0.5f, 2f), Mat("UmbrellaRed"));
            Sphere("VendorUmbrella_YellowTier", parent, position + new Vector3(0.3f, 2.5f, -0.2f), new Vector3(1.45f, 0.42f, 1.45f), Mat("UmbrellaYellow"));
            Sphere("VendorUmbrella_BlueTier", parent, position + new Vector3(0.3f, 2.65f, -0.2f), new Vector3(0.85f, 0.34f, 0.85f), Mat("UmbrellaBlue"));
        }

        static void PedestrianCrossingSign(Transform parent, Vector3 position)
        {
            Cylinder("CrossingSign_Pole", parent, position + new Vector3(0f, 1.15f, 0f), new Vector3(0.05f, 1.15f, 0.05f), Mat("PoleGrey"));
            Cube("CrossingSign_RedDiamond", parent, position + new Vector3(0f, 2.75f, 0f), new Vector3(0.95f, 0.95f, 0.07f), Mat("SignRed"), new Vector3(0f, 0f, 45f));
            Cube("CrossingSign_WhiteDiamond", parent, position + new Vector3(0f, 2.75f, 0f), new Vector3(0.68f, 0.68f, 0.08f), Mat("SignWhite"), new Vector3(0f, 0f, 45f));
            Cube("CrossingSign_WalkingFigure", parent, position + new Vector3(0f, 2.68f, 0f), new Vector3(0.1f, 0.34f, 0.1f), Mat("SignBlack"));
            Sphere("CrossingSign_FigureHead", parent, position + new Vector3(0f, 2.94f, 0f), new Vector3(0.12f, 0.12f, 0.12f), Mat("SignBlack"));
        }

        static void Pedestrian(Transform parent, Vector3 position, Material shirt)
        {
            Cylinder("Pedestrian_Legs", parent, position + new Vector3(-0.09f, 0.4f, 0f), new Vector3(0.08f, 0.4f, 0.08f), Mat("DenimBlue"));
            Cylinder("Pedestrian_Legs", parent, position + new Vector3(0.09f, 0.4f, 0f), new Vector3(0.08f, 0.4f, 0.08f), Mat("DenimBlue"));
            Capsule("Pedestrian_Torso", parent, position + new Vector3(0f, 1.05f, 0f), new Vector3(0.34f, 0.3f, 0.22f), shirt);
            Sphere("Pedestrian_Head", parent, position + new Vector3(0f, 1.55f, 0f), new Vector3(0.26f, 0.26f, 0.26f), Mat("RunnerSkin"));
        }

        static void ZebraCrossing(Transform parent, float zPosition)
        {
            for (int i = -4; i <= 4; i++)
            {
                Cube("ZebraCrossingStripe", parent, new Vector3(i * 1.15f, 0.06f, zPosition), new Vector3(0.6f, 0.04f, 2.4f), Mat("PaintWhite"));
            }
        }

        enum BuildingStyle
        {
            Office,
            BrickApartment,
            GlassTower,
            MixedUse
        }

        static void Building(Transform parent, Vector3 position, Vector3 size, Material wall, BuildingStyle style, string signText)
        {
            GameObject root = new GameObject(style.ToString());
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            Transform t = root.transform;

            Cube("BuildingMass", t, new Vector3(0f, size.y * 0.5f, 0f), size, wall);
            Cube("Parapet", t, new Vector3(0f, size.y + 0.22f, 0f), new Vector3(size.x + 0.25f, 0.42f, size.z + 0.25f), Mat("Concrete"));

            if (style == BuildingStyle.GlassTower)
            {
                AddWindowGrid(t, size, 6, 4, Mat("GlassDark"), true);
            }
            else
            {
                AddWindowGrid(t, size, Mathf.Max(3, Mathf.FloorToInt(size.y / 2f)), 3, Mat("Glass"), false);
            }

            if (style == BuildingStyle.MixedUse || style == BuildingStyle.BrickApartment)
            {
                float side = position.x < 0f ? 1f : -1f;
                Cube("GroundFloorShopfront", t, new Vector3(side * (size.x * 0.5f + 0.035f), 1.25f, 0f), new Vector3(0.08f, 1.6f, size.z * 0.78f), Mat("GlassDark"));
                Cube("ShopAwning", t, new Vector3(side * (size.x * 0.5f + 0.28f), 2.4f, 0f), new Vector3(0.45f, 0.28f, size.z * 0.9f), Mat("ShopAwningRed"));
            }

            FacadeText(t, position.x < 0f ? 1f : -1f, size, signText, 2.8f, 1.3f);
        }

        static void AddWindowGrid(Transform parent, Vector3 size, int rows, int columns, Material windowMaterial, bool fullGlass)
        {
            float side = parent.localPosition.x < 0f ? 1f : -1f;
            float x = side * (size.x * 0.5f + 0.045f);
            float windowHeight = fullGlass ? 0.9f : 0.55f;
            float windowWidth = fullGlass ? 0.08f : 0.07f;
            for (int row = 0; row < rows; row++)
            {
                float y = 2.5f + row * Mathf.Max(1.25f, (size.y - 3.4f) / Mathf.Max(1, rows));
                if (y > size.y - 0.8f)
                {
                    continue;
                }

                for (int column = 0; column < columns; column++)
                {
                    float z = Mathf.Lerp(-size.z * 0.32f, size.z * 0.32f, columns == 1 ? 0.5f : column / (float)(columns - 1));
                    Cube("Window", parent, new Vector3(x, y, z), new Vector3(windowWidth, windowHeight, size.z * 0.12f), windowMaterial);
                }
            }
        }

        static void FacadeText(Transform parent, float side, Vector3 size, string text, float y, float width)
        {
            Cube("FacadeSignPanel", parent, new Vector3(side * (size.x * 0.5f + 0.08f), y, 0f), new Vector3(0.12f, 0.7f, width * 2.2f), Mat("SignBlack"));
            WorldText(parent, text, new Vector3(side * (size.x * 0.5f + 0.16f), y - 0.05f, 0f), side > 0f ? 90f : -90f, 0.48f, Color.white);
        }

        static void Shopfront(Transform parent, Vector3 position, float yRotation, string label, Material awning)
        {
            GameObject root = new GameObject("Shopfront_" + label.Replace(" ", ""));
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            Transform t = root.transform;

            Cube("ShopBrickShell", t, new Vector3(0f, 2.6f, 0f), new Vector3(4.6f, 5.2f, 4.8f), Mat("Brick"));
            Cube("ShopGlassFront", t, new Vector3(0f, 1.35f, -2.43f), new Vector3(3.5f, 1.9f, 0.08f), Mat("GlassDark"));
            Cube("ShopAwning", t, new Vector3(0f, 2.55f, -2.75f), new Vector3(4.1f, 0.35f, 0.7f), awning);
            Cube("RollerDoor", t, new Vector3(-1.25f, 1.05f, -2.5f), new Vector3(1.35f, 1.55f, 0.07f), Mat("Metal"));
            WorldText(t, label, new Vector3(0f, 3.15f, -2.86f), 0f, 0.42f, Color.white);
        }

        static void MuralWall(Transform parent, Vector3 position, float yRotation, string label)
        {
            GameObject root = new GameObject("JoziMural");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            Transform t = root.transform;

            Cube("MuralBrickWall", t, new Vector3(0f, 2.3f, 0f), new Vector3(0.35f, 4.6f, 7.8f), Mat("Brick"));
            Cube("MuralColorBlockBlue", t, new Vector3(-0.2f, 2.5f, -1.6f), new Vector3(0.08f, 2.5f, 2.4f), Mat("MuralBlue"), new Vector3(0f, 0f, 18f));
            Cube("MuralColorBlockMagenta", t, new Vector3(-0.21f, 2.7f, 1.1f), new Vector3(0.08f, 2.2f, 2.2f), Mat("MuralMagenta"), new Vector3(0f, 0f, -22f));
            Cube("MuralColorBlockOrange", t, new Vector3(-0.22f, 1.4f, 0.1f), new Vector3(0.08f, 1.6f, 3.2f), Mat("MuralOrange"), new Vector3(0f, 0f, 7f));
            WorldText(t, label, new Vector3(-0.31f, 2.65f, 0f), -90f, 0.72f, Color.white);
        }

        static void JohannesburgRoadSign(Transform parent, Vector3 position, float yRotation, string title, string lineA, string lineB)
        {
            GameObject root = new GameObject("JohannesburgRoadSign");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            Transform t = root.transform;

            Cylinder("RoadSignPole_Left", t, new Vector3(-1.75f, 1.65f, 0f), new Vector3(0.07f, 1.65f, 0.07f), Mat("Metal"));
            Cylinder("RoadSignPole_Right", t, new Vector3(1.75f, 1.65f, 0f), new Vector3(0.07f, 1.65f, 0.07f), Mat("Metal"));
            Cube("GreenDirectionBoard", t, new Vector3(0f, 3.25f, 0f), new Vector3(4.0f, 2.2f, 0.12f), Mat("SignGreen"));
            Cube("SignDividerA", t, new Vector3(0f, 3.35f, -0.07f), new Vector3(3.7f, 0.035f, 0.04f), Mat("SignWhite"));
            Cube("SignDividerB", t, new Vector3(0f, 2.75f, -0.07f), new Vector3(3.7f, 0.035f, 0.04f), Mat("SignWhite"));
            WorldText(t, title, new Vector3(0f, 3.92f, -0.12f), 0f, 0.35f, Color.white);
            WorldText(t, lineA + "  ^", new Vector3(0f, 3.25f, -0.12f), 0f, 0.32f, Color.white);
            WorldText(t, lineB + "  >", new Vector3(0f, 2.63f, -0.12f), 0f, 0.32f, Color.white);
        }

        static void StreetNameSign(Transform parent, Vector3 position, string text)
        {
            Cylinder("StreetNamePole", parent, position + new Vector3(0f, 1.3f, 0f), new Vector3(0.055f, 1.3f, 0.055f), Mat("Metal"));
            Cube("StreetNamePlate", parent, position + new Vector3(0f, 2.75f, 0f), new Vector3(2.5f, 0.42f, 0.08f), Mat("SignGreen"));
            WorldText(parent, text, position + new Vector3(0f, 2.73f, -0.08f), 0f, 0.25f, Color.white);
        }

        static void WorldText(Transform parent, string text, Vector3 localPosition, float yRotation, float size, Color color)
        {
            GameObject textObject = new GameObject("Text_" + text.Replace(" ", ""));
            textObject.transform.SetParent(parent);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            TextMeshPro tmp = textObject.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.enableWordWrapping = false;
        }

        static void Bench(Transform parent, Vector3 position, float yRotation)
        {
            GameObject root = new GameObject("Bench");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            Transform t = root.transform;
            Cube("BenchSeat", t, new Vector3(0f, 0.65f, 0f), new Vector3(1.7f, 0.16f, 0.42f), Mat("CrateWood"));
            Cube("BenchBack", t, new Vector3(0f, 1.05f, 0.22f), new Vector3(1.7f, 0.55f, 0.12f), Mat("CrateWood"));
            Cube("BenchLegLeft", t, new Vector3(-0.65f, 0.35f, 0f), new Vector3(0.12f, 0.55f, 0.12f), Mat("Metal"));
            Cube("BenchLegRight", t, new Vector3(0.65f, 0.35f, 0f), new Vector3(0.12f, 0.55f, 0.12f), Mat("Metal"));
        }

        static void PublicBin(Transform parent, Vector3 position)
        {
            Cylinder("PublicBin", parent, position + new Vector3(0f, 0.45f, 0f), new Vector3(0.34f, 0.45f, 0.34f), Mat("MetalDark"));
            Cylinder("PublicBinLid", parent, position + new Vector3(0f, 0.94f, 0f), new Vector3(0.38f, 0.08f, 0.38f), Mat("Metal"));
        }

        static void NewspaperBox(Transform parent, Vector3 position)
        {
            Cube("NewspaperBox", parent, position + new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.85f, 0.55f), Mat("SignYellow"));
            Cube("NewspaperWindow", parent, position + new Vector3(0f, 0.62f, -0.29f), new Vector3(0.48f, 0.42f, 0.04f), Mat("Glass"));
        }

        static void Mailbox(Transform parent, Vector3 position)
        {
            Cube("Mailbox", parent, position + new Vector3(0f, 0.75f, 0f), new Vector3(0.62f, 0.85f, 0.42f), Mat("SignGreen"));
            Cube("MailboxSlot", parent, position + new Vector3(0f, 0.85f, -0.23f), new Vector3(0.42f, 0.07f, 0.04f), Mat("SignBlack"));
        }

        static void ElectricalCabinet(Transform parent, Vector3 position)
        {
            Cube("ElectricalCabinet", parent, position + new Vector3(0f, 0.78f, 0f), new Vector3(0.85f, 1.25f, 0.48f), Mat("Metal"));
            Cube("ElectricalCabinetWarning", parent, position + new Vector3(0f, 0.98f, -0.27f), new Vector3(0.38f, 0.28f, 0.04f), Mat("SignYellow"));
        }

        static void FireHydrant(Transform parent, Vector3 position)
        {
            Cylinder("FireHydrantBody", parent, position + new Vector3(0f, 0.45f, 0f), new Vector3(0.14f, 0.45f, 0.14f), Mat("SignRed"));
            Sphere("FireHydrantCap", parent, position + new Vector3(0f, 0.94f, 0f), new Vector3(0.2f, 0.16f, 0.2f), Mat("SignRed"));
            Cylinder("FireHydrantNozzle", parent, position + new Vector3(0.2f, 0.62f, 0f), new Vector3(0.08f, 0.18f, 0.08f), Mat("Metal"), new Vector3(0f, 0f, 90f));
        }

        static void ParkingMeter(Transform parent, Vector3 position)
        {
            Cylinder("ParkingMeterPole", parent, position + new Vector3(0f, 0.75f, 0f), new Vector3(0.045f, 0.75f, 0.045f), Mat("Metal"));
            Cube("ParkingMeterHead", parent, position + new Vector3(0f, 1.55f, 0f), new Vector3(0.28f, 0.42f, 0.22f), Mat("MetalDark"));
        }

        static void BusStop(Transform parent, Vector3 position, float yRotation)
        {
            GameObject root = new GameObject("BusStopShelter");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            Transform t = root.transform;
            Cube("BusStopBackGlass", t, new Vector3(0f, 1.45f, 0f), new Vector3(3.1f, 2.2f, 0.08f), Mat("Glass"));
            Cube("BusStopRoof", t, new Vector3(0f, 2.7f, -0.35f), new Vector3(3.5f, 0.16f, 1.15f), Mat("MetalDark"));
            Cylinder("BusStopPoleLeft", t, new Vector3(-1.55f, 1.35f, -0.5f), new Vector3(0.055f, 1.35f, 0.055f), Mat("Metal"));
            Cylinder("BusStopPoleRight", t, new Vector3(1.55f, 1.35f, -0.5f), new Vector3(0.055f, 1.35f, 0.055f), Mat("Metal"));
            Bench(t, new Vector3(0f, 0.05f, -0.55f), 0f);
            WorldText(t, "BUS STOP", new Vector3(0f, 2.2f, -0.08f), 0f, 0.28f, Color.white);
        }

        static void TrafficLight(Transform parent, Vector3 position, float yRotation)
        {
            GameObject root = new GameObject("TrafficLight");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            Transform t = root.transform;
            Cylinder("TrafficLightPole", t, new Vector3(0f, 2f, 0f), new Vector3(0.06f, 2f, 0.06f), Mat("Metal"));
            Cube("TrafficLightArm", t, new Vector3(0.55f, 3.75f, 0f), new Vector3(1.1f, 0.08f, 0.08f), Mat("Metal"));
            Cube("TrafficLightBox", t, new Vector3(1.2f, 3.25f, 0f), new Vector3(0.35f, 0.95f, 0.28f), Mat("MetalDark"));
            Sphere("TrafficRed", t, new Vector3(1.2f, 3.52f, -0.16f), new Vector3(0.11f, 0.11f, 0.04f), Mat("SignRed"));
            Sphere("TrafficAmber", t, new Vector3(1.2f, 3.25f, -0.16f), new Vector3(0.11f, 0.11f, 0.04f), Mat("PaintYellow"));
            Sphere("TrafficGreen", t, new Vector3(1.2f, 2.98f, -0.16f), new Vector3(0.11f, 0.11f, 0.04f), Mat("ShopAwningGreen"));
        }

        static void BollardRow(Transform parent, float x, float z)
        {
            for (int i = 0; i < 3; i++)
            {
                Cylinder("SidewalkBollard", parent, new Vector3(x, 0.45f, z + i * 1.3f), new Vector3(0.08f, 0.45f, 0.08f), Mat("MetalDark"));
            }
        }

        static void PavementTree(Transform parent, Vector3 position, float scale, int variant)
        {
            Cylinder("TreeBark", parent, position + new Vector3(0f, 1.55f * scale, 0f), new Vector3(0.18f * scale, 1.55f * scale, 0.18f * scale), Mat("TreeBark"));
            Sphere("TreeCrownA", parent, position + new Vector3(-0.45f * scale, 3.2f * scale, 0f), new Vector3(1.35f * scale, 1.05f * scale, 1.2f * scale), Mat(variant % 2 == 0 ? "Leaves" : "TreeGreen"));
            Sphere("TreeCrownB", parent, position + new Vector3(0.55f * scale, 3.35f * scale, 0.25f * scale), new Vector3(1.45f * scale, 1.05f * scale, 1.35f * scale), Mat("Leaves"));
            Sphere("TreeCrownC", parent, position + new Vector3(0.05f * scale, 3.85f * scale, -0.3f * scale), new Vector3(1.25f * scale, 0.95f * scale, 1.1f * scale), Mat("TreeGreen"));
        }

        static void ShrubCluster(Transform parent, Vector3 position, int variant)
        {
            Sphere("ShrubA", parent, position + new Vector3(-0.28f, 0f, 0f), new Vector3(0.5f, 0.32f, 0.45f), Mat("ShrubGreen"));
            Sphere("ShrubB", parent, position + new Vector3(0.26f, 0.03f, 0.2f), new Vector3(0.42f, 0.28f, 0.38f), Mat(variant % 2 == 0 ? "TreeGreen" : "Leaves"));
        }

        static void Planter(Transform parent, Vector3 position)
        {
            Cube("ConcretePlanter", parent, position, new Vector3(1.3f, 0.42f, 0.55f), Mat("Concrete"));
            ShrubCluster(parent, position + new Vector3(0f, 0.34f, 0f), 0);
        }

        static void StormDrain(Transform parent, Vector3 position, float yRotation)
        {
            GameObject root = new GameObject("StormDrain");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            Transform t = root.transform;
            Cube("StormDrainFrame", t, Vector3.zero, new Vector3(0.65f, 0.035f, 0.42f), Mat("MetalDark"));
            for (int i = -2; i <= 2; i++)
            {
                Cube("StormDrainSlot", t, new Vector3(i * 0.11f, 0.02f, 0f), new Vector3(0.035f, 0.02f, 0.38f), Mat("Metal"));
            }
        }

        static void PavementSection(Transform parent, Vector3 position, float yRotation)
        {
            GameObject root = new GameObject("PavementSection");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            Cube("ConcretePaving", root.transform, new Vector3(0f, 0f, 0f), new Vector3(3.5f, 0.2f, 6f), Mat("Pavement"));
            for (int z = -2; z <= 2; z++)
            {
                Cube("PavingJoint", root.transform, new Vector3(0f, 0.13f, z), new Vector3(3.3f, 0.02f, 0.035f), Mat("Concrete"));
            }
        }

        // ------------------------------------------------------------------
        // Vehicles
        // ------------------------------------------------------------------

        /// <summary>
        /// Different exporters use wildly different unit scales; scale the visual
        /// so the character reads clearly on mobile and rests its feet on the ground.
        /// </summary>
        static void NormalizeRunnerScale(GameObject visual, Transform playerRoot)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            if (bounds.size.y < 0.05f)
            {
                return;
            }

            float scale = RunnerVisualTargetHeight / bounds.size.y;
            if (Mathf.Abs(scale - 1f) > 0.15f)
            {
                visual.transform.localScale *= scale;
            }

            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            visual.transform.position += new Vector3(
                playerRoot.position.x - bounds.center.x,
                playerRoot.position.y - RunnerGroundSink - bounds.min.y,
                playerRoot.position.z - bounds.center.z);
        }

        static string FindModelPathByName(string modelName)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/") && Path.GetFileNameWithoutExtension(path).ToLowerInvariant() == modelName)
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// Instantiates a dropped-in FBX vehicle model, normalized to a sensible
        /// size, grounded, centered on its root, and textured (using textures
        /// extracted from the FBX when Unity fails to decode the embedded ones).
        /// Returns null when no such model exists so callers can fall back.
        /// </summary>
        /// <summary>
        /// Merges a vehicle's many small meshes into one mesh per material,
        /// collapsing hundreds of renderers (draw calls) into a handful. The
        /// combined meshes are saved as assets keyed by <paramref name="cacheKey"/>
        /// so prefabs can reference them.
        /// </summary>
        static void CombineVehicleMeshes(GameObject vehicleRoot, string cacheKey)
        {
            MeshFilter[] filters = vehicleRoot.GetComponentsInChildren<MeshFilter>();
            if (filters.Length <= 3)
            {
                return;
            }

            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (MeshFilter filter in filters)
            {
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || filter.sharedMesh == null)
                {
                    continue;
                }

                for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++)
                {
                    Material material = renderer.sharedMaterials[Mathf.Min(sub, renderer.sharedMaterials.Length - 1)];
                    if (material == null)
                    {
                        continue;
                    }

                    if (!groups.TryGetValue(material, out List<CombineInstance> instances))
                    {
                        instances = new List<CombineInstance>();
                        groups[material] = instances;
                    }

                    instances.Add(new CombineInstance
                    {
                        mesh = filter.sharedMesh,
                        subMeshIndex = sub,
                        transform = vehicleRoot.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix
                    });
                }
            }

            if (groups.Count == 0)
            {
                return;
            }

            Directory.CreateDirectory("Assets/Meshes");
            int partIndex = 0;
            int rendererCount = filters.Length;
            foreach (KeyValuePair<Material, List<CombineInstance>> group in groups)
            {
                Mesh combined = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                combined.CombineMeshes(group.Value.ToArray(), true, true);
                combined.name = $"{cacheKey}_Part{partIndex}";

                string path = $"Assets/Meshes/Combined_{cacheKey}_{partIndex}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(combined, path);

                GameObject part = new GameObject($"CombinedMesh_{partIndex}");
                part.transform.SetParent(vehicleRoot.transform);
                part.transform.localPosition = Vector3.zero;
                part.transform.localRotation = Quaternion.identity;
                part.transform.localScale = Vector3.one;
                part.AddComponent<MeshFilter>().sharedMesh = combined;
                part.AddComponent<MeshRenderer>().sharedMaterial = group.Key;
                partIndex++;
            }

            Transform oldVisual = vehicleRoot.transform.Find("VehicleVisual");
            if (oldVisual != null)
            {
                Object.DestroyImmediate(oldVisual.gameObject);
            }

            Debug.Log($"CombineVehicleMeshes {cacheKey}: {rendererCount} renderers -> {partIndex}");
        }

        static GameObject FbxVehicleInstance(string modelName, Transform parent, Vector3 position, float yRotation, float targetHeight, string combineCacheKey = null)
        {
            string modelPath = FindModelPathByName(modelName);
            if (modelPath == null)
            {
                return null;
            }

            bool hasTextures = false;
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer != null)
            {
                if (importer.animationType != ModelImporterAnimationType.None ||
                    importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
                {
                    importer.animationType = ModelImporterAnimationType.None;
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                    importer.SaveAndReimport();
                }

                hasTextures = ExtractEmbeddedTextures(importer, modelPath);
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                return null;
            }

            GameObject root = new GameObject($"{modelName}FbxVehicle");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;

            GameObject visual = Object.Instantiate(model, root.transform);
            visual.name = "VehicleVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            // Re-orient exports with non-standard axes so the vehicle stands upright
            // (up = Y) with its length along Z. Detected from the raw bounds: the
            // longest axis is the length; when height sits in Z the model is Z-up.
            Renderer[] rawRenderers = visual.GetComponentsInChildren<Renderer>();
            if (rawRenderers.Length > 0)
            {
                Bounds rawBounds = rawRenderers[0].bounds;
                foreach (Renderer renderer in rawRenderers)
                {
                    rawBounds.Encapsulate(renderer.bounds);
                }

                Vector3 size = rawBounds.size;
                if (size.x >= size.y && size.x >= size.z)
                {
                    visual.transform.localRotation = size.z > size.y
                        ? Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(-90f, 0f, 0f) // Z-up, X-length
                        : Quaternion.Euler(0f, 90f, 0f);                                  // Y-up, X-length
                }
                else if (size.z >= size.x && size.z >= size.y && size.x > size.y)
                {
                    // Length already along Z but Z-up (height in X): roll upright.
                    visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }

            // Bake the facing into the visual, not the root: spawners often
            // instantiate with identity rotation, which would undo a root facing.
            visual.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f) * visual.transform.localRotation;

            NormalizeVehicleBounds(visual, root.transform, targetHeight);
            SimplifyMeshes(visual, 100000);

            if (!hasTextures)
            {
                ApplyBestExtractedTexture(visual, modelPath);
            }

            if (combineCacheKey != null)
            {
                CombineVehicleMeshes(root, combineCacheKey);
            }

            foreach (Collider collider in root.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            return root;
        }

        static void NormalizeVehicleBounds(GameObject visual, Transform vehicleRoot, float targetHeight)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            if (bounds.size.y < 0.0001f)
            {
                return;
            }

            // Multiply the existing scale: FBX roots often carry a baked unit-conversion
            // scale that must be preserved.
            visual.transform.localScale *= targetHeight / bounds.size.y;

            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            Vector3 offset = vehicleRoot.position - bounds.center;
            offset.y = vehicleRoot.position.y - bounds.min.y;
            visual.transform.position += offset;
        }

        static void ApplyBestExtractedTexture(GameObject visual, string modelPath)
        {
            string modelName = Path.GetFileNameWithoutExtension(modelPath).Replace(" ", "");
            string folder = $"Assets/Textures/{modelName}Extracted";
            string bestPath = null;
            long bestBytes = -1;
            bool bestIsDiffuse = false;

            if (AssetDatabase.IsValidFolder(folder))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    bool isDiffuse = Path.GetFileName(path).ToLowerInvariant().Contains("diffuse");
                    long bytes = new FileInfo(path).Length;
                    if (bestPath == null || (isDiffuse && !bestIsDiffuse) || (isDiffuse == bestIsDiffuse && bytes > bestBytes))
                    {
                        bestPath = path;
                        bestBytes = bytes;
                        bestIsDiffuse = isDiffuse;
                    }
                }
            }

            Material replacement;
            if (bestPath != null)
            {
                replacement = CreateMaterial($"VehiclePhoto_{modelName}", Color.white);
                replacement.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(bestPath);
                EditorUtility.SetDirty(replacement);
            }
            else
            {
                replacement = Mat("TaxiYellow");
            }

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || materials[i].mainTexture == null)
                    {
                        materials[i] = replacement;
                    }
                }

                renderer.sharedMaterials = materials;
            }
        }

        /// <summary>Unlit opaque material showing a still cropped from the reference video.</summary>
        static Material UnlitPhotoMaterial(string textureName)
        {
            string texturePath = $"Assets/Textures/{textureName}.png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                return null;
            }

            string materialPath = $"Assets/Materials/{textureName}.mat";
            Material photo = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (photo == null)
            {
                photo = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(photo, materialPath);
            }

            photo.mainTexture = texture;
            photo.color = Color.white;
            EditorUtility.SetDirty(photo);
            return photo;
        }

        /// <summary>Unlit flat-colour material for glowing details like vehicle lights.</summary>
        static Material UnlitColorMaterial(string name, Color color)
        {
            string materialPath = $"Assets/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Locates a vehicle's real wheels by sampling mesh vertices in a thin
        /// band just above the ground: at that height almost only tyre geometry
        /// exists, so the per-axle median z and the outer x extent give the true
        /// wheel placement even on combined FBX meshes. Returns false when the
        /// low geometry is inconclusive.
        /// </summary>
        static bool TryMeasureWheels(GameObject root, Bounds bounds, out float frontAxleZ, out float rearAxleZ, out float halfTrack)
        {
            frontAxleZ = rearAxleZ = halfTrack = 0f;
            float bandBottom = bounds.min.y + bounds.size.y * 0.01f;
            float bandTop = bounds.min.y + bounds.size.y * 0.10f;

            var frontZs = new List<float>();
            var rearZs = new List<float>();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Matrix4x4 toRoot = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                {
                    Vector3 point = toRoot.MultiplyPoint3x4(vertex);
                    if (point.y < bandBottom || point.y > bandTop)
                    {
                        continue;
                    }

                    (point.z >= bounds.center.z ? frontZs : rearZs).Add(point.z);
                    halfTrack = Mathf.Max(halfTrack, Mathf.Abs(point.x - bounds.center.x));
                }
            }

            if (frontZs.Count < 8 || rearZs.Count < 8 || halfTrack < bounds.size.x * 0.25f)
            {
                return false;
            }

            frontZs.Sort();
            rearZs.Sort();
            frontAxleZ = frontZs[frontZs.Count / 2];
            rearAxleZ = rearZs[rearZs.Count / 2];

            // Sanity: the axles must sit a plausible distance apart.
            float wheelbase = frontAxleZ - rearAxleZ;
            return wheelbase > bounds.size.z * 0.3f && wheelbase < bounds.size.z * 0.95f;
        }

        /// <summary>
        /// Spinning wheel hubs, lit head/tail lights, and windshield wipers so a
        /// vehicle reads as driving instead of gliding. Placement is derived from
        /// the renderer bounds, so it fits both the FBX taxi (whose meshes are
        /// combined and cannot spin) and the primitive fallback. frontSign is the
        /// local-Z direction the nose points (+1 or -1). Must be called while the
        /// vehicle sits at the origin with identity rotation.
        /// </summary>
        static void AddVehicleRunningGear(GameObject root, float frontSign)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            float width = bounds.size.x;
            float height = bounds.size.y;
            float length = bounds.size.z;
            float wheelRadius = height * 0.16f;
            Vector3 center = bounds.center;

            Material headlight = UnlitColorMaterial("HeadlightGlow", new Color(1f, 0.96f, 0.72f));
            Material taillight = UnlitColorMaterial("TaillightGlow", new Color(1f, 0.16f, 0.1f));

            // Snap the spinning hubs onto the vehicle's real wheels; fall back to
            // a bounds heuristic only when the wheels cannot be located.
            float frontAxleZ = center.z + length * 0.30f;
            float rearAxleZ = center.z - length * 0.30f;
            float hubX = width * 0.5f - 0.01f;
            if (TryMeasureWheels(root, bounds, out float measuredFrontZ, out float measuredRearZ, out float measuredHalfTrack))
            {
                frontAxleZ = measuredFrontZ;
                rearAxleZ = measuredRearZ;
                hubX = measuredHalfTrack;
                Debug.Log($"{root.name}: wheels measured at z {rearAxleZ:F2}/{frontAxleZ:F2}, half-track {hubX:F2}");
            }
            else
            {
                Debug.LogWarning($"{root.name}: wheel measurement failed, using bounds heuristic for hub placement");
            }

            var wheels = new List<Transform>();
            foreach (Vector2 corner in new[] { new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f) })
            {
                GameObject hub = new GameObject("WheelHub");
                hub.transform.SetParent(root.transform);
                hub.transform.localPosition = new Vector3(
                    center.x + corner.x * hubX,
                    bounds.min.y + wheelRadius,
                    corner.y > 0f ? frontAxleZ : rearAxleZ);
                hub.transform.localRotation = Quaternion.identity;

                Cylinder("WheelDisc", hub.transform, Vector3.zero,
                    new Vector3(wheelRadius * 1.7f, 0.025f, wheelRadius * 1.7f), Mat("WheelBlack"), new Vector3(0f, 0f, 90f));
                Cube("WheelSpokeVertical", hub.transform, Vector3.zero,
                    new Vector3(0.09f, wheelRadius * 1.5f, 0.1f), Mat("PoleGrey"));
                Cube("WheelSpokeAcross", hub.transform, Vector3.zero,
                    new Vector3(0.09f, wheelRadius * 1.5f, 0.1f), Mat("PoleGrey"), new Vector3(90f, 0f, 0f));

                wheels.Add(hub.transform);
            }

            float noseZ = center.z + frontSign * (length * 0.5f + 0.02f);
            float tailZ = center.z - frontSign * (length * 0.5f + 0.02f);
            foreach (float side in new[] { -1f, 1f })
            {
                Cube("Headlight", root.transform,
                    new Vector3(center.x + side * width * 0.28f, bounds.min.y + height * 0.32f, noseZ),
                    new Vector3(0.28f, 0.18f, 0.06f), headlight);
                Cube("Taillight", root.transform,
                    new Vector3(center.x + side * width * 0.32f, bounds.min.y + height * 0.30f, tailZ),
                    new Vector3(0.22f, 0.14f, 0.06f), taillight);
            }

            var wipers = new List<Transform>();
            foreach (float pivotX in new[] { -width * 0.22f, width * 0.06f })
            {
                GameObject pivot = new GameObject("WiperPivot");
                pivot.transform.SetParent(root.transform);
                pivot.transform.localPosition = new Vector3(center.x + pivotX, bounds.min.y + height * 0.50f, center.z + frontSign * length * 0.44f);
                // Lean the blade back onto the windshield and park it tilted.
                pivot.transform.localRotation = Quaternion.Euler(-frontSign * 14f, 0f, -20f);
                Cube("WiperBlade", pivot.transform, new Vector3(0f, height * 0.14f, 0f),
                    new Vector3(0.035f, height * 0.28f, 0.035f), Mat("WheelBlack"));
                wipers.Add(pivot.transform);
            }

            // Running gear is decoration: strip its primitive colliders so they
            // never interfere with the obstacle hit box on the root.
            foreach (Transform gear in wheels)
            {
                StripColliders(gear.gameObject);
            }

            foreach (Transform gear in wipers)
            {
                StripColliders(gear.gameObject);
            }

            foreach (Transform child in root.transform)
            {
                if (child.name == "Headlight" || child.name == "Taillight")
                {
                    StripColliders(child.gameObject);
                }
            }

            VehicleMotion motion = root.AddComponent<VehicleMotion>();
            SetField(motion, "wheels", wheels.ToArray());
            SetField(motion, "wheelRadius", wheelRadius);
            SetField(motion, "wiperPivots", wipers.ToArray());
        }

        static GameObject MinibusTaxiVisual(Transform parent, Vector3 position, float yRotation, bool photoRear = false)
        {
            GameObject root = new GameObject("MinibusTaxi");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            Transform t = root.transform;

            Cube("TaxiBody", t, new Vector3(0f, 0.95f, 0f), new Vector3(1.9f, 1.15f, 3.4f), Mat("TaxiYellow"));
            Cube("TaxiRoof", t, new Vector3(0f, 1.56f, 0f), new Vector3(1.85f, 0.08f, 3.3f), Mat("TaxiWhite"));
            Cube("TaxiWindowBand", t, new Vector3(0f, 1.22f, 0.15f), new Vector3(1.94f, 0.42f, 2.4f), Mat("GlassWindow"));
            Cube("TaxiWindshield", t, new Vector3(0f, 1.22f, 1.68f), new Vector3(1.6f, 0.4f, 0.1f), Mat("GlassWindow"));
            Cube("TaxiRearWindow", t, new Vector3(0f, 1.25f, -1.68f), new Vector3(1.4f, 0.38f, 0.1f), Mat("GlassWindow"));

            // Black-and-white checker band across the rear like the reference taxi
            for (int i = 0; i < 6; i++)
            {
                Material checker = i % 2 == 0 ? Mat("CheckerBlack") : Mat("TaxiWhite");
                Cube("TaxiRearCheckerBand", t, new Vector3(-0.8f + i * 0.32f, 0.62f, -1.73f), new Vector3(0.32f, 0.3f, 0.06f), checker);
            }

            // Red-and-white hazard stripes on the rear bumper
            for (int i = 0; i < 6; i++)
            {
                Material stripe = i % 2 == 0 ? Mat("HazardRed") : Mat("TaxiWhite");
                Cube("TaxiHazardStripe", t, new Vector3(-0.8f + i * 0.32f, 0.38f, -1.75f), new Vector3(0.32f, 0.16f, 0.06f), stripe);
            }

            Cube("TaxiFrontBumper", t, new Vector3(0f, 0.35f, 1.74f), new Vector3(1.95f, 0.18f, 0.12f), Mat("TaxiWhite"));

            foreach (Vector3 wheelPosition in new[]
            {
                new Vector3(-0.95f, 0.32f, 1.05f), new Vector3(0.95f, 0.32f, 1.05f),
                new Vector3(-0.95f, 0.32f, -1.05f), new Vector3(0.95f, 0.32f, -1.05f)
            })
            {
                Cylinder("TaxiWheel", t, wheelPosition, new Vector3(0.62f, 0.12f, 0.62f), Mat("WheelBlack"), new Vector3(0f, 0f, 90f));
            }

            Material rearPhoto = photoRear ? UnlitPhotoMaterial("TaxiRearPhoto") : null;
            if (rearPhoto != null)
            {
                GameObject decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.name = "TaxiRearPhotoDecal";
                decal.transform.SetParent(t);
                decal.transform.localPosition = new Vector3(0f, 0.93f, -1.79f);
                decal.transform.localRotation = Quaternion.identity;
                decal.transform.localScale = new Vector3(1.88f, 1.38f, 1f);
                Object.DestroyImmediate(decal.GetComponent<Collider>());
                decal.GetComponent<Renderer>().sharedMaterial = rearPhoto;
            }

            return root;
        }

        // ------------------------------------------------------------------
        // Obstacle and pickup prefabs
        // ------------------------------------------------------------------

        static GameObject CreateTaxiObstaclePrefab()
        {
            GameObject holder = new GameObject("TaxiObstacleHolder");
            GameObject root = FbxVehicleInstance("taxi", holder.transform, Vector3.zero, 180f, 2.3f, "TaxiObstacle")
                ?? MinibusTaxiVisual(holder.transform, Vector3.zero, 180f);
            root.name = "MinibusTaxiObstacle";
            root.transform.SetParent(null);
            Object.DestroyImmediate(holder);

            // Fit the collider to whatever model is in use.
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            AddObstacleCollider(root, bounds.center - root.transform.position, bounds.size * 0.9f);

            // Obstacle taxis face the player, so their nose points down local -Z.
            AddVehicleRunningGear(root, -1f);

            // Oncoming traffic: taxi obstacles drive toward the player.
            MovingObstacle mover = root.AddComponent<MovingObstacle>();
            SetField(mover, "moveSpeed", 5f);

            Debug.Log($"Taxi obstacle triangle count: {CountTriangles(root)}");
            return SavePrefab(root, TaxiPrefabPath);
        }

        static GameObject CreateSceneryTaxiPrefab()
        {
            GameObject holder = new GameObject("SceneryTaxiHolder");
            GameObject root = FbxVehicleInstance("taxi", holder.transform, Vector3.zero, 0f, 2.3f, "SceneryTaxi")
                ?? MinibusTaxiVisual(holder.transform, Vector3.zero, 0f, true);
            root.name = "SceneryTaxi";
            root.transform.SetParent(null);
            Object.DestroyImmediate(holder);

            foreach (Collider collider in root.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            // Scenery taxis are built facing +Z (rear toward the camera).
            AddVehicleRunningGear(root, 1f);

            root.AddComponent<SceneryVehicle>();
            return SavePrefab(root, "Assets/Prefabs/SceneryTaxi.prefab");
        }

        static GameObject CreatePotholeObstaclePrefab()
        {
            GameObject root = new GameObject("PotholeObstacle");
            GameObject hole = Cylinder("PotholeObstacle_DarkPatch", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(1.55f, 0.08f, 1.1f), Mat("PotholeDarkAsphalt"));
            hole.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            AddObstacleCollider(root, new Vector3(0f, 0.45f, 0f), new Vector3(1.9f, 0.9f, 1.5f));
            return SavePrefab(root, PotholePrefabPath);
        }

        static GameObject CreateBarrierObstaclePrefab()
        {
            GameObject root = new GameObject("ConstructionBarrier");
            Cube("BarrierBody", root.transform, new Vector3(0f, 0.65f, 0f), new Vector3(2.35f, 1.05f, 0.42f), Mat("ConstructionBarrierOrange"));
            Cube("BarrierStripe", root.transform, new Vector3(0f, 0.8f, -0.24f), new Vector3(2.1f, 0.18f, 0.06f), Mat("PaintWhite"));
            Cube("BarrierBase", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(2.55f, 0.28f, 0.75f), Mat("ConstructionBarrierOrange"));
            AddObstacleCollider(root, new Vector3(0f, 0.62f, 0f), new Vector3(2.5f, 1.25f, 0.9f));
            return SavePrefab(root, BarrierPrefabPath);
        }

        static GameObject CreateCoinParticlePrefab()
        {
            Material coinGold = Mat("CoinGold");
            GameObject root = new GameObject("CoinCollectParticles");
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = 0.22f;
            main.startSpeed = 0.9f;
            main.startSize = 0.035f;
            main.maxParticles = 8;
            main.loop = false;
            main.playOnAwake = true;
            main.startColor = new ParticleSystem.MinMaxGradient(coinGold.color, Color.white);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.06f;

            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = coinGold;

            return SavePrefab(root, CoinParticlePrefabPath);
        }

        // Coin-collect animation prefab: the Higgsfield coin sprite that pops
        // at the pickup point (billboarded, rises and fades). Null when the art
        // is missing so coins simply collect without a pop.
        static GameObject CreateCoinPopPrefab()
        {
            Sprite sprite = LoadHiggsfieldSprite("PU_Coin");
            if (sprite == null)
            {
                return null;
            }

            GameObject root = new GameObject("CoinCollectPop");
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            float spriteHeight = sprite.bounds.size.y;
            if (spriteHeight > 0.01f)
            {
                root.transform.localScale = Vector3.one * (0.5f / spriteHeight);
            }

            root.AddComponent<Billboard>();
            root.AddComponent<CoinCollectPop>();
            return SavePrefab(root, "Assets/Prefabs/CoinCollectPop.prefab");
        }

        // Higgsfield coin art on a billboarded child, matching the menu/store
        // and the collect pop. Returns false when the sprite is missing.
        static bool AddSpriteCoinArt(Transform root, float heightUnits)
        {
            Sprite sprite = LoadHiggsfieldSprite("PU_Coin");
            if (sprite == null)
            {
                return false;
            }

            GameObject art = new GameObject("Art");
            art.transform.SetParent(root, false);
            SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            float spriteHeight = sprite.bounds.size.y;
            if (spriteHeight > 0.01f)
            {
                art.transform.localScale = Vector3.one * (heightUnits / spriteHeight);
            }

            art.AddComponent<Billboard>();
            return true;
        }

        static GameObject CreateCoinPrefab(GameObject coinPopPrefab)
        {
            GameObject root = new GameObject("GoldCoin");
            bool spriteCoin = AddSpriteCoinArt(root.transform, 0.95f);
            if (!spriteCoin)
            {
                if (!TryCreateR1CoinVisual(root.transform))
                {
                    GameObject coin = Cylinder("CoinVisual", root.transform, Vector3.zero, new Vector3(0.52f, 0.09f, 0.52f), Mat("CoinGold"));
                    coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                    GameObject rim = Cylinder("CoinInnerFace", root.transform, Vector3.zero, new Vector3(0.4f, 0.095f, 0.4f), Mat("PaintYellow"));
                    rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Sphere("CoinSparkle", root.transform, new Vector3(0.22f, 0.22f, -0.02f), new Vector3(0.1f, 0.1f, 0.1f), Mat("CoinGold"));
                }
                root.transform.localScale = Vector3.one * CoinPrefabScale;
            }

            foreach (Collider visualCollider in root.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(visualCollider);
            }

            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = CoinTriggerRadius;

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            Coin coinScript = root.AddComponent<Coin>();
            SetField(coinScript, "collectParticlePrefab", coinPopPrefab);
            if (spriteCoin)
            {
                SetField(coinScript, "rotationSpeed", 0f); // flat billboard must not spin edge-on
            }

            return SavePrefab(root, CoinPrefabPath);
        }

        // Rare R5: the R1 visual scaled up with a golden rim, worth five coins.
        static GameObject CreateRareCoinPrefab(GameObject coinPopPrefab)
        {
            GameObject root = new GameObject("RareCoinR5");
            bool spriteCoin = AddSpriteCoinArt(root.transform, 1.4f);
            if (!spriteCoin)
            {
                if (!TryCreateR1CoinVisual(root.transform))
                {
                    GameObject fallback = Cylinder("CoinVisual", root.transform, Vector3.zero, new Vector3(0.6f, 0.1f, 0.6f), Mat("CoinGold"));
                    fallback.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }

                GameObject rim = Cylinder("R5GoldRim", root.transform, Vector3.zero, new Vector3(1.04f, 0.035f, 1.04f), Mat("CoinGold"));
                rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                root.transform.localScale = Vector3.one * RareCoinPrefabScale;
            }

            foreach (Collider visualCollider in root.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(visualCollider);
            }

            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = spriteCoin ? CoinTriggerRadius * RareCoinPrefabScale : CoinTriggerRadius;

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            Coin coinScript = root.AddComponent<Coin>();
            SetField(coinScript, "collectParticlePrefab", coinPopPrefab);
            SetField(coinScript, "coinValue", 5);
            SetField(coinScript, "isRare", true);
            SetField(coinScript, "rotationSpeed", spriteCoin ? 0f : 180f);

            return SavePrefab(root, RareCoinPrefabPath);
        }

        // ------------------------------------------------------------------
        // Track chunks: hand-designed 12–15m road sections. Every chunk
        // declares which lanes are safe at its entry and exit (bitmask
        // 1=left, 2=centre, 4=right); the ChunkManager only chains chunks
        // whose masks are reachable, so no impossible situations can spawn.
        // Obstacles never sit in the first 3m of a chunk, leaving reaction
        // room across every chunk boundary.
        // ------------------------------------------------------------------

        static TrackChunk[] CreateTrackChunkPrefabs(GameObject taxi, GameObject coin, GameObject rareCoin, GameObject[] powerUps)
        {
            Directory.CreateDirectory("Assets/Prefabs/Chunks");
            var chunks = new List<TrackChunk>
            {
                // --- Easy: breathers, single hazards, simple coin lines ---
                NewChunkPrefab("Chunk_OpenRoad", ChunkDifficulty.Easy, 18f, 0b111, 0b111, 12f, _ => { }),
                NewChunkPrefab("Chunk_CoinLineCentre", ChunkDifficulty.Easy, 28f, 0b111, 0b111, 12f, t =>
                    ChunkCoinLine(t, coin, 1, 2f, 5)),
                NewChunkPrefab("Chunk_CoinLineLeft", ChunkDifficulty.Easy, 20f, 0b111, 0b111, 12f, t =>
                    ChunkCoinLine(t, coin, 0, 2f, 5)),
                NewChunkPrefab("Chunk_TaxiLeft", ChunkDifficulty.Easy, 24f, 0b110, 0b110, 14f, t =>
                {
                    ChunkObstacle(t, taxi, 0, 11f, true);
                    ChunkCoinLine(t, coin, 2, 3f, 4);
                }),
                NewChunkPrefab("Chunk_TaxiRight", ChunkDifficulty.Easy, 24f, 0b011, 0b011, 14f, t =>
                {
                    ChunkObstacle(t, taxi, 2, 11f, true);
                    ChunkCoinLine(t, coin, 0, 3f, 4);
                }),
                NewChunkPrefab("Chunk_TaxiCentre", ChunkDifficulty.Easy, 18f, 0b101, 0b101, 14f, t =>
                {
                    ChunkObstacle(t, taxi, 1, 11f, true);
                    ChunkCoinLine(t, coin, 0, 3f, 4);
                }),

                // --- Medium: combinations that steer the player ---
                NewChunkPrefab("Chunk_TaxiPair", ChunkDifficulty.Medium, 28f, 0b010, 0b010, 18f, t =>
                {
                    ChunkObstacle(t, taxi, 0, 8f, true);
                    ChunkObstacle(t, taxi, 2, 15f, true);
                    ChunkCoinLine(t, coin, 1, 2f, 6);
                }),
                NewChunkPrefab("Chunk_TaxiGateLeft", ChunkDifficulty.Medium, 20f, 0b100, 0b100, 18f, t =>
                {
                    ChunkObstacle(t, taxi, 0, 12f, true);
                    ChunkObstacle(t, taxi, 1, 12f, true);
                    ChunkCoinLine(t, coin, 2, 5f, 5);
                }),
                NewChunkPrefab("Chunk_TaxiGateRight", ChunkDifficulty.Medium, 20f, 0b001, 0b001, 18f, t =>
                {
                    ChunkObstacle(t, taxi, 1, 12f, true);
                    ChunkObstacle(t, taxi, 2, 12f, true);
                    ChunkCoinLine(t, coin, 0, 5f, 5);
                }),
                NewChunkPrefab("Chunk_TaxiCentreThenLeft", ChunkDifficulty.Medium, 16f, 0b100, 0b100, 20f, t =>
                {
                    ChunkObstacle(t, taxi, 1, 8f, true);
                    ChunkObstacle(t, taxi, 0, 15f, true);
                    ChunkCoinLine(t, coin, 2, 4f, 6);
                }),

                // --- Hard: taxi-only forced weaves and denser traffic ---
                NewChunkPrefab("Chunk_TaxiWeaveLeftToRight", ChunkDifficulty.Hard, 20f, 0b001, 0b100, 22f, t =>
                {
                    ChunkObstacle(t, taxi, 1, 7f, true);
                    ChunkObstacle(t, taxi, 2, 7f, true);
                    ChunkObstacle(t, taxi, 0, 16f, true);
                    ChunkObstacle(t, taxi, 1, 16f, true);
                    ChunkCoinLine(t, coin, 0, 8f, 2);
                    ChunkCoinLine(t, coin, 2, 17f, 2);
                }),
                NewChunkPrefab("Chunk_TaxiSwarm", ChunkDifficulty.Hard, 18f, 0b100, 0b100, 22f, t =>
                {
                    ChunkObstacle(t, taxi, 0, 7f, true);
                    ChunkObstacle(t, taxi, 1, 14f, true);
                    ChunkObstacle(t, taxi, 0, 19f, true);
                    ChunkCoinLine(t, coin, 2, 5f, 6);
                }),
                NewChunkPrefab("Chunk_TaxiThread", ChunkDifficulty.Hard, 16f, 0b010, 0b010, 22f, t =>
                {
                    ChunkObstacle(t, taxi, 0, 8f, true);
                    ChunkObstacle(t, taxi, 2, 8f, true);
                    ChunkObstacle(t, taxi, 0, 17f, true);
                    ChunkObstacle(t, taxi, 2, 17f, true);
                    ChunkCoinLine(t, coin, 1, 5f, 7);
                }),

                // --- Special: rewards ---
                NewChunkPrefab("Chunk_CoinTunnel", ChunkDifficulty.Special, 12f, 0b111, 0b111, 15f, t =>
                {
                    ChunkCoinLine(t, coin, 0, 2f, 6);
                    ChunkCoinLine(t, coin, 1, 2f, 6);
                    ChunkCoinLine(t, coin, 2, 2f, 6);
                }),
                NewChunkPrefab("Chunk_PowerUp", ChunkDifficulty.Special, 20f, 0b111, 0b111, 12f, t =>
                {
                    ChunkCoinLine(t, coin, 1, 2f, 3);
                    ChunkRandomPickOne(t, powerUps, 1, 9f, CoinTrailHeight);
                }),
                NewChunkPrefab("Chunk_HighReward", ChunkDifficulty.Special, 8f, 0b111, 0b111, 15f, t =>
                {
                    ChunkCoinLine(t, coin, 0, 2f, 6);
                    ChunkCoinLine(t, coin, 2, 2f, 6);
                    ChunkCoinArc(t, coin, 1, 2f, 5, 1.2f, CoinTrailHeight, CoinArcHeight);
                    GameObject reward = (GameObject)PrefabUtility.InstantiatePrefab(rareCoin, t);
                    reward.transform.localPosition = new Vector3(LaneX(1), CoinTrailHeight, 12.5f);
                }),
            };

            return chunks.ToArray();
        }

        static TrackChunk NewChunkPrefab(string name, ChunkDifficulty difficulty, float weight, int entryLanes, int exitLanes, float length, System.Action<Transform> build)
        {
            GameObject root = new GameObject(name);
            build(root.transform);

            TrackChunk chunk = root.AddComponent<TrackChunk>();
            SetField(chunk, "difficulty", difficulty);
            SetField(chunk, "weight", weight);
            SetField(chunk, "length", length);
            SetField(chunk, "entrySafeLanes", entryLanes);
            SetField(chunk, "exitSafeLanes", exitLanes);

            return SavePrefab(root, $"Assets/Prefabs/Chunks/{name}.prefab").GetComponent<TrackChunk>();
        }

        static void ChunkObstacle(Transform chunk, GameObject prefab, int lane, float z, bool moving)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, chunk);
            instance.transform.localPosition = new Vector3(LaneX(lane), prefab.GetComponent<Coin>() != null ? CoinTrailHeight : 0f, z);
            if (!moving)
            {
                MovingObstacle mover = instance.GetComponentInChildren<MovingObstacle>();
                if (mover != null)
                {
                    mover.enabled = false;
                }
            }
        }

        static void ChunkCoinLine(Transform chunk, GameObject coinPrefab, int lane, float zStart, int count, float spacing = 2f, float height = CoinTrailHeight)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk);
                coin.transform.localPosition = new Vector3(LaneX(lane), height, zStart + i * spacing);
            }
        }

        // Jump coins: an arch the player collects at the top of a jump.
        static void ChunkCoinArc(Transform chunk, GameObject coinPrefab, int lane, float zStart, int count, float spacing, float baseHeight, float arcHeight)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? (float)i / (count - 1) : 0.5f;
                float height = baseHeight + Mathf.Sin(t * Mathf.PI) * arcHeight;
                GameObject coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk);
                coin.transform.localPosition = new Vector3(LaneX(lane), height, zStart + i * spacing);
            }
        }

        // All options instantiated under a "RandomPickOne" group; TrackChunk
        // enables exactly one per spawn, so pooled chunks vary with zero
        // runtime instantiation.
        static void ChunkRandomPickOne(Transform chunk, GameObject[] options, int lane, float z, float height)
        {
            GameObject group = new GameObject("RandomPickOne");
            group.transform.SetParent(chunk, false);
            foreach (GameObject option in options)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(option, group.transform);
                instance.transform.localPosition = new Vector3(LaneX(lane), height, z);
            }
        }

        static float LaneX(int lane) => (lane - 1) * 2.7f;

        static GameObject[] CreatePowerUpPrefabs()
        {
            // Show the same Higgsfield pickup art the menu and store use, as a
            // camera-facing billboard in the world. Falls back to the built
            // primitive pickup if a sprite is missing.
            return new[]
            {
                SpritePickupOrPrimitive(PowerUpType.TaxiMagnet, "PU_Magnet", CreateMagnetPrefab),
                SpritePickupOrPrimitive(PowerUpType.JoziSneakers, "PU_Sneaker", CreateSneakersPrefab),
                SpritePickupOrPrimitive(PowerUpType.DroneBoost, "PU_TaxiBoost", CreateDronePrefab),
                SpritePickupOrPrimitive(PowerUpType.UbuntuMultiplier, "PU_Multiplier2x", CreateUbuntuStarPrefab),
                SpritePickupOrPrimitive(PowerUpType.Hoverboard, "PU_Shield", CreateHoverboardPrefab),
            };
        }

        static GameObject SpritePickupOrPrimitive(PowerUpType type, string spriteName, System.Func<GameObject> primitiveFallback)
        {
            Sprite sprite = LoadHiggsfieldSprite(spriteName);
            return sprite != null ? CreateSpritePickupPrefab(type, sprite) : primitiveFallback();
        }

        // Higgsfield art on a billboarded child; the root keeps the full-size
        // trigger so scaling the art never shrinks the pickup's reach.
        static GameObject CreateSpritePickupPrefab(PowerUpType type, Sprite sprite)
        {
            GameObject root = new GameObject($"PowerUp{type}");

            GameObject art = new GameObject("Art");
            art.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            float spriteHeight = sprite.bounds.size.y;
            if (spriteHeight > 0.01f)
            {
                art.transform.localScale = Vector3.one * (1.2f / spriteHeight);
            }

            art.AddComponent<Billboard>();

            // A flat sprite must not spin about Y (it would turn edge-on); the
            // billboard keeps it facing the camera and PowerUp still bobs it.
            return FinishPowerUpPrefab(root, type, 0f);
        }

        // 🧲 Taxi Magnet: red horseshoe with white tips.
        static GameObject CreateMagnetPrefab()
        {
            GameObject root = new GameObject("PowerUpTaxiMagnet");
            Cube("MagnetLegL", root.transform, new Vector3(-0.22f, -0.06f, 0f), new Vector3(0.18f, 0.5f, 0.18f), Mat("PowerRed"));
            Cube("MagnetLegR", root.transform, new Vector3(0.22f, -0.06f, 0f), new Vector3(0.18f, 0.5f, 0.18f), Mat("PowerRed"));
            Cube("MagnetArch", root.transform, new Vector3(0f, 0.24f, 0f), new Vector3(0.62f, 0.2f, 0.18f), Mat("PowerRed"));
            Cube("MagnetTipL", root.transform, new Vector3(-0.22f, -0.34f, 0f), new Vector3(0.18f, 0.12f, 0.19f), Mat("PaintWhite"));
            Cube("MagnetTipR", root.transform, new Vector3(0.22f, -0.34f, 0f), new Vector3(0.18f, 0.12f, 0.19f), Mat("PaintWhite"));
            return FinishPowerUpPrefab(root, PowerUpType.TaxiMagnet);
        }

        // 👟 Jozi Sneakers: white high-top with an orange sole and blue stripe.
        static GameObject CreateSneakersPrefab()
        {
            GameObject root = new GameObject("PowerUpJoziSneakers");
            Cube("SneakerSole", root.transform, new Vector3(0f, -0.2f, 0f), new Vector3(0.64f, 0.1f, 0.28f), Mat("ConstructionBarrierOrange"));
            Cube("SneakerBody", root.transform, new Vector3(-0.05f, -0.04f, 0f), new Vector3(0.52f, 0.22f, 0.26f), Mat("SneakerWhite"));
            Cube("SneakerToe", root.transform, new Vector3(0.26f, -0.09f, 0f), new Vector3(0.14f, 0.12f, 0.26f), Mat("SneakerWhite"));
            Cube("SneakerAnkle", root.transform, new Vector3(-0.2f, 0.15f, 0f), new Vector3(0.22f, 0.18f, 0.24f), Mat("SneakerWhite"));
            Cube("SneakerStripe", root.transform, new Vector3(-0.02f, -0.02f, 0.135f), new Vector3(0.34f, 0.08f, 0.02f), Mat("PowerBlue"));
            return FinishPowerUpPrefab(root, PowerUpType.JoziSneakers);
        }

        // 🚀 Drone Boost: dark quad body with purple rotor discs.
        static GameObject CreateDronePrefab()
        {
            GameObject root = new GameObject("PowerUpDroneBoost");
            Cube("DroneBody", root.transform, Vector3.zero, new Vector3(0.34f, 0.12f, 0.34f), Mat("MetalDark"));
            Sphere("DroneCamera", root.transform, new Vector3(0f, -0.06f, 0.14f), new Vector3(0.1f, 0.1f, 0.1f), Mat("PowerBlue"));
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Cube($"DroneArm{x}{z}", root.transform, new Vector3(x * 0.2f, 0f, z * 0.2f), new Vector3(0.24f, 0.05f, 0.06f), Mat("MetalDark"), new Vector3(0f, x * z * 45f, 0f));
                    Cylinder($"DroneRotor{x}{z}", root.transform, new Vector3(x * 0.3f, 0.06f, z * 0.3f), new Vector3(0.24f, 0.012f, 0.24f), Mat("PowerPurple"));
                }
            }

            return FinishPowerUpPrefab(root, PowerUpType.DroneBoost);
        }

        // ⭐ Ubuntu Multiplier: layered golden star.
        static GameObject CreateUbuntuStarPrefab()
        {
            GameObject root = new GameObject("PowerUpUbuntuMultiplier");
            Cube("StarBarA", root.transform, Vector3.zero, new Vector3(0.7f, 0.24f, 0.1f), Mat("CoinGold"));
            Cube("StarBarB", root.transform, Vector3.zero, new Vector3(0.24f, 0.7f, 0.1f), Mat("CoinGold"));
            Cube("StarBarC", root.transform, Vector3.zero, new Vector3(0.5f, 0.5f, 0.09f), Mat("CoinGold"), new Vector3(0f, 0f, 45f));
            return FinishPowerUpPrefab(root, PowerUpType.UbuntuMultiplier);
        }

        // 🛹 Hoverboard: green deck with white stripe and black wheels.
        static GameObject CreateHoverboardPrefab()
        {
            GameObject root = new GameObject("PowerUpHoverboard");
            Cube("BoardDeck", root.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.72f, 0.06f, 0.3f), Mat("PowerGreen"));
            Cube("BoardStripe", root.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.6f, 0.02f, 0.12f), Mat("PaintWhite"));
            Cylinder("BoardWheelF", root.transform, new Vector3(0.24f, -0.08f, 0f), new Vector3(0.12f, 0.08f, 0.12f), Mat("WheelBlack"), new Vector3(0f, 0f, 90f));
            Cylinder("BoardWheelB", root.transform, new Vector3(-0.24f, -0.08f, 0f), new Vector3(0.12f, 0.08f, 0.12f), Mat("WheelBlack"), new Vector3(0f, 0f, 90f));
            return FinishPowerUpPrefab(root, PowerUpType.Hoverboard);
        }

        static GameObject FinishPowerUpPrefab(GameObject root, PowerUpType type, float rotationSpeed = 90f)
        {
            StripColliders(root);
            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.75f;

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            PowerUp powerUp = root.AddComponent<PowerUp>();
            SetField(powerUp, "type", type);
            SetField(powerUp, "rotationSpeed", rotationSpeed);

            return SavePrefab(root, $"Assets/Prefabs/{root.name}.prefab");
        }

        // One R1 coin from the two Meshy face models mounted back to back:
        // tails (impala) faces -Z toward the chase camera, heads (coat of
        // arms) faces +Z down the road. Coin.Update supplies the spin.
        static bool TryCreateR1CoinVisual(Transform root)
        {
            GameObject tails = CreateR1CoinFace("R1TailsFace", CoinTailsModelPath, CoinTailsMaterialPath, root, true);
            GameObject heads = CreateR1CoinFace("R1HeadsFace", CoinHeadsModelPath, CoinHeadsMaterialPath, root, false);
            if (tails == null || heads == null)
            {
                Debug.LogWarning("R1 coin models missing; using generated fallback coin.");
                while (root.childCount > 0)
                {
                    Object.DestroyImmediate(root.GetChild(0).gameObject);
                }

                return false;
            }

            return true;
        }

        static GameObject CreateR1CoinFace(string name, string modelPath, string materialPath, Transform root, bool facesCamera)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                return null;
            }

            GameObject visual = Object.Instantiate(model, root);
            visual.name = name;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // The coin relief needs a bigger budget than scenery models or it
            // decimates into crumpled foil — but coins are also the most
            // numerous mesh on screen, so keep the budget as low as the
            // relief allows for mobile framerate.
            SimplifyMeshes(visual, 9000);
            AssignMaterial(visual, GetR1CoinFaceMaterial(name, modelPath, materialPath));

            Bounds bounds = CombinedRendererBounds(visual);
            if (bounds.size.sqrMagnitude < 0.0001f)
            {
                Object.DestroyImmediate(visual);
                return null;
            }

            // Point the thinnest bounds axis (the face normal) along Z.
            if (bounds.size.x <= bounds.size.y && bounds.size.x <= bounds.size.z)
            {
                visual.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
            else if (bounds.size.y <= bounds.size.x && bounds.size.y <= bounds.size.z)
            {
                visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            bounds = CombinedRendererBounds(visual);
            float diameter = Mathf.Max(bounds.size.x, bounds.size.y);
            if (diameter > 0.0001f)
            {
                visual.transform.localScale *= 0.46f / diameter;
            }

            if (facesCamera)
            {
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * visual.transform.localRotation;
            }

            // Both Meshy exports come out upside down once stood on edge;
            // spin them the right way up around the face normal (world Z,
            // since the coin root is built at identity).
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, 180f) * visual.transform.localRotation;

            bounds = CombinedRendererBounds(visual);
            float zOffset = (facesCamera ? -1f : 1f) * bounds.size.z * 0.25f;
            visual.transform.position += root.position + new Vector3(0f, 0f, zOffset) - bounds.center;
            return visual;
        }

        static Bounds CombinedRendererBounds(GameObject visual)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(visual.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        static Material GetR1CoinFaceMaterial(string name, string modelPath, string materialPath)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing != null)
            {
                return existing;
            }

            string basePath = modelPath.Substring(0, modelPath.Length - ".fbx".Length);
            Texture2D albedo = LoadCoinTexture($"{basePath}_basecolor.png", false);
            Texture2D normal = LoadCoinTexture($"{basePath}_normal.png", true);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = $"{name}_URP" };
            material.SetFloat("_Metallic", 0.55f);
            material.SetFloat("_Smoothness", 0.35f);

            if (albedo != null)
            {
                material.SetTexture("_BaseMap", albedo);
                material.SetTexture("_MainTex", albedo);
            }

            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        static Texture2D LoadCoinTexture(string path, bool isNormalMap)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.maxTextureSize > 1024)
                {
                    importer.maxTextureSize = 1024;
                    dirty = true;
                }

                if (isNormalMap && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void AddObstacleCollider(GameObject root, Vector3 center, Vector3 size)
        {
            foreach (Collider visualCollider in root.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(visualCollider);
            }

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            root.AddComponent<RunnerObstacle>();
        }

        static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------------
        // Scene: lighting, skyline, player, camera, systems, UI
        // ------------------------------------------------------------------

        static void CreateLighting()
        {
            Transform lighting = EnvironmentCategory("Lighting");
            RenderSettings.ambientLight = new Color(0.6f, 0.65f, 0.72f);

            GameObject sun = new GameObject("JohannesburgSun");
            sun.transform.SetParent(lighting);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(50f, -25f, 0f);

            Material skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (skybox == null)
            {
                skybox = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(skybox, SkyboxPath);
            }

            skybox.SetFloat("_SunSize", 0.045f);
            skybox.SetFloat("_SunSizeConvergence", 5f);
            skybox.SetFloat("_AtmosphereThickness", 1f);
            skybox.SetColor("_SkyTint", new Color(0.5f, 0.5f, 0.5f));
            skybox.SetColor("_GroundColor", new Color(0.55f, 0.55f, 0.55f));
            skybox.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(skybox);

            RenderSettings.skybox = skybox;
            RenderSettings.sun = light;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.79f, 0.86f);
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 260f;
        }

        static void CreateSkyline(Transform player)
        {
            GameObject skyline = new GameObject("JoburgSkylineBackdrop");
            skyline.transform.SetParent(EnvironmentCategory("Skybox"));
            SkylineFollower follower = skyline.AddComponent<SkylineFollower>();
            SetField(follower, "player", player);
            SetField(follower, "forwardOffset", 140f);
            skyline.transform.position = new Vector3(0f, 0f, player.position.z + 140f);
            Transform t = skyline.transform;

            // Telkom/Hillbrow-style tower from the supplied FBX, centre-left of the horizon.
            Vector3 towerBase = new Vector3(-9f, 0f, 20f);
            if (!CreateTelkomTower(t, towerBase))
            {
                Cylinder("HillbrowTower_Shaft", t, towerBase + new Vector3(0f, 21f, 0f), new Vector3(2.6f, 21f, 2.6f), Mat("TowerConcrete"));
                Cylinder("HillbrowTower_LowerDeck", t, towerBase + new Vector3(0f, 33f, 0f), new Vector3(5.2f, 1f, 5.2f), Mat("TowerConcrete"));
                Cylinder("HillbrowTower_MainDeck", t, towerBase + new Vector3(0f, 35.2f, 0f), new Vector3(5.6f, 1.6f, 5.6f), Mat("TowerConcrete"));
                Cylinder("HillbrowTower_UpperDeck", t, towerBase + new Vector3(0f, 37.2f, 0f), new Vector3(4.9f, 0.9f, 4.9f), Mat("TowerConcrete"));
                Cylinder("HillbrowTower_AntennaRed", t, towerBase + new Vector3(0f, 41f, 0f), new Vector3(0.5f, 3f, 0.5f), Mat("AntennaRed"));
                Cylinder("HillbrowTower_AntennaWhiteBand", t, towerBase + new Vector3(0f, 45.2f, 0f), new Vector3(0.4f, 1.2f, 0.4f), Mat("PaintWhite"));
                Cylinder("HillbrowTower_AntennaTip", t, towerBase + new Vector3(0f, 47.4f, 0f), new Vector3(0.28f, 1f, 0.28f), Mat("AntennaRed"));
            }

            // Photo billboard cropped from the reference video, behind the 3D towers
            CreateVideoSkylineBillboard(t);

            // High-rise band across the horizon
            Material[] towerMats = { Mat("SkyscraperGlass"), Mat("SkyscraperConcrete"), Mat("SkylineHaze") };
            for (int i = 0; i < 16; i++)
            {
                float x = -54f + i * 7.2f + (i % 3) * 1.6f;
                float height = 15f + ((i * 7) % 6) * 4.2f;
                float depth = 12f + (i % 4) * 9f;
                Vector3 size = new Vector3(4.2f + (i % 3) * 1.4f, height, 4.5f + (i % 2) * 2f);
                Cube("CitySkyscraper", t, new Vector3(x, height * 0.5f, depth), size, towerMats[i % 3]);

                if (i % 4 == 0)
                {
                    Cube("CitySkyscraper_RoofPlant", t, new Vector3(x, height + 1f, depth), new Vector3(1.6f, 2f, 1.6f), Mat("SkyscraperConcrete"));
                }
            }

            CloudCluster(t, new Vector3(-40f, 40f, 38f), 1.2f);
            CloudCluster(t, new Vector3(46f, 44f, 45f), 1.0f);
            CloudCluster(t, new Vector3(18f, 48f, 52f), 0.8f);
        }

        static bool CreateTelkomTower(Transform parent, Vector3 position)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(TelkomTowerModelPath);
            if (model == null)
            {
                Debug.LogWarning($"Telkom tower model not found at {TelkomTowerModelPath}; using generated fallback tower.");
                return false;
            }

            GameObject root = new GameObject("TelkomTower");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.identity;

            GameObject visual = Object.Instantiate(model, root.transform);
            visual.name = "TelkomTower_FBX";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            OrientTallModelUpright(visual);
            NormalizeStaticModelBounds(visual, root.transform, 48f);
            AssignMaterial(visual, GetTelkomTowerMaterial());
            SimplifyMeshes(visual, 60000);
            StripColliders(root);
            return true;
        }

        static Material GetTelkomTowerMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(TelkomTowerMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TelkomTowerAlbedoPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TelkomTowerNormalPath);
            if (normal != null)
            {
                TextureImporter importer = AssetImporter.GetAtPath(TelkomTowerNormalPath) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                    normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TelkomTowerNormalPath);
                }
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = "TelkomTower_URP",
                color = new Color(0.78f, 0.78f, 0.74f, 1f)
            };

            if (albedo != null)
            {
                material.SetTexture("_BaseMap", albedo);
                material.SetTexture("_MainTex", albedo);
            }

            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            AssetDatabase.CreateAsset(material, TelkomTowerMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        static void AssignMaterial(GameObject target, Material material)
        {
            if (material == null)
            {
                return;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        static void OrientTallModelUpright(GameObject visual)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            Vector3 size = bounds.size;
            if (size.z > size.y && size.z > size.x)
            {
                visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            }
            else if (size.x > size.y && size.x > size.z)
            {
                visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }

        static void NormalizeStaticModelBounds(GameObject visual, Transform root, float targetHeight)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            if (bounds.size.y < 0.0001f)
            {
                return;
            }

            visual.transform.localScale *= targetHeight / bounds.size.y;

            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            visual.transform.position += new Vector3(
                root.position.x - bounds.center.x,
                root.position.y - bounds.min.y,
                root.position.z - bounds.center.z);
        }

        static Transform EnvironmentCategory(string name)
        {
            GameObject environment = GameObject.Find("Environment");
            if (environment == null)
            {
                environment = new GameObject("Environment");
            }

            Transform existing = environment.transform.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject category = new GameObject(name);
            category.transform.SetParent(environment.transform);
            category.transform.localPosition = Vector3.zero;
            return category.transform;
        }

        static void CloudCluster(Transform parent, Vector3 position, float scale)
        {
            GameObject root = new GameObject("SoftCloud");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            Transform t = root.transform;
            Sphere("CloudPuffA", t, new Vector3(-1.2f, 0f, 0f), new Vector3(3.2f, 0.8f, 1.2f) * scale, Mat("CloudWhite"));
            Sphere("CloudPuffB", t, new Vector3(0.5f, 0.25f, 0.2f), new Vector3(3.8f, 1f, 1.4f) * scale, Mat("CloudWhite"));
            Sphere("CloudPuffC", t, new Vector3(2.3f, 0f, -0.2f), new Vector3(2.8f, 0.75f, 1.1f) * scale, Mat("CloudWhite"));
            StripColliders(root);
        }

        /// <summary>
        /// Mounts the skyline strip cropped from the reference video on a huge
        /// transparent quad behind the 3D towers. The texture mirrors horizontally
        /// so one frame can span the whole horizon without a visible seam.
        /// </summary>
        static void CreateVideoSkylineBillboard(Transform skylineRoot)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(VideoSkylineTexturePath);
            if (texture == null)
            {
                return;
            }

            TextureImporter textureImporter = AssetImporter.GetAtPath(VideoSkylineTexturePath) as TextureImporter;
            if (textureImporter != null && textureImporter.wrapModeU != TextureWrapMode.Mirror)
            {
                textureImporter.wrapModeU = TextureWrapMode.Mirror;
                textureImporter.wrapModeV = TextureWrapMode.Clamp;
                textureImporter.mipmapEnabled = true;
                textureImporter.SaveAndReimport();
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(VideoSkylineMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, VideoSkylineMaterialPath);
            }

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.mainTexture = texture;
            material.mainTextureScale = new Vector2(2f, 1f); // two mirrored copies across the width
            material.color = Color.white;
            EditorUtility.SetDirty(material);

            GameObject billboard = GameObject.CreatePrimitive(PrimitiveType.Quad);
            billboard.name = "VideoSkylineBillboard";
            billboard.transform.SetParent(skylineRoot);
            billboard.transform.localPosition = new Vector3(0f, 23f, 50f);
            billboard.transform.localScale = new Vector3(200f, 50f, 1f);
            Object.DestroyImmediate(billboard.GetComponent<Collider>());
            billboard.GetComponent<Renderer>().sharedMaterial = material;
        }

        static GameObject CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.05f, 2f);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.9f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 0.95f, 0f);

            if (TryCreateFbxRunnerVisual(player.transform))
            {
                foreach (Collider fbxCollider in player.GetComponentsInChildren<Collider>())
                {
                    if (fbxCollider.gameObject != player)
                    {
                        Object.DestroyImmediate(fbxCollider);
                    }
                }

                AddPlayerControlStack(player);
                return player;
            }

            GameObject visual = new GameObject("RunnerVisual");
            visual.transform.SetParent(player.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(7f, 0f, 0f); // sprint lean
            Transform v = visual.transform;

            // Torso: green running vest with white side trim over black shorts
            Capsule("GreenRunningVest", v, new Vector3(0f, 1.26f, 0f), new Vector3(0.46f, 0.36f, 0.3f), Mat("VestGreen"));
            Cube("VestWhiteTrim_Left", v, new Vector3(-0.21f, 1.24f, 0f), new Vector3(0.05f, 0.4f, 0.18f), Mat("VestWhite"));
            Cube("VestWhiteTrim_Right", v, new Vector3(0.21f, 1.24f, 0f), new Vector3(0.05f, 0.4f, 0.18f), Mat("VestWhite"));
            Sphere("LeftShoulder_Skin", v, new Vector3(-0.24f, 1.52f, 0f), new Vector3(0.16f, 0.16f, 0.16f), Mat("RunnerSkin"));
            Sphere("RightShoulder_Skin", v, new Vector3(0.24f, 1.52f, 0f), new Vector3(0.16f, 0.16f, 0.16f), Mat("RunnerSkin"));
            Cube("BlackRunningShorts", v, new Vector3(0f, 0.9f, 0f), new Vector3(0.42f, 0.3f, 0.27f), Mat("ShortsBlack"));

            // Head with short brown hair
            Cylinder("Neck_Skin", v, new Vector3(0f, 1.62f, 0f), new Vector3(0.09f, 0.07f, 0.09f), Mat("RunnerSkin"));
            Sphere("Head_Skin", v, new Vector3(0f, 1.8f, 0f), new Vector3(0.32f, 0.34f, 0.32f), Mat("RunnerSkin"));
            Sphere("ShortBrownHair", v, new Vector3(0f, 1.9f, -0.04f), new Vector3(0.34f, 0.24f, 0.34f), Mat("HairBrown"));

            // Two-segment arms: shoulder pivot swings, elbow holds a fixed runner's bend
            Transform leftArmPivot = Pivot("LeftArmPivot", v, new Vector3(-0.31f, 1.5f, 0f));
            Capsule("LeftUpperArm_Skin", leftArmPivot, new Vector3(0f, -0.16f, 0f), new Vector3(0.09f, 0.17f, 0.09f), Mat("RunnerSkin"));
            Transform leftElbow = Pivot("LeftElbowPivot", leftArmPivot, new Vector3(0f, -0.32f, 0f));
            leftElbow.localRotation = Quaternion.Euler(-70f, 0f, 0f);
            Capsule("LeftForearm_Skin", leftElbow, new Vector3(0f, -0.14f, 0f), new Vector3(0.08f, 0.15f, 0.08f), Mat("RunnerSkin"));
            Cube("BlackWristwatch", leftElbow, new Vector3(0f, -0.25f, 0f), new Vector3(0.11f, 0.05f, 0.11f), Mat("ShortsBlack"));
            Sphere("LeftHand_Skin", leftElbow, new Vector3(0f, -0.32f, 0f), new Vector3(0.11f, 0.11f, 0.11f), Mat("RunnerSkin"));

            Transform rightArmPivot = Pivot("RightArmPivot", v, new Vector3(0.31f, 1.5f, 0f));
            Capsule("RightUpperArm_Skin", rightArmPivot, new Vector3(0f, -0.16f, 0f), new Vector3(0.09f, 0.17f, 0.09f), Mat("RunnerSkin"));
            Transform rightElbow = Pivot("RightElbowPivot", rightArmPivot, new Vector3(0f, -0.32f, 0f));
            rightElbow.localRotation = Quaternion.Euler(-70f, 0f, 0f);
            Capsule("RightForearm_Skin", rightElbow, new Vector3(0f, -0.14f, 0f), new Vector3(0.08f, 0.15f, 0.08f), Mat("RunnerSkin"));
            Sphere("RightHand_Skin", rightElbow, new Vector3(0f, -0.32f, 0f), new Vector3(0.11f, 0.11f, 0.11f), Mat("RunnerSkin"));

            // Two-segment legs: hip pivot swings, knee pivot folds during the back-swing
            Transform leftLegPivot = Pivot("LeftLegPivot", v, new Vector3(-0.13f, 0.88f, 0f));
            Cylinder("LeftThigh_Skin", leftLegPivot, new Vector3(0f, -0.2f, 0f), new Vector3(0.105f, 0.2f, 0.105f), Mat("RunnerSkin"));
            Transform leftKnee = Pivot("LeftKneePivot", leftLegPivot, new Vector3(0f, -0.4f, 0f));
            Cylinder("LeftShin_Skin", leftKnee, new Vector3(0f, -0.18f, 0f), new Vector3(0.085f, 0.18f, 0.085f), Mat("RunnerSkin"));
            Cube("LeftOrangeSneaker", leftKnee, new Vector3(0f, -0.4f, 0.05f), new Vector3(0.2f, 0.11f, 0.34f), Mat("ShoeOrange"));
            Cube("LeftWhiteSole", leftKnee, new Vector3(0f, -0.46f, 0.05f), new Vector3(0.21f, 0.04f, 0.36f), Mat("ShoeWhite"));

            Transform rightLegPivot = Pivot("RightLegPivot", v, new Vector3(0.13f, 0.88f, 0f));
            Cylinder("RightThigh_Skin", rightLegPivot, new Vector3(0f, -0.2f, 0f), new Vector3(0.105f, 0.2f, 0.105f), Mat("RunnerSkin"));
            Transform rightKnee = Pivot("RightKneePivot", rightLegPivot, new Vector3(0f, -0.4f, 0f));
            Cylinder("RightShin_Skin", rightKnee, new Vector3(0f, -0.18f, 0f), new Vector3(0.085f, 0.18f, 0.085f), Mat("RunnerSkin"));
            Cube("RightOrangeSneaker", rightKnee, new Vector3(0f, -0.4f, 0.05f), new Vector3(0.2f, 0.11f, 0.34f), Mat("ShoeOrange"));
            Cube("RightWhiteSole", rightKnee, new Vector3(0f, -0.46f, 0.05f), new Vector3(0.21f, 0.04f, 0.36f), Mat("ShoeWhite"));

            foreach (Collider collider in player.GetComponentsInChildren<Collider>())
            {
                if (collider.gameObject != player)
                {
                    Object.DestroyImmediate(collider);
                }
            }

            AddPlayerControlStack(player);

            RunnerLimbSwing limbSwing = player.AddComponent<RunnerLimbSwing>();
            SetField(limbSwing, "visualRoot", visual.transform);
            SetField(limbSwing, "leftArmPivot", leftArmPivot);
            SetField(limbSwing, "rightArmPivot", rightArmPivot);
            SetField(limbSwing, "leftLegPivot", leftLegPivot);
            SetField(limbSwing, "rightLegPivot", rightLegPivot);
            SetField(limbSwing, "leftKneePivot", leftKnee);
            SetField(limbSwing, "rightKneePivot", rightKnee);
            SetField(limbSwing, "bobHeight", 0f);

            AddRunnerPresentation(visual, limbSwing);

            return player;
        }

        static void AddPlayerControlStack(GameObject player)
        {
            // Hierarchy: Player (moves, never rotates) → ModelLeanPivot
            // (cosmetic lane-change roll only) → character model.
            Transform leanPivot = null;
            if (player.transform.childCount > 0)
            {
                Transform model = player.transform.GetChild(0);
                Vector3 modelLocalPosition = model.localPosition;
                Quaternion modelLocalRotation = model.localRotation;
                Vector3 modelLocalScale = model.localScale;

                GameObject pivotObject = new GameObject("ModelLeanPivot");
                pivotObject.transform.SetParent(player.transform, false);
                pivotObject.transform.localPosition = Vector3.zero;
                pivotObject.transform.localRotation = Quaternion.identity;
                pivotObject.transform.localScale = Vector3.one;

                model.SetParent(pivotObject.transform, false);
                model.localPosition = modelLocalPosition;
                model.localRotation = modelLocalRotation;
                model.localScale = modelLocalScale;
                leanPivot = pivotObject.transform;
            }

            player.AddComponent<PlayerAnimator>();
            player.AddComponent<RollController>();
            player.AddComponent<PlayerController>();

            RunnerLeanVisual lean = player.AddComponent<RunnerLeanVisual>();
            SetField(lean, "leanPivot", leanPivot);
        }

        /// <summary>
        /// Uses a rigged humanoid FBX (e.g. a Mixamo download dropped into Assets/)
        /// as the player visual with its run animation looping. Returns false when
        /// no suitable model exists so the primitive runner is used instead.
        /// </summary>
        static bool TryCreateFbxRunnerVisual(Transform playerRoot)
        {
            string modelPath = FindRunnerModelPath();
            if (modelPath == null)
            {
                return false;
            }

            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                return false;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.loopTime = true;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            // Non-standard rigs that fail humanoid mapping fall back to Generic,
            // which plays the clip bundled in the same file without retargeting.
            Avatar avatar = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is Avatar foundAvatar)
                {
                    avatar = foundAvatar;
                    break;
                }
            }

            if (avatar == null || !avatar.isValid)
            {
                Debug.LogWarning($"Humanoid mapping failed for {modelPath}; importing as Generic rig.");
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.SaveAndReimport();
            }

            bool hasTextures = ExtractEmbeddedTextures(importer, modelPath);

            AnimationClip runClip = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview"))
                {
                    runClip = clip;
                    break;
                }
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null || runClip == null)
            {
                Debug.LogWarning($"Found runner model at {modelPath} but could not load a run clip; using primitive runner.");
                return false;
            }

            GameObject visual = Object.Instantiate(model, playerRoot);
            visual.name = "RunnerVisual_Fbx";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = CreateRunnerAnimatorController(runClip);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Recompute skinned bounds from the live pose every frame. This
            // keeps the mesh measured at its real extents — so scale and
            // grounding fit any rig — and culled only when genuinely off
            // screen. (The old lane-change vanishing came from stale tight
            // bounds under updateWhenOffscreen=false, not from tight bounds
            // as such; a fixed oversized box fixed culling but broke scale
            // and grounding for models sized differently from the first one.)
            foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.updateWhenOffscreen = true;
            }

            NormalizeRunnerScale(visual, playerRoot);
            SimplifyMeshes(visual, 50000);

            if (!hasTextures && !TryApplyExtractedTextures(visual, modelPath)
                && !TryApplyExternalPbrTextures(visual, modelPath))
            {
                RemapUntexturedRunnerMaterials(visual);
            }

            AddRunnerPresentation(visual, null);

            Debug.Log($"Player visual using rigged model '{modelPath}' with clip '{runClip.name}' (textured: {hasTextures}, triangles: {CountTriangles(visual)}).");
            return true;
        }

        static void AddRunnerPresentation(GameObject visual, RunnerLimbSwing limbSwing)
        {
            PlayerRollVisual rollVisual = visual.AddComponent<PlayerRollVisual>();
            SetField(rollVisual, "visualRoot", visual.transform);
            SetField(rollVisual, "limbSwing", limbSwing);

            PlayerDeathVisual deathVisual = visual.AddComponent<PlayerDeathVisual>();
            SetField(deathVisual, "visualRoot", visual.transform);
            SetField(deathVisual, "limbSwing", limbSwing);
            SetField(deathVisual, "rollVisual", rollVisual);

            RunnerVisualGrounder grounder = visual.AddComponent<RunnerVisualGrounder>();
            SetField(grounder, "visualRoot", visual.transform);
            SetField(grounder, "groundRoot", visual.transform.root);
            SetField(grounder, "groundSink", RunnerGroundSink);

            // Faces the camera while idling/dancing; gameManager wired in CreateSystems.
            IdleFacing idleFacing = visual.AddComponent<IdleFacing>();
            SetField(idleFacing, "visual", visual.transform);
        }

        /// <summary>
        /// Extracts textures embedded in the FBX into the project (equivalent to the
        /// Inspector's "Extract Textures" button) so the character's materials pick
        /// them up. Returns true when the model has textures.
        /// </summary>
        static bool ExtractEmbeddedTextures(ModelImporter importer, string modelPath)
        {
            bool hasEmbedded = false;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is Texture)
                {
                    hasEmbedded = true;
                    break;
                }
            }

            if (!hasEmbedded)
            {
                return false;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            {
                AssetDatabase.CreateFolder("Assets", "Textures");
            }

            string folder = "Assets/Textures/" + Path.GetFileNameWithoutExtension(modelPath).Replace(" ", "");
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Textures", Path.GetFileName(folder));
                importer.ExtractTextures(folder);
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
            }

            return true;
        }

        /// <summary>
        /// When Unity fails to decode an FBX's embedded textures, use diffuse maps
        /// that were extracted from the FBX into Assets/Textures/&lt;Model&gt;Extracted
        /// and assign them per material slot (Mixamo tile 1001 → first material,
        /// 1002 → "...1"/eyelash materials). Returns false when none are available.
        /// </summary>
        static bool TryApplyExtractedTextures(GameObject visual, string modelPath)
        {
            string folder = "Assets/Textures/" + Path.GetFileNameWithoutExtension(modelPath).Replace(" ", "") + "Extracted";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return false;
            }

            List<string> diffusePaths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path).ToLowerInvariant().Contains("diffuse"))
                {
                    diffusePaths.Add(path);
                }
            }

            if (diffusePaths.Count == 0)
            {
                return false;
            }

            diffusePaths.Sort();
            Material[] tileMaterials = new Material[diffusePaths.Count];
            for (int i = 0; i < diffusePaths.Count; i++)
            {
                Material material = CreateMaterial($"RunnerCharacterPart{i}", Color.white);
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePaths[i]);
                EditorUtility.SetDirty(material);
                tileMaterials[i] = material;
            }

            foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string slotName = materials[i] != null ? materials[i].name.ToLowerInvariant() : "";
                    int tile = slotName.EndsWith("1") || slotName.Contains("eyelash") ? 1 : 0;
                    materials[i] = tileMaterials[Mathf.Min(tile, tileMaterials.Length - 1)];
                }

                renderer.sharedMaterials = materials;
            }

            return true;
        }

        /// <summary>
        /// Mixamo mannequins (X Bot / Y Bot) ship without textures and render as
        /// flat default colors. Recolor them with the game palette: green surface
        /// panels with black joints to echo the reference runner's green-and-black
        /// kit. Clothed characters with textures are left untouched.
        /// </summary>
        static void RemapUntexturedRunnerMaterials(GameObject visual)
        {
            foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || materials[i].mainTexture != null)
                    {
                        continue;
                    }

                    string materialName = materials[i].name.ToLowerInvariant();
                    materials[i] = materialName.Contains("joint") ? Mat("ShortsBlack") : Mat("VestGreen");
                }

                renderer.sharedMaterials = materials;
            }
        }

        /// <summary>
        /// Builds a URP/Lit material from PBR maps that ship beside a Meshy-style
        /// FBX (a base-colour "..texture_0.png" plus optional _normal/_metallic
        /// maps) and assigns it to every skinned mesh. Returns false when no
        /// base-colour map sits next to the model so callers fall back to the
        /// flat-colour remap. Unity does not reliably auto-bind these external
        /// maps through the FBX material import, which left the runner untextured.
        /// </summary>
        static bool TryApplyExternalPbrTextures(GameObject visual, string modelPath)
        {
            string folder = Path.GetDirectoryName(modelPath).Replace("\\", "/");
            string baseColorPath = null, normalPath = null, metallicPath = null;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path).Replace("\\", "/") != folder)
                {
                    continue; // sibling maps only, not textures in nested folders
                }

                string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (name.Contains("normal"))
                {
                    normalPath = path;
                }
                else if (name.Contains("metallic"))
                {
                    metallicPath = path;
                }
                else if (name.Contains("roughness"))
                {
                    // URP/Lit has no roughness slot; ignore so it is not mistaken for base colour.
                }
                else if (name.Contains("basecolor") || name.Contains("albedo")
                    || name.Contains("diffuse") || name.Contains("texture_0"))
                {
                    baseColorPath = path;
                }
            }

            if (baseColorPath == null)
            {
                return false;
            }

            SetTextureSrgb(baseColorPath, true);
            if (metallicPath != null)
            {
                SetTextureSrgb(metallicPath, false);
            }
            if (normalPath != null)
            {
                SetTextureNormalMap(normalPath);
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(RunnerPbrMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, RunnerPbrMaterialPath);
            }

            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
            material.SetTexture("_BaseMap", baseColor);
            material.mainTexture = baseColor;
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.3f);

            if (normalPath != null)
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                material.EnableKeyword("_NORMALMAP");
                material.SetFloat("_BumpScale", 1f);
            }

            if (metallicPath != null)
            {
                material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath));
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Metallic", 1f);
            }

            EditorUtility.SetDirty(material);

            foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }

            Debug.Log($"Applied external PBR runner material from '{baseColorPath}' " +
                $"(normal: {normalPath != null}, metallic: {metallicPath != null}).");
            return true;
        }

        static void SetTextureSrgb(string path, bool sRGB)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.sRGBTexture != sRGB)
            {
                importer.sRGBTexture = sRGB;
                importer.SaveAndReimport();
            }
        }

        static void SetTextureNormalMap(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer
                && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        static string FindRunnerModelPath()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayableRunnerModelPath) != null)
            {
                return PlayableRunnerModelPath;
            }

            Debug.LogWarning($"Playable runner model not found at {PlayableRunnerModelPath}; falling back to model search.");

            // The most recently added FBX dropped into Assets/ becomes the player,
            // so a newly imported character always wins over older downloads.
            string bestPath = null;
            System.DateTime bestTime = System.DateTime.MinValue;
            foreach (string guid in AssetDatabase.FindAssets("t:Model"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/") || path.Contains("/FPS/") || path.Contains("/ModAssets/")
                    || path.Contains("/NavMeshComponents/") || path.Contains("/TextMesh Pro/"))
                {
                    continue;
                }

                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Vehicle models are scenery/obstacles, never the playable character.
                string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (fileName.Contains("taxi") || fileName.Contains("bus") || fileName.Contains("car") || fileName.Contains("vehicle"))
                {
                    continue;
                }

                System.DateTime modifiedTime = File.GetLastWriteTimeUtc(path);
                if (modifiedTime > bestTime)
                {
                    bestTime = modifiedTime;
                    bestPath = path;
                }
            }

            return bestPath;
        }

        static RuntimeAnimatorController CreateRunnerAnimatorController(AnimationClip runClip)
        {
            Directory.CreateDirectory("Assets/Animations");
            AssetDatabase.DeleteAsset(RunnerAnimatorPath);
            // Idle plays the real Meshy dance clip (looping) as the pre-run showcase.
            AnimationClip idleClip = LoadRunnerClipFromModel(RunnerIdleModelPath, loop: true)
                ?? CreateGeneratedClip(RunnerIdleClipPath, 1f);
            // Jump and air-roll play the real Meshy clips (same rig, retargeted);
            // the generated stubs remain as a fallback if a model goes missing.
            AnimationClip jumpClip = LoadRunnerClipFromModel(RunnerJumpModelPath, loop: false)
                ?? CreateGeneratedClip(RunnerJumpClipPath, 0.45f);
            AnimationClip airRollClip = LoadRunnerClipFromModel(RunnerAirRollModelPath, loop: false)
                ?? CreateGeneratedClip(RunnerRollClipPath, 0.7f);
            AnimationClip rollClip = CreateGeneratedClip(RunnerRollClipPath, 0.7f);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(RunnerAnimatorPath);
            controller.AddParameter("isRunning", AnimatorControllerParameterType.Bool);
            controller.AddParameter("isJumping", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Roll", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AirRoll", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(260f, 50f, 0f));
            AnimatorState runState = stateMachine.AddState("Run", new Vector3(260f, 150f, 0f));
            AnimatorState jumpState = stateMachine.AddState("Jump", new Vector3(520f, 150f, 0f));
            AnimatorState rollState = stateMachine.AddState("Roll", new Vector3(260f, 300f, 0f));
            AnimatorState airRollState = stateMachine.AddState("AirRoll", new Vector3(520f, 300f, 0f));

            idleState.motion = idleClip;
            runState.motion = runClip;
            jumpState.motion = jumpClip;
            rollState.motion = rollClip;
            airRollState.motion = airRollClip;
            // Start in Idle (the dance) before the run; Idle→Run fires when the
            // player controller sets isRunning true on the first PLAY.
            stateMachine.defaultState = idleState;

            AddBoolTransition(idleState, runState, "isRunning", true, 0.05f);
            AddBoolTransition(runState, idleState, "isRunning", false, 0.08f);
            AddBoolTransition(runState, jumpState, "isJumping", true, 0.05f);
            AddJumpReturnTransition(jumpState, runState, 0.08f);
            AddTriggerTransition(runState, rollState, "Roll", 0.05f);
            AddTriggerTransition(idleState, rollState, "Roll", 0.05f);

            // Swipe down while airborne: dive-roll. Reachable from the jump
            // apex or from run (coyote window before the jump pose settles).
            AddTriggerTransition(jumpState, airRollState, "AirRoll", 0.05f);
            AddTriggerTransition(runState, airRollState, "AirRoll", 0.05f);

            AnimatorStateTransition rollToRun = rollState.AddTransition(runState);
            rollToRun.hasExitTime = true;
            rollToRun.exitTime = 0.95f;
            rollToRun.duration = 0.05f;

            AnimatorStateTransition airRollToRun = airRollState.AddTransition(runState);
            airRollToRun.hasExitTime = true;
            airRollToRun.exitTime = 0.9f;
            airRollToRun.duration = 0.08f;

            Debug.Log($"Runner animator clips — idle: '{idleClip.name}' ({idleClip.length:0.00}s), " +
                $"jump: '{jumpClip.name}' ({jumpClip.length:0.00}s), " +
                $"airRoll: '{airRollClip.name}' ({airRollClip.length:0.00}s).");
            return controller;
        }

        /// <summary>
        /// Imports an FBX's bundled animation as a Humanoid clip (so it retargets
        /// onto the runner's avatar) and returns the first real clip, or null when
        /// the model or a clip is missing.
        /// </summary>
        static AnimationClip LoadRunnerClipFromModel(string modelPath, bool loop)
        {
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Runner clip model not found at {modelPath}; using generated fallback.");
                return null;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.loopTime = loop;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview"))
                {
                    return clip;
                }
            }

            Debug.LogWarning($"No animation clip found in {modelPath}; using generated fallback.");
            return null;
        }

        static AnimationClip CreateGeneratedClip(string path, float length)
        {
            AssetDatabase.DeleteAsset(path);
            AnimationClip clip = new AnimationClip
            {
                frameRate = 30f
            };
            AnimationCurve holdCurve = AnimationCurve.Linear(0f, 0f, length, 0f);
            clip.SetCurve("", typeof(Transform), "localPosition.x", holdCurve);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value, float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        static void AddTriggerTransition(AnimatorState from, AnimatorState to, string parameter, float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        static void AddJumpReturnTransition(AnimatorState from, AnimatorState to, float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "isJumping");
        }

        static Transform Pivot(string name, Transform parent, Vector3 localPosition)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent);
            pivot.transform.localPosition = localPosition;
            return pivot.transform;
        }

        static Camera CreateCamera(Transform player)
        {
            Vector3 offset = new Vector3(0f, 2.05f, -4.35f);
            const float lookHeight = 1.05f;
            Vector3 idleOffset = new Vector3(0f, 1.2f, -5.2f);
            const float idleLookHeight = 0.95f;

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            // Author at the idle framing: that is the initial (menu) state, so
            // the scene opens on the centered dance shot with no first-frame pop.
            cameraObject.transform.position = player.position + idleOffset;
            cameraObject.transform.rotation = Quaternion.LookRotation(player.position + Vector3.up * idleLookHeight - cameraObject.transform.position);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 50f;
            camera.farClipPlane = 400f;
            cameraObject.AddComponent<AudioListener>();
            CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
            SetField(follow, "target", player);
            SetField(follow, "offset", offset);
            SetField(follow, "lookHeight", lookHeight);
            SetField(follow, "idleOffset", idleOffset);
            SetField(follow, "idleLookHeight", idleLookHeight);
            SetField(follow, "lateralFollowFraction", 1f);
            SetField(follow, "lateralFollowSpeed", 18f);
            return camera;
        }

        static void CreateSystems(Transform player, GameObject roadPrefab, TrackChunk[] chunkPrefabs)
        {
            GameObject gameManagerObject = new GameObject("GameManager");
            GameManager gameManager = gameManagerObject.AddComponent<GameManager>();

            GameObject scoreManagerObject = new GameObject("ScoreManager");
            ScoreManager scoreManager = scoreManagerObject.AddComponent<ScoreManager>();
            SetField(scoreManager, "player", player);

            GameObject powerUpManagerObject = new GameObject("PowerUpManager");
            PowerUpManager powerUpManager = powerUpManagerObject.AddComponent<PowerUpManager>();
            SetField(powerUpManager, "player", player);
            SetField(powerUpManager, "gameManager", gameManager);
            SetField(scoreManager, "powerUpManager", powerUpManager);

            GameObject roadSpawnerObject = new GameObject("Road");
            roadSpawnerObject.transform.SetParent(EnvironmentCategory("Road"));
            RoadSegmentSpawner roadSpawner = roadSpawnerObject.AddComponent<RoadSegmentSpawner>();
            SetField(roadSpawner, "player", player);
            SetField(roadSpawner, "roadSegmentPrefab", roadPrefab);
            int[] openingDistricts = { 0, 0, 2, 1, 3, 0 };
            for (int i = 0; i < 6; i++)
            {
                GameObject segment = Object.Instantiate(roadPrefab, new Vector3(0f, 0f, i * 30f), Quaternion.identity, roadSpawnerObject.transform);
                RoadSegmentVisuals visuals = segment.GetComponent<RoadSegmentVisuals>();
                if (visuals != null)
                {
                    visuals.SetDistrict(openingDistricts[i % openingDistricts.Length]);
                }
            }

            GameObject chunkManagerObject = new GameObject("ChunkManager");
            ChunkManager chunkManager = chunkManagerObject.AddComponent<ChunkManager>();
            SetField(chunkManager, "player", player);
            SetField(chunkManager, "gameManager", gameManager);
            SetField(chunkManager, "chunkPrefabs", chunkPrefabs);

            CameraFollow cameraFollow = Object.FindAnyObjectByType<CameraFollow>();
            if (cameraFollow != null)
            {
                SetField(cameraFollow, "gameManager", gameManager);
            }

            IdleFacing idleFacing = player.GetComponentInChildren<IdleFacing>();
            if (idleFacing != null)
            {
                SetField(idleFacing, "gameManager", gameManager);
            }

            PlayerController controller = player.GetComponent<PlayerController>();
            SetField(controller, "gameManager", gameManager);
            SetField(controller, "powerUpManager", powerUpManager);
            RollController rollController = player.GetComponent<RollController>();
            SetField(rollController, "gameManager", gameManager);
            SetField(gameManager, "player", controller);
            SetField(gameManager, "scoreManager", scoreManager);
            SetField(scoreManager, "gameManager", gameManager);
        }

        static void CreateSceneryTraffic(Transform player, GameObject sceneryTaxiPrefab)
        {
            GameObject trafficObject = new GameObject("SceneryTraffic");
            trafficObject.transform.SetParent(EnvironmentCategory("Props"));
            SceneryTraffic traffic = trafficObject.AddComponent<SceneryTraffic>();
            SetField(traffic, "player", player);
            // All taxis approach from the front: oncoming lane only, no rear-view traffic.
            SetField(traffic, "oncomingPrefabs", new[] { sceneryTaxiPrefab });
            SetField(traffic, "oncomingInterval", 7f);
        }

        static void CreateUi(PlayerController playerController)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            GameObject canvasObject = new GameObject("RunnerCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Color gold = new Color(1f, 0.8f, 0.3f);

            // HUD lives inside the device safe area so notches and
            // punch-holes never cover the score.
            GameObject safeArea = new GameObject("SafeArea");
            safeArea.transform.SetParent(canvasObject.transform, false);
            RectTransform safeRect = safeArea.AddComponent<RectTransform>();
            Anchor(safeRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safeArea.AddComponent<SafeAreaPanel>();

            GameObject scorePill = RoundedPanel(safeArea.transform, "ScorePill", new Color(0f, 0f, 0f, 0.45f), rounded, 0.25f);
            Anchor(scorePill.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -32f), new Vector2(470f, 104f));

            TextMeshProUGUI scoreText = Text(scorePill.transform, "ScoreText", "Score: 0  x1", 48, TextAlignmentOptions.Left);
            scoreText.fontStyle = FontStyles.Bold;
            Anchor(scoreText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scoreText.rectTransform.offsetMin = new Vector2(36f, 0f);
            scoreText.rectTransform.offsetMax = new Vector2(-24f, 0f);

            GameObject coinPill = RoundedPanel(safeArea.transform, "CoinPill", new Color(0f, 0f, 0f, 0.45f), rounded, 0.25f);
            Anchor(coinPill.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -32f), new Vector2(340f, 104f));

            Image coinIcon = new GameObject("CoinIcon").AddComponent<Image>();
            coinIcon.transform.SetParent(coinPill.transform, false);
            Sprite coinSprite = LoadHiggsfieldSprite("PU_Coin");
            coinIcon.sprite = coinSprite != null ? coinSprite : knob;
            coinIcon.color = coinSprite != null ? Color.white : gold;
            coinIcon.preserveAspect = true;
            Anchor(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(52f, 52f));
            coinIcon.rectTransform.pivot = new Vector2(0f, 0.5f);

            TextMeshProUGUI coinText = Text(coinPill.transform, "CoinText", "0", 48, TextAlignmentOptions.Right);
            coinText.fontStyle = FontStyles.Bold;
            coinText.color = gold;
            Anchor(coinText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            coinText.rectTransform.offsetMin = new Vector2(92f, 0f);
            coinText.rectTransform.offsetMax = new Vector2(-32f, 0f);

            TextMeshProUGUI powerUpStatus = Text(safeArea.transform, "PowerUpStatusText", "", 40, TextAlignmentOptions.Center);
            powerUpStatus.fontStyle = FontStyles.Bold;
            Anchor(powerUpStatus.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(960f, 56f));

            GameObject gameOverPanel = Panel(canvasObject.transform, "GameOverPanel", new Color(0f, 0f, 0f, 0.8f));
            Anchor(gameOverPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject cardFrame = RoundedPanel(gameOverPanel.transform, "CardFrame", gold, rounded, 0.3f);
            Anchor(cardFrame.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(904f, 1224f));

            GameObject card = RoundedPanel(cardFrame.transform, "Card", new Color(0.07f, 0.08f, 0.12f, 0.98f), rounded, 0.3f);
            Anchor(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 1200f));

            TextMeshProUGUI gameOverTitle = Text(card.transform, "GameOverTitle", "GAME OVER", 92, TextAlignmentOptions.Center);
            gameOverTitle.fontStyle = FontStyles.Bold;
            gameOverTitle.color = gold;
            gameOverTitle.characterSpacing = 8f;
            Anchor(gameOverTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(780f, 120f));

            TextMeshProUGUI finalScore = Text(card.transform, "FinalScoreText", "Final score: 0", 46, TextAlignmentOptions.Center);
            Anchor(finalScore.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(780f, 420f));

            // Buttons sit below every line of text, sized for thumbs.
            Button continueUiButton = UiButton(card.transform, "ContinueButton", "CONTINUE · 1 R5", 54, gold, rounded,
                new Vector2(0.5f, 0f), new Vector2(0f, 400f), new Vector2(620f, 150f));
            TextMeshProUGUI continueLabel = continueUiButton.GetComponentInChildren<TextMeshProUGUI>();
            continueLabel.color = new Color(0.16f, 0.12f, 0.03f);
            ContinueButton continueButton = continueUiButton.gameObject.AddComponent<ContinueButton>();

            Button restartUiButton = UiButton(card.transform, "RestartButton", "RESTART", 60, new Color(1f, 0.6f, 0.05f, 1f), rounded,
                new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(620f, 140f));
            RestartButton restartButton = restartUiButton.gameObject.AddComponent<RestartButton>();

            Button menuUiButton = UiButton(card.transform, "MenuButton", "MENU", 50, new Color(0.25f, 0.28f, 0.35f, 1f), rounded,
                new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(620f, 115f));
            MenuButton menuButton = menuUiButton.gameObject.AddComponent<MenuButton>();

            Color panelDark = new Color(0.05f, 0.06f, 0.09f, 0.97f);
            Color rowDark = new Color(0.12f, 0.14f, 0.19f, 1f);
            Color buttonOrange = new Color(1f, 0.6f, 0.05f, 1f);
            Color buttonGrey = new Color(0.25f, 0.28f, 0.35f, 1f);

            // Higgsfield-authored pickup art, falling back to a live render of the
            // prefab when a sprite is missing so the HUD is never blank.
            Vector3 pickupEuler = new Vector3(-16f, 28f, 0f);
            Sprite[] pickupIcons =
            {
                LoadHiggsfieldSprite("PU_Magnet") ?? RenderPrefabIcon("Assets/Prefabs/PowerUpTaxiMagnet.prefab", "IconTaxiMagnet", pickupEuler),
                LoadHiggsfieldSprite("PU_Sneaker") ?? RenderPrefabIcon("Assets/Prefabs/PowerUpJoziSneakers.prefab", "IconJoziSneakers", pickupEuler),
                LoadHiggsfieldSprite("PU_TaxiBoost") ?? RenderPrefabIcon("Assets/Prefabs/PowerUpDroneBoost.prefab", "IconDroneBoost", new Vector3(-35f, 30f, 0f)),
                LoadHiggsfieldSprite("PU_Multiplier2x") ?? RenderPrefabIcon("Assets/Prefabs/PowerUpUbuntuMultiplier.prefab", "IconUbuntuMultiplier", pickupEuler),
                LoadHiggsfieldSprite("PU_Shield") ?? RenderPrefabIcon("Assets/Prefabs/PowerUpHoverboard.prefab", "IconHoverboard", new Vector3(-28f, 24f, 0f)),
            };
            Sprite r5Icon = LoadHiggsfieldSprite("PU_Coin") ?? RenderPrefabIcon(RareCoinPrefabPath, "IconRareCoinR5", new Vector3(0f, 25f, 0f));

            // ---------------- Main menu ----------------
            GameObject menuPanel = Panel(canvasObject.transform, "MenuPanel", new Color(0f, 0f, 0f, 0.55f));
            Anchor(menuPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TextMeshProUGUI menuTitle = Text(menuPanel.transform, "MenuTitle", "JOZI RUNNER", 112, TextAlignmentOptions.Center);
            menuTitle.fontStyle = FontStyles.Bold;
            menuTitle.color = gold;
            menuTitle.characterSpacing = 10f;
            Anchor(menuTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(980f, 140f));

            TextMeshProUGUI menuSubtitle = Text(menuPanel.transform, "MenuSubtitle", "JOHANNESBURG STREET RUN", 40, TextAlignmentOptions.Center);
            menuSubtitle.color = new Color(0.85f, 0.87f, 0.92f);
            menuSubtitle.characterSpacing = 6f;
            Anchor(menuSubtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -440f), new Vector2(900f, 60f));

            // Showcase strip: the five power-ups plus the rare R5 coin.
            Sprite[] showcase = { pickupIcons[0], pickupIcons[1], pickupIcons[2], pickupIcons[3], pickupIcons[4], r5Icon };
            for (int i = 0; i < showcase.Length; i++)
            {
                GameObject chip = RoundedPanel(menuPanel.transform, $"ShowcaseChip{i}", rowDark, rounded, 0.25f);
                Anchor(chip.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2((i - (showcase.Length - 1) * 0.5f) * 150f, -590f), new Vector2(136f, 136f));
                IconImage(chip.transform, "Icon", showcase[i], new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f));
            }

            Button playButton = UiButton(menuPanel.transform, "PlayButton", "PLAY", 74, buttonOrange, rounded,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(640f, 180f));
            Button storeButton = UiButton(menuPanel.transform, "StoreButton", "STORE", 52, rowDark, rounded,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(640f, 140f));
            Button collectablesButton = UiButton(menuPanel.transform, "CollectablesButton", "COLLECTABLES", 52, rowDark, rounded,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -430f), new Vector2(640f, 140f));

            // ---------------- Store ----------------
            GameObject storePanel = Panel(canvasObject.transform, "StorePanel", panelDark);
            Anchor(storePanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TextMeshProUGUI storeTitle = Text(storePanel.transform, "StoreTitle", "STORE", 84, TextAlignmentOptions.Center);
            storeTitle.fontStyle = FontStyles.Bold;
            storeTitle.color = gold;
            storeTitle.characterSpacing = 8f;
            Anchor(storeTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(600f, 110f));

            TextMeshProUGUI storeBalance = Text(storePanel.transform, "StoreBalance", "Coins: <color=#FFC845>0</color>    ·    R5: <color=#FFC845>0</color>", 46, TextAlignmentOptions.Center);
            Anchor(storeBalance.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(700f, 60f));

            PowerUpType[] storeOrder =
            {
                PowerUpType.TaxiMagnet,
                PowerUpType.JoziSneakers,
                PowerUpType.DroneBoost,
                PowerUpType.UbuntuMultiplier,
                PowerUpType.Hoverboard,
            };

            var storeItemLabels = new TextMeshProUGUI[storeOrder.Length];
            var storeUpgradeButtons = new Button[storeOrder.Length];
            var storeUpgradeLabels = new TextMeshProUGUI[storeOrder.Length];
            for (int i = 0; i < storeOrder.Length; i++)
            {
                GameObject row = RoundedPanel(storePanel.transform, $"StoreRow{i}", rowDark, rounded, 0.25f);
                Anchor(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f - i * 185f), new Vector2(960f, 165f));

                IconImage(row.transform, "ItemIcon", pickupIcons[i], new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(132f, 132f));

                TextMeshProUGUI itemLabel = Text(row.transform, "ItemLabel",
                    $"<b>{PowerUpManager.DisplayName(storeOrder[i])}</b>  <color=#FFC845>Lv 0/{PowerUpManager.MaxUpgradeLevel}</color>\n" +
                    $"<size=30><color=#AEB4C2>{PowerUpManager.Description(storeOrder[i])} · {PowerUpManager.Duration(storeOrder[i]):0}s</color></size>",
                    40, TextAlignmentOptions.Left);
                Anchor(itemLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                itemLabel.rectTransform.offsetMin = new Vector2(180f, 0f);
                itemLabel.rectTransform.offsetMax = new Vector2(-270f, 0f);

                Button upgradeButton = UiButton(row.transform, "UpgradeButton", "150", 46, buttonOrange, rounded,
                    new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(210f, 112f));
                storeItemLabels[i] = itemLabel;
                storeUpgradeButtons[i] = upgradeButton;
                storeUpgradeLabels[i] = upgradeButton.GetComponentInChildren<TextMeshProUGUI>();
            }

            // Specials: consumable run boosts paid for with rare R5 coins.
            TextMeshProUGUI specialsHeader = Text(storePanel.transform, "SpecialsHeader", "SPECIALS · <color=#FFC845>R5 COINS</color>", 40, TextAlignmentOptions.Center);
            specialsHeader.fontStyle = FontStyles.Bold;
            specialsHeader.characterSpacing = 4f;
            Anchor(specialsHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1280f), new Vector2(700f, 50f));

            string[] specialNames = { "Head Start", "Shield Start" };
            string[] specialDescriptions = { "Begin your next run flying the Drone", "Begin your next run with a Hoverboard shield" };
            int[] specialCosts = { 1, 2 };
            Sprite[] specialIcons = { pickupIcons[2], pickupIcons[4] };

            var specialItemLabels = new TextMeshProUGUI[specialNames.Length];
            var specialBuyButtons = new Button[specialNames.Length];
            var specialBuyLabels = new TextMeshProUGUI[specialNames.Length];
            for (int i = 0; i < specialNames.Length; i++)
            {
                GameObject row = RoundedPanel(storePanel.transform, $"SpecialRow{i}", rowDark, rounded, 0.25f);
                Anchor(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1355f - i * 185f), new Vector2(960f, 165f));

                IconImage(row.transform, "ItemIcon", specialIcons[i], new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(132f, 132f));

                TextMeshProUGUI itemLabel = Text(row.transform, "ItemLabel",
                    $"<b>{specialNames[i]}</b>  <color=#FFC845>Owned x0</color>\n" +
                    $"<size=30><color=#AEB4C2>{specialDescriptions[i]}</color></size>",
                    40, TextAlignmentOptions.Left);
                Anchor(itemLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                itemLabel.rectTransform.offsetMin = new Vector2(180f, 0f);
                itemLabel.rectTransform.offsetMax = new Vector2(-270f, 0f);

                Button buyButton = UiButton(row.transform, "BuyButton", $"{specialCosts[i]} R5", 44, gold, rounded,
                    new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(210f, 112f));
                TextMeshProUGUI buyLabel = buyButton.GetComponentInChildren<TextMeshProUGUI>();
                buyLabel.color = new Color(0.16f, 0.12f, 0.03f);
                specialItemLabels[i] = itemLabel;
                specialBuyButtons[i] = buyButton;
                specialBuyLabels[i] = buyLabel;
            }

            Button storeBackButton = UiButton(storePanel.transform, "StoreBackButton", "BACK", 48, buttonGrey, rounded,
                new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(400f, 120f));

            // ---------------- Collectables ----------------
            GameObject collectablesPanel = Panel(canvasObject.transform, "CollectablesPanel", panelDark);
            Anchor(collectablesPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TextMeshProUGUI collectablesTitle = Text(collectablesPanel.transform, "CollectablesTitle", "COLLECTABLES", 84, TextAlignmentOptions.Center);
            collectablesTitle.fontStyle = FontStyles.Bold;
            collectablesTitle.color = gold;
            collectablesTitle.characterSpacing = 8f;
            Anchor(collectablesTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(920f, 110f));

            GameObject collectablesCard = RoundedPanel(collectablesPanel.transform, "CollectablesCard", rowDark, rounded, 0.3f);
            Anchor(collectablesCard.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(920f, 980f));

            TextMeshProUGUI collectablesText = Text(collectablesCard.transform, "CollectablesText",
                "<size=44>R1 Coins banked   <color=#FFC845>0</color></size>\n" +
                "<size=44>R5 Rare Coins   <color=#FFC845>0</color></size>\n" +
                "<size=44>Best Score   <color=#FFC845>0</color></size>\n" +
                "\n<size=36><color=#AEB4C2>POWER-UPS COLLECTED</color></size>" +
                "<size=38>\nTaxi Magnet   <color=#FFC845>x0</color>\nJozi Sneakers   <color=#FFC845>x0</color>\nDrone Boost   <color=#FFC845>x0</color>\nUbuntu Multiplier   <color=#FFC845>x0</color>\nHoverboard   <color=#FFC845>x0</color></size>",
                44, TextAlignmentOptions.Center);
            Anchor(collectablesText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            collectablesText.rectTransform.offsetMin = new Vector2(48f, 40f);
            collectablesText.rectTransform.offsetMax = new Vector2(-48f, -48f);

            Button collectablesBackButton = UiButton(collectablesPanel.transform, "CollectablesBackButton", "BACK", 48, buttonGrey, rounded,
                new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(400f, 120f));

            // ---------------- Wiring ----------------
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            ScoreManager scoreManager = Object.FindAnyObjectByType<ScoreManager>();
            PowerUpManager powerUpManager = Object.FindAnyObjectByType<PowerUpManager>();
            SetField(gameManager, "gameOverPanel", gameOverPanel);
            SetField(gameManager, "finalScoreText", finalScore);
            SetField(scoreManager, "scoreText", scoreText);
            SetField(scoreManager, "coinText", coinText);
            SetField(powerUpManager, "statusText", powerUpStatus);
            SetField(restartButton, "gameManager", gameManager);
            SetField(menuButton, "gameManager", gameManager);
            SetField(continueButton, "gameManager", gameManager);
            SetField(gameManager, "continueButton", continueUiButton.gameObject);
            SetField(gameManager, "continueLabel", continueLabel);

            MenuController menuController = canvasObject.AddComponent<MenuController>();
            SetField(menuController, "gameManager", gameManager);
            SetField(menuController, "scoreManager", scoreManager);
            SetField(menuController, "hudRoot", safeArea);
            SetField(menuController, "menuPanel", menuPanel);
            SetField(menuController, "storePanel", storePanel);
            SetField(menuController, "collectablesPanel", collectablesPanel);
            SetField(menuController, "playButton", playButton);
            SetField(menuController, "storeButton", storeButton);
            SetField(menuController, "collectablesButton", collectablesButton);
            SetField(menuController, "storeBackButton", storeBackButton);
            SetField(menuController, "collectablesBackButton", collectablesBackButton);
            SetField(menuController, "powerUpManager", powerUpManager);
            SetField(menuController, "storeBalanceText", storeBalance);
            SetField(menuController, "storeItemLabels", storeItemLabels);
            SetField(menuController, "storeUpgradeButtons", storeUpgradeButtons);
            SetField(menuController, "storeUpgradeLabels", storeUpgradeLabels);
            SetField(menuController, "specialItemLabels", specialItemLabels);
            SetField(menuController, "specialBuyButtons", specialBuyButtons);
            SetField(menuController, "specialBuyLabels", specialBuyLabels);
            SetField(menuController, "collectablesText", collectablesText);

            gameOverPanel.SetActive(false);
            menuPanel.SetActive(false);
            storePanel.SetActive(false);
            collectablesPanel.SetActive(false);
        }

        // Renders a prefab to a small icon texture on the UI's dark card
        // colour, so Image widgets can show real pictures of the pickups.
        static Sprite RenderPrefabIcon(string prefabPath, string iconName, Vector3 modelEuler)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Icon prefab missing: {prefabPath}");
                return null;
            }

            bool previousAsyncCompilation = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;

            // Stage the model far above the scene so nothing else is in frame.
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3(0f, 400f, 0f);
            instance.transform.rotation = Quaternion.Euler(modelEuler);

            Bounds bounds = CombinedRendererBounds(instance);
            GameObject cameraObject = new GameObject("IconCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = bounds.center + Vector3.back * 4f;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.19f, 1f);

            const int iconSize = 256;
            RenderTexture renderTexture = new RenderTexture(iconSize, iconSize, 24);
            camera.targetTexture = renderTexture;
            camera.Render();
            camera.Render();

            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(iconSize, iconSize, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, iconSize, iconSize), 0, 0);
            texture.Apply();

            camera.targetTexture = null;
            RenderTexture.active = null;
            EditorSettings.asyncShaderCompilation = previousAsyncCompilation;

            Directory.CreateDirectory("Assets/Textures/Icons");
            string pngPath = $"Assets/Textures/Icons/{iconName}.png";
            File.WriteAllBytes(pngPath, texture.EncodeToPNG());

            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(instance);

            AssetDatabase.ImportAsset(pngPath);
            TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.maxTextureSize != 256))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.maxTextureSize = 256;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        /// <summary>
        /// Loads a hand-authored Higgsfield pickup sprite from
        /// Assets/Textures/Higgsfield/, forcing the importer to Sprite/transparent
        /// so it can drop straight into a UI Image. Returns null when the art is
        /// absent so callers can fall back to the rendered-prefab icon.
        /// </summary>
        static Sprite LoadHiggsfieldSprite(string name)
        {
            string path = $"Assets/Textures/Higgsfield/{name}.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
            {
                return null;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null &&
                (importer.textureType != TextureImporterType.Sprite ||
                 importer.spriteImportMode != SpriteImportMode.Single ||
                 !importer.alphaIsTransparency ||
                 importer.mipmapEnabled))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Image IconImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image image = new GameObject(name).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.preserveAspect = true;
            Anchor(image.rectTransform, anchor, anchor, position, size);
            return image;
        }

        static Button UiButton(Transform parent, string name, string label, int fontSize, Color color, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = RoundedPanel(parent, name, color, sprite, 0.3f);
            Anchor(buttonObject.GetComponent<RectTransform>(), anchor, anchor, position, size);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();

            TextMeshProUGUI text = Text(buttonObject.transform, $"{name}Label", label, fontSize, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 4f;
            Anchor(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        static TextMeshProUGUI Text(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        static GameObject Panel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        // cornerScale below 1 enlarges the sliced sprite's rounded corners.
        static GameObject RoundedPanel(Transform parent, string name, Color color, Sprite sprite, float cornerScale)
        {
            GameObject panel = Panel(parent, name, color);
            Image image = panel.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = cornerScale;
            return panel;
        }

        static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = min == max ? min : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        // ------------------------------------------------------------------
        // Primitive helpers
        // ------------------------------------------------------------------

        static GameObject Cube(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Vector3 localEuler = default)
        {
            return Primitive(PrimitiveType.Cube, name, parent, localPosition, scale, material, localEuler);
        }

        static GameObject Cylinder(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Vector3 localEuler = default)
        {
            return Primitive(PrimitiveType.Cylinder, name, parent, localPosition, scale, material, localEuler);
        }

        static GameObject Sphere(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Vector3 localEuler = default)
        {
            return Primitive(PrimitiveType.Sphere, name, parent, localPosition, scale, material, localEuler);
        }

        static GameObject Capsule(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Vector3 localEuler = default)
        {
            return Primitive(PrimitiveType.Capsule, name, parent, localPosition, scale, material, localEuler);
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Vector3 localEuler)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = Quaternion.Euler(localEuler);
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        static void StripColliders(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(collider);
            }
        }

        static void SetField<T>(Object target, string fieldName, T value)
        {
            if (target == null)
            {
                return;
            }

            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }
    }
}
