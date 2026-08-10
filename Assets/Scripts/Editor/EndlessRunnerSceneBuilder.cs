using System.Collections.Generic;
using System.IO;
using JoburgRunner;
using JoburgRunner.Environment.Decor;
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
        const string TaxiPrefabPath = "Assets/Prefabs/SouthAfricanTaxiObstacle.prefab";
        const string Taxi2ModelPath = "Assets/Art/GameReady/Vehicles/taxi2/taxi2_production.fbx";
        const string Taxi2MaterialPath = "Assets/Materials/Taxi2_URP.mat";
        const string Taxi2AlbedoPath = "Assets/Art/GameReady/Vehicles/taxi2/Meshy_AI_Low_poly_1980s_South__0731072049_texture.png";
        const string Taxi2NormalPath = "Assets/Art/GameReady/Vehicles/taxi2/Meshy_AI_Low_poly_1980s_South__0731072049_texture_normal.png";
        const string DustbinModelPath = "Assets/Art/GameReady/Props/dustbin/dustbin_upright.fbx";
        const string BusStopStandModelPath = "Assets/Art/GameReady/Props/bus_stop_stand/bus_stop_stand_right_side_up.fbx";
        const string DustbinAlbedoPath = "Assets/Art/GameReady/Props/dustbin/Meshy_AI_Low_poly_modern_Johan_0731082557_texture.png";
        const string DustbinNormalPath = "Assets/Art/GameReady/Props/dustbin/Meshy_AI_Low_poly_modern_Johan_0731082557_texture_normal.png";
        const string BusStopStandAlbedoPath = "Assets/Art/GameReady/Props/bus_stop_stand/Meshy_AI_Create_a_game_ready_J_0731074501_texture.png";
        const string BusStopStandNormalPath = "Assets/Art/GameReady/Props/bus_stop_stand/Meshy_AI_Create_a_game_ready_J_0731074501_texture_normal.png";
        const string ConcretePotplantModelPath = "Assets/Art/GameReady/Props/concrete_potplant/concrete_potplant_production.fbx";
        const string ConcretePotplantAlbedoPath = "Assets/Art/GameReady/Props/concrete_potplant/Meshy_AI_Low_poly_rectangular__0731084256_texture.png";
        const string ConcretePotplantNormalPath = "Assets/Art/GameReady/Props/concrete_potplant/Meshy_AI_Low_poly_rectangular__0731084256_texture_normal.png";
        const string ElectricBoxModelPath = "Assets/Art/GameReady/Props/electric_box/electric_box_upright.fbx";
        const string ElectricBoxAlbedoPath = "Assets/Art/GameReady/Props/electric_box/Meshy_AI_Low_poly_roadside_ele_0731085731_texture.png";
        const string ElectricBoxNormalPath = "Assets/Art/GameReady/Props/electric_box/Meshy_AI_Low_poly_roadside_ele_0731085731_texture_normal.png";
        const string DirectionSignModelPath = "Assets/Art/GameReady/Props/road_sign/road_sign_unity.fbx";
        // NB: copied to a clean name — the original PNG shares the FBX's base name
        // (..._texture.fbx / ..._texture.png), so Unity's FBX importer claims it and
        // LoadAssetAtPath<Texture2D> returns null (the sign rendered white).
        const string DirectionSignAlbedoPath = "Assets/Art/GameReady/Props/road_sign/roadsign_base_albedo.png";
        const string DirectionSignNormalPath = "Assets/Art/GameReady/Props/road_sign/Meshy_AI_Create_a_low_poly_mod_0802182911_texture_normal.png";
        const string DirectionSignPrefabPath = "Assets/Prefabs/Decor/DecorDirectionSign.prefab";
        // Higgsfield-generated taxi: a wheel-less body GLB plus a separate wheel
        // GLB, assembled into one prefab so the wheels are real spinning children.
        const string TaxiBodyModelPath = "Assets/Models/Higgsfield/Taxi/TaxiBody.glb";
        const string TaxiWheelModelPath = "Assets/Models/Higgsfield/Taxi/TaxiWheel.glb";
        const string PotholePrefabPath = "Assets/Prefabs/PotholeObstacle.prefab";
        const string BarrierPrefabPath = "Assets/Prefabs/ConstructionBarrier.prefab";
        const string CoinPrefabPath = "Assets/Prefabs/GoldCoin.prefab";
        const string RareCoinPrefabPath = "Assets/Prefabs/RareCoinR5.prefab";
        const string CoinParticlePrefabPath = "Assets/Prefabs/CoinCollectParticles.prefab";
        const string CoinCollectClipPath = "Assets/Audio/CoinCollect.wav";
        const string BackgroundMusicClipPath = "Assets/Audio/Remix.mp3";
        const string LaneSwitchClipPath = "Assets/Audio/LaneSwitch.mp3";
        // Ubuntu Pulse: the glowing collectible orb (Meshy static prop, has an
        // emission map) plus its own pickup/impact/power-down one-shots.
        const string UbuntuOrbModelPath = "Assets/Meshy_AI_Create_a_game_ready_3_0719103918_texture_fbx/Meshy_AI_Create_a_game_ready_3_0719103918_texture.fbx";
        const string UbuntuOrbMaterialPath = "Assets/Materials/UbuntuOrb_URP.mat";
        const string UbuntuPulsePickupClipPath = "Assets/Audio/UbuntuPulsePickup.wav";
        const string UbuntuPulseImpactClipPath = "Assets/Audio/UbuntuPulseImpact.wav";
        const string UbuntuPulsePowerDownClipPath = "Assets/Audio/UbuntuPulsePowerDown.wav";
        const string UbuntuPulseHumClipPath = "Assets/Audio/UbuntuPulseHum.wav";
        const string UbuntuPulseCoinAttractionClipPath = "Assets/Audio/UbuntuPulseCoinAttraction.wav";
        const string UbuntuPulsePrefabPath = "Assets/Prefabs/PowerUpUbuntuPulse.prefab";
        const string UbuntuDissolveParticlePrefabPath = "Assets/Prefabs/UbuntuDissolveParticles.prefab";
        // Hoverboard boosters: the Meshy anti-grav deck the runner rides while
        // the Hoverboard shield is active. To add a new board later: append to
        // BoardNames/BoardTaglines and create one more visual in
        // CreateHoverboardVisuals — the Boards page and HoverboardVisual are
        // both length-generic.
        const string HoverboardModelPath = "Assets/Meshy_AI_Extract_the_futuristi_0719124050_texture_fbx/Meshy_AI_Extract_the_futuristi_0719124050_texture.fbx";
        const string HoverboardMaterialPath = "Assets/Materials/HoverboardIon_URP.mat";
        // Clockwise (viewed from above) yaw applied to the flat ride deck.
        const float BoardYawDegrees = 90f;
        static readonly string[] BoardNames = { "Ion Cruiser" };
        static readonly string[] BoardTaglines = { "Anti-grav deck from the future of Jozi" };
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
        // Combined jump-into-roll mocap (same Beaded Warrior rig). One continuous
        // take: anticipation → aerial flip → landing IMPACT → forward roll →
        // recovery. Split into a standalone Jump clip (takeoff→landing) and a
        // Roll clip (landing→recovery) plus the full clip for the AirRoll dodge.
        // Blender authoring (meshy_jump_roll/create_animation.py): frames 18–83,
        // markers TAKEOFF 21 / PEAK 36 / IMPACT 46 / ROLL 53 / RECOVERY 76.
        const string RunnerJumpRollModelPath = "Assets/Characters/Meshy_AI_Beaded_Warrior_biped/jumpandroll.fbx";
        const int JumpRollAuthorStartFrame = 18;
        const int JumpRollAuthorEndFrame = 83;
        const int JumpRollAuthorImpactFrame = 46; // jump ends / roll begins at the landing
        const string RunnerIdleModelPath = "Assets/Characters/Meshy_AI_Beaded_Warrior_biped/Meshy_AI_Beaded_Warrior_biped_Animation_Hip_Hop_Dance_2_withSkin.fbx";
        // Falling loop held while the Drone Boost carries the runner through the air.
        const string RunnerFallModelPath = "Assets/Animations/MainRunner@Falling.fbx";
        // Horizontal superman-style fly loop played while the Drone Boost is active
        // (same Beaded Warrior rig). The clip bakes the flat aerodynamic pose into
        // the skeleton itself (Hips pitched 90°), so it renders horizontal on its
        // own — the DroneFlightVisual prone pitch is zeroed for it. Authoring:
        // meshy_jump_roll/create_fly_horizontal.py (frames 1–61 @30fps, loops).
        const string RunnerFlyModelPath = "Assets/Characters/Meshy_AI_Beaded_Warrior_biped/Fly_Horizontal_Loop.fbx";
        // Dedicated swipe-down forward-roll (same Beaded Warrior rig). Like the fly
        // clip it authors the body rotation into the Hips (the humanoid root); the
        // full 0→360° somersault must be baked into the pose or it vanishes under
        // the runner's root-motion-off Animator. Rolls in place (forward travel
        // left as discarded root motion — the world scrolls under it). Authoring:
        // meshy_jump_roll/create_roll_forward.py (frames 1–30 @30fps, one-shot).
        const string RunnerRollForwardModelPath = "Assets/Characters/Meshy_AI_Beaded_Warrior_biped/Roll_Forward.fbx";
        // Second selectable character on the ME page ("Themba"); Mgijimi above
        // stays the default. Its merged-animation FBX bundles the skin plus
        // several takes (Running, run_fast, Walking); BuildRunnerVisual picks
        // the run take and retargets the shared idle/jump/roll clips onto its
        // humanoid avatar. It has its own animator controller and PBR material.
        const string SecondRunnerModelPath = "Assets/Characters/Meshy_AI_Full_body_character_c_biped/Meshy_AI_Full_body_character_c_biped_Meshy_AI_Meshy_Merged_Animations.fbx";
        const string SecondRunnerAnimatorPath = "Assets/Animations/RunnerAnimator2.controller";
        const string SecondRunnerPbrMaterialPath = "Assets/Materials/Runner2ExternalPbr.mat";
        // JRPD traffic officer who chases the runner after a taxi side-swipe.
        const string OfficerModelPath = "Assets/Meshy_AI_JRPD_Traffic_Officer_biped/Meshy_AI_JRPD_Traffic_Officer_biped_Animation_Male_Run_Forward_Pick_Up_Left_withSkin.fbx";
        const string OfficerAnimatorPath = "Assets/Animations/OfficerAnimator.controller";
        const string OfficerPbrMaterialPath = "Assets/Materials/OfficerExternalPbr.mat";
        const string VideoSkylineTexturePath = "Assets/Textures/VideoSkyline.png";
        const string VideoSkylineMaterialPath = "Assets/Materials/VideoSkylinePhoto.mat";
        const string TelkomTowerModelPath = "Assets/Environment/TelkomTower/Meshy_AI_Telkom_Tower_Skyline_0705203244_texture_fbx/Meshy_AI_Telkom_Tower_Skyline_0705203244_texture.fbx";
        const string TelkomTowerAlbedoPath = "Assets/Environment/TelkomTower/Meshy_AI_Telkom_Tower_Skyline_0705203244_texture_fbx/Meshy_AI_Telkom_Tower_Skyline_0705203244_texture.png";
        const string TelkomTowerNormalPath = "Assets/Environment/TelkomTower/Meshy_AI_Telkom_Tower_Skyline_0705203244_texture_fbx/Meshy_AI_Telkom_Tower_Skyline_0705203244_texture_normal.png";
        const string TelkomTowerMaterialPath = "Assets/Environment/TelkomTower/TelkomTower_URP.mat";
        const string JacarandaTreeModelPath = "Assets/Environment/Roadside/Jacaranda_Tree.fbx";
        const string TrafficLightModelPath = "Assets/Environment/Roadside/Traffic_Light.fbx";
        const string ApprovedTrafficLightPrefabPath = "Assets/Art/Props/PF_traffic_light_optimized.prefab";
        const string ApkPath = "Builds/JoburgEndlessRunner.apk";
        const string PreviewPath = "Builds/preview.png";
        const string RunnerPbrMaterialPath = "Assets/Materials/RunnerExternalPbr.mat";
        // Runner is measured at its real extents now, so this is a true world
        // height: ~1.8 units reads as human next to the 2.3-unit taxis and
        // Production lane spacing. (It was 6.0 to counter the old inflated-bounds
        // measurement, which never produced a 6-unit runner on screen.)
        const float RunnerVisualTargetHeight = 1.85f;
        const float RunnerGroundSink = 0.35f;
        const float CoinTrailHeight = 1.25f;
        const float CoinArcHeight = 1.7f;
        const float CoinPrefabScale = 1f;
        const float RareCoinPrefabScale = 1.25f;
        const float CoinTriggerRadius = 0.65f;

        static readonly Dictionary<string, Material> Palette = new Dictionary<string, Material>();
        static CartoonVisualSettings CartoonSettings;

        [MenuItem("Joburg Runner/Build Minimum Playable Scene")]
        public static void BuildMinimumPlayableScene()
        {
            EnsureFolders();
            CartoonSettings = CreateCartoonVisualSettings();
            CreatePalette();
            CreateEnvironmentLibraryPrefabs();
            EnvironmentZoneProfileBuilder.GenerateExampleZoneProfiles();
            TuneRenderPipelineForMobile();
            CompressTextures();

            JunctionSpawnSettings junctionSettings = CreateJunctionSpawnSettings();
            CreateJunctionVariantPrefabs();
            GameObject roadPrefab = CreateRoadSegmentPrefab();
            GameObject taxiPrefab = CreateTaxiObstaclePrefab();
            GameObject coinPopPrefab = CreateCoinPopPrefab();
            GameObject coinPrefab = CreateCoinPrefab(coinPopPrefab);
            GameObject rareCoinPrefab = CreateRareCoinPrefab(coinPopPrefab);
            GameObject ubuntuDissolvePrefab = CreateUbuntuDissolveParticlePrefab();
            GameObject barrierPrefab = CreateBarrierObstaclePrefab();
            GameObject potholePrefab = CreatePotholeObstaclePrefab();
            SetField(barrierPrefab.GetComponent<ObstacleDissolveEffect>(), "dissolveParticlePrefab", ubuntuDissolvePrefab);
            SetField(potholePrefab.GetComponent<ObstacleDissolveEffect>(), "dissolveParticlePrefab", ubuntuDissolvePrefab);
            GameObject[] powerUpPrefabs = CreatePowerUpPrefabs(ubuntuDissolvePrefab);
            TrackChunk[] chunkPrefabs = CreateTrackChunkPrefabs(taxiPrefab, coinPrefab, rareCoinPrefab, powerUpPrefabs, barrierPrefab, potholePrefab);

            // Phase 1 socket decoration: poolable prop prefabs + tunable district profiles.
            WomanNpcBuilder.CreatePrefab();
            GameObject fruitsStallPrefab = FruitsStallPrefabBuilder.CreatePrefab();
            GameObject gentlemanPrefab = GentlemanNpcBuilder.CreatePrefab();
            GameObject wavingGentlemanPrefab = GentlemanWaveNpcBuilder.CreatePrefab();
            GameObject walkingWomanPrefab = PavementWalkerNpcBuilder.CreatePrefab();
            GameObject walkingManPrefab = PavementWalkerManNpcBuilder.CreatePrefab();
            DecorPrefabSet decorPrefabs = CreateDecorPrefabs();
            DistrictDecorProfile[] decorProfiles = CreateDistrictDecorProfiles(
                decorPrefabs, fruitsStallPrefab, gentlemanPrefab, wavingGentlemanPrefab,
                walkingWomanPrefab, walkingManPrefab);
            // Hero-building catalogue asset, wired into the decor director below.
            HeroBuildingSet heroSet = CreateHeroBuildingSet();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "JoburgEndlessRunner";

            CreateLighting();
            GameObject player = CreatePlayer();
            CreateCamera(player.transform);
            CreateRoadsideSpawnHaze(player.transform);
            CreateSkyline(player.transform);
            // Taxis are the only obstacle: they come head-on across the three lanes.
            CreateSystems(player.transform, roadPrefab, chunkPrefabs, junctionSettings);
            CreateUi(player.GetComponent<PlayerController>());
            CreateGameSystems(player);
            CreateDecorDirector(decorProfiles, heroSet);
            CreatePigeonSystem(player.transform);

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
            foreach (Canvas sceneCanvas in Object.FindObjectsByType<Canvas>())
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
            // Rebuild and render the real obstacle prefab (with its spinning
            // wheels) so the closeup shows the taxi exactly as it appears in play,
            // not a bare model.
            CreateTaxiObstaclePrefab();
            GameObject holder = new GameObject("TaxiCloseupHolder");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TaxiPrefabPath);
            GameObject taxi = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform)
                : FbxVehicleInstance("taxi", holder.transform, new Vector3(0f, 0f, 7f), 180f, 2.3f);
            if (taxi == null)
            {
                Debug.LogError("taxi model not found for closeup.");
                Object.DestroyImmediate(holder);
                return;
            }

            taxi.transform.position = new Vector3(0f, 0f, 7f);
            CapturePreviewScreenshot();
            Object.DestroyImmediate(holder);
        }

        [MenuItem("Joburg Runner/Debug Roadside Models")]
        public static void DebugRoadsideModels()
        {
            CreatePalette();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject parent = new GameObject("DebugParent");
            var placements = new[]
            {
                (JacarandaTreeModelPath, new Vector3(-8.75f, 0f, 6f), 40f, 6.5f, "JacarandaTree"),
                (TrafficLightModelPath, new Vector3(-5.95f, 0f, 4f), 90f, 4.8f, "TrafficLight"),
            };
            foreach (var (path, pos, yRot, targetH, key) in placements)
            {
                RoadsideModel(path, parent.transform, pos, yRot, targetH, key);
            }

            foreach (Transform child in parent.transform)
            {
                Renderer[] rs = child.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0)
                {
                    Debug.Log($"ROADSIDE-DEBUG placed '{child.name}' NO renderers");
                    continue;
                }

                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }

                Transform visual = child.GetChild(0);
                Debug.Log($"ROADSIDE-DEBUG placed '{child.name}' visualScale={visual.localScale} FINAL-WORLD-SIZE=({b.size.x:F2},{b.size.y:F2},{b.size.z:F2}) center=({b.center.x:F2},{b.center.y:F2},{b.center.z:F2})");
            }
        }

        // Renders one dressed tile per district to Builds/preview_district_{i}.png.
        // Runs the decorator in edit mode (Awake does not fire, so SegmentDecorator
        // lazily caches its sockets) with the intersection flag on, so CBD/Business
        // traffic lights are visible.
        [MenuItem("Joburg Runner/Capture District Decor (all 4)")]
        public static void CaptureAllDistrictDecor()
        {
            // Isolated throwaway scene with its own camera/light so we never open,
            // dirty or depend on the saved game scene (which has no baked road – the
            // spawner builds tiles at runtime).
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            CreatePalette();

            bool prevAsync = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;

            Camera cam = Camera.main;
            cam.transform.position = new Vector3(0f, 5.2f, -7f);
            cam.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.72f, 0.88f);
            cam.fieldOfView = 55f;
            cam.farClipPlane = 200f;

            string[] files =
            {
                "Assets/Environment/Decor/District_CBD.asset",
                "Assets/Environment/Decor/District_Commissioner.asset",
                "Assets/Environment/Decor/District_Park.asset",
                "Assets/Environment/Decor/District_Business.asset",
            };

            GameObject roadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoadPrefabPath);
            GameObject idle = new GameObject("EditDecorPoolRoot");
            idle.SetActive(false);
            var pool = new DecorPool(idle.transform);

            for (int district = 0; district < files.Length; district++)
            {
                var profile = AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>(files[district]);
                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(roadPrefab);
                tile.transform.position = Vector3.zero;
                tile.GetComponent<RoadSegmentVisuals>().SetDistrict(district);

                var decorator = tile.GetComponent<SegmentDecorator>();
                decorator.Decorate(profile, seed: district * 1000 + 7, isIntersection: true, pool);
                Debug.Log($"DECOR-DEBUG district {district} ({(profile != null ? profile.districtName : "null")}): {decorator.DebugState()}");

                RenderCameraToFile(cam, $"Builds/preview_district_{district}.png");
                Object.DestroyImmediate(tile);
            }

            Object.DestroyImmediate(idle);
            EditorSettings.asyncShaderCompilation = prevAsync;
        }

        // Renders the walking-woman pedestrian up close to confirm she imports
        // upright at human scale, stands on the pavement, and — under a yaw-180
        // socket like the runtime one — faces world -Z (toward the camera behind
        // the player). Writes Builds/preview_walker.png and logs her bounds.
        [MenuItem("Joburg Runner/Capture Pavement Walker Preview")]
        public static void CapturePavementWalkerPreview()
        {
            CaptureWalkerPrefab(PavementWalkerNpcBuilder.PrefabPath, "Builds/preview_walker.png");
        }

        [MenuItem("Joburg Runner/Capture Pavement Walker Man Preview")]
        public static void CapturePavementWalkerManPreview()
        {
            CaptureWalkerPrefab(PavementWalkerManNpcBuilder.PrefabPath, "Builds/preview_walker_man.png");
        }

        static void CaptureWalkerPrefab(string prefabPath, string outPath)
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            CreatePalette();

            bool prevAsync = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;

            Camera cam = Camera.main;
            // Camera behind the player looking +Z; they face -Z, so we see the front.
            // Kept low so the feet/ground contact is clearly in frame.
            cam.transform.position = new Vector3(1.2f, 0.9f, -3.6f);
            cam.transform.rotation = Quaternion.Euler(2f, -16f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.72f, 0.88f);
            cam.fieldOfView = 45f;
            cam.farClipPlane = 200f;

            // Grey ground quad at y=0 so grounding is verifiable against pavement level.
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundRef";
            ground.transform.localScale = new Vector3(2f, 1f, 2f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Walker prefab missing at {prefabPath}; run the scene build first.");
                EditorSettings.asyncShaderCompilation = prevAsync;
                return;
            }

            // Replicate the runtime socket: yaw 180 so the root's forward is world -Z.
            GameObject socket = new GameObject("SocketSim");
            socket.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetParent(socket.transform, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;

            Transform visual = inst.transform.childCount > 0
                ? inst.transform.GetChild(0)
                : inst.transform;
            Animator animator = visual.GetComponent<Animator>();
            AnimationClip walk = animator != null && animator.runtimeAnimatorController != null
                && animator.runtimeAnimatorController.animationClips.Length > 0
                ? animator.runtimeAnimatorController.animationClips[0]
                : null;
            if (walk != null)
            {
                walk.SampleAnimation(visual.gameObject, walk.length * 0.3f);
            }

            Renderer[] renderers = inst.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    b.Encapsulate(renderers[i].bounds);
                }
                Debug.Log($"WALKER-DEBUG center={b.center} size={b.size} minY={b.min.y:0.###} " +
                          $"clip={(walk != null ? walk.name : "none")}");
            }

            RenderCameraToFile(cam, outPath); // warm-up
            RenderCameraToFile(cam, outPath);
            EditorSettings.asyncShaderCompilation = prevAsync;
        }

        // Renders the pigeon (tight, mid-flap, on a ground plane) and the swapped
        // junction road sign, to verify scale/orientation/texture and clip import.
        // Writes Builds/preview_pigeon.png and Builds/preview_roadsign.png + logs.
        [MenuItem("Joburg Runner/Capture Pigeon Preview")]
        public static void CapturePigeonPreview()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            CreatePalette();
            bool prevAsync = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundRef";
            ground.transform.localScale = new Vector3(2f, 1f, 2f);

            Camera cam = Camera.main;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.72f, 0.88f);
            cam.farClipPlane = 200f;

            // --- Pigeon (tight) ---
            GameObject pigeonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PigeonBuilder.PigeonPrefabPath);
            if (pigeonPrefab != null)
            {
                GameObject pigeon = (GameObject)PrefabUtility.InstantiatePrefab(pigeonPrefab);
                pigeon.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 35f, 0f));
                Animator animator = pigeon.GetComponentInChildren<Animator>();
                AnimationClip pose = null;
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    foreach (AnimationClip c in animator.runtimeAnimatorController.animationClips)
                    {
                        if (c.name.Contains("Idle")) { pose = c; break; }
                    }
                }
                if (pose != null)
                {
                    pose.SampleAnimation(animator.gameObject, 0f);
                }
                Renderer[] rs = pigeon.GetComponentsInChildren<Renderer>(true);
                Bounds b = rs.Length > 0 ? rs[0].bounds : default;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                int clipCount = animator != null && animator.runtimeAnimatorController != null
                    ? animator.runtimeAnimatorController.animationClips.Length : 0;
                Debug.Log($"PIGEON-DEBUG bounds size={b.size} center={b.center} clips={clipCount} " +
                          $"renderers={rs.Length}");

                cam.transform.position = new Vector3(0.48f, 0.24f, 0.44f);
                cam.transform.LookAt(new Vector3(0f, 0.13f, 0f));
                cam.fieldOfView = 34f;
                RenderCameraToFile(cam, "Builds/preview_pigeon.png"); // warm-up
                RenderCameraToFile(cam, "Builds/preview_pigeon.png");
            }
            else
            {
                Debug.LogError("Pigeon prefab missing; run the scene build first.");
            }

            // --- Swapped road sign (wider) ---
            GameObject signPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DirectionSignPrefabPath);
            if (signPrefab != null)
            {
                GameObject sign = (GameObject)PrefabUtility.InstantiatePrefab(signPrefab);
                sign.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 30f, 0f));
                Renderer[] srs = sign.GetComponentsInChildren<Renderer>(true);
                Bounds sb = srs.Length > 0 ? srs[0].bounds : default;
                for (int i = 1; i < srs.Length; i++) sb.Encapsulate(srs[i].bounds);
                Debug.Log($"ROADSIGN-DEBUG size={sb.size} center={sb.center}");
                float h = Mathf.Max(sb.size.y, 0.5f);
                cam.transform.position = new Vector3(0f, sb.center.y, -(h * 2.2f + 2f));
                cam.transform.LookAt(sb.center);
                cam.fieldOfView = 45f;
                RenderCameraToFile(cam, "Builds/preview_roadsign.png"); // warm-up
                RenderCameraToFile(cam, "Builds/preview_roadsign.png");
            }

            EditorSettings.asyncShaderCompilation = prevAsync;
        }

        // Renders the six hero-building validation scenarios from the actual running
        // camera pose (player at origin), mimicking what the director does at runtime:
        // dress CBD props, then SetHero on a pooled hero. Confirms spawn, placeholder
        // hide, release/restore, and façade visibility.
        [MenuItem("Joburg Runner/Capture Hero Scenarios")]
        public static void CaptureHeroScenarios()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            CreatePalette();
            bool prevAsync = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;

            // Running camera pose: pulled back slightly to show more of the street.
            Camera cam = Camera.main;
            Vector3 player = Vector3.zero;
            cam.transform.position = player + new Vector3(0f, 2.25f, -5.15f);
            cam.transform.rotation = Quaternion.LookRotation(player + Vector3.up * 1.05f - cam.transform.position);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.72f, 0.88f);
            cam.fieldOfView = 50f;
            cam.farClipPlane = 400f;

            GameObject roadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoadPrefabPath);
            var cbd = AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>("Assets/Environment/Decor/District_CBD.asset");
            var heroSet = AssetDatabase.LoadAssetAtPath<HeroBuildingSet>(HeroBuildingSetPath);

            GameObject propIdle = new GameObject("PropIdle") { }; propIdle.SetActive(false);
            GameObject heroIdle = new GameObject("HeroIdle") { }; heroIdle.SetActive(false);
            var propPool = new DecorPool(propIdle.transform);
            var heroPool = new DecorPool(heroIdle.transform);

            HeroEntry a = heroSet != null && heroSet.Count > 0 ? heroSet.entries[0] : null;
            HeroEntry b = heroSet != null && heroSet.Count > 1 ? heroSet.entries[1] : null;

            (string name, HeroEntry entry, HeroSide side, bool release)[] scenarios =
            {
                ("Builds/preview_hero_0_none.png",     null, HeroSide.None,  false),
                ("Builds/preview_hero_1_A_left.png",   a,    HeroSide.Left,  false),
                ("Builds/preview_hero_2_A_right.png",  a,    HeroSide.Right, false),
                ("Builds/preview_hero_3_B_left.png",   b,    HeroSide.Left,  false),
                ("Builds/preview_hero_4_B_right.png",  b,    HeroSide.Right, false),
                ("Builds/preview_hero_5_recycled.png", a,    HeroSide.Left,  true),
            };

            foreach (var s in scenarios)
            {
                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(roadPrefab);
                // Push the tile forward so its side buildings fall inside the narrow
                // portrait horizontal FOV, as they do in-game when a CBD block is ahead.
                tile.transform.position = new Vector3(0f, 0f, 30f);
                tile.GetComponent<RoadSegmentVisuals>().SetDistrict(0);
                var decorator = tile.GetComponent<SegmentDecorator>();
                decorator.Decorate(cbd, seed: 7, isIntersection: true, propPool);

                decorator.ClearHeroes(heroPool);
                if (s.entry != null)
                {
                    decorator.SetHero(s.entry, s.side, heroPool);
                    if (s.release)
                    {
                        // Simulate recycling to a non-CBD tile: hero released, placeholders restored.
                        decorator.ClearHeroes(heroPool);
                    }
                }

                foreach (var lg in tile.GetComponentsInChildren<LODGroup>(true))
                {
                    lg.ForceLOD(0);
                }

                RenderCameraToFile(cam, s.name);
                Debug.Log($"HERO-CAPTURE {s.name}: {decorator.DebugState()}");
                Object.DestroyImmediate(tile);
            }

            Object.DestroyImmediate(propIdle);
            Object.DestroyImmediate(heroIdle);
            EditorSettings.asyncShaderCompilation = prevAsync;
        }

        // Representative runner-camera capture: a row of real tiles (foreground + a CBD
        // hero tile + backdrop) dressed by the real director, shot from the actual game
        // camera pose. Produces the six required scenarios plus a forward-approach
        // sequence to confirm the façade stays visible for ~2 s of movement.
        [MenuItem("Joburg Runner/Capture Hero Runner View")]
        public static void CaptureHeroRunnerView()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            CreatePalette();
            bool prevAsync = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;

            var profiles = new[]
            {
                AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>("Assets/Environment/Decor/District_CBD.asset"),
                AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>("Assets/Environment/Decor/District_Commissioner.asset"),
                AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>("Assets/Environment/Decor/District_Park.asset"),
                AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>("Assets/Environment/Decor/District_Business.asset"),
                AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>("Assets/Environment/Decor/District_Bridge.asset"),
            };
            var heroSet = AssetDatabase.LoadAssetAtPath<HeroBuildingSet>(HeroBuildingSetPath);

            var go = new GameObject("EnvironmentDecorDirector");
            var director = go.AddComponent<EnvironmentDecorDirector>();
            SetField(director, "useSocketDecoration", true);
            SetField(director, "heroOnlyMode", false);
            SetField(director, "profilesByDistrict", profiles);
            SetField(director, "intersectionEvery", 3);
            SetField(director, "prewarmPerPrefab", 16);
            SetField(director, "heroSet", heroSet);
            SetField(director, "heroAllDistricts", true);
            SetField(director, "heroPrewarm", 2);
            SetField(director, "heroMinGap", 1);
            SetField(director, "heroMaxGap", 1);
            SetField(director, "heroEarlyGuaranteeWithin", 1);
            SetField(director, "heroAvoidRecent", 1);
            SetField(director, "heroBothSides", true);
            InvokePrivate(director, "Awake");
            InvokePrivate(director, "Start");

            GameObject roadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoadPrefabPath);

            // Row of tiles: foreground non-CBD, CBD hero tile at Z=30, then backdrop.
            int[] districts = { 2, 1, 5, 3, 4, 6 };  // index 2 (Z=30) is CBD (5 % 5 == 0)
            var tiles = new GameObject[districts.Length];
            SegmentDecorator target = null;
            for (int i = 0; i < districts.Length; i++)
            {
                tiles[i] = (GameObject)PrefabUtility.InstantiatePrefab(roadPrefab);
                tiles[i].transform.position = new Vector3(0f, 0f, -30f + i * 30f);
                tiles[i].GetComponent<RoadSegmentVisuals>().SetDistrict(districts[i]);
                var dec = tiles[i].GetComponent<SegmentDecorator>();
                director.DecorateSegment(dec, districts[i]);
                if (i == 2) target = dec;
            }

            Camera cam = Camera.main;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.72f, 0.88f);
            cam.fieldOfView = 50f;
            cam.farClipPlane = 400f;

            HeroEntry a = heroSet != null && heroSet.Count > 0 ? heroSet.entries[0] : null;
            HeroEntry b = heroSet != null && heroSet.Count > 1 ? heroSet.entries[1] : null;
            var heroPoolField = typeof(EnvironmentDecorDirector).GetField("heroPool",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var heroPool = (DecorPool)heroPoolField.GetValue(director);

            (string name, HeroEntry entry, HeroSide side, bool release)[] scenarios =
            {
                ("Builds/preview_runner_0_none.png",     null, HeroSide.None,  false),
                ("Builds/preview_runner_1_A_left.png",   a,    HeroSide.Left,  false),
                ("Builds/preview_runner_2_A_right.png",  a,    HeroSide.Right, false),
                ("Builds/preview_runner_3_B_left.png",   b,    HeroSide.Left,  false),
                ("Builds/preview_runner_4_B_right.png",  b,    HeroSide.Right, false),
                ("Builds/preview_runner_5_recycled.png", a,    HeroSide.Left,  true),
            };

            foreach (var s in scenarios)
            {
                target.ClearHeroes(heroPool);
                if (s.entry != null) target.SetHero(s.entry, s.side, heroPool);
                if (s.release) target.ClearHeroes(heroPool);
                ForceLod0(tiles[2]);
                PlaceRunnerCamera(cam, 0f);
                RenderCameraToFile(cam, s.name);
            }

            // Forward-approach sequence (Building A on the left): ~2 s of travel at 9 m/s.
            target.ClearHeroes(heroPool);
            target.SetHero(a, HeroSide.Left, heroPool);
            ForceLod0(tiles[2]);
            float[] playerZ = { 0f, 8f, 16f, 24f };
            for (int i = 0; i < playerZ.Length; i++)
            {
                PlaceRunnerCamera(cam, playerZ[i]);
                RenderCameraToFile(cam, $"Builds/preview_runner_approach_{i}.png");
            }

            for (int i = 0; i < tiles.Length; i++) Object.DestroyImmediate(tiles[i]);
            Object.DestroyImmediate(go);
            EditorSettings.asyncShaderCompilation = prevAsync;
        }

        static void PlaceRunnerCamera(Camera cam, float playerZ)
        {
            Vector3 playerPos = new Vector3(0f, 0f, playerZ);
            cam.transform.position = playerPos + new Vector3(0f, 2.05f, -4.35f);
            cam.transform.rotation = Quaternion.LookRotation(playerPos + Vector3.up * 1.05f - cam.transform.position);
        }

        static void ForceLod0(GameObject tile)
        {
            foreach (var lg in tile.GetComponentsInChildren<LODGroup>(true)) lg.ForceLOD(0);
        }

        // Headless simulation of the road treadmill: drives the real EnvironmentDecorDirector
        // and real RoadSegment tiles through N recycled dresses, recording hero cadence,
        // side alternation, no-repeat, placeholder hide/restore and overlap stats.
        [MenuItem("Joburg Runner/Validate Hero Spawning")]
        public static void ValidateHeroSpawning()
        {
            const int dresses = 120;      // >= 100 recycled segments
            const int ringSize = 7;       // matches RoadSegmentSpawner.visibleSegments
            const float segLen = 30f;

            var warnings = new List<string>();
            var errors = new List<string>();
            Application.LogCallback cb = (msg, stack, type) =>
            {
                if (type == LogType.Warning && msg.Contains("DecorPool")) warnings.Add(msg);
                if (type == LogType.Error || type == LogType.Exception) errors.Add(msg);
            };
            Application.logMessageReceived += cb;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Real district profiles + hero set, exactly as the scene wires them.
            var profiles = new[]
            {
                AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>("Assets/Environment/Decor/District_CBD.asset"),
                AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>("Assets/Environment/Decor/District_Commissioner.asset"),
                AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>("Assets/Environment/Decor/District_Park.asset"),
                AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>("Assets/Environment/Decor/District_Business.asset"),
                AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>("Assets/Environment/Decor/District_Bridge.asset"),
            };
            var heroSet = AssetDatabase.LoadAssetAtPath<HeroBuildingSet>(HeroBuildingSetPath);

            var go = new GameObject("EnvironmentDecorDirector");
            var director = go.AddComponent<EnvironmentDecorDirector>();
            SetField(director, "useSocketDecoration", true);
            SetField(director, "profilesByDistrict", profiles);
            SetField(director, "intersectionEvery", 3);
            SetField(director, "prewarmPerPrefab", 16);
            SetField(director, "heroSet", heroSet);
            SetField(director, "heroPrewarm", 2);
            SetField(director, "heroMinGap", 1);
            SetField(director, "heroMaxGap", 2);
            SetField(director, "heroEarlyGuaranteeWithin", 1);
            SetField(director, "heroAvoidRecent", 1);
            SetField(director, "heroBothSides", true);
            InvokePrivate(director, "Awake");
            InvokePrivate(director, "Start"); // builds prop + hero pools

            GameObject roadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoadPrefabPath);
            var ring = new GameObject[ringSize];
            for (int i = 0; i < ringSize; i++)
            {
                ring[i] = (GameObject)PrefabUtility.InstantiatePrefab(roadPrefab);
                ring[i].transform.position = new Vector3(0f, 0f, i * segLen);
            }

            // Stats
            int cbdCount = 0, heroCount = 0, leftCount = 0, rightCount = 0;
            int consecutiveRepeat = 0, hideFailures = 0, restoreFailures = 0;
            int permOverlaps = 0, propOverlaps = 0, maxActiveHeroes = 0;
            int firstHeroStep = -1, firstHeroCbd = -1, lastHeroStep = -1;
            string lastHeroPrefab = null;
            var gapsTiles = new List<int>();
            var prefabCounts = new Dictionary<string, int>();

            for (int step = 0; step < dresses; step++)
            {
                int districtIndex = step + 1;            // spawner's nextDistrictIndex starts at 1
                bool isCbd = districtIndex % profiles.Length == 0;
                if (isCbd) cbdCount++;

                GameObject tile = ring[step % ringSize];
                tile.GetComponent<RoadSegmentVisuals>().SetDistrict(districtIndex);
                var decorator = tile.GetComponent<SegmentDecorator>();
                director.DecorateSegment(decorator, districtIndex);

                // Observe the freshly-dressed tile (a hero block may fill both sides).
                GameObject hL = decorator.EditorHeroLeft;
                GameObject hR = decorator.EditorHeroRight;
                var groups = tile.GetComponentsInChildren<HeroPlaceholderGroup>(true);
                HeroPlaceholderGroup gL = null, gR = null;
                foreach (var g in groups)
                {
                    if (g.name.EndsWith("_L")) gL = g; else if (g.name.EndsWith("_R")) gR = g;
                }

                // Left-then-right emission order (matches how the director places them).
                foreach (var (hero, left, occ, other) in new[] {
                    (hL, true, gL, gR), (hR, false, gR, gL) })
                {
                    if (hero == null)
                    {
                        continue;
                    }

                    heroCount++;
                    if (left) leftCount++; else rightCount++;

                    string prefab = hero.name.Replace("(Clone)", "").Trim();
                    prefabCounts.TryGetValue(prefab, out int pc); prefabCounts[prefab] = pc + 1;
                    if (lastHeroPrefab != null && prefab == lastHeroPrefab) consecutiveRepeat++;
                    lastHeroPrefab = prefab;

                    if (occ == null || !occ.HasHidden) hideFailures++;
                    OverlapCounts(tile, hero, other, ref permOverlaps, ref propOverlaps);
                }

                // A side with no hero must have its placeholders fully restored.
                if (hL == null && gL != null && gL.HasHidden) restoreFailures++;
                if (hR == null && gR != null && gR.HasHidden) restoreFailures++;

                if (hL != null || hR != null)
                {
                    if (firstHeroStep < 0) { firstHeroStep = step; firstHeroCbd = cbdCount; }
                    if (lastHeroStep >= 0) gapsTiles.Add(step - lastHeroStep);
                    lastHeroStep = step;
                }

                int activeNow = 0;
                for (int r = 0; r < ringSize; r++)
                {
                    var d = ring[r].GetComponent<SegmentDecorator>();
                    if (d.EditorHeroLeft != null) activeNow++;
                    if (d.EditorHeroRight != null) activeNow++;
                }
                maxActiveHeroes = Mathf.Max(maxActiveHeroes, activeNow);
            }

            // Report
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("===== HERO SPAWNING VALIDATION =====");
            sb.AppendLine($"config: minGap=1 maxGap=2 earlyGuarantee<=1 avoidRecent=1 heroPrewarm=2 bothSides=true ring={ringSize} segLen={segLen}m");
            sb.AppendLine($"total road segments processed : {dresses}");
            sb.AppendLine($"CBD appearances               : {cbdCount}");
            sb.AppendLine($"hero appearances              : {heroCount}");
            sb.AppendLine($"first hero: step {firstHeroStep} (CBD appearance #{firstHeroCbd}) = {firstHeroStep * segLen:F0} m");
            if (gapsTiles.Count > 0)
            {
                float min = gapsTiles[0], max = gapsTiles[0], sum = 0;
                foreach (int g in gapsTiles) { min = Mathf.Min(min, g); max = Mathf.Max(max, g); sum += g; }
                sb.AppendLine($"hero gaps (tiles): min={min} max={max} avg={sum / gapsTiles.Count:F1}  " +
                              $"=> metres: min={min * segLen:F0} max={max * segLen:F0} avg={sum / gapsTiles.Count * segLen:F0}");
                // In CBD-appearance terms (CBD every {profiles.Length} tiles):
                sb.AppendLine($"hero gaps (CBD appearances): min={min / profiles.Length:F0} max={max / profiles.Length:F0} (target 1-2)");
            }
            sb.AppendLine($"left / right spawns            : {leftCount} / {rightCount}");
            sb.Append("prefab selection counts       : ");
            foreach (var kv in prefabCounts) sb.Append($"{kv.Key}={kv.Value}  ");
            sb.AppendLine();
            sb.AppendLine($"consecutive-repeat violations : {consecutiveRepeat}");
            sb.AppendLine($"placeholder hide failures     : {hideFailures}");
            sb.AppendLine($"placeholder restore failures  : {restoreFailures}");
            sb.AppendLine($"overlaps w/ other-side buildings: {permOverlaps}");
            sb.AppendLine($"overlaps w/ trees/lights/props : {propOverlaps}");
            sb.AppendLine($"max simultaneous active heroes : {maxActiveHeroes}");
            sb.AppendLine($"pool growth warnings           : {warnings.Count}");
            sb.AppendLine($"console errors / exceptions    : {errors.Count}");
            foreach (var e in errors) sb.AppendLine("   ERROR: " + e);
            Debug.Log(sb.ToString());
            Directory.CreateDirectory("Builds");
            File.WriteAllText("Builds/hero_validation_report.txt", sb.ToString());

            Application.logMessageReceived -= cb;
            for (int i = 0; i < ringSize; i++) Object.DestroyImmediate(ring[i]);
            Object.DestroyImmediate(go);
        }

        static void OverlapCounts(GameObject tile, GameObject hero, HeroPlaceholderGroup otherSide,
                                  ref int permOverlaps, ref int propOverlaps)
        {
            if (!TryBounds(hero, out Bounds hb)) return;

            // Other-side placeholders (should never overlap a same-side hero).
            if (otherSide != null)
            {
                foreach (Transform child in otherSide.transform)
                {
                    if (child.gameObject.activeSelf && TryBounds(child.gameObject, out Bounds ob) && hb.Intersects(ob))
                    {
                        permOverlaps++;
                    }
                }
            }

            // Pooled props (trees/lamps/traffic/etc.) parented under DecorSockets sockets.
            Transform sockets = tile.transform.Find("DecorSockets");
            if (sockets == null) return;
            foreach (Transform socket in sockets)
            {
                var ds = socket.GetComponent<JoburgRunner.Environment.Decor.DecorSocket>();
                if (ds == null || ds.SocketType == JoburgRunner.Environment.Decor.DecorSocketType.HeroBuilding) continue;
                foreach (Transform prop in socket)
                {
                    if (prop.gameObject.activeSelf && TryBounds(prop.gameObject, out Bounds pb) && hb.Intersects(pb))
                    {
                        propOverlaps++;
                    }
                }
            }
        }

        static bool TryBounds(GameObject go, out Bounds b)
        {
            var rends = go.GetComponentsInChildren<Renderer>(false);
            b = default;
            if (rends.Length == 0) return false;
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return true;
        }

        static void InvokePrivate(object target, string method)
        {
            var m = target.GetType().GetMethod(method,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            m?.Invoke(target, null);
        }

        static void RenderCameraToFile(Camera cam, string path)
        {
            const int w = 720, h = 1280;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.aspect = (float)w / h;
            cam.Render();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            cam.ResetAspect();
            RenderTexture.active = null;

            Directory.CreateDirectory("Builds");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }

        [MenuItem("Joburg Runner/Capture Roadside Closeup")]
        public static void CaptureRoadsideCloseup()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            CreatePalette();
            GameObject holder = new GameObject("RoadsideCloseupHolder");
            RoadsideModel(JacarandaTreeModelPath, holder.transform, new Vector3(-3f, 0f, 8f), 0f, 6.5f, "JacarandaTree");
            RoadsideModel(TrafficLightModelPath, holder.transform, new Vector3(3f, 0f, 8f), 0f, 4.8f, "TrafficLight");
            CapturePreviewScreenshot();
            Object.DestroyImmediate(holder);
        }

        /// <summary>
        /// Same as the closeup but turned side-on, to judge wheel-arch fit,
        /// wheel track, and which way the rims face.
        /// </summary>
        [MenuItem("Joburg Runner/Capture Taxi Side View")]
        public static void CaptureTaxiSideView()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            CreatePalette();
            CreateTaxiObstaclePrefab();
            GameObject holder = new GameObject("TaxiSideViewHolder");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TaxiPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("taxi prefab not found for side view.");
                Object.DestroyImmediate(holder);
                return;
            }

            GameObject taxi = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            taxi.transform.position = new Vector3(0f, 0f, 6f);
            taxi.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            CapturePreviewScreenshot();
            Object.DestroyImmediate(holder);
        }

        [MenuItem("Joburg Runner/Capture Officer Closeup")]
        public static void CaptureOfficerCloseup()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            CreatePalette();
            TrafficOfficerChase chase = CreateTrafficOfficer(null, null);
            if (chase == null)
            {
                Debug.LogError("Traffic officer could not be built for closeup.");
                return;
            }

            // In edit mode Awake has not run, so the visual is still active.
            // Off to the side so the idle runner does not hide him.
            chase.transform.position = new Vector3(-1.9f, 0f, 5f);
            CapturePreviewScreenshot();
            Object.DestroyImmediate(chase.gameObject);
        }

        [MenuItem("Joburg Runner/Capture Coin Closeup")]
        [MenuItem("Joburg Runner/Capture Ubuntu Pulse Closeup")]
        public static void CaptureUbuntuPulseCloseup()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UbuntuPulsePrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Ubuntu Pulse prefab not found at {UbuntuPulsePrefabPath}.");
                return;
            }

            GameObject holder = new GameObject("UbuntuPulseCloseupHolder");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            instance.transform.position = new Vector3(0f, 1.2f, 5f);
            CapturePreviewScreenshot();
            Object.DestroyImmediate(holder);
        }

        /// <summary>
        /// Screenshots the running-dust rig mid-simulation: continuous foot
        /// dust plus a landing burst. Awake doesn't run in edit mode, so the
        /// runtime dust material and colour are applied manually here for a
        /// faithful preview. The scene is not saved afterwards.
        /// </summary>
        public static void CaptureRunningDustPreview()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject player = GameObject.Find("Player");
            Transform anchor = player != null ? player.transform.Find("RunningDustAnchor") : null;
            if (anchor == null)
            {
                Debug.LogError("RunningDustAnchor not found on the player.");
                return;
            }

            Color dustColour = new Color(0.62f, 0.55f, 0.47f, 0.6f);
            Material previewDust = new Material(UnlitTransparentMaterial("RunningDust", dustColour));
            previewDust.SetTexture("_BaseMap", UbuntuPulseVisual.SoftGlowTexture());
            previewDust.SetTexture("_MainTex", UbuntuPulseVisual.SoftGlowTexture());
            foreach (ParticleSystemRenderer dustRenderer in anchor.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                dustRenderer.sharedMaterial = previewDust;
            }

            foreach (ParticleSystem system in anchor.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                main.startColor = dustColour;
                if (system.name == "LandingDustBurst")
                {
                    system.Emit(12);
                    system.Simulate(0.15f, true, false);
                }
                else
                {
                    system.Simulate(0.6f, true, true);
                }

            }

            CapturePreviewScreenshot();
        }

        /// <summary>
        /// Samples the split jumpandroll clips onto the live runner avatar and
        /// screenshots the mid-jump (aerial flip) and mid-roll (ground tuck)
        /// poses, proving the Beaded-Warrior mocap retargets onto Mgijimi. Writes
        /// Builds/preview_jumppose.png and Builds/preview_rollpose.png. Edit mode
        /// never ticks the animator, so SampleAnimation writes each pose directly.
        /// The scene is not saved.
        /// </summary>
        [MenuItem("Joburg Runner/Capture Jump Roll Poses")]
        public static void CaptureJumpRollPoses()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Transform runnerVisual = null;
            PlayerController controller = Object.FindAnyObjectByType<PlayerController>();
            if (controller != null)
            {
                foreach (Transform child in controller.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "RunnerVisual_Fbx") { runnerVisual = child; break; }
                }
            }

            if (runnerVisual == null)
            {
                Debug.LogError("RunnerVisual_Fbx not found for jump/roll pose capture.");
                return;
            }

            if (!LoadRunnerJumpRollClips(out AnimationClip jump, out AnimationClip roll, out AnimationClip _)
                || jump == null || roll == null)
            {
                Debug.LogError("Jump/roll sub-clips unavailable for pose capture.");
                return;
            }

            // Mid-air flip (~60% through the takeoff→landing Jump clip).
            jump.SampleAnimation(runnerVisual.gameObject, jump.length * 0.6f);
            CapturePreviewScreenshot();
            File.Copy(PreviewPath, "Builds/preview_jumppose.png", true);

            // Ground tuck (~45% through the landing→recovery Roll clip).
            roll.SampleAnimation(runnerVisual.gameObject, roll.length * 0.45f);
            CapturePreviewScreenshot();
            File.Copy(PreviewPath, "Builds/preview_rollpose.png", true);

            Debug.Log($"Captured jump pose (t={jump.length * 0.6f:0.00}s of {jump.length:0.00}s) " +
                $"and roll pose (t={roll.length * 0.45f:0.00}s of {roll.length:0.00}s).");
        }

        /// <summary>
        /// Screenshots the Perfect Dodge feedback forced on: the pooled
        /// streak burst simulated mid-flight around the player plus the
        /// "PERFECT DODGE!" label. Verifies the visual package without
        /// needing a live near miss. The scene is not saved afterwards.
        /// </summary>
        public static void CapturePerfectDodgePreview()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject player = GameObject.Find("Player");
            PerfectDodge dodge = player != null ? player.GetComponent<PerfectDodge>() : null;
            if (dodge == null)
            {
                Debug.LogError("Player with PerfectDodge not found in the scene.");
                return;
            }

            GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PerfectDodgeVFX.prefab");
            if (vfxPrefab != null)
            {
                GameObject vfx = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
                vfx.transform.SetPositionAndRotation(player.transform.position + Vector3.up * 0.9f, Quaternion.identity);
                vfx.SetActive(true);
                // Awake (and its glow-material swap) doesn't run in edit
                // mode, so apply the runtime material here for a faithful
                // preview of what the device renders.
                Material previewGlow = UbuntuPulseVisual.MakeAdditiveGlowMaterial(
                    UnlitColorMaterial("PerfectDodgeStreak", Color.white), UbuntuPulseVisual.SoftGlowTexture());
                foreach (ParticleSystemRenderer previewRenderer in vfx.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    previewRenderer.sharedMaterial = previewGlow;
                }

                foreach (ParticleSystem system in vfx.GetComponentsInChildren<ParticleSystem>(true))
                {
                    system.Simulate(0.07f, true, true);
                }
            }

            SerializedObject serializedDodge = new SerializedObject(dodge);
            SerializedProperty labelProperty = serializedDodge.FindProperty("dodgeLabel");
            TextMeshProUGUI label = labelProperty != null ? labelProperty.objectReferenceValue as TextMeshProUGUI : null;
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.rectTransform.localScale = new Vector3(1.12f, 1.12f, 1f);
            }

            CapturePreviewScreenshot();
        }

        /// <summary>
        /// Screenshots the Ubuntu Pulse Lane Shift mid-dash. A TrailRenderer
        /// only lays vertices from real movement in play mode, so both
        /// ribbons are laid manually with AddPosition along the arc a dash
        /// from the left lane would trace, and the spark/ground systems are
        /// emitted and simulated a few frames in. The scene is not saved.
        /// </summary>
        public static void CaptureUbuntuLaneShiftPreview()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject player = GameObject.Find("Player");
            UbuntuLaneShift shift = player != null ? player.GetComponentInChildren<UbuntuLaneShift>() : null;
            if (shift == null)
            {
                Debug.LogError("Player with UbuntuLaneShift not found in the scene.");
                return;
            }

            // Awake (and its glow-material swap) doesn't run in edit mode,
            // so apply the runtime materials here for a faithful preview.
            UbuntuLaneShift.ApplyRuntimeMaterials(shift.gameObject);

            foreach (TrailRenderer ribbon in shift.GetComponentsInChildren<TrailRenderer>(true))
            {
                ribbon.emitting = true;
                ribbon.widthMultiplier = ribbon.name.Contains("Glow") ? 0.26f : 0.16f;
                Vector3 head = ribbon.transform.position;
                // Behind and to the left: the runner races forward while
                // sliding right, so the ribbon sweeps back toward the
                // departed lane.
                Vector3 tail = head + new Vector3(-2.4f, 0.1f, -2f);
                const int points = 12;
                for (int i = 0; i <= points; i++)
                {
                    float t = i / (float)points;
                    Vector3 position = Vector3.Lerp(tail, head, t);
                    // Ease the sideways component so the ribbon curves into
                    // the new lane instead of cutting a straight diagonal.
                    position.x = head.x + (tail.x - head.x) * (1f - t) * (1f - t);
                    ribbon.AddPosition(position);
                }
            }

            foreach (ParticleSystem system in shift.GetComponentsInChildren<ParticleSystem>(true))
            {
                system.Emit(system.name.Contains("Ground") ? 6 : 10);
                // restart:false — the bursts are Emit()-driven; restarting
                // would clear the emitted particles.
                system.Simulate(0.08f, true, false);
            }

            CapturePreviewScreenshot();
        }

        public static void BuildSceneAndCaptureUbuntuLaneShiftPreview()
        {
            BuildMinimumPlayableScene();
            CapturePreviewScreenshot();
            File.Copy(PreviewPath, "Builds/preview_scene.png", true);
            CaptureUbuntuLaneShiftPreview();
        }

        /// <summary>
        /// Screenshots the Ubuntu Pulse effect rig forced active on the
        /// player — shield shader, rings, particles, shockwave, ground glow —
        /// so the active-effect VFX can be verified without playing a run.
        /// The scene is not saved afterwards.
        /// </summary>
        public static void CaptureUbuntuPulseActiveEffect()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject player = GameObject.Find("Player");
            UbuntuPulseVisual visual = player != null ? player.GetComponent<UbuntuPulseVisual>() : null;
            if (visual == null)
            {
                Debug.LogError("Player with UbuntuPulseVisual not found in the scene.");
                return;
            }

            visual.ForcePreview();
            CapturePreviewScreenshot();
        }

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

        [MenuItem("Joburg Runner/Capture Double Coin Stack")]
        public static void CaptureDoubleCoinStack()
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
                Debug.LogError("No main camera found for double coin stack capture.");
                return;
            }

            GameObject holder = new GameObject("DoubleCoinStackHolder");
            // Left coin stays normal for comparison; the right one is staged
            // with the Double Coins pair sprite manually — Awake doesn't run
            // in edit mode, so Coin can't do its own swap here. Mirrors the
            // sprite/scale math in Coin.ApplyDoubleStack.
            GameObject single = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            GameObject doubled = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            Vector3 anchor = camera.transform.position + camera.transform.forward * 3.4f;
            single.transform.position = anchor + camera.transform.right * -0.75f;
            doubled.transform.position = anchor + camera.transform.right * 0.75f;

            SpriteRenderer doubledArt = doubled.GetComponentInChildren<SpriteRenderer>();
            Sprite pairSprite = LoadHiggsfieldSprite("CoinDoubleStack");
            if (doubledArt != null && pairSprite != null && doubledArt.sprite != null)
            {
                float sizeRatio = doubledArt.sprite.bounds.size.y / pairSprite.bounds.size.y;
                doubledArt.sprite = pairSprite;
                doubledArt.transform.localScale *= sizeRatio * 1.4f;
            }

            CapturePreviewScreenshot();
            Object.DestroyImmediate(holder);
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

        [MenuItem("Joburg Runner/Capture Me Preview")]
        public static void CaptureMePreview() => CaptureCanvasPanel("MePanel");

        [MenuItem("Joburg Runner/Capture Boards Preview")]
        public static void CaptureBoardsPreview() => CaptureCanvasPanel("BoardsPanel");

        // Logs every renderer inside the hoverboard FBX with its local bounds,
        // to tell the deck apart from any extra geometry Meshy shipped with it.
        public static void DebugHoverboardModel()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(HoverboardModelPath);
            if (model == null)
            {
                Debug.LogError($"No model at {HoverboardModelPath}");
                return;
            }

            GameObject instance = Object.Instantiate(model);
            foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
            {
                Bounds b = filter.sharedMesh != null ? filter.sharedMesh.bounds : new Bounds();
                Debug.Log($"BOARDMESH '{filter.gameObject.name}' verts={filter.sharedMesh?.vertexCount ?? 0} " +
                    $"center=({b.center.x:0.####},{b.center.y:0.####},{b.center.z:0.####}) " +
                    $"size=({b.size.x:0.####},{b.size.y:0.####},{b.size.z:0.####}) scale={filter.transform.lossyScale}");

                if (filter.sharedMesh == null)
                {
                    continue;
                }

                // Vertex deciles along each axis, to find where the deck slab
                // sits versus the frame/kickstand appendages.
                Vector3[] vertices = filter.sharedMesh.vertices;
                for (int axis = 0; axis < 3; axis++)
                {
                    float[] values = new float[vertices.Length];
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        values[i] = vertices[i][axis];
                    }

                    System.Array.Sort(values);
                    string deciles = "";
                    for (int d = 0; d <= 10; d++)
                    {
                        int at = Mathf.Clamp(d * (values.Length - 1) / 10, 0, values.Length - 1);
                        deciles += $"{values[at]:0.####} ";
                    }

                    Debug.Log($"BOARDMESH axis {"XYZ"[axis]} deciles: {deciles}");
                }
            }

            Object.DestroyImmediate(instance);
        }

        // Renders the fully BUILT board (flip + trim + scale) from a 3/4 angle,
        // straight top and straight bottom, so the deck profile and which face
        // rides up can be verified. Writes BuiltBoard*.png under Icons.
        public static void CaptureBuiltBoardProfile()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Vector3[] eulers = { new Vector3(28f, 18f, 0f), new Vector3(90f, 0f, 0f), new Vector3(-90f, 0f, 0f) };
            string[] names = { "BuiltBoard34", "BuiltBoardTop", "BuiltBoardBottom" };
            for (int i = 0; i < eulers.Length; i++)
            {
                GameObject holder = new GameObject("BuiltBoardHolder");
                GameObject board = CreateIonCruiserBoard(holder.transform, 0);
                board.SetActive(true);
                RenderInstanceIcon(holder, names[i], eulers[i]);
            }
        }

        // Renders the raw hoverboard FBX from the three canonical axes into
        // Assets/Textures/Icons/BoardAxis*.png so its real layout can be seen
        // before deciding on an orientation correction.
        public static void CaptureBoardAxes()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(HoverboardModelPath);
            if (model == null)
            {
                Debug.LogError($"No model at {HoverboardModelPath}");
                return;
            }

            Material material = GetHoverboardMaterial();
            Vector3[] eulers = { new Vector3(0f, 0f, 0f), new Vector3(0f, 90f, 0f), new Vector3(90f, 0f, 0f) };
            string[] names = { "BoardAxisFront", "BoardAxisSide", "BoardAxisTop" };
            for (int i = 0; i < eulers.Length; i++)
            {
                GameObject instance = Object.Instantiate(model);
                AssignMaterial(instance, material);
                RenderInstanceIcon(instance, names[i], eulers[i]);
            }
        }

        // Fast iteration view for board art: rebuilds only the board visual in
        // front of the camera (fresh from the FBX, no scene rebuild) from two
        // angles so orientation and scale can be judged against the road.
        [MenuItem("Joburg Runner/Capture Board Closeup")]
        public static void CaptureBoardCloseup()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("No main camera found for board closeup.");
                return;
            }

            GameObject holder = new GameObject("BoardCloseupHolder");
            GameObject side = CreateIonCruiserBoard(holder.transform, 0);
            GameObject front = CreateIonCruiserBoard(holder.transform, 1);
            side.SetActive(true);
            front.SetActive(true);

            Vector3 anchor = camera.transform.position + camera.transform.forward * 5f;
            side.transform.position = anchor + camera.transform.right * -1.1f;
            side.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // length across the frame
            front.transform.position = anchor + camera.transform.right * 1.1f;

            CapturePreviewScreenshot();
            Object.DestroyImmediate(holder);
        }

        // Diagnostic: samples the Drone Boost's Falling clip onto the active
        // runner, lifted off the road as the drone carries him, to eyeball the
        // retargeted flying pose without a device build.
        public static void CaptureDroneFallPreview()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Transform activeCharacter = null;
            Transform leanPivot = null;
            Transform flightPivot = null;
            foreach (Transform child in Object.FindAnyObjectByType<PlayerController>()
                .GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "RunnerVisual_Fbx") activeCharacter = child;
                else if (child.name == "ModelLeanPivot") leanPivot = child;
                else if (child.name == "DroneFlightPivot") flightPivot = child;
            }

            if (activeCharacter == null || leanPivot == null || flightPivot == null)
            {
                Debug.LogError("Runner rig not found for drone fall preview.");
                return;
            }

            Vector3 leanRest = leanPivot.localPosition;
            Quaternion flightRest = flightPivot.localRotation;
            AnimationClip fallClip = LoadRunnerFlyClip()
                ?? LoadRunnerClipFromModel(RunnerFallModelPath, loop: true);
            if (fallClip != null)
            {
                fallClip.SampleAnimation(activeCharacter.gameObject, fallClip.length * 0.5f);
            }

            // Mirror DroneFlightVisual: lift the runner into the air. The fly clip
            // already bakes a flat horizontal pose, so no extra prone pitch is
            // applied (proneDegrees is 0 for it).
            leanPivot.localPosition = leanRest + Vector3.up * 1.6f;
            flightPivot.localRotation = Quaternion.identity;

            CapturePreviewScreenshot();

            leanPivot.localPosition = leanRest;
            flightPivot.localRotation = flightRest;
        }

        // Diagnostic: samples the swipe-down forward-roll clip at five points
        // through the somersault and renders each from the SIDE, so the full
        // 0→360° rotation (not just a tuck) can be confirmed. Writes
        // Builds/preview_roll_0..4.png. Scene not saved.
        [MenuItem("Joburg Runner/Capture Roll Forward Strip")]
        public static void CaptureRollForwardStrip()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Transform activeCharacter = null;
            foreach (Transform child in Object.FindAnyObjectByType<PlayerController>()
                .GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "RunnerVisual_Fbx") { activeCharacter = child; break; }
            }

            if (activeCharacter == null)
            {
                Debug.LogError("RunnerVisual_Fbx not found for roll strip.");
                return;
            }

            AnimationClip rollClip = LoadRunnerBakedRootClip(RunnerRollForwardModelPath, loop: false, bakeForwardTravel: false);
            if (rollClip == null)
            {
                Debug.LogError("Roll clip unavailable for roll strip.");
                return;
            }

            Camera camera = Camera.main;
            Vector3 camPos = camera.transform.position;
            Quaternion camRot = camera.transform.rotation;
            Vector3 focus = activeCharacter.position + Vector3.up * 0.9f;
            camera.transform.position = focus + new Vector3(5.5f, 0.3f, 0.4f);
            camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position);

            float[] times = { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f };
            for (int i = 0; i < times.Length; i++)
            {
                rollClip.SampleAnimation(activeCharacter.gameObject, rollClip.length * times[i]);
                CapturePreviewScreenshot();
                File.Copy(PreviewPath, $"Builds/preview_roll_{i}.png", true);
            }

            camera.transform.position = camPos;
            camera.transform.rotation = camRot;
            Debug.Log($"Captured roll strip for '{rollClip.name}' ({rollClip.length:0.00}s).");
        }

        // Diagnostic: samples the drone fly clip onto the runner and renders him
        // from the SIDE (the in-game camera only ever sees his back), so the
        // horizontal glide orientation can be compared to the authoring
        // fly_side.png. Writes Builds/preview_flyside.png. Scene not saved.
        [MenuItem("Joburg Runner/Capture Drone Fly Profile")]
        public static void CaptureDroneFlyProfile()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Transform activeCharacter = null;
            foreach (Transform child in Object.FindAnyObjectByType<PlayerController>()
                .GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "RunnerVisual_Fbx") { activeCharacter = child; break; }
            }

            if (activeCharacter == null)
            {
                Debug.LogError("RunnerVisual_Fbx not found for fly profile.");
                return;
            }

            AnimationClip flyClip = LoadRunnerFlyClip();
            if (flyClip == null)
            {
                Debug.LogError("Fly clip unavailable for fly profile.");
                return;
            }
            flyClip.SampleAnimation(activeCharacter.gameObject, flyClip.length * 0.5f);

            Camera camera = Camera.main;
            Vector3 camPos = camera.transform.position;
            Quaternion camRot = camera.transform.rotation;

            // Frame the character from his right side at torso height.
            Vector3 focus = activeCharacter.position + Vector3.up * 1.1f;
            camera.transform.position = focus + new Vector3(5.5f, 0.4f, 0.4f);
            camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position);

            CapturePreviewScreenshot();
            File.Copy(PreviewPath, "Builds/preview_flyside.png", true);

            camera.transform.position = camPos;
            camera.transform.rotation = camRot;
        }

        // Shows the runner riding the selected board: enables Board_0 and
        // stages the runtime ride result manually — the character standing on
        // the board, both lifted off the road — since edit mode runs no
        // Updates (so HoverboardVisual/RunnerVisualGrounder never tick).
        [MenuItem("Joburg Runner/Capture Hoverboard Ride Preview")]
        public static void CaptureHoverboardRidePreview()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            Transform leanPivot = null;
            Transform board = null;
            Transform activeCharacter = null;
            if (player != null)
            {
                foreach (Transform child in player.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "ModelLeanPivot")
                    {
                        leanPivot = child;
                    }
                    else if (child.name == "Board_0")
                    {
                        board = child;
                    }
                    else if (child.name == "RunnerVisual_Fbx")
                    {
                        activeCharacter = child;
                    }
                }
            }

            if (leanPivot == null || board == null || activeCharacter == null)
            {
                Debug.LogError("Hoverboard rig not found for ride preview.");
                return;
            }

            Vector3 leanRest = leanPivot.localPosition;
            Vector3 boardRest = board.localPosition;

            board.gameObject.SetActive(true);

            // Pose the character on the same frozen Idle stance HoverboardVisual
            // holds at runtime, so the preview shows the true standing pose (edit
            // mode never ticks the animator). SampleAnimation writes the clip's
            // pose straight onto the transforms.
            AnimationClip standClip = LoadRunnerClipFromModel(RunnerIdleModelPath, loop: false);
            if (standClip != null)
            {
                standClip.SampleAnimation(activeCharacter.gameObject, 0f);
            }

            // Lift the whole rig off the road first (runtime does this via the
            // grounder + mount), then seat the board so its deck top meets the
            // lifted feet — measuring after the lift keeps the board planted
            // under the stance regardless of the pose's foot spread.
            const float rideHeight = 0.5f;
            leanPivot.localPosition = leanRest + Vector3.up * rideHeight;

            const float footPlant = 0.16f;
            float feetY = CombinedRendererBounds(activeCharacter.gameObject).min.y;
            Bounds boardBounds = CombinedRendererBounds(board.gameObject);
            board.position += new Vector3(0f, feetY - boardBounds.max.y + footPlant, 0f);

            CapturePreviewScreenshot();

            board.localPosition = boardRest;
            board.gameObject.SetActive(false);
            leanPivot.localPosition = leanRest;
        }

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
                UbuntuPulsePrefabPath,
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
            CaptureMePreview();
            File.Copy(PreviewPath, "Builds/preview_me.png", true);
            CaptureBoardsPreview();
            File.Copy(PreviewPath, "Builds/preview_boards.png", true);
            CaptureHoverboardRidePreview();
            File.Copy(PreviewPath, "Builds/preview_hoverboard.png", true);
            CapturePickupsCloseup();
            File.Copy(PreviewPath, "Builds/preview_pickups.png", true);
        }

        [MenuItem("Joburg Runner/Build Android APK")]
        public static void BuildAndroidApk()
        {
            BuildAndroidApk(BuildOptions.None, ApkPath);
        }

        /// <summary>
        /// Development build: enables DebugOverlay's cheat buttons (power-ups,
        /// coin grants, time scale), which are compiled out of release. Use it
        /// to drive a specific effect on device instead of playing for it.
        /// </summary>
        [MenuItem("Joburg Runner/Build Android APK (Development)")]
        public static void BuildAndroidApkDevelopment()
        {
            BuildAndroidApk(BuildOptions.Development, ApkPath);
        }

        [MenuItem("Joburg Runner/Build Intersection Roads Test APK")]
        public static void BuildIntersectionRoadsTestApk()
        {
            BuildAndroidApk(
                BuildOptions.Development,
                "Builds/JoziRunner_IntersectionRoads_Test.apk");
        }

        static void BuildAndroidApk(BuildOptions options, string outputPath)
        {
            BuildMinimumPlayableScene();
            JunctionSegmentValidator.ValidateOrThrow();
            Directory.CreateDirectory("Builds");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Release-size profile for mobile. The project is content-heavy, so keep
            // only code and mesh channels reachable from the production scene, ask
            // IL2CPP to optimise for size, minify Java bytecode/resources, and store
            // textures in a GPU-native ASTC format rather than expanding them on disk.
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.stripUnusedMeshComponents = true;
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Android, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(
                NamedBuildTarget.Android, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.Android.minifyRelease = true;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = options
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

        // Every folder whose textures actually ship in the build. The player
        // character and environment props (2048² albedo/normal maps) live outside
        // Assets/Textures, so compressing only that folder left the biggest VRAM
        // and APK costs untouched.
        static readonly string[] ShippedTextureFolders =
        {
            "Assets/Textures",
            "Assets/Characters",
            "Assets/Environment",
            "Assets/Materials",
        };

        static void CompressTextures()
        {
            var folders = new List<string>();
            foreach (string folder in ShippedTextureFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    folders.Add(folder);
                }
            }

            if (folders.Count == 0)
            {
                return;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", folders.ToArray()))
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

                // Force ASTC on Android so a texture can't fall back to a bulky
                // uncompressed format at build time.
                TextureImporterPlatformSettings android = textureImporter.GetPlatformTextureSettings("Android");
                if (!android.overridden || android.format != TextureImporterFormat.ASTC_6x6 || android.maxTextureSize > 1024)
                {
                    android.overridden = true;
                    android.format = TextureImporterFormat.ASTC_6x6;
                    android.maxTextureSize = 1024;
                    textureImporter.SetPlatformTextureSettings(android);
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
            // Heavy decimation smears textures unless edges along UV seams and
            // mesh borders are kept intact.
            UnityMeshSimplifier.SimplificationOptions options = UnityMeshSimplifier.SimplificationOptions.Default;
            options.PreserveBorderEdges = true;
            options.PreserveUVSeamEdges = true;
            simplifier.SimplificationOptions = options;
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
            Pal("RoadAsphalt", new Color(0.13f, 0.15f, 0.20f));
            Pal("Asphalt", new Color(0.10f, 0.12f, 0.16f));
            Pal("AsphaltCrack", new Color(0.1f, 0.1f, 0.105f));
            Pal("PaintYellow", new Color(1f, 0.78f, 0.06f));
            Pal("PaintWhite", new Color(1f, 0.99f, 0.92f));
            Pal("RoadMarkingWhite", new Color(1f, 0.99f, 0.94f));
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
            bool cartoon = !emissive && CartoonStyleIsEnabled();
            Shader desiredShader = cartoon
                ? Shader.Find("Jozi Runner/Mobile Toon")
                : Shader.Find("Universal Render Pipeline/Lit");
            if (desiredShader == null)
            {
                desiredShader = Shader.Find("Standard");
            }

            if (material == null)
            {
                material = new Material(desiredShader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = name;
            material.shader = desiredShader;
            color = cartoon ? StylizeColor(color) : color;
            material.color = color;
            material.enableInstancing = true;
            if (cartoon)
            {
                material.SetColor("_ShadowColor", new Color(0.42f, 0.48f, 0.64f, 1f));
                material.SetColor("_RimColor", new Color(0.82f, 0.9f, 1f, 1f));
                material.SetFloat("_RimStrength", 0.075f);
            }
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.4f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        static CartoonVisualSettings CreateCartoonVisualSettings()
        {
            const string path = "Assets/Environment/CartoonVisualSettings.asset";
            CartoonVisualSettings settings =
                AssetDatabase.LoadAssetAtPath<CartoonVisualSettings>(path);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<CartoonVisualSettings>();
                AssetDatabase.CreateAsset(settings, path);
            }
            settings.enabled = false;
            settings.saturationBoost = 0f;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        static bool CartoonStyleIsEnabled()
        {
            if (CartoonSettings == null)
            {
                CartoonSettings = AssetDatabase.LoadAssetAtPath<CartoonVisualSettings>(
                    "Assets/Environment/CartoonVisualSettings.asset");
            }
            return CartoonSettings == null || CartoonSettings.enabled;
        }

        static Color StylizeColor(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            float boost = CartoonSettings != null ? CartoonSettings.saturationBoost : 0.18f;
            s = Mathf.Clamp01(s + boost * (1f - s));
            v = Mathf.Clamp01(Mathf.Lerp(v, v > 0.55f ? 0.9f : 0.62f, 0.08f));
            Color result = Color.HSVToRGB(h, s, v);
            result.a = color.a;
            return result;
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

        // Imported FBX hero-building prefabs, spawned at runtime by the socket
        // decoration system (see HeroBuildingSet + EnvironmentDecorDirector); no
        // longer baked into the road prefab.
        const string JhbBuilding02PrefabPath = "Assets/Environment/Buildings/JHB_Building02/Prefabs/PF_JHB_Building02.prefab";
        const string JhbSkylinePop02PrefabPath = "Assets/Environment/Buildings/JHB_SkylinePop_02/Prefabs/PF_JHB_SkylinePop_02.prefab";
        const string JhbHeroBuilding03PrefabPath = "Assets/Environment/Buildings/ImportedHeroes/JHB_HeroBuilding03/Prefabs/JHB_HeroBuilding03.prefab";
        const string JhbHeroBuilding04PrefabPath = "Assets/Environment/Buildings/ImportedHeroes/JHB_HeroBuilding04/Prefabs/JHB_HeroBuilding04.prefab";
        const string JhbHeroBuilding05PrefabPath = "Assets/Environment/Buildings/ImportedHeroes/JHB_HeroBuilding05/Prefabs/JHB_HeroBuilding05.prefab";
        const string JhbHeroBuilding06PrefabPath = "Assets/Environment/Buildings/ImportedHeroes/JHB_HeroBuilding06/Prefabs/JHB_HeroBuilding06.prefab";
        const string JhbHeroBuilding07PrefabPath = "Assets/Environment/Buildings/ImportedHeroes/JHB_HeroBuilding07/Prefabs/JHB_HeroBuilding07.prefab";
        const string JhbJoziFurniturePrefabPath = "Assets/Environment/Buildings/ImportedHeroes/JHB_JoziFurniture/Prefabs/JHB_JoziFurniture.prefab";

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
            Transform legacyDecor = Category("LegacyDecor", t);
            Transform decorSockets = Category("DecorSockets", t);
            Transform pedestrianCrossing = Category("PedestrianCrossing", t);
            Transform junctions = Category("JunctionVisuals", t);

            BuildRoadSurface(road);
            BuildSidewalks(sidewalks);
            BuildJunctionVisuals(root, junctions);
            // The active traffic-light pair resolves to the NW/NE sockets at z=7.
            // Centre the zebra stripes on that same line so both poles frame the
            // middle of the crossing rather than sitting beyond its far edge.
            PedestrianCrossingTreatment(pedestrianCrossing, 7f);
            pedestrianCrossing.gameObject.SetActive(false);
            // LEGACY decoration (fixed trees + traffic lights on every tile). Kept
            // under its own toggleable root so the runtime feature flag can hide it
            // and use the new socket system instead. See SegmentDecorator.
            BuildRoadsideModels(legacyDecor, legacyDecor);
            // NEW: hand-placed, gameplay-safe sockets the runtime decorator fills
            // from the active district profile.
            BuildDecorSockets(decorSockets);

            // Runtime decorator wiring: hide/show legacy vs socket decoration.
            var decorator = root.AddComponent<JoburgRunner.Environment.Decor.SegmentDecorator>();
            SetField(decorator, "legacyDecorRoot", legacyDecor.gameObject);
            SetField(decorator, "pedestrianCrossingRoot", pedestrianCrossing.gameObject);

            GameObject[] districts =
            {
                District("CBDOfficeStreet", buildings),
                District("RetailCafeStreet", buildings),
                District("BrickMuralStreet", buildings),
                District("ApartmentMixedUseStreet", buildings),
                District("MandelaBridge", buildings)
            };

            BuildSuburbanStreet(districts[0].transform);
            BuildVendorMarketStreet(districts[1].transform);
            BuildTaxiStreet(districts[2].transform);
            BuildMixedCommercialStreet(districts[3].transform);
            BuildMandelaBridge(districts[4].transform);

            foreach (GameObject district in districts)
            {
                AddDistrictLod(district);
            }

            // Link CBD hero sockets to their side's placeholder group (both now exist).
            WireHeroPlaceholderSockets(root.transform, districts[0].transform);

            StripColliders(buildings.gameObject);
            StripColliders(trees.gameObject);
            StripColliders(props.gameObject);
            StripColliders(signs.gameObject);
            StripColliders(junctions.gameObject);

            SetField(visuals, "districtRoots", districts);
            visuals.SetDistrict(0);

            return SavePrefab(root, RoadPrefabPath);
        }

        static void BuildMandelaBridge(Transform root)
        {
            Material steel = Mat("MetalDark");
            Material concrete = Mat("Concrete");
            Material cable = Mat("RoadMarkingWhite");
            Material accent = Mat("PaintYellow");

            // Deep bridge edges hide the normal pavement and sell the elevated
            // deck, while remaining purely visual so obstacle gameplay is unchanged.
            Cube("BridgeDeckLeft", root, new Vector3(-7.4f, 0.15f, 15f), new Vector3(4.1f, 0.55f, 30f), concrete);
            Cube("BridgeDeckRight", root, new Vector3(7.4f, 0.15f, 15f), new Vector3(4.1f, 0.55f, 30f), concrete);
            Cube("LeftSafetyBarrier", root, new Vector3(-5.65f, 0.75f, 15f), new Vector3(0.28f, 1.3f, 30f), steel);
            Cube("RightSafetyBarrier", root, new Vector3(5.65f, 0.75f, 15f), new Vector3(0.28f, 1.3f, 30f), steel);

            foreach (float z in new[] { 5f, 25f })
            {
                foreach (float x in new[] { -8.4f, 8.4f })
                {
                    Cube("BridgePylon", root, new Vector3(x, 7.2f, z), new Vector3(1.15f, 14.4f, 1.3f), concrete);
                    Cube("PylonGoldBand", root, new Vector3(x, 10.2f, z), new Vector3(1.32f, 0.35f, 1.48f), accent);
                }

                Cube("BridgeCrossBeam", root, new Vector3(0f, 13.5f, z), new Vector3(17.4f, 0.8f, 1.15f), concrete);
            }

            BridgeCable(root, "LeftMainCable", -8.4f, cable);
            BridgeCable(root, "RightMainCable", 8.4f, cable);

            for (int z = 2; z <= 28; z += 3)
            {
                float archHeight = 3.2f + 8.5f * Mathf.Sin(Mathf.PI * z / 30f);
                Cube("LeftSuspender", root, new Vector3(-8.4f, archHeight * 0.5f, z),
                    new Vector3(0.07f, archHeight, 0.07f), cable);
                Cube("RightSuspender", root, new Vector3(8.4f, archHeight * 0.5f, z),
                    new Vector3(0.07f, archHeight, 0.07f), cable);
            }

            WorldText(root, "NELSON MANDELA BRIDGE", new Vector3(0f, 12.9f, 5.7f), 180f, 0.8f, new Color(1f, 0.78f, 0.2f));
        }

        static void BridgeCable(Transform parent, string name, float x, Material material)
        {
            GameObject cable = new GameObject(name);
            cable.transform.SetParent(parent, false);
            LineRenderer line = cable.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.widthMultiplier = 0.16f;
            line.positionCount = 17;
            for (int i = 0; i < line.positionCount; i++)
            {
                float z = i * (30f / (line.positionCount - 1));
                float y = 3.2f + 8.5f * Mathf.Sin(Mathf.PI * z / 30f);
                line.SetPosition(i, new Vector3(x, y, z));
            }
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
            Cube("ThreeLaneAsphalt", road, new Vector3(0f, -0.08f, 15f), new Vector3(RoadMetrics.RoadWidth, 0.16f, 30f), Mat("Asphalt"));
            Cube("LeftYellowShoulderLine", road, new Vector3(-4.0f, 0.055f, 15f), new Vector3(0.12f, 0.045f, 30f), Mat("PaintYellow"));
            Cube("RightYellowShoulderLine", road, new Vector3(4.0f, 0.055f, 15f), new Vector3(0.12f, 0.045f, 30f), Mat("PaintYellow"));

            for (int z = 2; z < 30; z += 6)
            {
                Cube("WhiteDashedLaneMark_Left", road, new Vector3(-RoadMetrics.LaneSpacing * 0.5f, 0.06f, z), new Vector3(0.12f, 0.045f, 2.45f), Mat("RoadMarkingWhite"));
                Cube("WhiteDashedLaneMark_Right", road, new Vector3(RoadMetrics.LaneSpacing * 0.5f, 0.06f, z + 3f), new Vector3(0.12f, 0.045f, 2.45f), Mat("RoadMarkingWhite"));
            }

            // Round drain covers, storm-drain grates, and asphalt crack/tar-repair
            // patches removed by request: keep the road surface clean so nothing
            // reads as a pothole or obstacle. Only lane markings remain.
        }

        static void BuildSidewalks(Transform sidewalks)
        {
            Cube("LeftConcreteSidewalk", sidewalks, new Vector3(-5.82f, 0f, 15f), new Vector3(2.95f, 0.22f, 30f), Mat("Pavement"));
            Cube("RightConcreteSidewalk", sidewalks, new Vector3(5.82f, 0f, 15f), new Vector3(2.95f, 0.22f, 30f), Mat("SidewalkConcrete"));
            Cube("LeftCurb", sidewalks, new Vector3(-4.38f, 0.12f, 15f), new Vector3(0.30f, 0.28f, 30f), Mat("KerbStone"));
            Cube("RightCurb", sidewalks, new Vector3(4.38f, 0.12f, 15f), new Vector3(0.30f, 0.28f, 30f), Mat("KerbStone"));
            Cube("LeftGrassStrip", sidewalks, new Vector3(-7.5f, 0.03f, 15f), new Vector3(0.45f, 0.08f, 30f), Mat("Grass"));
            Cube("RightGrassStrip", sidewalks, new Vector3(7.5f, 0.03f, 15f), new Vector3(0.45f, 0.08f, 30f), Mat("Grass"));

            for (int z = 2; z < 30; z += 4)
            {
                Cube("PavementExpansionJoint_Left", sidewalks, new Vector3(-5.82f, 0.13f, z), new Vector3(2.7f, 0.02f, 0.05f), Mat("Concrete"));
                Cube("PavementExpansionJoint_Right", sidewalks, new Vector3(5.82f, 0.13f, z + 1f), new Vector3(2.7f, 0.02f, 0.05f), Mat("Concrete"));
            }
        }

        static void BuildJunctionVisuals(GameObject segmentRoot, Transform parent)
        {
            GameObject crossroad = InstantiateJunctionVariant(
                "Assets/Prefabs/Road/Junctions/RoadSegment_CrossIntersection.prefab", parent);
            GameObject tLeft = InstantiateJunctionVariant(
                "Assets/Prefabs/Road/Junctions/RoadSegment_TJunction_Left.prefab", parent);
            GameObject tRight = InstantiateJunctionVariant(
                "Assets/Prefabs/Road/Junctions/RoadSegment_TJunction_Right.prefab", parent);

            JunctionVisuals junctionVisuals = segmentRoot.AddComponent<JunctionVisuals>();
            SetField(junctionVisuals, "crossroadRoot", crossroad);
            SetField(junctionVisuals, "tLeftRoot", tLeft);
            SetField(junctionVisuals, "tRightRoot", tRight);
            junctionVisuals.SetType(JunctionType.None);
        }

        static GameObject InstantiateJunctionVariant(string path, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new System.InvalidOperationException($"Missing junction variant prefab: {path}");
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        static void CreateJunctionVariantPrefabs()
        {
            EnsureAssetFolder("Assets/Prefabs", "Road");
            EnsureAssetFolder("Assets/Prefabs/Road", "Junctions");
            EnsureAssetFolder("Assets/Prefabs/Road", "Modules");
            CreateDirectionSignPrefab();
            CreateJunctionModulePrefabs();
            CreateJunctionVariantPrefab("RoadSegment_CrossIntersection", true, true);
            CreateJunctionVariantPrefab("RoadSegment_TJunction_Left", true, false);
            CreateJunctionVariantPrefab("RoadSegment_TJunction_Right", false, true);
        }

        static void CreateJunctionModulePrefabs()
        {
            CreateSideRoadModule("MOD_SideRoad_Left", true);
            CreateSideRoadModule("MOD_SideRoad_Right", false);
            CreateMarkingModule("MOD_CrossIntersectionMarkings", true, true);
            CreateMarkingModule("MOD_TJunctionLeftMarkings", true, false);
            CreateMarkingModule("MOD_TJunctionRightMarkings", false, true);
        }

        static void CreateSideRoadModule(string name, bool left)
        {
            GameObject root = new GameObject(name);
            Transform road = Category("SideRoad", root.transform);
            Transform corners = Category("CornerPavements", root.transform);
            BuildSideRoadArm(road, left);
            BuildJunctionCorners(corners, left);
            StripColliders(root);
            root.transform.localScale = Vector3.one;
            SavePrefab(root, $"Assets/Prefabs/Road/Modules/{name}.prefab");
        }

        static void CreateMarkingModule(string name, bool left, bool right)
        {
            GameObject root = new GameObject(name);
            BuildIntersectionMarkings(root.transform, left, right);
            StripColliders(root);
            root.transform.localScale = Vector3.one;
            SavePrefab(root, $"Assets/Prefabs/Road/Modules/{name}.prefab");
        }

        static JunctionSpawnSettings CreateJunctionSpawnSettings()
        {
            const string path = "Assets/Environment/JunctionSpawnSettings.asset";
            JunctionSpawnSettings settings =
                AssetDatabase.LoadAssetAtPath<JunctionSpawnSettings>(path);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<JunctionSpawnSettings>();
                AssetDatabase.CreateAsset(settings, path);
            }

            settings.straightWeight = 70f;
            settings.crossroadWeight = 20f;
            settings.tJunctionWeight = 10f;
            settings.minimumStraightSegments = 4;
            settings.openingSegmentCount = 9;
            settings.guaranteeFirstJunctionAfterOpening = true;
            settings.allowInJacarandaAvenue = false;
            settings.deterministicSeed = 2407;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        static void CreateJunctionVariantPrefab(string name, bool left, bool right)
        {
            GameObject root = new GameObject(name);
            Transform visualLayout = Category("VisualRoadLayout", root.transform);
            Transform decoration = Category("IntersectionDecoration", root.transform);

            if (left)
            {
                InstantiateModule("MOD_SideRoad_Left", visualLayout);
                BuildJunctionDecorationSockets(decoration, true);
            }
            if (right)
            {
                InstantiateModule("MOD_SideRoad_Right", visualLayout);
                BuildJunctionDecorationSockets(decoration, false);
            }
            InstantiateModule(
                left && right ? "MOD_CrossIntersectionMarkings" :
                left ? "MOD_TJunctionLeftMarkings" : "MOD_TJunctionRightMarkings",
                visualLayout);

            GameObject directionSign = AssetDatabase.LoadAssetAtPath<GameObject>(DirectionSignPrefabPath);
            if (directionSign != null)
            {
                GameObject sign = PrefabUtility.InstantiatePrefab(directionSign, decoration) as GameObject;
                sign.name = "ApproachDirectionSign";
                sign.transform.localPosition = new Vector3(-6.65f, 0f, 0.75f);
                sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                sign.transform.localScale = Vector3.one;
            }

            StripColliders(root);
            root.transform.localScale = Vector3.one;
            SavePrefab(root, $"Assets/Prefabs/Road/Junctions/{name}.prefab");
        }

        [MenuItem("Joburg Runner/Debug Direction Sign Textures")]
        public static void DebugDirectionSignTextures()
        {
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(DirectionSignAlbedoPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(DirectionSignNormalPath);
            Debug.Log($"SIGNTEX-DEBUG albedoPath='{DirectionSignAlbedoPath}' loaded={(albedo != null)} " +
                      $"normalPath loaded={(normal != null)}");
            AssetDatabase.DeleteAsset("Assets/Materials/DirectionSignGenerated_URP.mat");
            CreateDirectionSignPrefab();
            Material m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DirectionSignGenerated_URP.mat");
            Debug.Log($"SIGNTEX-DEBUG after rebuild baseMap={(m != null ? (m.GetTexture("_BaseMap") != null).ToString() : "no mat")}");
        }

        static GameObject CreateDirectionSignPrefab()
        {
            GameObject sign = BuildFbxDecorPrefab(
                DirectionSignModelPath, DirectionSignPrefabPath, 4.0f, "DirectionSign",
                resetImportedRotation: true);
            ApplyGeneratedDecorTextures(
                sign, "DirectionSignGenerated_URP",
                DirectionSignAlbedoPath, DirectionSignNormalPath);
            return sign;
        }

        static void InstantiateModule(string name, Transform parent)
        {
            string path = $"Assets/Prefabs/Road/Modules/{name}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new System.InvalidOperationException($"Missing road module: {path}");
            }
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
        }

        static void BuildJunctionDecorationSockets(Transform parent, bool left)
        {
            float sign = left ? -1f : 1f;
            JunctionSocket(parent, $"TrafficLight_{(left ? "L" : "R")}_Near",
                JunctionDecorSocketType.TrafficLight, new Vector3(sign * 5.25f, 0f, 2f));
            JunctionSocket(parent, $"TrafficLight_{(left ? "L" : "R")}_Far",
                JunctionDecorSocketType.TrafficLight, new Vector3(sign * 5.25f, 0f, 12f));
            JunctionSocket(parent, $"PedestrianLight_{(left ? "L" : "R")}",
                JunctionDecorSocketType.PedestrianTrafficLight, new Vector3(sign * 5.55f, 0f, 11.7f));
            JunctionSocket(parent, $"StreetLight_{(left ? "L" : "R")}",
                JunctionDecorSocketType.StreetLight, new Vector3(sign * 6.55f, 0f, 1.1f));
            JunctionSocket(parent, $"RoadSign_{(left ? "L" : "R")}",
                JunctionDecorSocketType.RoadSign, new Vector3(sign * 6.85f, 0f, 0.75f));
            JunctionSocket(parent, $"UtilityBox_{(left ? "L" : "R")}",
                JunctionDecorSocketType.UtilityBox, new Vector3(sign * 8.55f, 0f, 1.2f));
            // Pedestrian markers remain on the corner/side road, outside |x| < 4.8.
            JunctionSocket(parent, $"Pedestrian_{(left ? "L" : "R")}",
                JunctionDecorSocketType.Pedestrian, new Vector3(sign * 6.45f, 0f, 12.4f));
            JunctionSocket(parent, $"Vehicle_{(left ? "L" : "R")}",
                JunctionDecorSocketType.Vehicle, new Vector3(sign * 16f, 0f, 7f));
            JunctionSocket(parent, $"DeliveryVan_{(left ? "L" : "R")}",
                JunctionDecorSocketType.DeliveryVan, new Vector3(sign * 25f, 0f, 9.2f));
            JunctionSocket(parent, $"Vendor_{(left ? "L" : "R")}",
                JunctionDecorSocketType.Vendor, new Vector3(sign * 11f, 0f, 13f));
            JunctionSocket(parent, $"BusStop_{(left ? "L" : "R")}",
                JunctionDecorSocketType.BusStop, new Vector3(sign * 20f, 0f, 13f));
            JunctionSocket(parent, $"Bench_{(left ? "L" : "R")}",
                JunctionDecorSocketType.Bench, new Vector3(sign * 14f, 0f, 13f));
            JunctionSocket(parent, $"Bin_{(left ? "L" : "R")}",
                JunctionDecorSocketType.Bin, new Vector3(sign * 9f, 0f, 13f));
            JunctionSocket(parent, $"Bollard_{(left ? "L" : "R")}",
                JunctionDecorSocketType.Bollard, new Vector3(sign * 7.4f, 0f, 1.1f));
            JunctionSocket(parent, $"Planter_{(left ? "L" : "R")}",
                JunctionDecorSocketType.Planter, new Vector3(sign * 10.2f, 0f, 1.2f));
            JunctionSocket(parent, $"Jacaranda_{(left ? "L" : "R")}",
                JunctionDecorSocketType.JacarandaTree, new Vector3(sign * 17f, 0f, 1f));
            JunctionSocket(parent, $"Barrier_{(left ? "L" : "R")}",
                JunctionDecorSocketType.Barrier, new Vector3(sign * 34f, 0f, 7f));
        }

        static void JunctionSocket(
            Transform parent, string name, JunctionDecorSocketType type, Vector3 position)
        {
            GameObject socket = new GameObject(name);
            socket.transform.SetParent(parent, false);
            socket.transform.localPosition = position;
            JunctionDecorSocket marker = socket.AddComponent<JunctionDecorSocket>();
            SetField(marker, "socketType", type);
        }

        // A side-road arm begins at the edge of the unchanged three-lane runner
        // road and extends behind the building line. Everything is visual-only.
        static void BuildSideRoadArm(Transform parent, bool left)
        {
            float sign = left ? -1f : 1f;
            const float junctionZ = 7f;
            float armCentreX = sign * 19.625f;

            Cube("SideStreetAsphalt", parent,
                // Its surface must sit clear of the retained main-road sidewalk
                // (top Y=0.11). Equal surfaces overlap at the junction mouth and
                // z-fight on mobile, which reads as the left/right road blinking.
                new Vector3(armCentreX, 0.06f, junctionZ),
                new Vector3(30.75f, 0.14f, 8.8f), Mat("Asphalt"));

            // Pavements and kerbs follow the two sides of the joining street.
            foreach (float edgeSign in new[] { -1f, 1f })
            {
                float edgeZ = junctionZ + edgeSign * 5.25f;
                Cube("SideStreetPavement", parent,
                    new Vector3(armCentreX, 0.08f, edgeZ),
                    new Vector3(30.75f, 0.18f, 1.7f), Mat("Pavement"));
                Cube("SideStreetKerb", parent,
                    new Vector3(armCentreX, 0.13f, junctionZ + edgeSign * 4.45f),
                    new Vector3(30.75f, 0.24f, 0.24f), Mat("KerbStone"));
            }

            // Road-edge lines and a broken centre line make the side arm read as
            // a real street rather than a flat asphalt patch.
            Cube("SideStreetEdgeLineNear", parent,
                new Vector3(armCentreX, 0.15f, junctionZ - 3.95f),
                new Vector3(30.75f, 0.025f, 0.11f), Mat("PaintYellow"));
            Cube("SideStreetEdgeLineFar", parent,
                new Vector3(armCentreX, 0.15f, junctionZ + 3.95f),
                new Vector3(30.75f, 0.025f, 0.11f), Mat("PaintYellow"));

            for (int i = 0; i < 4; i++)
            {
                float distance = 8f + i * 5f;
                float x = sign * distance;
                Cube("SideStreetCentreDash", parent,
                    new Vector3(x, 0.15f, junctionZ),
                    new Vector3(2.4f, 0.025f, 0.12f), Mat("RoadMarkingWhite"));
            }

            // Stop line before entering the runner's street. It is deliberately
            // outside the gameplay road and carries no collider.
            Cube("SideStreetStopLine", parent,
                new Vector3(sign * 5.6f, 0.155f, junctionZ),
                new Vector3(0.16f, 0.025f, 7.1f), Mat("RoadMarkingWhite"));

            // Low termination geometry prevents the decorative road reading as
            // infinite; furniture sockets can later add parked vehicles/barriers.
            Cube("SideStreetEndBarrier", parent,
                new Vector3(sign * 34.2f, 0.55f, junctionZ),
                new Vector3(0.35f, 1.1f, 8.4f), Mat("Concrete"));
        }

        static void BuildJunctionCorners(Transform parent, bool left)
        {
            float side = left ? -1f : 1f;
            foreach (float longitudinal in new[] { -1f, 1f })
            {
                float cornerZ = 7f + longitudinal * 5.35f;
                float cornerX = side * 5.9f;

                Cube("CornerPavement", parent,
                    new Vector3(cornerX, 0.12f, cornerZ),
                    new Vector3(2.8f, 0.22f, 2.1f), Mat("Pavement"));

                // Five short tangent kerb pieces approximate a clean 1.35 m
                // radius without a bespoke high-poly mesh or negative scaling.
                for (int i = 0; i < 5; i++)
                {
                    float angle = 9f + i * 18f;
                    float radians = angle * Mathf.Deg2Rad;
                    float x = side * (4.45f + Mathf.Cos(radians) * 1.35f);
                    float z = 7f + longitudinal * (4.0f + Mathf.Sin(radians) * 1.35f);
                    Cube("CurvedKerb", parent,
                        new Vector3(x, 0.16f, z),
                        new Vector3(0.72f, 0.28f, 0.24f), Mat("KerbStone"),
                        new Vector3(0f, side * longitudinal * angle, 0f));
                }

                // Flush ramp plus tiny covers provide intersection detail while
                // remaining outside the active road and carrying no colliders.
                Cube("PavementRamp", parent,
                    new Vector3(side * 5.1f, 0.075f, 7f + longitudinal * 4.55f),
                    new Vector3(1.25f, 0.10f, 1.15f), Mat("Concrete"));
                Cylinder("DrainCover", parent,
                    new Vector3(side * 4.8f, 0.135f, 7f + longitudinal * 3.55f),
                    new Vector3(0.34f, 0.025f, 0.34f), Mat("MetalDark"));
                Cube("UtilityCover", parent,
                    new Vector3(side * 6.5f, 0.245f, cornerZ),
                    new Vector3(0.65f, 0.025f, 0.9f), Mat("MetalDark"));
            }
        }

        static void BuildIntersectionMarkings(Transform parent, bool left, bool right)
        {
            Material white = Mat("RoadMarkingWhite");

            // Two crossings on the forward road: one before and one after the junction.
            foreach (float z in new[] { 1.7f, 12.3f })
            {
                for (int stripe = -3; stripe <= 3; stripe++)
                {
                    Cube("MainRoadZebra", parent,
                        new Vector3(stripe * 1.15f, 0.14f, z),
                        new Vector3(0.62f, 0.025f, 1.65f), white);
                }
                Cube("MainRoadStopLine", parent,
                    new Vector3(0f, 0.145f, z + (z < 7f ? -1.2f : 1.2f)),
                    new Vector3(7.7f, 0.025f, 0.16f), white);
            }

            if (left) BuildSideRoadZebraAndArrow(parent, -1f, white);
            if (right) BuildSideRoadZebraAndArrow(parent, 1f, white);
        }

        static void BuildSideRoadZebraAndArrow(Transform parent, float side, Material white)
        {
            float crossingX = side * 6.7f;
            for (int stripe = -3; stripe <= 3; stripe++)
            {
                Cube("SideRoadZebra", parent,
                    new Vector3(crossingX, 0.15f, 7f + stripe * 1.0f),
                    new Vector3(1.55f, 0.025f, 0.5f), white);
            }

            // Simple straight-ahead arrow deeper on the decorative arm.
            Cube("DirectionArrowStem", parent,
                new Vector3(side * 17f, 0.15f, 7f),
                new Vector3(2.4f, 0.025f, 0.18f), white);
            Cube("DirectionArrowHeadA", parent,
                new Vector3(side * 18.15f, 0.15f, 6.55f),
                new Vector3(1.1f, 0.025f, 0.16f), white,
                new Vector3(0f, side * 35f, 0f));
            Cube("DirectionArrowHeadB", parent,
                new Vector3(side * 18.15f, 0.15f, 7.45f),
                new Vector3(1.1f, 0.025f, 0.16f), white,
                new Vector3(0f, side * -35f, 0f));
        }

        // Roadside props built from the supplied FBX models: jacaranda trees on
        // the pavements and traffic lights at the kerb. Baked into the pooled
        // RoadSegment prefab so they repeat consistently down the street.
        static void BuildRoadsideModels(Transform trees, Transform signs)
        {
            foreach (float z in new[] { 6f, 16f, 26f })
            {
                RoadsideModel(JacarandaTreeModelPath, trees, new Vector3(-8.75f, 0f, z), 40f, 6.5f, "JacarandaTree");
            }
            foreach (float z in new[] { 11f, 21f, 29f })
            {
                RoadsideModel(JacarandaTreeModelPath, trees, new Vector3(8.75f, 0f, z), 210f, 6.5f, "JacarandaTree");
            }

            // Kerb-side traffic lights, arm facing across the road toward traffic.
            RoadsideModel(TrafficLightModelPath, signs, new Vector3(-5.95f, 0f, 4f), 90f, 4.8f, "TrafficLight");
            RoadsideModel(TrafficLightModelPath, signs, new Vector3(5.95f, 0f, 27f), -90f, 4.8f, "TrafficLight");
        }

        static void RoadsideModel(string modelPath, Transform parent, Vector3 position, float yRotation, float targetHeight, string keyPrefix)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogWarning($"Roadside model not found at {modelPath}; skipping.");
                return;
            }

            EnsureModelMaterialsImported(modelPath);

            GameObject root = new GameObject(keyPrefix);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

            GameObject visual = Object.Instantiate(model, root.transform);
            visual.name = keyPrefix + "_FBX";
            visual.transform.localPosition = Vector3.zero;
            // Keep the FBX's authored import rotation/scale so the model stays
            // upright as designed. Resetting rotation to identity drops Blender's
            // Y-up conversion, and re-deriving orientation from world bounds fails
            // once the parent's facing rotation swaps the X/Z axes — that tipped
            // the traffic light onto its side and blew its scale up ~9x.

            NormalizeStaticModelBounds(visual, root.transform, targetHeight);
            ConvertImportedMaterialsToUrp(visual, keyPrefix);
            StripColliders(root);
        }

        // FBX materials import as Standard (magenta under URP). Rebuild each one
        // as a URP/Lit material that keeps the model's authored diffuse colour,
        // so the tree's foliage/trunk and the light's lenses stay distinct.
        static void ConvertImportedMaterialsToUrp(GameObject visual, string keyPrefix)
        {
            var cache = new Dictionary<Material, Material>();
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] source = renderer.sharedMaterials;
                Material[] converted = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    Material s = source[i];
                    if (s == null)
                    {
                        continue;
                    }

                    if (!cache.TryGetValue(s, out Material urp))
                    {
                        Color color = s.HasProperty("_BaseColor") ? s.GetColor("_BaseColor")
                            : s.HasProperty("_Color") ? s.GetColor("_Color")
                            : s.color;
                        string raw = s.name.Replace(" ", "").Replace(".", "").Replace("/", "").Replace(":", "");
                        urp = CreateMaterial($"{keyPrefix}_{cache.Count}_{raw}", color);
                        cache[s] = urp;
                    }

                    converted[i] = urp;
                }

                renderer.sharedMaterials = converted;
            }
        }

        static void EnsureModelMaterialsImported(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
            {
                return;
            }

            if (importer.materialImportMode == ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.SaveAndReimport();
            }
        }

        // ==================================================================
        // Socket-based district decoration (Phase 1)
        // ==================================================================
        //
        // Sockets are hand-placed anchors baked onto the shared road tile. All
        // sit on the pavement (|x| >= 5.95 > the 4.6 m lane keep-out) so nothing
        // the decorator spawns can intrude on the three gameplay lanes. The
        // runtime SegmentDecorator fills them from the active DistrictDecorProfile.

        static void BuildDecorSockets(Transform p)
        {
            // Trees – both pavements, staggered so probability + spacing never
            // yields a perfect row.
            DecorSocketPoint(p, "TreeSocket_L1", DecorSocketType.Tree, new Vector3(-6.55f, 0f, 5f), 0f, 1.7f);
            DecorSocketPoint(p, "TreeSocket_L2", DecorSocketType.Tree, new Vector3(-6.55f, 0f, 16f), 0f, 1.7f);
            DecorSocketPoint(p, "TreeSocket_L3", DecorSocketType.Tree, new Vector3(-6.55f, 0f, 27f), 0f, 1.7f);
            DecorSocketPoint(p, "TreeSocket_R1", DecorSocketType.Tree, new Vector3(6.55f, 0f, 10f), 0f, 1.7f);
            DecorSocketPoint(p, "TreeSocket_R2", DecorSocketType.Tree, new Vector3(6.55f, 0f, 21f), 0f, 1.7f);

            // Lamps – kerb line, alternating left/right for a steady rhythm.
            DecorSocketPoint(p, "LampSocket_L1", DecorSocketType.Lamp, new Vector3(-4.8f, 0f, 8f), 90f, 0.8f);
            DecorSocketPoint(p, "LampSocket_R1", DecorSocketType.Lamp, new Vector3(4.8f, 0f, 22f), -90f, 0.8f);

            // Stand-alone bench (parks only, via profile).
            DecorSocketPoint(p, "BenchSocket_L1", DecorSocketType.Bench, new Vector3(-5.65f, 0.12f, 19f), 90f, 1.2f);

            // Street furniture spread — electric box (utility box), bus stand,
            // dustbin and potplant are kept a full 10 m (SegmentLength / 3) apart
            // so no two of them ever spawn next to each other. Each pavement holds
            // three of the four types at z = 9 / 19 / 29; that 10 m pitch also holds
            // across the tile seam (a prop at z = 29 sits 10 m from the next tile's
            // z = 9 prop), so the "never adjacent" guarantee survives tile chaining.
            // The small per-instance z-jitter (<=0.8 m) can never close a 10 m gap.
            // z >= 9 also keeps them clear of the leading-edge traffic-light and
            // crossing sockets. Bin and utility box are capped at one per tile, so
            // each gets a single socket (box left, bin right) not a mirrored pair.
            DecorSocketPoint(p, "UtilityBoxSocket_L1", DecorSocketType.UtilityBox, new Vector3(-5.45f, 0f, 9f), 90f, 1.0f);
            // Market-stall anchors on both pavements. Their different socket yaws
            // mirror the shared prefab so each storefront angles toward both the
            // road and the approaching player.
            DecorSocketPoint(p, "BusStopSocket_L1", DecorSocketType.BusStop, new Vector3(-5.55f, 0.1f, 19f), 90f, 2.5f);
            DecorSocketPoint(p, "BusStopSocket_R1", DecorSocketType.BusStop, new Vector3(5.55f, 0.1f, 9f), 180f, 2.5f);
            DecorSocketPoint(p, "BinSocket_R1", DecorSocketType.Bin, new Vector3(5.25f, 0.15f, 19f), -90f, 0.6f);
            DecorSocketPoint(p, "PlanterSocket_L1", DecorSocketType.Planter, new Vector3(-5.4f, 0f, 29f), 90f, 1.25f);
            DecorSocketPoint(p, "PlanterSocket_R1", DecorSocketType.Planter, new Vector3(5.4f, 0f, 29f), -90f, 1.25f);
            // Billboard/bench sockets are currently rule-less (never spawned); kept
            // for future districts, parked clear of the furniture rhythm.
            DecorSocketPoint(p, "BillboardSocket_R1", DecorSocketType.Billboard, new Vector3(5.65f, 0f, 14f), -110f, 1.7f);

            // Traffic-light anchors on both sides of an intersection. Profiles cap this
            // type at two, and deterministic name ordering resolves NE then NW, yielding
            // exactly one road-facing robot per side.
            DecorSocketPoint(p, "TrafficLightSocket_SW", DecorSocketType.TrafficLight, new Vector3(-4.8f, 0f, 3f), 90f, 1.0f);
            DecorSocketPoint(p, "TrafficLightSocket_SE", DecorSocketType.TrafficLight, new Vector3(4.8f, 0f, 3f), -90f, 1.0f);
            DecorSocketPoint(p, "TrafficLightSocket_NW", DecorSocketType.TrafficLight, new Vector3(-4.8f, 0f, 7f), 90f, 1.0f);
            DecorSocketPoint(p, "TrafficLightSocket_NE", DecorSocketType.TrafficLight, new Vector3(4.8f, 0f, 7f), -90f, 1.0f);
            DecorSocketPoint(p, "PedestrianSocket_Crossing", DecorSocketType.Pedestrian,
                new Vector3(-4.2f, 0.06f, 7f), 90f, 0.2f);
            DecorSocketPoint(p, "WavingPedestrianSocket_R", DecorSocketType.WavingPedestrian,
                new Vector3(6.45f, 0.1f, 7f), 180f, 0.6f);
            // Walking pedestrians on both pavements, yawed 180 so they face and
            // travel world -Z — opposite the player. Spawned near the tile's far
            // end so they have a full stretch to stroll toward the approaching
            // runner and past. The PavementWalker mover supplies the translation.
            DecorSocketPoint(p, "WalkingPedestrianSocket_L", DecorSocketType.WalkingPedestrian,
                new Vector3(-6.2f, 0f, 25f), 180f, 0.3f);
            DecorSocketPoint(p, "WalkingPedestrianSocket_R", DecorSocketType.WalkingPedestrian,
                new Vector3(6.2f, 0f, 22f), 180f, 0.3f);

            // Hero-building anchors at the CBD building line (just beyond the ~±9.15 m
            // pavement edge), centred along the tile and facing the road (left forward
            // = +X, right forward = -X). Only CBD tiles the director flags eligible fill
            // these; per-building offset/scale is authored in the HeroBuildingSet.
            // Keep façades dense but give the closest geometry a little breathing room.
            // Socket yaw alone faces each façade squarely toward the road.
            DecorSocketPoint(p, "HeroBuildingSocket_L", DecorSocketType.HeroBuilding, new Vector3(-11.45f, 0f, 15f), 90f, 9f);
            DecorSocketPoint(p, "HeroBuildingSocket_R", DecorSocketType.HeroBuilding, new Vector3(11.45f, 0f, 15f), -90f, 9f);
        }

        // ---- Hero building catalogue (data-driven, scales to 20+) -----------

        const string HeroBuildingSetPath = "Assets/Environment/Decor/HeroBuildingSet.asset";

        static HeroBuildingSet CreateHeroBuildingSet()
        {
            EnsureAssetFolder("Assets/Environment", "Decor");

            HeroBuildingSet set = AssetDatabase.LoadAssetAtPath<HeroBuildingSet>(HeroBuildingSetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<HeroBuildingSet>();
                AssetDatabase.CreateAsset(set, HeroBuildingSetPath);
            }

            GameObject a = AssetDatabase.LoadAssetAtPath<GameObject>(JhbBuilding02PrefabPath);
            GameObject b = AssetDatabase.LoadAssetAtPath<GameObject>(JhbSkylinePop02PrefabPath);
            GameObject c = AssetDatabase.LoadAssetAtPath<GameObject>(JhbHeroBuilding03PrefabPath);
            GameObject d = AssetDatabase.LoadAssetAtPath<GameObject>(JhbHeroBuilding04PrefabPath);
            GameObject e = AssetDatabase.LoadAssetAtPath<GameObject>(JhbHeroBuilding05PrefabPath);
            GameObject f = AssetDatabase.LoadAssetAtPath<GameObject>(JhbHeroBuilding06PrefabPath);
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(JhbHeroBuilding07PrefabPath);
            GameObject h = AssetDatabase.LoadAssetAtPath<GameObject>(JhbJoziFurniturePrefabPath);
            if (a == null || b == null || c == null || d == null || e == null || f == null || g == null || h == null)
            {
                Debug.LogWarning("Hero building prefab(s) missing; HeroBuildingSet left with null entries.");
            }

            // Footprint radii from the imported bounds (half of the larger planar axis):
            // JHB_Building02 18.17 x 14.55 -> ~9.1; JHB_SkylinePop_02 22.4 x 15.45 -> ~11.2.
            set.entries = new[]
            {
                new HeroEntry
                {
                    label = "JHB_Building02", prefab = a, weight = 1f, allowedSides = HeroSide.Both,
                    localPositionOffset = new Vector3(-1.5f, 0f, -2f), eulerOffset = Vector3.zero,
                    facePlayerDegrees = 0f, uniformScale = 0.92f,
                    colorTint = new Color(1f, 0.96f, 0.9f, 1f),
                    footprintRadius = 10.5f, minRepeatDistance = 0f, enabled = true
                },
                new HeroEntry
                {
                    label = "JHB_SkylinePop_02", prefab = b, weight = 1f, allowedSides = HeroSide.Both,
                    localPositionOffset = new Vector3(1.5f, 0f, -3f), eulerOffset = Vector3.zero,
                    facePlayerDegrees = 0f, uniformScale = 1.02f,
                    colorTint = new Color(0.9f, 0.96f, 1f, 1f),
                    footprintRadius = 12.5f, minRepeatDistance = 0f, enabled = true
                },
                new HeroEntry
                {
                    label = "JHB_HeroBuilding03", prefab = c, weight = 1f, allowedSides = HeroSide.Both,
                    localPositionOffset = new Vector3(-2f, 0f, -1f), eulerOffset = Vector3.zero,
                    facePlayerDegrees = 0f, uniformScale = 0.95f,
                    colorTint = new Color(1f, 0.93f, 0.82f, 1f),
                    footprintRadius = 5.6f, minRepeatDistance = 0f, enabled = true
                },
                new HeroEntry
                {
                    label = "JHB_HeroBuilding04", prefab = d, weight = 1f, allowedSides = HeroSide.Both,
                    localPositionOffset = new Vector3(2.5f, 0f, -1.5f), eulerOffset = Vector3.zero,
                    facePlayerDegrees = 0f, uniformScale = 0.9f,
                    colorTint = new Color(0.78f, 0.9f, 1f, 1f),
                    footprintRadius = 5.6f, minRepeatDistance = 0f, enabled = true
                },
                new HeroEntry
                {
                    label = "JHB_HeroBuilding05", prefab = e, weight = 1f, allowedSides = HeroSide.Both,
                    localPositionOffset = new Vector3(-1f, 0f, -1f), eulerOffset = Vector3.zero,
                    facePlayerDegrees = 0f, uniformScale = 1.05f,
                    colorTint = new Color(1f, 0.88f, 0.78f, 1f),
                    footprintRadius = 5.6f, minRepeatDistance = 0f, enabled = true
                },
                new HeroEntry
                {
                    label = "JHB_HeroBuilding06", prefab = f, weight = 1f, allowedSides = HeroSide.Both,
                    localPositionOffset = new Vector3(1f, 0f, -1f), eulerOffset = Vector3.zero,
                    facePlayerDegrees = 0f, uniformScale = 1.1f,
                    colorTint = new Color(0.9f, 1f, 0.9f, 1f),
                    footprintRadius = 4.4f, minRepeatDistance = 0f, enabled = true
                },
                new HeroEntry
                {
                    label = "JHB_HeroBuilding07", prefab = g, weight = 1f, allowedSides = HeroSide.Both,
                    localPositionOffset = new Vector3(-2.5f, 0f, -2f), eulerOffset = Vector3.zero,
                    facePlayerDegrees = 0f, uniformScale = 0.98f,
                    colorTint = Color.white,
                    footprintRadius = 6.9f, minRepeatDistance = 0f, enabled = true
                },
                new HeroEntry
                {
                    label = "JoziFurniture", prefab = h, weight = 1.75f, allowedSides = HeroSide.Both,
                    localPositionOffset = Vector3.zero, eulerOffset = Vector3.zero,
                    facePlayerDegrees = 0f, uniformScale = 1f,
                    colorTint = Color.white,
                    footprintRadius = 11f, minRepeatDistance = 0f, enabled = true
                },
            };

            EditorUtility.SetDirty(set);
            return set;
        }

        static void DecorSocketPoint(Transform parent, string name, DecorSocketType type, Vector3 localPos, float yaw, float clearance)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            DecorSocket socket = go.AddComponent<DecorSocket>();
            SetField(socket, "socketType", type);
            SetField(socket, "clearanceRadius", clearance);
        }

        // ---- Poolable decoration prefabs -----------------------------------

        struct DecorPrefabSet
        {
            public GameObject[] Trees;
            public GameObject[] TrafficLights;
            public GameObject Lamp;
            public GameObject Bench;
            public GameObject Bin;
            public GameObject Billboard;
            public GameObject BusStopCluster;
            public GameObject Planter;
            public GameObject UtilityBox;
        }

        static DecorPrefabSet CreateDecorPrefabs()
        {
            EnsureAssetFolder("Assets/Prefabs", "Decor");
            const string dir = "Assets/Prefabs/Decor";
            GameObject approvedTrafficLight =
                AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedTrafficLightPrefabPath);
            var trafficLights = new List<GameObject>();
            if (approvedTrafficLight != null)
            {
                trafficLights.Add(BuildAnimatedTrafficLightPrefab(
                    approvedTrafficLight, $"{dir}/DecorAnimatedTrafficLight.prefab"));
            }
            else
            {
                Debug.LogWarning($"Approved roadside traffic light not found at {ApprovedTrafficLightPrefabPath}.");
                trafficLights.Add(BuildFbxDecorPrefab(
                    TrafficLightModelPath, $"{dir}/DecorTrafficLight_A.prefab", 4.7f, "TrafficLight"));
                trafficLights.Add(BuildFbxDecorPrefab(
                    TrafficLightModelPath, $"{dir}/DecorTrafficLight_B.prefab", 5.1f, "TrafficLight"));
            }

            GameObject dustbin = BuildFbxDecorPrefab(
                DustbinModelPath, $"{dir}/DecorBin.prefab", 1.1f, "Dustbin");
            Transform dustbinVisual = dustbin != null
                ? dustbin.transform.Find("Dustbin_FBX")
                : null;
            if (dustbinVisual != null)
            {
                // Flip the bin end-for-end so its concrete/base end becomes the
                // pavement contact, then ground and preserve its approved height.
                dustbinVisual.localPosition = Vector3.zero;
                dustbinVisual.localRotation = Quaternion.Euler(0f, 0f, 180f);
                NormalizeStaticModelBounds(dustbinVisual.gameObject, dustbin.transform, 1.1f);
            }
            ApplyGeneratedDecorTextures(
                dustbin, "DustbinGenerated_URP", DustbinAlbedoPath, DustbinNormalPath);

            GameObject busStopStand = BuildFbxDecorPrefab(
                BusStopStandModelPath, $"{dir}/DecorBusStopCluster.prefab", 3.0f, "BusStopStand");
            ApplyGeneratedDecorTextures(
                busStopStand, "BusStopStandGenerated_URP",
                BusStopStandAlbedoPath, BusStopStandNormalPath);

            GameObject planter = BuildFbxDecorPrefab(
                ConcretePotplantModelPath, $"{dir}/DecorConcretePotplant.prefab", 1.2f, "ConcretePotplant",
                roadsideRotationOffset: new Vector3(0f, 270f, 90f));
            Transform planterVisual = planter != null
                ? planter.transform.Find("ConcretePotplant_FBX")
                : null;
            if (planterVisual != null)
            {
                // Blender's intended +Y foliage axis arrives on Unity +Z in this
                // particular Meshy export. Rotate the visual so leaves grow up,
                // then remeasure, centre, and ground it at the approved 1.2 m.
                planterVisual.localPosition = Vector3.zero;
                planterVisual.localRotation = Quaternion.Euler(90f, 0f, 0f);
                NormalizeStaticModelBounds(planterVisual.gameObject, planter.transform, 1.2f);
            }
            ApplyGeneratedDecorTextures(
                planter, "ConcretePotplantGenerated_URP",
                ConcretePotplantAlbedoPath, ConcretePotplantNormalPath);

            GameObject utilityBox = BuildFbxDecorPrefab(
                ElectricBoxModelPath, $"{dir}/DecorElectricBox.prefab", 1.5f, "ElectricBox",
                roadsideRotationOffset: Vector3.zero);
            Transform utilityVisual = utilityBox != null
                ? utilityBox.transform.Find("ElectricBox_FBX")
                : null;
            if (utilityVisual != null)
            {
                // The FBX uses Y as its vertical axis but is authored top-down:
                // its concrete plinth is above the cabinet. Flip top/bottom around
                // Z (preserving the model's front/back axis), then centre and
                // ground the complete plinth surface instead of only an edge.
                utilityVisual.localPosition = Vector3.zero;
                utilityVisual.localRotation = Quaternion.Euler(0f, 0f, 180f);
                utilityVisual.localScale = Vector3.one;
                NormalizeStaticModelBounds(utilityVisual.gameObject, utilityBox.transform, 1.5f);
            }
            ApplyGeneratedDecorTextures(
                utilityBox, "ElectricBoxGenerated_URP",
                ElectricBoxAlbedoPath, ElectricBoxNormalPath);

            var set = new DecorPrefabSet
            {
                // Several tree / light variants (shared materials via a common key
                // prefix) so random selection reads as hand-placed, not cloned.
                Trees = new[]
                {
                    BuildFbxDecorPrefab(JacarandaTreeModelPath, $"{dir}/DecorTree_JacarandaA.prefab", 6.0f, "JacarandaTree"),
                    BuildFbxDecorPrefab(JacarandaTreeModelPath, $"{dir}/DecorTree_JacarandaB.prefab", 6.9f, "JacarandaTree"),
                },
                TrafficLights = trafficLights.ToArray(),
                Lamp = BuildGeneratedDecorPrefab($"{dir}/DecorCurvedStreetLight.prefab",
                    t => CurvedStreetLight(t, Vector3.zero)),
                Bench = BuildGeneratedDecorPrefab($"{dir}/DecorBench.prefab", t => Bench(t, Vector3.zero, 0f)),
                Bin = dustbin,
                Billboard = BuildGeneratedDecorPrefab($"{dir}/DecorBillboard.prefab", t => RoadsideBillboard(t, Vector3.zero, 0f, "HF_Street")),
                BusStopCluster = busStopStand,
                Planter = planter,
                UtilityBox = utilityBox,
            };

            return set;
        }

        static void ApplyGeneratedDecorTextures(
            GameObject prefab, string materialName, string albedoPath, string normalPath)
        {
            if (prefab == null)
            {
                return;
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            Texture2D normal = LoadAsNormalMap(normalPath);
            Material material = CreateMaterial(materialName, Color.white);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", albedo);
            material.mainTexture = albedo;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.28f);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }
                renderer.sharedMaterials = materials;
            }

            PrefabUtility.SavePrefabAsset(prefab);
        }

        static GameObject BuildAnimatedTrafficLightPrefab(GameObject approved, string prefabPath)
        {
            GameObject root = Object.Instantiate(approved);
            root.name = Path.GetFileNameWithoutExtension(prefabPath);

            Transform overlay = Category("AnimatedSignalLenses", root.transform);
            Material red = UnlitColorMaterial("TrafficSignalRedGlow", new Color(1f, 0.04f, 0.02f));
            Material amber = UnlitColorMaterial("TrafficSignalAmberGlow", new Color(1f, 0.55f, 0.02f));
            Material green = UnlitColorMaterial("TrafficSignalGreenGlow", new Color(0.05f, 1f, 0.16f));
            Material mask = UnlitColorMaterial("TrafficSignalLensOff", new Color(0.015f, 0.018f, 0.016f));

            float[] lensY = { 3.16f, 2.87f, 2.58f };
            for (int i = 0; i < lensY.Length; i++)
            {
                Sphere($"LensMaskFront_{i}", overlay, new Vector3(0f, lensY[i], 0.455f),
                    new Vector3(0.205f, 0.205f, 0.035f), mask);
                Sphere($"LensMaskBack_{i}", overlay, new Vector3(0f, lensY[i], -0.455f),
                    new Vector3(0.205f, 0.205f, 0.035f), mask);
                Sphere($"LensMaskLeft_{i}", overlay, new Vector3(-0.455f, lensY[i], 0f),
                    new Vector3(0.035f, 0.205f, 0.205f), mask);
                Sphere($"LensMaskRight_{i}", overlay, new Vector3(0.455f, lensY[i], 0f),
                    new Vector3(0.035f, 0.205f, 0.205f), mask);
            }

            GameObject redLens = BuildSignalLensPair(overlay, "AnimatedRedLens", lensY[0], red);
            GameObject amberLens = BuildSignalLensPair(overlay, "AnimatedAmberLens", lensY[1], amber);
            GameObject greenLens = BuildSignalLensPair(overlay, "AnimatedGreenLens", lensY[2], green);

            var cycle = root.AddComponent<TrafficLightCycle>();
            SetField(cycle, "redLens", redLens);
            SetField(cycle, "amberLens", amberLens);
            SetField(cycle, "greenLens", greenLens);
            SetField(cycle, "redSeconds", 4f);
            SetField(cycle, "amberSeconds", 1f);
            SetField(cycle, "greenSeconds", 4f);

            StripColliders(overlay.gameObject);
            return SavePrefab(root, prefabPath);
        }

        static GameObject BuildSignalLensPair(Transform parent, string name, float y, Material material)
        {
            Transform group = Category(name, parent);
            Sphere($"{name}_Front", group, new Vector3(0f, y, 0.495f),
                new Vector3(0.175f, 0.175f, 0.035f), material);
            Sphere($"{name}_Back", group, new Vector3(0f, y, -0.495f),
                new Vector3(0.175f, 0.175f, 0.035f), material);
            Sphere($"{name}_Left", group, new Vector3(-0.495f, y, 0f),
                new Vector3(0.035f, 0.175f, 0.175f), material);
            Sphere($"{name}_Right", group, new Vector3(0.495f, y, 0f),
                new Vector3(0.035f, 0.175f, 0.175f), material);
            return group.gameObject;
        }

        static GameObject BuildFbxDecorPrefab(
            string modelPath, string prefabPath, float targetHeight, string keyPrefix,
            bool resetImportedRotation = false,
            Vector3? roadsideRotationOffset = null)
        {
            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model != null)
            {
                EnsureModelMaterialsImported(modelPath);
                GameObject visual = Object.Instantiate(model, root.transform);
                visual.name = keyPrefix + "_FBX";
                visual.transform.localPosition = Vector3.zero;
                if (resetImportedRotation)
                {
                    // Meshy FBX imports can retain a -90 degree axis-conversion
                    // transform even when their validated mesh bounds are Y-up.
                    // Clear it before measuring so depth is not mistaken for height.
                    visual.transform.localRotation = Quaternion.identity;
                    visual.transform.localScale = Vector3.one;
                }
                // Keep the FBX's authored import orientation (see RoadsideModel note).
                NormalizeStaticModelBounds(visual, root.transform, targetHeight);
                ConvertImportedMaterialsToUrp(visual, keyPrefix);
            }
            else
            {
                Debug.LogWarning($"Decor model missing at {modelPath}; prefab {prefabPath} will be empty.");
            }

            EnableInstancing(root);
            StripColliders(root);
            // The cabinet, for example, uses (0,180,0) because its access doors
            // are authored opposite the standard +Z roadside-facing convention.
            ConfigureRoadsideOrientation(root, roadsideRotationOffset ?? Vector3.zero);
            return SavePrefab(root, prefabPath);
        }

        static GameObject BuildGeneratedDecorPrefab(string prefabPath, System.Action<Transform> build)
        {
            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
            build(root.transform);
            EnableInstancing(root);
            StripColliders(root);
            ConfigureRoadsideOrientation(root, Vector3.zero);
            return SavePrefab(root, prefabPath);
        }

        /// <summary>
        /// Stores a prefab-authored facing correction. Runtime placement always
        /// composes this after the socket's left/right road-facing rotation and
        /// yaw variation. Future props only need this component configured on
        /// their prefab; SegmentDecorator does not need asset-specific branches.
        /// </summary>
        static void ConfigureRoadsideOrientation(GameObject prefabRoot, Vector3 rotationOffset)
        {
            if (prefabRoot == null)
            {
                return;
            }

            RoadsidePropOrientation orientation =
                prefabRoot.GetComponent<RoadsidePropOrientation>();
            if (orientation == null)
            {
                orientation = prefabRoot.AddComponent<RoadsidePropOrientation>();
            }
            orientation.SetRotationOffset(rotationOffset);
        }

        static void EnableInstancing(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        material.enableInstancing = true;
                    }
                }
            }
        }

        // ---- District profiles (data-driven, tunable in the Inspector) ------

        static DistrictDecorProfile[] CreateDistrictDecorProfiles(
            DecorPrefabSet d, GameObject fruitsStall, GameObject gentleman,
            GameObject wavingGentleman, GameObject walkingWoman, GameObject walkingMan)
        {
            EnsureAssetFolder("Assets/Environment", "Decor");

            // [0] Johannesburg CBD – dense, few trees, more traffic lights.
            var cbd = SaveDecorProfile("District_CBD", "Johannesburg CBD", new List<DecorRule>
            {
                MakeRule(DecorSocketType.Tree, 0.15f, 1, 15f, d.Trees, new Vector2(0.75f, 0.9f), new Vector2(0f, 360f), new Vector2(0.4f, 1.5f), 0f, 1.6f, 0.12f, false),
                MakeRule(DecorSocketType.Lamp, 0.3f, 1, 20f, new[] { d.Lamp }, Vector2.one, Vector2.zero, new Vector2(0f, 0.5f), 0f, 0.8f, 0f, false),
                MakeRule(DecorSocketType.TrafficLight, 1f, 2, 3f, d.TrafficLights, new Vector2(0.95f, 1.05f), new Vector2(0f, 0f), new Vector2(0f, 0f), 0f, 1.0f, 0f, true),
                MakeRule(DecorSocketType.Pedestrian, 1f, 1, 30f, new[] { gentleman }, Vector2.one, Vector2.zero, Vector2.zero, 0f, 0.1f, 0f, true, true),
                MakeRule(DecorSocketType.WavingPedestrian, 1f, 1, 30f, new[] { wavingGentleman }, Vector2.one, Vector2.zero, Vector2.zero, 0f, 0.5f, 0.18f, true, false, true),
                MakeRule(DecorSocketType.WalkingPedestrian, 0.7f, 2, 30f, new[] { walkingWoman, walkingMan }, new Vector2(0.95f, 1.05f), Vector2.zero, Vector2.zero, 0f, 0.3f, 0f, false),
                MakeRule(DecorSocketType.Bin, 0.55f, 1, 15f, new[] { d.Bin }, Vector2.one, new Vector2(0f, 360f), new Vector2(0f, 0.4f), 0f, 0.55f, 0f, false),
                MakeRule(DecorSocketType.Planter, 0.5f, 2, 9f, new[] { d.Planter }, new Vector2(0.9f, 1.05f), Vector2.zero, new Vector2(0f, 0.6f), 0f, 1.1f, 0f, false),
                MakeRule(DecorSocketType.UtilityBox, 0.4f, 1, 12f, new[] { d.UtilityBox }, new Vector2(0.95f, 1.05f), new Vector2(-5f, 5f), new Vector2(0f, 0.5f), 0f, 0.9f, 0f, false),
                MakeRule(DecorSocketType.BusStop, 0.6f, 2, 30f, new[] { fruitsStall, d.BusStopCluster }, Vector2.one, new Vector2(225f, 225f), new Vector2(0f, 0.8f), 0.5f, 1.8f, 0f, false, minimumDecorationSequence: 7),
            });

            // [1] Commissioner Street – medium density, many jacarandas, no lights.
            var commissioner = SaveDecorProfile("District_Commissioner", "Commissioner Street", new List<DecorRule>
            {
                MakeRule(DecorSocketType.Tree, 0.45f, 2, 12f, d.Trees, new Vector2(0.75f, 0.95f), new Vector2(0f, 360f), new Vector2(0.5f, 2f), 0.2f, 1.6f, 0.15f, false),
                MakeRule(DecorSocketType.Lamp, 0.35f, 1, 20f, new[] { d.Lamp }, Vector2.one, Vector2.zero, new Vector2(0f, 0.5f), 0f, 0.8f, 0f, false),
                MakeRule(DecorSocketType.WalkingPedestrian, 0.6f, 2, 30f, new[] { walkingWoman, walkingMan }, new Vector2(0.95f, 1.05f), Vector2.zero, Vector2.zero, 0f, 0.3f, 0f, false),
                MakeRule(DecorSocketType.Bin, 0.45f, 1, 15f, new[] { d.Bin }, Vector2.one, new Vector2(0f, 360f), new Vector2(0f, 0.4f), 0f, 0.55f, 0f, false),
                MakeRule(DecorSocketType.Planter, 0.6f, 2, 9f, new[] { d.Planter }, new Vector2(0.9f, 1.08f), Vector2.zero, new Vector2(0f, 0.7f), 0f, 1.1f, 0f, false),
                MakeRule(DecorSocketType.UtilityBox, 0.35f, 1, 12f, new[] { d.UtilityBox }, new Vector2(0.95f, 1.05f), new Vector2(-5f, 5f), new Vector2(0f, 0.5f), 0f, 0.9f, 0f, false),
                MakeRule(DecorSocketType.BusStop, 0.6f, 2, 30f, new[] { fruitsStall, d.BusStopCluster }, Vector2.one, new Vector2(225f, 225f), new Vector2(0f, 0.8f), 0.5f, 1.8f, 0f, false, minimumDecorationSequence: 7),
            });

            // [2] Park District – lush, benches, very few lights.
            var park = SaveDecorProfile("District_Park", "Park District", new List<DecorRule>
            {
                MakeRule(DecorSocketType.Tree, 0.65f, 3, 10f, d.Trees, new Vector2(0.8f, 1f), new Vector2(0f, 360f), new Vector2(0.6f, 2f), 0.3f, 1.6f, 0.15f, false),
                MakeRule(DecorSocketType.Lamp, 0.2f, 1, 20f, new[] { d.Lamp }, Vector2.one, Vector2.zero, new Vector2(0f, 0.5f), 0f, 0.8f, 0f, false),
                MakeRule(DecorSocketType.WalkingPedestrian, 0.4f, 2, 30f, new[] { walkingWoman, walkingMan }, new Vector2(0.95f, 1.05f), Vector2.zero, Vector2.zero, 0f, 0.3f, 0f, false),
                MakeRule(DecorSocketType.Bin, 0.4f, 1, 15f, new[] { d.Bin }, Vector2.one, new Vector2(0f, 360f), new Vector2(0f, 0.5f), 0f, 0.6f, 0f, false),
                MakeRule(DecorSocketType.Planter, 0.75f, 2, 9f, new[] { d.Planter }, new Vector2(0.9f, 1.1f), Vector2.zero, new Vector2(0f, 0.8f), 0f, 1.1f, 0f, false),
                MakeRule(DecorSocketType.UtilityBox, 0.2f, 1, 12f, new[] { d.UtilityBox }, new Vector2(0.95f, 1.05f), new Vector2(-5f, 5f), new Vector2(0f, 0.5f), 0f, 0.9f, 0f, false),
            });

            // [3] Business District – tall glass, modern lamps, more lights, few trees.
            var business = SaveDecorProfile("District_Business", "Business District", new List<DecorRule>
            {
                MakeRule(DecorSocketType.Tree, 0.1f, 1, 15f, d.Trees, new Vector2(0.75f, 0.9f), new Vector2(0f, 360f), new Vector2(0.4f, 1.2f), 0f, 1.6f, 0.1f, false),
                MakeRule(DecorSocketType.Lamp, 0.5f, 1, 20f, new[] { d.Lamp }, Vector2.one, Vector2.zero, new Vector2(0f, 0.5f), 0f, 0.8f, 0f, false),
                MakeRule(DecorSocketType.TrafficLight, 1f, 2, 3f, d.TrafficLights, new Vector2(0.95f, 1.05f), new Vector2(0f, 0f), new Vector2(0f, 0f), 0f, 1.0f, 0f, true),
                MakeRule(DecorSocketType.Pedestrian, 1f, 1, 30f, new[] { gentleman }, Vector2.one, Vector2.zero, Vector2.zero, 0f, 0.1f, 0f, true, true),
                MakeRule(DecorSocketType.WavingPedestrian, 1f, 1, 30f, new[] { wavingGentleman }, Vector2.one, Vector2.zero, Vector2.zero, 0f, 0.5f, 0.18f, true, false, true),
                MakeRule(DecorSocketType.WalkingPedestrian, 0.65f, 2, 30f, new[] { walkingWoman, walkingMan }, new Vector2(0.95f, 1.05f), Vector2.zero, Vector2.zero, 0f, 0.3f, 0f, false),
                MakeRule(DecorSocketType.Bin, 0.6f, 1, 15f, new[] { d.Bin }, Vector2.one, new Vector2(0f, 360f), new Vector2(0f, 0.4f), 0f, 0.55f, 0f, false),
                MakeRule(DecorSocketType.Planter, 0.45f, 2, 9f, new[] { d.Planter }, new Vector2(0.9f, 1.05f), Vector2.zero, new Vector2(0f, 0.6f), 0f, 1.1f, 0f, false),
                MakeRule(DecorSocketType.UtilityBox, 0.65f, 1, 12f, new[] { d.UtilityBox }, new Vector2(0.95f, 1.05f), new Vector2(-5f, 5f), new Vector2(0f, 0.5f), 0f, 0.9f, 0f, false),
                MakeRule(DecorSocketType.BusStop, 0.6f, 2, 30f, new[] { fruitsStall, d.BusStopCluster }, Vector2.one, new Vector2(225f, 225f), new Vector2(0f, 0.8f), 0.5f, 1.8f, 0f, false, minimumDecorationSequence: 7),
            });

            // [4] Mandela Bridge – elevated deck, no roadside decoration.
            var bridge = SaveDecorProfile("District_Bridge", "Mandela Bridge", new List<DecorRule>());

            return new[] { cbd, commissioner, park, business, bridge };
        }

        static DecorRule MakeRule(DecorSocketType type, float prob, int maxCount, float minSpacing, GameObject[] prefabs,
            Vector2 scaleRange, Vector2 yawJitter, Vector2 posJitter, float sidewalkOffset,
            float collisionRadius, float tint, bool requiresIntersection,
            bool requiresPedestrianCrossing = false,
            bool requiresNoPedestrianCrossing = false,
            int minimumDecorationSequence = 0)
        {
            return new DecorRule
            {
                socketType = type,
                spawnProbability = prob,
                maxCountPerSegment = maxCount,
                minSpacing = minSpacing,
                maxSpacing = Mathf.Max(minSpacing, 30f),
                allowedPrefabs = prefabs,
                allowConsecutiveRepetition = false,
                scaleRange = scaleRange,
                yawJitterRange = yawJitter,
                positionJitter = posJitter,
                sidewalkOffset = sidewalkOffset,
                tintVariation = tint,
                collisionRadius = collisionRadius,
                minimumDecorationSequence = minimumDecorationSequence,
                requiresIntersection = requiresIntersection,
                requiresPedestrianCrossing = requiresPedestrianCrossing,
                requiresNoPedestrianCrossing = requiresNoPedestrianCrossing,
            };
        }

        static DistrictDecorProfile SaveDecorProfile(string fileName, string districtName, List<DecorRule> rules)
        {
            string path = $"Assets/Environment/Decor/{fileName}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<DistrictDecorProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.districtName = districtName;
            profile.rules = rules.ToArray();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        // Living-street pigeons: builds the pooled pigeon + flock prefabs and drops
        // a single PigeonSpawner into the scene, wired to the player. The spawner is
        // the only pigeon component with an Update; it owns the pool and flocks.
        static void CreatePigeonSystem(Transform player)
        {
            // The stylized animated pigeon is the production bird. Build it here so
            // regenerating the scene can never overwrite it with the legacy HD model.
            PigeonStylizedBuilder.BuildAll();
            GameObject pigeonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PigeonStylizedBuilder.PigeonPrefabPath);
            GameObject flockPrefab = PigeonBuilder.BuildFlockPrefab();
            if (pigeonPrefab == null || flockPrefab == null)
            {
                Debug.LogWarning("Pigeon prefabs missing; skipping PigeonSpawner.");
                return;
            }

            var settings = PigeonBuilder.BuildFlockSettings();

            GameObject go = new GameObject("PigeonSpawner");
            var spawner = go.AddComponent<JoburgRunner.Environment.Pigeons.PigeonSpawner>();
            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("pigeonPrefab").objectReferenceValue = pigeonPrefab;
            so.FindProperty("flockPrefab").objectReferenceValue = flockPrefab;
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("settings").objectReferenceValue = settings;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateDecorDirector(DistrictDecorProfile[] profiles, HeroBuildingSet heroSet)
        {
            GameObject go = new GameObject("EnvironmentDecorDirector");
            var director = go.AddComponent<EnvironmentDecorDirector>();
            SetField(director, "useSocketDecoration", true);
            SetField(director, "profilesByDistrict", profiles);
            SetField(director, "jacarandaAvenueProfile",
                AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>(
                    "Assets/Environment/Decor/District_JacarandaAvenue.asset"));
            SetField(director, "intersectionEvery", 3);
            SetField(director, "prewarmPerPrefab", 16);
            // Hero-building spawning: frequent, both sides, no-repeat.
            SetField(director, "heroSet", heroSet);
            SetField(director, "heroAllDistricts", true);
            SetField(director, "heroPrewarm", 4);       // avoid large hero pool growth during play
            SetField(director, "heroMinGap", 1);        // a hero block on every segment
            SetField(director, "heroMaxGap", 1);
            SetField(director, "heroEarlyGuaranteeWithin", 1);
            SetField(director, "heroAvoidRecent", 1);
            SetField(director, "heroBothSides", true);  // a (different) building on each side
        }

        static void EnsureAssetFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                AssetDatabase.CreateFolder(parent, child);
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

        static void CurvedStreetLight(Transform parent, Vector3 position)
        {
            Material pole = Mat("PoleGrey");
            Cylinder("CurvedStreetLight_Pole", parent, position + new Vector3(0f, 2.05f, 0f),
                new Vector3(0.09f, 2.05f, 0.09f), pole);

            // Three low-poly arm sections form a smooth-looking forward bend.
            // The lamp extends along local +Z; the ±90° socket yaw turns +Z
            // toward the road from both pavements.
            Cylinder("CurvedStreetLight_ArmLower", parent, position + new Vector3(0f, 4.32f, 0.16f),
                new Vector3(0.085f, 0.46f, 0.085f), pole, new Vector3(20f, 0f, 0f));
            Cylinder("CurvedStreetLight_ArmMiddle", parent, position + new Vector3(0f, 4.75f, 0.52f),
                new Vector3(0.08f, 0.42f, 0.08f), pole, new Vector3(48f, 0f, 0f));
            Cylinder("CurvedStreetLight_ArmUpper", parent, position + new Vector3(0f, 5.02f, 1.02f),
                new Vector3(0.075f, 0.40f, 0.075f), pole, new Vector3(72f, 0f, 0f));

            Cube("CurvedStreetLight_LampHousing", parent, position + new Vector3(0f, 5.13f, 1.46f),
                new Vector3(0.42f, 0.16f, 0.58f), Mat("MetalDark"));
            Cube("CurvedStreetLight_LampGlow", parent, position + new Vector3(0f, 5.04f, 1.5f),
                new Vector3(0.30f, 0.035f, 0.40f), Mat("PaintWhite"));
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
            // Permanent CBD buildings (may remain static-batched). Empty today — none of
            // the current buildings sit clear of the hero footprints — but the root marks
            // where future never-hidden buildings go.
            Category("PermanentBuildings", parent);

            // Hero placeholders, split by side and marked HeroPlaceholderGroup so they are
            // excluded from static batching and can be hidden/restored individually when a
            // hero building occupies that side's socket.
            Transform phL = Category("HeroPlaceholders_L", parent);
            Transform phR = Category("HeroPlaceholders_R", parent);
            phL.gameObject.AddComponent<HeroPlaceholderGroup>();
            phR.gameObject.AddComponent<HeroPlaceholderGroup>();

            Building(phL, new Vector3(-12.4f, 0f, 5f), new Vector3(5.5f, 12f, 6f), Mat("Concrete"), BuildingStyle.Office, "CITY OFFICES");
            Building(phL, new Vector3(-12.2f, 0f, 14f), new Vector3(5f, 8.5f, 5f), Mat("Brick"), BuildingStyle.BrickApartment, "LOFTS");
            Building(phL, new Vector3(-12.5f, 0f, 24f), new Vector3(6f, 16f, 6f), Mat("Glass"), BuildingStyle.GlassTower, "BANK");
            Building(phR, new Vector3(12.3f, 0f, 4f), new Vector3(5f, 9f, 5f), Mat("BrickWallTan"), BuildingStyle.Office, "WORKHUB");
            Building(phR, new Vector3(12.5f, 0f, 13f), new Vector3(5.8f, 14f, 6f), Mat("GlassDark"), BuildingStyle.GlassTower, "TOWER");
            Building(phR, new Vector3(12.1f, 0f, 24f), new Vector3(5f, 11f, 5f), Mat("SkyscraperConcrete"), BuildingStyle.Office, "CITY");
            // Roadside signs and billboards removed by request.
        }

        // Links each hero socket to the placeholder group on its street side so the
        // decorator can hide/restore the buildings a hero there overlaps.
        static void WireHeroPlaceholderSockets(Transform segmentRoot, Transform cbdDistrict)
        {
            HeroPlaceholderGroup gL = cbdDistrict.Find("HeroPlaceholders_L")?.GetComponent<HeroPlaceholderGroup>();
            HeroPlaceholderGroup gR = cbdDistrict.Find("HeroPlaceholders_R")?.GetComponent<HeroPlaceholderGroup>();
            LinkHeroSocket(segmentRoot, "DecorSockets/HeroBuildingSocket_L", gL);
            LinkHeroSocket(segmentRoot, "DecorSockets/HeroBuildingSocket_R", gR);
        }

        static void LinkHeroSocket(Transform segmentRoot, string path, HeroPlaceholderGroup group)
        {
            Transform socket = segmentRoot.Find(path);
            if (socket == null)
            {
                Debug.LogWarning($"Hero socket not found for wiring: {path}");
                return;
            }
            HeroBuildingSocket hbs = socket.gameObject.AddComponent<HeroBuildingSocket>();
            SetField(hbs, "placeholderGroup", group);
        }

        static void BuildVendorMarketStreet(Transform parent)
        {
            Shopfront(parent, new Vector3(-11.7f, 0f, 4f), 90f, "CAFE", Mat("ShopAwningGreen"));
            Shopfront(parent, new Vector3(-11.7f, 0f, 11f), 90f, "BARBER", Mat("ShopAwningBlue"));
            Shopfront(parent, new Vector3(-11.7f, 0f, 18f), 90f, "CORNER STORE", Mat("ShopAwningRed"));
            Shopfront(parent, new Vector3(11.7f, 0f, 6f), -90f, "RESTAURANT", Mat("ShopAwningRed"));
            Shopfront(parent, new Vector3(11.7f, 0f, 15f), -90f, "CAFE", Mat("ShopAwningGreen"));
            Shopfront(parent, new Vector3(11.7f, 0f, 24f), -90f, "HAIR SALON", Mat("ShopAwningBlue"));
            // Vendor stalls, bus stop, and billboard removed by request.
        }

        static void BuildTaxiStreet(Transform parent)
        {
            MuralWall(parent, new Vector3(-10.9f, 0f, 8f), 90f, "JOZI RUNNER");
            MuralWall(parent, new Vector3(10.9f, 0f, 20f), -90f, "JOZI MY HOME");
            Building(parent, new Vector3(-12.4f, 0f, 22f), new Vector3(5f, 8f, 5f), Mat("Brick"), BuildingStyle.BrickApartment, "SOWETO IS US");
            Building(parent, new Vector3(12.4f, 0f, 7f), new Vector3(5.6f, 10f, 5.5f), Mat("BrickWallRed"), BuildingStyle.BrickApartment, "LOFTS");
            Building(parent, new Vector3(12.2f, 0f, 26f), new Vector3(5f, 7f, 5f), Mat("PlasterCream"), BuildingStyle.Office, "STUDIO");
            ZebraCrossing(parent, 6f);
            // Pedestrian crossing signs and billboard removed by request.
        }

        static void BuildMixedCommercialStreet(Transform parent)
        {
            Building(parent, new Vector3(-12.3f, 0f, 4f), new Vector3(5f, 7f, 5f), Mat("BrickWallTan"), BuildingStyle.MixedUse, "APARTMENTS");
            Shopfront(parent, new Vector3(-11.7f, 0f, 12f), 90f, "RETAIL", Mat("ShopAwningGreen"));
            Building(parent, new Vector3(-12.2f, 0f, 22f), new Vector3(5.8f, 13f, 5.5f), Mat("Concrete"), BuildingStyle.Office, "MEDIA");
            Shopfront(parent, new Vector3(11.7f, 0f, 5f), -90f, "FOOD", Mat("ShopAwningRed"));
            Building(parent, new Vector3(12.5f, 0f, 14f), new Vector3(5f, 9.5f, 5f), Mat("Brick"), BuildingStyle.MixedUse, "ROOMS");
            Building(parent, new Vector3(12.3f, 0f, 24f), new Vector3(5.8f, 16f, 5.5f), Mat("Glass"), BuildingStyle.GlassTower, "OFFICES");
            // Road sign removed by request.
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
                Cube("ZebraCrossingStripe", parent, new Vector3(i * 1.0f, 0.06f, zPosition), new Vector3(0.55f, 0.04f, 2.4f), Mat("PaintWhite"));
            }
        }

        static void PedestrianCrossingTreatment(Transform parent, float zPosition)
        {
            ZebraCrossing(parent, zPosition);

            // Slightly raised pavement islands frame the crossing like a real
            // signalised Johannesburg kerb treatment. These remain visual-only.
            Cube("RaisedPavement_Left", parent, new Vector3(-5.82f, 0.15f, zPosition),
                new Vector3(2.9f, 0.08f, 4.2f), Mat("Pavement"));
            Cube("RaisedPavement_Right", parent, new Vector3(5.82f, 0.15f, zPosition),
                new Vector3(2.9f, 0.08f, 4.2f), Mat("SidewalkConcrete"));

            Cube("RaisedKerb_Left", parent, new Vector3(-4.36f, 0.20f, zPosition),
                new Vector3(0.28f, 0.18f, 4.2f), Mat("KerbStone"));
            Cube("RaisedKerb_Right", parent, new Vector3(4.36f, 0.20f, zPosition),
                new Vector3(0.28f, 0.18f, 4.2f), Mat("KerbStone"));

            // Yellow tactile pads sit beside the stripe ends, clear of all lanes.
            Cube("TactilePaving_Left", parent, new Vector3(-4.9f, 0.205f, zPosition),
                new Vector3(0.72f, 0.03f, 1.15f), Mat("PaintYellow"));
            Cube("TactilePaving_Right", parent, new Vector3(4.9f, 0.205f, zPosition),
                new Vector3(0.72f, 0.03f, 1.15f), Mat("PaintYellow"));
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
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
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

        static readonly string[] VehicleModelExtensions = { ".glb", ".gltf", ".fbx", ".obj" };

        static string FindModelPathByName(string modelName)
        {
            // Search by name across all assets (not just t:Model) so glTF/GLB
            // imported by glTFast — which are GameObject assets, not ModelImporter
            // assets — are found too. Preference order favours .glb/.gltf so a new
            // Higgsfield-generated taxi wins over an older FBX of the same name.
            string lower = modelName.ToLowerInvariant();
            string bestPath = null;
            int bestRank = int.MaxValue;
            foreach (string guid in AssetDatabase.FindAssets(modelName))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/") ||
                    Path.GetFileNameWithoutExtension(path).ToLowerInvariant() != lower)
                {
                    continue;
                }

                int rank = System.Array.IndexOf(VehicleModelExtensions, Path.GetExtension(path).ToLowerInvariant());
                if (rank >= 0 && rank < bestRank)
                {
                    bestRank = rank;
                    bestPath = path;
                }
            }

            return bestPath;
        }

        static bool IsGltfModel(string path) =>
            !string.IsNullOrEmpty(path) &&
            (path.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase) ||
             path.EndsWith(".gltf", System.StringComparison.OrdinalIgnoreCase));

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

            // glTFast imports GLB/glTF with its embedded textures and materials
            // already applied, so treat those as textured and skip the FBX-only
            // texture-extraction/override path below.
            bool isGltf = IsGltfModel(modelPath);
            bool hasTextures = isGltf;
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (!isGltf && importer != null)
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
            // glTFast already imports glTF/GLB upright (Y-up), so skip this FBX-only
            // heuristic for them — it would tip the vehicle onto its side.
            Renderer[] rawRenderers = visual.GetComponentsInChildren<Renderer>();
            if (!isGltf && modelPath.Contains("/HiAce/"))
            {
                // The Meshy HiAce taxi exports Z-up with its length along X and
                // nose at +X, but its near-square cross-section (height ~= width)
                // makes the bounds heuristic below guess Y-up and leave it rolled
                // onto its side; state the correction explicitly.
                visual.transform.localRotation = Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(-90f, 0f, 0f);
            }
            else if (!isGltf && rawRenderers.Length > 0)
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

            // glTFast imports this taxi upright but with its length along X; add a
            // base -90° yaw so the length runs down the road (Z) with the front
            // pointing the same way the FBX heuristic produced for the old taxi
            // (so an oncoming obstacle shows its front to the approaching player).
            if (isGltf)
            {
                visual.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            }

            // Bake the facing into the visual, not the root: spawners often
            // instantiate with identity rotation, which would undo a root facing.
            visual.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f) * visual.transform.localRotation;

            NormalizeVehicleBounds(visual, root.transform, targetHeight);
            SimplifyMeshes(visual, 100000);

            if (!hasTextures)
            {
                hasTextures = TryApplyVehicleSiblingTextures(visual, modelPath);
            }

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

        static Bounds WorldRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        /// <summary>
        /// glTF materials default to metallicFactor 1, so a SAM/Higgsfield export
        /// with no metal-roughness map renders near-black under the scene's simple
        /// lighting. Rebuild a plain URP Lit material around the GLB's base-colour
        /// texture instead and apply it to every renderer.
        /// </summary>
        static void ApplyGltfLitMaterial(GameObject visual, string materialName, float smoothness)
        {
            Texture baseMap = null;
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
            {
                Material source = renderer.sharedMaterial;
                if (source == null)
                {
                    continue;
                }

                foreach (string property in new[] { "baseColorTexture", "_BaseMap", "_BaseColorTexture", "_MainTex" })
                {
                    if (source.HasProperty(property) && source.GetTexture(property) != null)
                    {
                        baseMap = source.GetTexture(property);
                        break;
                    }
                }

                if (baseMap == null)
                {
                    baseMap = source.mainTexture;
                }

                if (baseMap != null)
                {
                    break;
                }
            }

            Material material = CreateMaterial(materialName, Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", smoothness);
            if (baseMap != null)
            {
                material.SetTexture("_BaseMap", baseMap);
                material.mainTexture = baseMap;
            }

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
            }
        }

        /// <summary>
        /// Finds the two wheel-arch openings of a wheel-less vehicle body. Along
        /// the body's bottom edge the rocker/bumper vertices form a dense line of
        /// z samples, and each arch is a wide gap in that line; the axles sit at
        /// the centres of the two widest gaps. Returned nose-agnostically as
        /// (higher z, lower z) — the caller maps them to front/rear. Also reports
        /// the narrower arch opening (to size the wheels) and how far the rocker
        /// panels sit from the centreline (to inset the wheels past the mirrors,
        /// which inflate the full body bounds).
        /// </summary>
        static bool TryMeasureArchCentres(GameObject body, Transform root, out float zHigh, out float zLow, out float archSpan, out float sideHalfWidth)
        {
            zHigh = zLow = 0f;
            archSpan = sideHalfWidth = 0f;
            Bounds bounds = WorldRendererBounds(body);
            Vector3 min = root.InverseTransformPoint(bounds.min);
            Vector3 max = root.InverseTransformPoint(bounds.max);
            float bandTop = min.y + (max.y - min.y) * 0.10f;
            float centerX = (min.x + max.x) * 0.5f;
            float halfWidth = (max.x - min.x) * 0.5f;

            var samples = new List<float>();
            foreach (MeshFilter filter in body.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Matrix4x4 toRoot = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                {
                    Vector3 point = toRoot.MultiplyPoint3x4(vertex);
                    if (point.y <= bandTop && Mathf.Abs(point.x - centerX) > halfWidth * 0.55f)
                    {
                        samples.Add(point.z);
                        sideHalfWidth = Mathf.Max(sideHalfWidth, Mathf.Abs(point.x - centerX));
                    }
                }
            }

            if (samples.Count < 40)
            {
                return false;
            }

            samples.Sort();
            float bestGap = 0f, secondGap = 0f, bestCentre = 0f, secondCentre = 0f;
            for (int i = 1; i < samples.Count; i++)
            {
                float gap = samples[i] - samples[i - 1];
                float centre = (samples[i] + samples[i - 1]) * 0.5f;
                if (gap > bestGap)
                {
                    secondGap = bestGap;
                    secondCentre = bestCentre;
                    bestGap = gap;
                    bestCentre = centre;
                }
                else if (gap > secondGap)
                {
                    secondGap = gap;
                    secondCentre = centre;
                }
            }

            float length = max.z - min.z;
            if (secondGap < length * 0.06f)
            {
                return false;
            }

            zHigh = Mathf.Max(bestCentre, secondCentre);
            zLow = Mathf.Min(bestCentre, secondCentre);
            archSpan = secondGap;

            // Sanity: the gaps must sit a plausible wheelbase apart.
            float wheelbase = zHigh - zLow;
            return wheelbase > length * 0.35f && wheelbase < length * 0.85f;
        }

        /// <summary>
        /// Assembles the Higgsfield taxi from a wheel-less body GLB plus four
        /// instances of a separate wheel GLB. Unlike the single-mesh FBX path —
        /// whose baked-in wheels need a primitive overlay to appear to spin —
        /// the wheels here are real child pivots (FrontLeft/FrontRight/RearLeft/
        /// RearRightWheel) that VehicleMotion rotates by the distance driven.
        /// Axles are located from the gaps the arches leave in the body's bottom
        /// edge. Returns null when either model is missing so callers can fall
        /// back to the single-mesh or primitive taxi.
        /// </summary>
        static GameObject AssembledTaxiInstance(Transform parent, Vector3 position, float yRotation, float targetHeight)
        {
            GameObject bodyModel = AssetDatabase.LoadAssetAtPath<GameObject>(TaxiBodyModelPath);
            GameObject wheelModel = AssetDatabase.LoadAssetAtPath<GameObject>(TaxiWheelModelPath);
            if (bodyModel == null || wheelModel == null)
            {
                return null;
            }

            GameObject root = new GameObject("AssembledTaxi");
            root.transform.SetParent(parent);
            root.transform.localPosition = position;

            GameObject body = Object.Instantiate(bodyModel, root.transform);
            body.name = "TaxiBody";
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            if (body.GetComponentsInChildren<Renderer>().Length == 0)
            {
                Object.DestroyImmediate(root);
                return null;
            }

            // glTFast imports Y-up; turn the body's length onto Z when it arrives
            // along X, then bake the requested facing into the visual (spawners
            // instantiate with identity rotation, exactly as with the FBX path).
            Bounds rawBounds = WorldRendererBounds(body);
            if (rawBounds.size.x > rawBounds.size.z)
            {
                body.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            }

            body.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f) * body.transform.localRotation;

            // The finished vehicle is body over ground clearance: normalize the
            // body to the above-clearance share of the height, then lift it so
            // the wheels have room to stand inside the arches.
            float groundClearance = targetHeight * 0.10f;
            NormalizeVehicleBounds(body, root.transform, targetHeight - groundClearance);
            body.transform.position += Vector3.up * groundClearance;
            SimplifyMeshes(body, 60000);
            ApplyGltfLitMaterial(body, "TaxiBodyLit", 0.4f);

            Bounds bodyBounds = WorldRendererBounds(body);
            Vector3 centre = root.transform.InverseTransformPoint(bodyBounds.center);
            float length = bodyBounds.size.z;
            float halfWidth = bodyBounds.size.x * 0.5f;

            float zHigh = centre.z + length * 0.28f;
            float zLow = centre.z - length * 0.28f;
            // Mirrors inflate the body bounds, so default the wheels' track to a
            // touch inside them and prefer the measured rocker width.
            float rockerHalf = halfWidth * 0.92f;
            float wheelRadius = targetHeight * 0.16f;
            if (TryMeasureArchCentres(body, root.transform, out float measuredHigh, out float measuredLow, out float archSpan, out float measuredRockerHalf))
            {
                zHigh = measuredHigh;
                zLow = measuredLow;
                rockerHalf = measuredRockerHalf;
                // The wheel must fill most of the arch opening without scraping it.
                wheelRadius = Mathf.Clamp(archSpan * 0.42f, targetHeight * 0.13f, targetHeight * 0.185f);
                Debug.Log($"AssembledTaxi: arches measured at z {zLow:F2}/{zHigh:F2}, span {archSpan:F2}, rocker half-width {rockerHalf:F2}");
            }
            else
            {
                Debug.LogWarning("AssembledTaxi: arch measurement failed, using bounds heuristic for axle placement");
            }

            // A wheel smaller than the clearance would hang in the air below the
            // rockers; keep its centre above the body's bottom edge.
            wheelRadius = Mathf.Max(wheelRadius, groundClearance * 1.3f);

            // Which local-Z direction the nose points after yRotation was baked.
            float frontSign = Mathf.Cos(yRotation * Mathf.Deg2Rad) >= 0f ? 1f : -1f;
            float frontAxleZ = frontSign > 0f ? zHigh : zLow;
            float rearAxleZ = frontSign > 0f ? zLow : zHigh;

            var wheels = new List<Transform>();
            foreach ((float side, bool front) in new (float, bool)[] { (-1f, true), (1f, true), (-1f, false), (1f, false) })
            {
                // Named from the driver's seat: facing the nose, left is -X when
                // the nose points +Z and +X when it points -Z.
                bool isLeft = side * frontSign < 0f;
                GameObject pivot = new GameObject($"{(front ? "Front" : "Rear")}{(isLeft ? "Left" : "Right")}Wheel");
                pivot.transform.SetParent(root.transform);
                pivot.transform.localRotation = Quaternion.identity;

                GameObject wheel = Object.Instantiate(wheelModel, pivot.transform);
                wheel.name = "WheelVisual";
                wheel.transform.localPosition = Vector3.zero;

                // Orient the wheel so its axle (the thinnest bounds axis) runs
                // along X and it rolls when the pivot spins about X.
                Bounds wheelRaw = WorldRendererBounds(wheel);
                if (wheelRaw.size.z <= wheelRaw.size.x && wheelRaw.size.z <= wheelRaw.size.y)
                {
                    wheel.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else if (wheelRaw.size.y <= wheelRaw.size.x && wheelRaw.size.y <= wheelRaw.size.z)
                {
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }

                // Outer rim face toward the street side of the vehicle.
                if (side < 0f)
                {
                    wheel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * wheel.transform.localRotation;
                }

                Bounds wheelBounds = WorldRendererBounds(wheel);
                float diameter = Mathf.Max(wheelBounds.size.y, wheelBounds.size.z);
                wheel.transform.localScale *= wheelRadius * 2f / diameter;

                wheelBounds = WorldRendererBounds(wheel);
                pivot.transform.localPosition = new Vector3(
                    centre.x + side * (rockerHalf - wheelBounds.size.x * 0.55f),
                    wheelRadius,
                    front ? frontAxleZ : rearAxleZ);

                // Centre the wheel mesh on its pivot so the spin has no wobble.
                wheelBounds = WorldRendererBounds(wheel);
                wheel.transform.position += pivot.transform.position - wheelBounds.center;

                SimplifyMeshes(wheel, 8000);
                ApplyGltfLitMaterial(wheel, "TaxiWheelLit", 0.25f);
                wheels.Add(pivot.transform);
            }

            VehicleMotion motion = root.AddComponent<VehicleMotion>();
            SetField(motion, "wheels", wheels.ToArray());
            SetField(motion, "wheelRadius", wheelRadius);

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

        /// <summary>
        /// Meshy FBX exports reference their textures at absolute /tmp paths from
        /// the export machine, so Unity's importer leaves the materials blank.
        /// The maps ship as sibling PNGs instead: build one URP Lit material from
        /// &lt;model&gt;_basecolor.png (+ _normal.png when present) and apply it to
        /// every renderer. Returns false when no sibling base color exists.
        /// </summary>
        static bool TryApplyVehicleSiblingTextures(GameObject visual, string modelPath)
        {
            string folder = Path.GetDirectoryName(modelPath).Replace('\\', '/');
            string baseName = Path.GetFileNameWithoutExtension(modelPath);
            Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{baseName}_basecolor.png");
            if (baseMap == null)
            {
                return false;
            }

            Material material = CreateMaterial($"VehicleLit_{baseName.Replace(" ", "")}", Color.white);
            material.SetTexture("_BaseMap", baseMap);
            material.mainTexture = baseMap;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.35f);

            Texture2D normalMap = LoadAsNormalMap($"{folder}/{baseName}_normal.png");
            if (normalMap != null)
            {
                material.SetTexture("_BumpMap", normalMap);
                material.EnableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }

            return true;
        }

        static Texture2D LoadAsNormalMap(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return null;
            }

            if (importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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

        // Same as UnlitColorMaterial but with URP's Transparent surface type
        // set up (alpha blend, no depth write) — required for anything whose
        // alpha is meant to actually show through, e.g. the Ubuntu Pulse
        // shield sphere and ground glow.
        static Material UnlitTransparentMaterial(string name, Color color)
        {
            string materialPath = $"Assets/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = 3000;
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

        /// <summary>
        /// Lays four spinning wheels over the GLB taxi's static, baked-in wheels
        /// (a single mesh can't rotate its own wheels). Positions are measured
        /// from the mesh so they align with the modelled wheels; a silver rim with
        /// lug bolts makes the rotation read, and VehicleMotion spins them by the
        /// distance driven. The black tyres sit slightly proud of the modelled
        /// wheels so any small misalignment reads as black-on-black.
        /// </summary>
        static void AddSpinningWheels(GameObject root)
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

            float length = bounds.size.z;
            float width = bounds.size.x;
            float height = bounds.size.y;

            float frontAxleZ = bounds.center.z + length * 0.30f;
            float rearAxleZ = bounds.center.z - length * 0.30f;
            float hubX = width * 0.5f - 0.06f;
            if (TryMeasureWheels(root, bounds, out float measuredFrontZ, out float measuredRearZ, out float measuredHalfTrack))
            {
                frontAxleZ = measuredFrontZ;
                rearAxleZ = measuredRearZ;
                hubX = measuredHalfTrack;
                Debug.Log($"{root.name}: spinning wheels aligned to measured axles z {rearAxleZ:F2}/{frontAxleZ:F2}, half-track {hubX:F2}");
            }

            float wheelRadius = height * 0.185f;
            float wheelY = bounds.min.y + wheelRadius;
            float tyreWidth = 0.24f;

            Material tyre = Mat("WheelBlack");
            Material rim = Mat("PoleGrey");

            var wheels = new List<Transform>();
            foreach (Vector2 corner in new[] { new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f) })
            {
                GameObject hub = new GameObject("WheelHub");
                hub.transform.SetParent(root.transform);
                hub.transform.localPosition = new Vector3(
                    bounds.center.x + corner.x * hubX,
                    wheelY,
                    corner.y > 0f ? frontAxleZ : rearAxleZ);
                hub.transform.localRotation = Quaternion.identity;

                // Tyre: cylinder laid on its side so its axle runs along X and it
                // rolls when the hub spins about X.
                Cylinder("Tyre", hub.transform, Vector3.zero,
                    new Vector3(wheelRadius * 2f, tyreWidth * 0.5f, wheelRadius * 2f), tyre, new Vector3(0f, 0f, 90f));

                // Silver rim + centre cap + lug bolts on the outward face so the
                // spin is clearly visible as the taxi passes.
                float outerX = corner.x * (tyreWidth * 0.5f + 0.015f);
                Cylinder("Rim", hub.transform, new Vector3(outerX, 0f, 0f),
                    new Vector3(wheelRadius * 1.2f, 0.02f, wheelRadius * 1.2f), rim, new Vector3(0f, 0f, 90f));
                Cylinder("HubCap", hub.transform, new Vector3(outerX * 1.25f, 0f, 0f),
                    new Vector3(wheelRadius * 0.4f, 0.02f, wheelRadius * 0.4f), tyre, new Vector3(0f, 0f, 90f));
                for (int b = 0; b < 5; b++)
                {
                    float angle = b * Mathf.PI * 2f / 5f;
                    Cube("Lug", hub.transform, new Vector3(
                        outerX * 1.25f,
                        Mathf.Sin(angle) * wheelRadius * 0.6f,
                        Mathf.Cos(angle) * wheelRadius * 0.6f),
                        new Vector3(0.05f, 0.07f, 0.07f), tyre);
                }

                wheels.Add(hub.transform);
            }

            foreach (Transform wheel in wheels)
            {
                StripColliders(wheel.gameObject);
            }

            VehicleMotion motion = root.AddComponent<VehicleMotion>();
            SetField(motion, "wheels", wheels.ToArray());
            SetField(motion, "wheelRadius", wheelRadius);
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
            // Prefer the Higgsfield body+wheels assembly, then a dropped-in
            // single-mesh model, then the primitive minibus.
            GameObject assembledRoot = AssembledTaxiInstance(holder.transform, Vector3.zero, 180f, 2.3f);
            GameObject modelRoot = assembledRoot ?? FbxVehicleInstance("taxi", holder.transform, Vector3.zero, 180f, 2.3f, "TaxiObstacle");
            GameObject classic = modelRoot ?? MinibusTaxiVisual(holder.transform, Vector3.zero, 180f);
            classic.name = "ClassicTaxiVisual";
            classic.transform.SetParent(null);
            Object.DestroyImmediate(holder);

            GameObject root = new GameObject("SouthAfricanTaxiObstacle");
            classic.transform.SetParent(root.transform, true);
            GameObject taxi2 = CreateTaxi2Visual(root.transform);
            GameObject[] variants = taxi2 != null
                ? new[] { classic, taxi2 }
                : new[] { classic };

            // Fit one shared collider to both visuals. This keeps obstacle
            // clearance identical regardless of which taxi body is selected.
            foreach (GameObject variant in variants)
            {
                variant.SetActive(true);
            }
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            AddObstacleCollider(root, bounds.center - root.transform.position, bounds.size * 0.9f);

            // Obstacle taxis face the player, so their nose points down local -Z.
            // The assembled taxi's real wheel children already spin via the
            // VehicleMotion added during assembly; a single-mesh model needs
            // spinning wheels laid over its static, baked-in wheels; the
            // primitive fallback gets the full running gear.
            if (assembledRoot == null && modelRoot != null)
            {
                AddSpinningWheels(classic);
            }
            else if (modelRoot == null)
            {
                AddVehicleRunningGear(classic, -1f);
            }

            TaxiObstacleVisualVariants selector = root.AddComponent<TaxiObstacleVisualVariants>();
            SetField(selector, "variants", variants);
            classic.SetActive(true);
            if (taxi2 != null)
            {
                taxi2.SetActive(false);
            }

            // Oncoming traffic: taxi obstacles drive toward the player, each with
            // its own cruising speed, an occasional pull-over, and a gentle sway.
            MovingObstacle mover = root.AddComponent<MovingObstacle>();
            SetField(mover, "minSpeed", 4f);
            SetField(mover, "maxSpeed", 7.5f);

            Debug.Log($"Taxi obstacle triangle count: {CountTriangles(root)}");
            return SavePrefab(root, TaxiPrefabPath);
        }

        static GameObject CreateTaxi2Visual(Transform parent)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(Taxi2ModelPath);
            if (model == null)
            {
                Debug.LogWarning($"Taxi2 model has not imported yet: {Taxi2ModelPath}");
                return null;
            }

            GameObject visual = new GameObject("Taxi2Visual");
            visual.transform.SetParent(parent);
            visual.transform.localPosition = Vector3.zero;
            // Taxi obstacles travel toward the runner, so the vehicle's nose
            // must point down the track toward local -Z.
            visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            visual.transform.localScale = Vector3.one;

            GameObject body = Object.Instantiate(model, visual.transform);
            body.name = "Taxi2Body";
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = Vector3.one;

            Material material = CreateTaxi2Material();
            Renderer[] renderers = body.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }
                renderer.sharedMaterials = materials;
            }

            // Use the exact separate Higgsfield wheel asset fitted to the original
            // assembled taxi, rather than the procedural wheel-disc overlay.
            AddOriginalTaxiWheels(visual);
            // Raise only the body. Wheel pivots remain direct children of the
            // assembly at their measured ground-aligned positions.
            body.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            renderers = visual.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length > 0)
            {
                LODGroup lodGroup = visual.AddComponent<LODGroup>();
                lodGroup.SetLODs(new[]
                {
                    new LOD(0.08f, renderers),
                    new LOD(0.015f, System.Array.Empty<Renderer>()),
                });
                lodGroup.RecalculateBounds();
            }

            return visual;
        }

        static void AddOriginalTaxiWheels(GameObject root)
        {
            GameObject wheelModel = AssetDatabase.LoadAssetAtPath<GameObject>(TaxiWheelModelPath);
            Renderer[] bodyRenderers = root.GetComponentsInChildren<Renderer>();
            if (wheelModel == null || bodyRenderers.Length == 0)
            {
                Debug.LogWarning("Taxi2: original TaxiWheel.glb is unavailable");
                return;
            }

            Bounds bounds = WorldRendererBounds(root);
            float frontAxleZ = bounds.center.z + bounds.size.z * 0.30f;
            float rearAxleZ = bounds.center.z - bounds.size.z * 0.30f;
            float halfTrack = bounds.size.x * 0.5f;
            if (TryMeasureWheels(root, bounds, out float measuredFront, out float measuredRear, out float measuredTrack))
            {
                frontAxleZ = measuredFront;
                rearAxleZ = measuredRear;
                halfTrack = measuredTrack;
            }

            float wheelRadius = bounds.size.y * 0.16f;
            var wheels = new List<Transform>();
            foreach ((float side, bool front) in new (float, bool)[]
                     { (-1f, true), (1f, true), (-1f, false), (1f, false) })
            {
                GameObject pivot = new GameObject(
                    $"{(front ? "Front" : "Rear")}{(side < 0f ? "Left" : "Right")}Wheel");
                pivot.transform.SetParent(root.transform);
                pivot.transform.localRotation = Quaternion.identity;

                GameObject wheel = Object.Instantiate(wheelModel, pivot.transform);
                wheel.name = "OriginalTaxiWheelVisual";
                wheel.transform.localPosition = Vector3.zero;
                wheel.transform.localRotation = Quaternion.identity;

                Bounds raw = WorldRendererBounds(wheel);
                if (raw.size.z <= raw.size.x && raw.size.z <= raw.size.y)
                {
                    wheel.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else if (raw.size.y <= raw.size.x && raw.size.y <= raw.size.z)
                {
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
                if (side < 0f)
                {
                    wheel.transform.localRotation =
                        Quaternion.Euler(0f, 180f, 0f) * wheel.transform.localRotation;
                }

                Bounds fitted = WorldRendererBounds(wheel);
                float diameter = Mathf.Max(fitted.size.y, fitted.size.z);
                wheel.transform.localScale *= wheelRadius * 2f / diameter;
                fitted = WorldRendererBounds(wheel);

                pivot.transform.localPosition = new Vector3(
                    bounds.center.x + side * (halfTrack - fitted.size.x * 0.45f),
                    bounds.min.y + wheelRadius,
                    front ? frontAxleZ : rearAxleZ);
                fitted = WorldRendererBounds(wheel);
                wheel.transform.position += pivot.transform.position - fitted.center;

                SimplifyMeshes(wheel, 8000);
                ApplyGltfLitMaterial(wheel, "TaxiWheelLit", 0.25f);
                StripColliders(wheel);
                wheels.Add(pivot.transform);
            }

            VehicleMotion motion = root.AddComponent<VehicleMotion>();
            SetField(motion, "wheels", wheels.ToArray());
            SetField(motion, "wheelRadius", wheelRadius);
            Debug.Log(
                $"Taxi2: fitted original taxi wheels at z {rearAxleZ:F2}/{frontAxleZ:F2}, " +
                $"half-track {halfTrack:F2}, radius {wheelRadius:F2}");
        }

        static Material CreateTaxi2Material()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(Taxi2MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = "Taxi2_URP" };
                AssetDatabase.CreateAsset(material, Taxi2MaterialPath);
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(Taxi2AlbedoPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Taxi2NormalPath);
            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0.12f);
            material.SetFloat("_Smoothness", 0.32f);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject CreateSceneryTaxiPrefab()
        {
            GameObject holder = new GameObject("SceneryTaxiHolder");
            GameObject assembledRoot = AssembledTaxiInstance(holder.transform, Vector3.zero, 0f, 2.3f);
            GameObject modelRoot = assembledRoot ?? FbxVehicleInstance("taxi", holder.transform, Vector3.zero, 0f, 2.3f, "SceneryTaxi");
            GameObject root = modelRoot ?? MinibusTaxiVisual(holder.transform, Vector3.zero, 0f, true);
            root.name = "SceneryTaxi";
            root.transform.SetParent(null);
            Object.DestroyImmediate(holder);

            foreach (Collider collider in root.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            // Scenery taxis are built facing +Z (rear toward the camera). The
            // assembled taxi's wheels already spin; a single-mesh model needs the
            // spinning-wheel overlay; the primitive gets full running gear.
            if (assembledRoot == null && modelRoot != null)
            {
                AddSpinningWheels(root);
            }
            else if (modelRoot == null)
            {
                AddVehicleRunningGear(root, 1f);
            }

            root.AddComponent<SceneryVehicle>();
            return SavePrefab(root, "Assets/Prefabs/SceneryTaxi.prefab");
        }

        static GameObject CreatePotholeObstaclePrefab()
        {
            GameObject root = new GameObject("PotholeObstacle");
            GameObject hole = Cylinder("PotholeObstacle_DarkPatch", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(1.55f, 0.08f, 1.1f), Mat("PotholeDarkAsphalt"));
            hole.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            AddObstacleCollider(root, new Vector3(0f, 0.45f, 0f), new Vector3(1.9f, 0.9f, 1.5f));
            root.AddComponent<ObstacleDissolveEffect>();
            return SavePrefab(root, PotholePrefabPath);
        }

        static GameObject CreateBarrierObstaclePrefab()
        {
            GameObject root = new GameObject("ConstructionBarrier");
            Cube("BarrierBody", root.transform, new Vector3(0f, 0.65f, 0f), new Vector3(2.35f, 1.05f, 0.42f), Mat("ConstructionBarrierOrange"));
            Cube("BarrierStripe", root.transform, new Vector3(0f, 0.8f, -0.24f), new Vector3(2.1f, 0.18f, 0.06f), Mat("PaintWhite"));
            Cube("BarrierBase", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(2.55f, 0.28f, 0.75f), Mat("ConstructionBarrierOrange"));
            AddObstacleCollider(root, new Vector3(0f, 0.62f, 0f), new Vector3(2.5f, 1.25f, 0.9f));
            root.AddComponent<ObstacleDissolveEffect>();
            return SavePrefab(root, BarrierPrefabPath);
        }

        // Shared blue burst used both for Ubuntu Pulse's own pickup and for
        // small obstacles it dissolves.
        static GameObject CreateUbuntuDissolveParticlePrefab()
        {
            Material glow = UnlitColorMaterial("UbuntuDissolveGlow", new Color(0.45f, 0.85f, 1f));
            GameObject root = new GameObject("UbuntuDissolveParticles");
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 2.2f;
            main.startSize = 0.08f;
            main.maxParticles = 24;
            main.loop = false;
            main.playOnAwake = true;
            main.startColor = new ParticleSystem.MinMaxGradient(glow.color, Color.white);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = glow;

            return SavePrefab(root, UbuntuDissolveParticlePrefabPath);
        }

        // Wheel-lug-style segmented ring (small elongated cubes around a
        // circle), tilted and spinning on its own axis at its own speed —
        // three of these layered give Ubuntu Pulse's "rotating energy rings".
        static void AddEnergyRing(Transform parent, string name, float radius, float tiltDegrees, Material material, float spinDegreesPerSecond, int segments = 18)
        {
            GameObject ring = new GameObject(name);
            ring.transform.SetParent(parent, false);
            ring.transform.localRotation = Quaternion.Euler(tiltDegrees, 0f, 0f);

            float segmentLength = radius * 2f * Mathf.PI / segments * 0.7f;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                Cube($"Segment{i}", ring.transform, position, new Vector3(segmentLength, 0.03f, 0.03f), material,
                    new Vector3(0f, 0f, angle * Mathf.Rad2Deg + 90f));
            }

            StripColliders(ring);
            SpinAxis spin = ring.AddComponent<SpinAxis>();
            SetField(spin, "axis", Vector3.forward);
            SetField(spin, "degreesPerSecond", spinDegreesPerSecond);
        }

        /// <summary>
        /// A ">" chevron built from two angled bars meeting at a tip on local
        /// +Z — i.e. pointing the direction the player always travels (the
        /// Player root never rotates). Returns the pivot so callers can
        /// reposition/layer multiple chevrons.
        /// </summary>
        static Transform BuildForwardChevron(Transform parent, string name, float width, float depth, Material material, float thickness = 0.09f)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);

            float halfWidth = width * 0.5f;
            float armLength = Mathf.Sqrt(halfWidth * halfWidth + depth * depth);
            float angle = Mathf.Atan2(halfWidth, depth) * Mathf.Rad2Deg;

            Cube("ArmLeft", pivot.transform, new Vector3(-halfWidth * 0.5f, 0f, 0f), new Vector3(thickness, thickness, armLength), material, new Vector3(0f, angle, 0f));
            Cube("ArmRight", pivot.transform, new Vector3(halfWidth * 0.5f, 0f, 0f), new Vector3(thickness, thickness, armLength), material, new Vector3(0f, -angle, 0f));

            StripColliders(pivot);
            return pivot.transform;
        }

        static Material GetUbuntuOrbMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(UbuntuOrbMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            string folder = Path.GetDirectoryName(UbuntuOrbModelPath).Replace("\\", "/");
            string baseName = Path.GetFileNameWithoutExtension(UbuntuOrbModelPath);
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{baseName}.png");
            Texture2D normal = LoadAsNormalMap($"{folder}/{baseName}_normal.png");

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = "UbuntuOrb_URP" };

            // This orb's own base colour IS the wanted look: engraved gold and
            // bronze rings around a bright blue crystal core. Show it directly
            // (the earlier orb was flat gold and had to be faked blue). A
            // base-colour emission map at modest strength self-lights the mesh
            // so the gold, bronze and blue all read vividly in the flatly-lit,
            // dark-background icon render — and glow a little in world — without
            // depending on environment reflections, which render bare metal
            // dark. Its emission map ships essentially empty, so drive emission
            // from the base colour instead. The normal map keeps the engraving.
            if (albedo != null)
            {
                material.SetTexture("_BaseMap", albedo);
                material.SetTexture("_MainTex", albedo);
                material.SetTexture("_EmissionMap", albedo);
            }

            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            material.color = Color.white;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.white * 0.45f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetFloat("_Smoothness", 0.55f);
            material.SetFloat("_Metallic", 0.35f);

            AssetDatabase.CreateAsset(material, UbuntuOrbMaterialPath);
            return material;
        }

        /// <summary>
        /// Ubuntu Pulse's collectible: the Meshy orb (falls back to a plain
        /// glowing sphere if the model is missing) plus three independently
        /// spinning segmented energy rings.
        /// </summary>
        static GameObject CreateUbuntuPulsePrefab(GameObject dissolveParticlePrefab)
        {
            GameObject root = new GameObject("PowerUpUbuntuPulse");
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(UbuntuOrbModelPath);
            if (model != null)
            {
                GameObject visual = Object.Instantiate(model, root.transform);
                visual.name = "OrbVisual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                NormalizeStaticModelBounds(visual, root.transform, 0.55f);
                AssignMaterial(visual, GetUbuntuOrbMaterial());
                SimplifyMeshes(visual, 20000);
                StripColliders(visual);

                // The source asset is itself a cluster of engraved rings with
                // blue gem accents, not a plain ball — it already reads as
                // "energy rings" on its own, so no separate ring geometry is
                // layered on top (that only produced two competing, badly
                // scaled ring silhouettes).
                SpinAxis modelSpin = visual.AddComponent<SpinAxis>();
                SetField(modelSpin, "axis", Vector3.up);
                SetField(modelSpin, "degreesPerSecond", 24f);
            }
            else
            {
                Debug.LogWarning($"Ubuntu Pulse orb model not found at {UbuntuOrbModelPath}; using primitive fallback with generated rings.");
                Sphere("OrbVisual", root.transform, Vector3.zero, Vector3.one * 0.5f,
                    UnlitColorMaterial("UbuntuOrbGlow", new Color(0.4f, 0.8f, 1f)));

                Material ringMaterial = UnlitColorMaterial("UbuntuRingGlow", new Color(0.5f, 0.85f, 1f));
                AddEnergyRing(root.transform, "EnergyRingA", 0.32f, 0f, ringMaterial, 40f);
                AddEnergyRing(root.transform, "EnergyRingB", 0.38f, 55f, ringMaterial, -28f);
                AddEnergyRing(root.transform, "EnergyRingC", 0.44f, 110f, ringMaterial, 18f);
            }

            StripColliders(root);
            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.85f;

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            PowerUp powerUp = root.AddComponent<PowerUp>();
            SetField(powerUp, "type", PowerUpType.UbuntuPulse);
            SetField(powerUp, "rotationSpeed", 55f);
            SetField(powerUp, "collectClip", AssetDatabase.LoadAssetAtPath<AudioClip>(UbuntuPulsePickupClipPath));
            SetField(powerUp, "collectParticlePrefab", dissolveParticlePrefab);

            return SavePrefab(root, UbuntuPulsePrefabPath);
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
            AddCoinSparkleBurst(root.transform);
            return SavePrefab(root, "Assets/Prefabs/CoinCollectPop.prefab");
        }

        // Gold sparkles plus a single large fast-fading particle that reads
        // as the collect "pop" flash. The material here is a placeholder:
        // CoinCollectPop swaps these renderers to a soft additive glow at
        // runtime, since the glow sprite texture is runtime-generated.
        static void AddCoinSparkleBurst(Transform parent)
        {
            Material sparkleGold = UnlitColorMaterial("CoinSparkleGold", new Color(1f, 0.84f, 0.35f));

            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.8f, 0.3f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });

            GameObject sparkles = new GameObject("Sparkles");
            sparkles.transform.SetParent(parent, false);
            ParticleSystem sparkleSystem = sparkles.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule sparkleMain = sparkleSystem.main;
            sparkleMain.loop = false;
            sparkleMain.playOnAwake = true;
            sparkleMain.duration = 0.5f;
            sparkleMain.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            sparkleMain.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
            sparkleMain.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
            sparkleMain.maxParticles = 16;
            sparkleMain.gravityModifier = 0.35f;
            sparkleMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), Color.white);
            // The pop root is scaled way down to fit the coin sprite and then
            // animates scale and rise; keep sizes in world units and don't
            // drag already-emitted sparkles along with the rising sprite.
            sparkleMain.simulationSpace = ParticleSystemSimulationSpace.World;
            sparkleMain.scalingMode = ParticleSystemScalingMode.Shape;

            ParticleSystem.EmissionModule sparkleEmission = sparkleSystem.emission;
            sparkleEmission.rateOverTime = 0f;
            sparkleEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

            ParticleSystem.ShapeModule sparkleShape = sparkleSystem.shape;
            sparkleShape.shapeType = ParticleSystemShapeType.Sphere;
            sparkleShape.radius = 0.1f;

            ParticleSystem.SizeOverLifetimeModule sparkleSize = sparkleSystem.sizeOverLifetime;
            sparkleSize.enabled = true;
            sparkleSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            ParticleSystem.ColorOverLifetimeModule sparkleColor = sparkleSystem.colorOverLifetime;
            sparkleColor.enabled = true;
            sparkleColor.color = fade;

            ParticleSystemRenderer sparkleRenderer = sparkles.GetComponent<ParticleSystemRenderer>();
            sparkleRenderer.sharedMaterial = sparkleGold;
            sparkleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sparkleRenderer.receiveShadows = false;

            GameObject flash = new GameObject("BurstFlash");
            flash.transform.SetParent(parent, false);
            ParticleSystem flashSystem = flash.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule flashMain = flashSystem.main;
            flashMain.loop = false;
            flashMain.playOnAwake = true;
            flashMain.duration = 0.2f;
            flashMain.startLifetime = 0.16f;
            flashMain.startSpeed = 0f;
            flashMain.startSize = 0.5f;
            flashMain.maxParticles = 1;
            flashMain.startColor = new Color(1f, 0.9f, 0.55f, 0.9f);
            flashMain.simulationSpace = ParticleSystemSimulationSpace.World;
            flashMain.scalingMode = ParticleSystemScalingMode.Shape;

            ParticleSystem.EmissionModule flashEmission = flashSystem.emission;
            flashEmission.rateOverTime = 0f;
            flashEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            ParticleSystem.ColorOverLifetimeModule flashColor = flashSystem.colorOverLifetime;
            flashColor.enabled = true;
            flashColor.color = fade;

            ParticleSystemRenderer flashRenderer = flash.GetComponent<ParticleSystemRenderer>();
            flashRenderer.sharedMaterial = sparkleGold;
            flashRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flashRenderer.receiveShadows = false;
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
            // Visual is 25% smaller for near-camera readability; trigger radius stays unchanged.
            bool spriteCoin = AddSpriteCoinArt(root.transform, 0.45f);
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
            SetField(coinScript, "collectClip", AssetDatabase.LoadAssetAtPath<AudioClip>(CoinCollectClipPath));
            if (spriteCoin)
            {
                SetField(coinScript, "rotationSpeed", 0f); // flat billboard must not spin edge-on

                // Double Coins swaps the art for the stacked-pair sprite
                // (user-supplied Higgsfield art, background removed).
                SetField(coinScript, "artRenderer", root.GetComponentInChildren<SpriteRenderer>());
                SetField(coinScript, "doubleStackSprite", LoadHiggsfieldSprite("CoinDoubleStack"));
            }

            return SavePrefab(root, CoinPrefabPath);
        }

        // Rare R5: the R1 visual scaled up with a golden rim, worth five coins.
        static GameObject CreateRareCoinPrefab(GameObject coinPopPrefab)
        {
            GameObject root = new GameObject("RareCoinR5");
            bool spriteCoin = AddSpriteCoinArt(root.transform, 0.70f);
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
            SetField(coinScript, "collectClip", AssetDatabase.LoadAssetAtPath<AudioClip>(CoinCollectClipPath));
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

        static TrackChunk[] CreateTrackChunkPrefabs(GameObject taxi, GameObject coin, GameObject rareCoin, GameObject[] powerUps, GameObject barrier, GameObject pothole)
        {
            Directory.CreateDirectory("Assets/Prefabs/Chunks");
            GameObject sceneryTaxi = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SceneryTaxi.prefab");
            GameObject fruitsStall = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Decor/DecorFruitsStall.prefab");
            GameObject streetCrate = CreateStreetCrateObstaclePrefab();
            var chunks = new List<TrackChunk>
            {
                // --- Easy: breathers, single hazards, zig-zag coin trails ---
                NewChunkPrefab("Chunk_OpenRoad", ChunkDifficulty.Easy, 18f, 0b111, 0b111, 12f, _ => { }),
                NewChunkPrefab("Chunk_CoinZigZagLeft", ChunkDifficulty.Easy, 28f, 0b111, 0b111, 26f, t =>
                    ChunkCoinZigZag(t, coin, 0, 2f, 3, 3)),
                NewChunkPrefab("Chunk_CoinZigZagRight", ChunkDifficulty.Easy, 20f, 0b111, 0b111, 26f, t =>
                    ChunkCoinZigZag(t, coin, 2, 2f, 3, 3)),
                NewChunkPrefab("Chunk_TaxiLeft", ChunkDifficulty.Easy, 24f, 0b110, 0b110, 14f, t =>
                {
                    ChunkObstacle(t, taxi, 0, 11f, true);
                    ChunkCoinLine(t, coin, 2, 3f, 4);
                }),
                // Barricade and pothole obstacle chunks removed by request:
                // taxis are now the only obstacle.
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
                // Johannesburg situation A: a stopped minibus partly occupying
                // the left lane. Centre/right remain readable escape routes.
                NewChunkPrefab("Chunk_JoburgTaxiStop", ChunkDifficulty.Easy, 12f, 0b110, 0b110, 22f, t =>
                {
                    ChunkObstacle(t, taxi, 0, 13f, false);
                    ChunkCoinLine(t, coin, 2, 4f, 6);
                    ChunkScenery(t, fruitsStall, -7.2f, 16f, 0f);
                }),
                // Johannesburg situation B: roadworks with a low, jumpable
                // barricade and restrained cone guidance. Both side lanes stay open.
                NewChunkPrefab("Chunk_JoburgRoadworks", ChunkDifficulty.Easy, 11f, 0b111, 0b111, 22f, t =>
                {
                    ChunkObstacle(t, barrier, 1, 13f, false);
                    ChunkRoadCone(t, LaneX(1)-1.15f, 10.5f);
                    ChunkRoadCone(t, LaneX(1)+1.15f, 10.5f);
                    ChunkCoinLine(t, coin, 0, 4f, 5);
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
                // Johannesburg situation D: taxi-rank atmosphere stays on the
                // pavement; one controlled taxi decision enters the road.
                NewChunkPrefab("Chunk_JoburgTaxiRank", ChunkDifficulty.Medium, 10f, 0b011, 0b011, 28f, t =>
                {
                    ChunkObstacle(t, taxi, 2, 17f, false);
                    ChunkScenery(t, sceneryTaxi, -7.4f, 8f, 0f);
                    ChunkScenery(t, sceneryTaxi, -7.4f, 20f, 0f);
                    ChunkScenery(t, fruitsStall, 7.2f, 13f, 180f);
                    ChunkCoinLine(t, coin, 0, 4f, 7);
                }),
                // Johannesburg situation E: everyday street-vendor activity.
                // The stall is safely off-road; only one small crate enters a lane.
                NewChunkPrefab("Chunk_JoburgStreetActivity", ChunkDifficulty.Medium, 10f, 0b101, 0b101, 24f, t =>
                {
                    ChunkObstacle(t, streetCrate, 1, 14f, false);
                    ChunkScenery(t, fruitsStall, -7.0f, 13f, 0f);
                    ChunkScenery(t, streetCrate, -5.8f, 10f, 0f);
                    ChunkScenery(t, streetCrate, -6.4f, 17f, 18f);
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

        static void ChunkScenery(Transform chunk, GameObject prefab, float x, float z, float yaw)
        {
            if (prefab == null) return;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, chunk);
            instance.name = "Scenery_" + prefab.name;
            instance.transform.localPosition = new Vector3(x, 0f, z);
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (RunnerObstacle obstacle in instance.GetComponentsInChildren<RunnerObstacle>(true))
                Object.DestroyImmediate(obstacle);
            foreach (MovingObstacle mover in instance.GetComponentsInChildren<MovingObstacle>(true))
                Object.DestroyImmediate(mover);
        }

        static void ChunkRoadCone(Transform chunk, float x, float z)
        {
            GameObject cone = Cylinder("RoadCone", chunk, new Vector3(x, .22f, z), new Vector3(.24f, .44f, .24f), Mat("ConstructionBarrierOrange"));
            StripColliders(cone);
            Cylinder("ConeStripe", cone.transform, new Vector3(0f,.06f,0f), new Vector3(1.06f,.18f,1.06f), Mat("PaintWhite"));
            StripColliders(cone);
        }

        static GameObject CreateStreetCrateObstaclePrefab()
        {
            const string path = "Assets/Prefabs/StreetCrateObstacle.prefab";
            GameObject root = new GameObject("StreetCrateObstacle");
            Cube("Crate", root.transform, new Vector3(0f,.42f,0f), new Vector3(.9f,.84f,.9f), Mat("CrateWood"));
            BoxCollider trigger = root.AddComponent<BoxCollider>(); trigger.center = new Vector3(0f,.42f,0f); trigger.size = new Vector3(1.05f,.9f,1.05f);
            root.AddComponent<RunnerObstacle>();
            return SavePrefab(root, path);
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

        /// <summary>
        /// Coins weaving across the lanes. Each leg lays a few coins in one
        /// lane, then the trail steps to the adjacent lane, leaving a gap long
        /// enough for one swipe (a lane change settles in ~0.4s ≈ 5m at top
        /// speed), so a runner following the trail collects every coin.
        /// </summary>
        static void ChunkCoinZigZag(Transform chunk, GameObject coinPrefab, int startLane, float zStart, int coinsPerLeg, int legs, float spacing = 2f, float legGap = 5f)
        {
            int lane = Mathf.Clamp(startLane, 0, 2);
            int direction = lane == 2 ? -1 : 1;
            float z = zStart;
            for (int leg = 0; leg < legs; leg++)
            {
                for (int i = 0; i < coinsPerLeg; i++)
                {
                    GameObject coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, chunk);
                    coin.transform.localPosition = new Vector3(LaneX(lane), CoinTrailHeight, z);
                    z += spacing;
                }

                z += legGap - spacing;
                if (lane + direction > 2 || lane + direction < 0)
                {
                    direction = -direction; // bounce off the outside lanes
                }

                lane += direction;
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

        static float LaneX(int lane) => RoadMetrics.LaneX(lane);

        static GameObject[] CreatePowerUpPrefabs(GameObject ubuntuDissolvePrefab)
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
                SpritePickupOrPrimitive(PowerUpType.DoubleCoins, "PU_DoubleCoins", CreateDoubleCoinsPrefab),
                CreateUbuntuPulsePrefab(ubuntuDissolvePrefab),
            };
        }

        // 🪙🪙 Double Coins: two overlapping gold coin discs.
        static GameObject CreateDoubleCoinsPrefab()
        {
            GameObject root = new GameObject("PowerUpDoubleCoins");
            Cylinder("CoinFront", root.transform, new Vector3(0.12f, -0.08f, 0.02f), new Vector3(0.5f, 0.03f, 0.5f), Mat("TaxiYellow"), new Vector3(90f, 0f, 0f));
            Cylinder("CoinBack", root.transform, new Vector3(-0.14f, 0.12f, -0.04f), new Vector3(0.5f, 0.03f, 0.5f), Mat("TaxiYellow"), new Vector3(90f, 0f, 0f));
            return FinishPowerUpPrefab(root, PowerUpType.DoubleCoins);
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
                visual.transform.localScale *= 0.345f / diameter;
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
            // One coherent daylight rig: tri-light ambient keeps shaded faces
            // readable while still grounding props better than a flat fill.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.61f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.43f, 0.49f, 0.58f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.32f, 0.36f);

            GameObject sun = new GameObject("JohannesburgSun");
            sun.transform.SetParent(lighting);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.18f;
            light.color = new Color(1f, 0.91f, 0.76f);
            light.shadows = LightShadows.Hard;
            light.shadowStrength = 0.38f;
            light.shadowBias = 0.08f;
            light.shadowNormalBias = 0.35f;
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
            skybox.SetColor("_SkyTint", new Color(0.34f, 0.52f, 0.78f));
            skybox.SetColor("_GroundColor", new Color(0.48f, 0.5f, 0.58f));
            skybox.SetFloat("_Exposure", 1.18f);
            EditorUtility.SetDirty(skybox);

            RenderSettings.skybox = skybox;
            RenderSettings.sun = light;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.79f, 0.86f);
            RenderSettings.fogStartDistance = 110f;
            RenderSettings.fogEndDistance = 155f;
        }

        static void CreateRoadsideSpawnHaze(Transform player)
        {
            const string materialPath = "Assets/Materials/RoadsideSpawnHaze.mat";
            Material haze = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (haze == null)
            {
                haze = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(haze, materialPath);
            }

            Color hazeColor = new Color(0.72f, 0.79f, 0.86f, 0.82f);
            haze.SetColor("_BaseColor", hazeColor);
            haze.SetFloat("_Surface", 1f);
            haze.SetFloat("_Blend", 0f);
            haze.SetFloat("_ZWrite", 0f);
            haze.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            haze.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(haze);

            GameObject root = new GameObject("RoadsideSpawnHaze");
            root.transform.SetParent(EnvironmentCategory("Skybox"));
            root.transform.position = new Vector3(0f, 0f, player.position.z + 123f);
            RoadsideSpawnHaze controller = root.AddComponent<RoadsideSpawnHaze>();
            SetField(controller, "runner", player);
            SetField(controller, "forwardDistance", 123f);
            SetField(controller, "fogStart", 110f);
            SetField(controller, "fogEnd", 155f);

            // Leave a clear opening over the narrowed gameplay road. Two
            // large side panels hide buildings, trees and decoration as pooled
            // segments are dressed, with only four transparent draw calls total.
            for (int layer = 0; layer < 2; layer++)
            {
                float localZ = layer == 0 ? 0f : 2.5f;
                float alpha = layer == 0 ? 0.55f : 0.82f;
                string layerPath = $"Assets/Materials/RoadsideSpawnHaze_{layer}.mat";
                Material layerMaterial = AssetDatabase.LoadAssetAtPath<Material>(layerPath);
                if (layerMaterial == null)
                {
                    layerMaterial = new Material(haze);
                    AssetDatabase.CreateAsset(layerMaterial, layerPath);
                }
                layerMaterial.SetColor("_BaseColor",
                    new Color(hazeColor.r, hazeColor.g, hazeColor.b, alpha));
                layerMaterial.SetFloat("_Surface", 1f);
                layerMaterial.SetFloat("_ZWrite", 0f);
                layerMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                layerMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                EditorUtility.SetDirty(layerMaterial);

                foreach (float side in new[] { -1f, 1f })
                {
                    GameObject panel = Cube(
                        side < 0f ? $"HazeLeft_{layer}" : $"HazeRight_{layer}",
                        root.transform,
                        new Vector3(side * 30.2f, 18f, localZ),
                        new Vector3(50f, 36f, 0.08f),
                        layerMaterial);
                    Collider collider = panel.GetComponent<Collider>();
                    if (collider != null)
                    {
                        Object.DestroyImmediate(collider);
                    }
                }
            }
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

            // Reuse the two active roadside heroes as recognizable, collision-free skyline
            // silhouettes. Opposite-side placement frames the road without blocking its vanishing point.
            CreateHeroBackdropBuilding(t, JhbBuilding02PrefabPath, "Backdrop_JHB_Building02",
                new Vector3(-27f, 0f, 13f), 1.25f);
            CreateHeroBackdropBuilding(t, JhbSkylinePop02PrefabPath, "Backdrop_JHB_SkylinePop_02",
                new Vector3(27f, 0f, 19f), 1.15f);

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
                // One inset glass panel breaks up the old blank placeholder silhouette
                // without adding dense geometry to the distant skyline.
                float facadeZ = depth - size.z * 0.5f - 0.03f;
                Cube("CitySkyscraper_WindowPanel", t,
                    new Vector3(x, height * 0.55f, facadeZ),
                    new Vector3(size.x * 0.72f, height * 0.58f, 0.08f),
                    Mat("SkyscraperGlass"));

                if (i % 4 == 0)
                {
                    Cube("CitySkyscraper_RoofPlant", t, new Vector3(x, height + 1f, depth), new Vector3(1.6f, 2f, 1.6f), Mat("SkyscraperConcrete"));
                }
            }

            CloudCluster(t, new Vector3(-40f, 40f, 38f), 1.2f);
            CloudCluster(t, new Vector3(46f, 44f, 45f), 1.0f);
            CloudCluster(t, new Vector3(18f, 48f, 52f), 0.8f);
        }

        static void CreateHeroBackdropBuilding(
            Transform parent, string prefabPath, string name, Vector3 position, float scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            instance.transform.localScale = Vector3.one * scale;
            StripColliders(instance);
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
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

        /// <summary>
        /// Builds one inactive board visual per catalog entry under the mount.
        /// Board index order must match BoardNames — the Boards page and
        /// BoardInventory.SelectedIndex both address boards by that index.
        /// </summary>
        static GameObject[] CreateHoverboardVisuals(Transform mount)
        {
            return new[]
            {
                CreateIonCruiserBoard(mount, 0),
            };
        }

        static GameObject CreateIonCruiserBoard(Transform mount, int index)
        {
            GameObject root = new GameObject($"Board_{index}");
            root.transform.SetParent(mount, false);

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(HoverboardModelPath);
            if (model != null)
            {
                GameObject visual = Object.Instantiate(model, root.transform);
                visual.name = "BoardModel";
                visual.transform.localPosition = Vector3.zero;
                // The Meshy export stands the deck on edge (its thin dimension —
                // the deck thickness — runs along Z), so tip it -90° about X to
                // lie flat with the clean Z-emblem deck face up to ride on and
                // the glowing turbine face pointing down (reads as hover thrust).
                // The chrome carry handle is trimmed separately by
                // TrimBoardKickstand.
                visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                visual.transform.localScale = Vector3.one;
                AssignMaterial(visual, GetHoverboardMaterial());
                // Trim first: the kickstand cut keys off world-X, which is only
                // valid before the board is yawed. Then spin the flat deck
                // clockwise (viewed from above) about world-up so it rides at
                // the desired heading.
                TrimBoardKickstand(visual);
                visual.transform.localRotation =
                    Quaternion.Euler(0f, BoardYawDegrees, 0f) * visual.transform.localRotation;
                SimplifyMeshes(visual, 16000);
                StripColliders(visual);
                NormalizeBoardBounds(visual, root.transform, 1.7f);
            }
            else
            {
                Debug.LogWarning($"Hoverboard model unavailable ({HoverboardModelPath}); using primitive deck fallback.");
                Cube("BoardDeck", root.transform, new Vector3(0f, -0.06f, 0f), new Vector3(0.6f, 0.1f, 1.7f),
                    UnlitColorMaterial("HoverboardFallback", new Color(0.25f, 0.7f, 1f)));
                StripColliders(root);
            }

            // Saved inactive: HoverboardVisual enables the selected board only
            // while the shield runs, and editor previews shouldn't show it.
            root.SetActive(false);
            return root;
        }

        // The U-shaped carry handle occupies the leftmost ~35% of the model's
        // X span. Cut by spatial extent (not vertex percentile—the dense handle
        // has disproportionate vertex count) so the complete deck remains.
        const float BoardHandleCutFraction = 0.35f;

        // Cuts the chrome kickstand/handle off the deck. After the board is
        // flipped the hook curves out to one side (world -X), beyond the deck
        // footprint, so triangles whose centre sits past an -X threshold are
        // removed. SimplifyMeshes runs afterwards and persists the result.
        static void TrimBoardKickstand(GameObject visual)
        {
            MeshFilter filter = visual.GetComponentInChildren<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }

            Mesh src = filter.sharedMesh;
            Matrix4x4 toWorld = filter.transform.localToWorldMatrix;
            Vector3[] vertices = src.vertices;
            float[] worldX = new float[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                worldX[i] = toWorld.MultiplyPoint3x4(vertices[i]).x;
            }

            float minX = Mathf.Min(worldX);
            float maxX = Mathf.Max(worldX);
            float cutX = Mathf.Lerp(minX, maxX, BoardHandleCutFraction);
            Debug.Log($"[BOARD] handle cut: x={minX:0.#####}..{maxX:0.#####}, cutX={cutX:0.#####}");

            Vector3[] normals = src.normals;
            Vector4[] tangents = src.tangents;
            Vector2[] uv = src.uv;
            int[] triangles = src.triangles;

            var indexMap = new Dictionary<int, int>();
            var nv = new List<Vector3>();
            var nn = new List<Vector3>();
            var nt = new List<Vector4>();
            var nu = new List<Vector2>();
            var ntri = new List<int>();

            for (int t = 0; t < triangles.Length; t += 3)
            {
                float centreX = (worldX[triangles[t]] + worldX[triangles[t + 1]] + worldX[triangles[t + 2]]) / 3f;
                if (centreX < cutX)
                {
                    continue;
                }

                for (int c = 0; c < 3; c++)
                {
                    int si = triangles[t + c];
                    if (!indexMap.TryGetValue(si, out int ni))
                    {
                        ni = nv.Count;
                        indexMap[si] = ni;
                        nv.Add(vertices[si]);
                        if (normals.Length > si) nn.Add(normals[si]);
                        if (tangents.Length > si) nt.Add(tangents[si]);
                        if (uv.Length > si) nu.Add(uv[si]);
                    }

                    ntri.Add(ni);
                }
            }

            if (ntri.Count == 0 || ntri.Count == triangles.Length)
            {
                return;
            }

            Mesh trimmed = new Mesh
            {
                name = src.name + "_Deck",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            };
            trimmed.SetVertices(nv);
            if (nn.Count == nv.Count) trimmed.SetNormals(nn);
            if (nt.Count == nv.Count) trimmed.SetTangents(nt);
            if (nu.Count == nv.Count) trimmed.SetUVs(0, nu);
            trimmed.SetTriangles(ntri, 0);
            trimmed.RecalculateBounds();
            filter.sharedMesh = trimmed;
            Debug.Log($"[BOARD] kickstand trim: {src.vertexCount} -> {trimmed.vertexCount} verts");
        }

        /// <summary>
        /// The Meshy "futuristic board" export fuses the rideable deck with a
        /// carry frame and a kickstand strut (~30% of the mesh, spread through
        /// +Z in mesh space); this cuts the mesh down to the deck slab
        /// (mesh-local z ≤ -0.0044, measured from the vertex distribution).
        /// Cached as an asset — delete Assets/Meshes/HoverboardDeck.asset to
        /// re-cut after changing the threshold.
        /// </summary>
        static Mesh GetHoverboardDeckMesh()
        {
            const string path = "Assets/Meshes/HoverboardDeck.asset";
            Mesh cached = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (cached != null)
            {
                return cached;
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(HoverboardModelPath);
            MeshFilter sourceFilter = model != null ? model.GetComponentInChildren<MeshFilter>() : null;
            Mesh source = sourceFilter != null ? sourceFilter.sharedMesh : null;
            if (source == null)
            {
                return null;
            }

            const float deckZMax = -0.0044f;
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            Vector4[] tangents = source.tangents;
            Vector2[] uv = source.uv;
            int[] triangles = source.triangles;

            var indexMap = new Dictionary<int, int>();
            var newVertices = new List<Vector3>();
            var newNormals = new List<Vector3>();
            var newTangents = new List<Vector4>();
            var newUv = new List<Vector2>();
            var newTriangles = new List<int>();

            for (int t = 0; t < triangles.Length; t += 3)
            {
                if (vertices[triangles[t]].z > deckZMax ||
                    vertices[triangles[t + 1]].z > deckZMax ||
                    vertices[triangles[t + 2]].z > deckZMax)
                {
                    continue;
                }

                for (int corner = 0; corner < 3; corner++)
                {
                    int sourceIndex = triangles[t + corner];
                    if (!indexMap.TryGetValue(sourceIndex, out int newIndex))
                    {
                        newIndex = newVertices.Count;
                        indexMap[sourceIndex] = newIndex;
                        newVertices.Add(vertices[sourceIndex]);
                        if (normals.Length > sourceIndex)
                        {
                            newNormals.Add(normals[sourceIndex]);
                        }

                        if (tangents.Length > sourceIndex)
                        {
                            newTangents.Add(tangents[sourceIndex]);
                        }

                        if (uv.Length > sourceIndex)
                        {
                            newUv.Add(uv[sourceIndex]);
                        }
                    }

                    newTriangles.Add(newIndex);
                }
            }

            if (newTriangles.Count == 0)
            {
                Debug.LogWarning("Hoverboard deck cut removed everything; check the deckZMax threshold.");
                return null;
            }

            Mesh deck = new Mesh
            {
                name = "HoverboardDeck",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            };
            deck.SetVertices(newVertices);
            if (newNormals.Count == newVertices.Count)
            {
                deck.SetNormals(newNormals);
            }

            if (newTangents.Count == newVertices.Count)
            {
                deck.SetTangents(newTangents);
            }

            if (newUv.Count == newVertices.Count)
            {
                deck.SetUVs(0, newUv);
            }

            deck.SetTriangles(newTriangles, 0);
            deck.RecalculateBounds();

            Directory.CreateDirectory("Assets/Meshes");
            AssetDatabase.CreateAsset(deck, path);
            Debug.Log($"Hoverboard deck cut: {source.vertexCount} -> {deck.vertexCount} verts");
            return deck;
        }

        // Like NormalizeStaticModelBounds but sized by length (Z) and parked
        // with the deck's TOP surface at the mount origin — the player root
        // sits at the feet, so the board hangs just below them.
        static void NormalizeBoardBounds(GameObject visual, Transform root, float targetLength)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = CombinedRendererBounds(visual);
            // Size by the board's longest horizontal (planar) axis. The deck
            // lies flat, so its up axis (Y) is the thin deck thickness; sizing
            // by that (or by a near-square minor axis) would blow the board up
            // to many metres. Guarding on the planar span keeps a squarish or
            // oddly-proportioned Meshy deck at a sane on-road footprint.
            float planarLength = Mathf.Max(bounds.size.x, bounds.size.z);
            if (planarLength < 0.0001f)
            {
                return;
            }

            visual.transform.localScale *= targetLength / planarLength;

            bounds = CombinedRendererBounds(visual);
            visual.transform.position += new Vector3(
                root.position.x - bounds.center.x,
                root.position.y - bounds.max.y,
                root.position.z - bounds.center.z);
        }

        static Material GetHoverboardMaterial()
        {
            // Same stale-material rule as the coin faces: this early-out means
            // changing the values below has no effect until the .mat asset is
            // deleted first.
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(HoverboardMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            string folder = Path.GetDirectoryName(HoverboardModelPath).Replace("\\", "/");
            string baseName = Path.GetFileNameWithoutExtension(HoverboardModelPath);
            string baseColorPath = $"{folder}/{baseName}.png";
            string normalPath = $"{folder}/{baseName}_normal.png";
            string metallicPath = $"{folder}/{baseName}_metallic.png";
            string emissionPath = $"{folder}/{baseName}_emission.png";

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = "HoverboardIon_URP" };

            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
            if (baseColor != null)
            {
                SetTextureSrgb(baseColorPath, true);
                material.SetTexture("_BaseMap", baseColor);
                material.mainTexture = baseColor;
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.55f);

            Texture2D normal = LoadAsNormalMap(normalPath);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
            if (metallic != null)
            {
                SetTextureSrgb(metallicPath, false);
                material.SetTexture("_MetallicGlossMap", metallic);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Metallic", 1f);
            }

            // A black emission map simply contributes nothing, so wiring it is
            // safe even if this export's map turns out mostly empty.
            Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath);
            if (emission != null)
            {
                SetTextureSrgb(emissionPath, true);
                material.SetTexture("_EmissionMap", emission);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.white * 1.4f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            AssetDatabase.CreateAsset(material, HoverboardMaterialPath);
            return material;
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

        // Adds the additive, event-driven systems introduced in the P1–P5
        // overhaul: progression (stats/missions/achievements/daily rewards),
        // the environment zone director, quality control, and the dev overlay.
        // These only observe central GameEvents and never duplicate existing
        // effects/sounds, so they are safe to activate now. The pooled VFX and
        // audio MANAGERS are intentionally NOT added here yet — activating them
        // requires migrating gameplay's ad-hoc effect/sound calls first, or the
        // same coin/dodge feedback would play twice.
        static void CreateGameSystems(GameObject player)
        {
            GameObject systems = new GameObject("GameSystems");

            systems.AddComponent<JoburgRunner.Progression.StatsRecorder>();

            var missionManager = systems.AddComponent<JoburgRunner.Progression.MissionManager>();
            SetField(missionManager, "missionPool",
                LoadAllAssets<JoburgRunner.Progression.MissionDefinition>("Assets/GameplayContent/Missions"));

            var achievementManager = systems.AddComponent<JoburgRunner.Progression.AchievementManager>();
            SetField(achievementManager, "achievements",
                LoadAllAssets<JoburgRunner.Progression.AchievementDefinition>("Assets/GameplayContent/Achievements"));

            var dailyReward = systems.AddComponent<JoburgRunner.Progression.DailyRewardManager>();
            SetField(dailyReward, "cycle",
                AssetDatabase.LoadAssetAtPath<JoburgRunner.Progression.DailyRewardCycle>("Assets/GameplayContent/DailyRewardCycle.asset"));

            var quality = systems.AddComponent<JoburgRunner.Core.QualityController>();
            SetField(quality, "low", AssetDatabase.LoadAssetAtPath<JoburgRunner.Core.GameQualitySettings>("Assets/GameplayContent/Quality_Low.asset"));
            SetField(quality, "medium", AssetDatabase.LoadAssetAtPath<JoburgRunner.Core.GameQualitySettings>("Assets/GameplayContent/Quality_Medium.asset"));
            SetField(quality, "high", AssetDatabase.LoadAssetAtPath<JoburgRunner.Core.GameQualitySettings>("Assets/GameplayContent/Quality_High.asset"));

            var envDirector = systems.AddComponent<JoburgRunner.Environment.EnvironmentDirector>();
            SetField(envDirector, "catalog", AssetDatabase.LoadAssetAtPath<JoburgRunner.Environment.ZoneCatalog>("Assets/Environment/Zones/ZoneCatalog.asset"));
            SetField(envDirector, "player", player.transform);
            SetField(envDirector, "sun", RenderSettings.sun);

            systems.AddComponent<JoburgRunner.Core.DebugOverlay>();
        }

        static System.Collections.Generic.List<T> LoadAllAssets<T>(string folder) where T : Object
        {
            var list = new System.Collections.Generic.List<T>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }

        static void AddPlayerControlStack(GameObject player)
        {
            // Hierarchy: Player (moves, never rotates) → DroneFlightPivot
            // (cosmetic prone pitch while the drone flies) → ModelLeanPivot
            // (cosmetic lane-change roll only) → character visuals, one per
            // selectable character; CharacterSelector activates the chosen one.
            Transform leanPivot = null;
            Transform flightPivot = null;
            var characterVisuals = new List<Transform>();
            foreach (Transform child in player.transform)
            {
                characterVisuals.Add(child);
            }

            if (characterVisuals.Count > 0)
            {
                GameObject flightObject = new GameObject("DroneFlightPivot");
                flightObject.transform.SetParent(player.transform, false);
                flightObject.transform.localPosition = Vector3.zero;
                flightObject.transform.localRotation = Quaternion.identity;
                flightObject.transform.localScale = Vector3.one;
                flightPivot = flightObject.transform;

                GameObject pivotObject = new GameObject("ModelLeanPivot");
                pivotObject.transform.SetParent(flightObject.transform, false);
                pivotObject.transform.localPosition = Vector3.zero;
                pivotObject.transform.localRotation = Quaternion.identity;
                pivotObject.transform.localScale = Vector3.one;

                // Reparent under identity pivots with locals preserved.
                foreach (Transform model in characterVisuals)
                {
                    model.SetParent(pivotObject.transform, false);
                }

                leanPivot = pivotObject.transform;

                CharacterSelector selector = player.AddComponent<CharacterSelector>();
                var visualObjects = new GameObject[characterVisuals.Count];
                for (int i = 0; i < characterVisuals.Count; i++)
                {
                    visualObjects[i] = characterVisuals[i].gameObject;
                    // Save the scene in the default state — only the first
                    // character visible — so editor previews (which never run
                    // CharacterSelector) don't show the characters overlapping.
                    visualObjects[i].SetActive(i == 0);
                }

                SetField(selector, "characterVisuals", visualObjects);
            }

            player.AddComponent<PlayerAnimator>();
            player.AddComponent<RollController>();
            player.AddComponent<PlayerController>();

            RunnerLeanVisual lean = player.AddComponent<RunnerLeanVisual>();
            SetField(lean, "leanPivot", leanPivot);

            DroneFlightVisual droneVisual = player.AddComponent<DroneFlightVisual>();
            SetField(droneVisual, "flightPivot", flightPivot);
            // The horizontal fly clip already bakes a flat superman pose, so the
            // legacy forward-dive pitch (needed by the old upright Falling clip)
            // is zeroed — otherwise it would over-rotate past horizontal.
            SetField(droneVisual, "proneDegrees", 0f);

            // Hoverboard boosters: one board visual per catalog entry, mounted
            // under the lean pivot so the deck leans into lane changes with
            // the character. HoverboardVisual lifts the pivot and shows the
            // selected board while the Hoverboard shield runs.
            if (leanPivot != null)
            {
                GameObject boardMount = new GameObject("HoverboardMount");
                boardMount.transform.SetParent(leanPivot, false);
                GameObject[] boardVisuals = CreateHoverboardVisuals(boardMount.transform);

                HoverboardVisual hoverboardVisual = player.AddComponent<HoverboardVisual>();
                SetField(hoverboardVisual, "boardMount", boardMount.transform);
                SetField(hoverboardVisual, "boardVisuals", boardVisuals);
                SetField(hoverboardVisual, "groundSink", RunnerGroundSink);
            }

            AddUbuntuPulseRig(player);
            AddPerfectDodgeSystem(player);
            AddRunningDustRig(player);
            AddUbuntuLaneShiftRig(player);
        }

        /// <summary>
        /// Ubuntu Pulse Lane Shift rig — the signature lane-change feedback.
        /// One anchor under the player holds the main energy ribbon and the
        /// softer glow ribbon at the torso (the player root sits at the
        /// feet, matching UbuntuMotionTrail's mount point), the spark and
        /// ground-burst particle systems, and one cached AudioSource for the
        /// pitch-randomized swipe whoosh. Every renderer ships an unlit
        /// placeholder that UbuntuLaneShift swaps for the shared additive
        /// glow materials at runtime.
        /// </summary>
        static void AddUbuntuLaneShiftRig(GameObject player)
        {
            Material ribbonMaterial = UnlitTransparentMaterial("UbuntuLaneRibbon", new Color(0.62f, 0.88f, 1f, 0.75f));
            Material ribbonGlowMaterial = UnlitTransparentMaterial("UbuntuLaneRibbonGlow", new Color(0.3f, 0.62f, 1f, 0.3f));
            Material sparkMaterial = UnlitTransparentMaterial("UbuntuLaneSpark", new Color(0.75f, 0.92f, 1f, 0.8f));

            GameObject anchor = new GameObject("UbuntuLaneShiftAnchor");
            anchor.transform.SetParent(player.transform, false);
            anchor.transform.localPosition = Vector3.zero;

            TrailRenderer ribbon = CreateLaneRibbon(
                anchor.transform, "UbuntuLaneRibbon", ribbonMaterial, 0.22f, RibbonEnergyGradient(0.85f, 0.5f));
            TrailRenderer glowRibbon = CreateLaneRibbon(
                anchor.transform, "UbuntuLaneGlowRibbon", ribbonGlowMaterial, 0.3f, RibbonEnergyGradient(0.35f, 0.2f));
            // The glow halo sits a touch further back so the two additive
            // ribbons layer instead of rendering coplanar.
            glowRibbon.transform.localPosition = new Vector3(0f, 0.75f, -0.17f);

            ParticleSystem sparks = CreateUbuntuSparkSystem(anchor.transform, sparkMaterial);
            ParticleSystem groundBurst = CreateUbuntuGroundBurstSystem(anchor.transform, sparkMaterial);

            // One reusable 2D source: PlayClipAtPoint cannot randomize pitch
            // and spawns a temp GameObject per play; this allocates nothing.
            AudioSource swipeSource = anchor.AddComponent<AudioSource>();
            swipeSource.playOnAwake = false;
            swipeSource.spatialBlend = 0f;

            UbuntuLaneShift shift = anchor.AddComponent<UbuntuLaneShift>();
            SetField(shift, "ubuntuRibbon", ribbon);
            SetField(shift, "glowRibbon", glowRibbon);
            SetField(shift, "energySparks", sparks);
            SetField(shift, "groundBurst", groundBurst);
            SetField(shift, "swipeSource", swipeSource);
            SetField(shift, "swipeClip", AssetDatabase.LoadAssetAtPath<AudioClip>(LaneSwitchClipPath));

            PlayerController controller = player.GetComponent<PlayerController>();
            SetField(shift, "playerController", controller);
            SetField(controller, "ubuntuLaneShift", shift);
        }

        static TrailRenderer CreateLaneRibbon(Transform parent, string name, Material material, float lifetime, Gradient gradient)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = new Vector3(0f, 0.75f, -0.12f);

            TrailRenderer ribbon = item.AddComponent<TrailRenderer>();
            ribbon.sharedMaterial = material;
            // Short on purpose: the camera sits directly behind the runner,
            // and a longer trail reads as a beam splitting the screen.
            ribbon.time = lifetime;
            ribbon.widthMultiplier = 0f;
            // Comet taper: full width at the runner, nothing at the old end.
            ribbon.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            ribbon.minVertexDistance = 0.04f;
            ribbon.numCornerVertices = 2;
            ribbon.numCapVertices = 2;
            ribbon.alignment = LineAlignment.View;
            ribbon.textureMode = LineTextureMode.Tile;
            ribbon.generateLightingData = false;
            ribbon.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            ribbon.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ribbon.receiveShadows = false;
            ribbon.emitting = false;
            ribbon.colorGradient = gradient;
            return ribbon;
        }

        // The Ubuntu energy signature: bright white head → electric blue →
        // azure → transparent, with no harsh steps.
        static Gradient RibbonEnergyGradient(float headAlpha, float midAlpha)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.35f, 0.62f, 1f), 0.45f),
                    new GradientColorKey(new Color(0f, 0.5f, 1f), 0.8f),
                },
                new[]
                {
                    new GradientAlphaKey(headAlpha, 0f),
                    new GradientAlphaKey(midAlpha, 0.5f),
                    new GradientAlphaKey(midAlpha * 0.5f, 0.8f),
                    new GradientAlphaKey(0f, 1f),
                });
            return gradient;
        }

        static ParticleSystem CreateUbuntuSparkSystem(Transform parent, Material material)
        {
            GameObject sparks = new GameObject("UbuntuLaneSparks");
            sparks.transform.SetParent(parent, false);
            sparks.transform.localPosition = new Vector3(0f, 0.75f, -0.12f);

            ParticleSystem system = sparks.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.duration = 0.25f;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.45f, 0.75f, 1f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = 36;

            // Emit()-driven from UbuntuLaneShift.Play — no rate, no bursts.
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            // Peel away from the ribbon: drift backwards relative to the
            // run (world -Z) while the runner races on.
            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            // Unity requires all three velocity axes to use the same curve
            // mode. Explicit zero ranges keep X/Y stationary while matching
            // Z's TwoConstants mode on Android players.
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(-1.4f, -0.5f);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = DustFadeGradient();

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            ParticleSystemRenderer sparkRenderer = sparks.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.sharedMaterial = material;
            sparkRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sparkRenderer.receiveShadows = false;
            return system;
        }

        /// <summary>
        /// Tiny radial energy pulse under the shoes, replacing a dust puff —
        /// played only when the lane change starts grounded.
        /// </summary>
        static ParticleSystem CreateUbuntuGroundBurstSystem(Transform parent, Material material)
        {
            GameObject burst = new GameObject("UbuntuGroundBurst");
            burst.transform.SetParent(parent, false);
            // Just above the road (the player root is at the feet); circle
            // emits in its local XY plane, so pitch it flat.
            burst.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            burst.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            ParticleSystem system = burst.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.duration = 0.2f;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.22f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            // Low-opacity blue-white so it stays a whisper, not a stomp.
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.75f, 0.92f, 1f, 0.45f), new Color(0.45f, 0.75f, 1f, 0.35f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = 6;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.14f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = DustFadeGradient();

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            ParticleSystemRenderer burstRenderer = burst.GetComponent<ParticleSystemRenderer>();
            burstRenderer.sharedMaterial = material;
            burstRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            burstRenderer.receiveShadows = false;
            return system;
        }

        /// <summary>
        /// Foot-dust rig: an anchor just behind and below the runner's feet
        /// (the player root sits ~1.1m above the road at the capsule centre)
        /// holding the continuous running dust and the landing burst.
        /// PlayerRunningDustController drives emission from ground contact
        /// and forward speed, and swaps in the shared alpha-blended dust
        /// material at runtime (the soft sprite texture is runtime-generated).
        /// </summary>
        static void AddRunningDustRig(GameObject player)
        {
            Material dustMaterial = UnlitTransparentMaterial("RunningDust", new Color(0.62f, 0.55f, 0.47f, 0.5f));

            // The player root sits at the FEET (the Ubuntu rig, coin-pull
            // target and dodge probe all treat root+1 as the torso), so the
            // anchor needs only a small lift to hover just above the road.
            GameObject anchor = new GameObject("RunningDustAnchor");
            anchor.transform.SetParent(player.transform, false);
            anchor.transform.localPosition = new Vector3(0f, 0.08f, -0.35f);

            ParticleSystem continuousDust = CreateContinuousDustSystem(anchor.transform, dustMaterial);
            ParticleSystem landingDust = CreateLandingDustSystem(anchor.transform, dustMaterial);

            PlayerRunningDustController dust = player.AddComponent<PlayerRunningDustController>();
            SetField(dust, "continuousDust", continuousDust);
            SetField(dust, "landingDust", landingDust);
            SetField(dust, "groundedReference", player.GetComponent<PlayerController>());
            SetField(dust, "rollController", player.GetComponent<RollController>());
        }

        static ParticleSystem CreateContinuousDustSystem(Transform parent, Material material)
        {
            GameObject dust = new GameObject("ContinuousRunningDust");
            dust.transform.SetParent(parent, false);
            // Box emits along local +Z; face it backward with a slight
            // upward tilt so puffs kick out behind the heels.
            dust.transform.localRotation = Quaternion.Euler(-15f, 180f, 0f);

            ParticleSystem system = dust.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.duration = 1f;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = 30;

            // Builder default only: PlayerRunningDustController drives this
            // between 0 and its min/max range from ground contact and speed.
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 12f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.45f, 0.06f, 0.2f);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = DustFadeGradient();

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.55f), new Keyframe(0.4f, 0.85f), new Keyframe(1f, 1f)));

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            // The player root never rotates and always travels +Z, so world
            // -Z is "behind the runner".
            velocity.z = new ParticleSystem.MinMaxCurve(-0.6f, -0.2f);

            ConfigureDustRenderer(dust, material);
            return system;
        }

        static ParticleSystem CreateLandingDustSystem(Transform parent, Material material)
        {
            GameObject dust = new GameObject("LandingDustBurst");
            dust.transform.SetParent(parent, false);
            // Hemisphere emits along local +Z; point it upward.
            dust.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem system = dust.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.duration = 0.3f;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = 20;

            // No rate and no configured burst: the controller calls Emit()
            // with a count scaled by fall speed, so the module stays empty.
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.35f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = DustFadeGradient();

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.6f), new Keyframe(0.5f, 0.95f), new Keyframe(1f, 1f)));

            ConfigureDustRenderer(dust, material);
            return system;
        }

        static Gradient DustFadeGradient()
        {
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.65f, 0.15f),
                    new GradientAlphaKey(0f, 1f),
                });
            return fade;
        }

        static void ConfigureDustRenderer(GameObject dust, Material material)
        {
            ParticleSystemRenderer dustRenderer = dust.GetComponent<ParticleSystemRenderer>();
            dustRenderer.sharedMaterial = material;
            dustRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            dustRenderer.maxParticleSize = 0.25f;
            dustRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dustRenderer.receiveShadows = false;
        }

        /// <summary>
        /// Wires the Perfect Dodge near-miss reward onto the player: the
        /// detector/orchestrator component, its pooled streak-burst prefab,
        /// and the lane-switch whoosh as the dodge sound. The HUD label is
        /// wired later, when the canvas exists.
        /// </summary>
        static void AddPerfectDodgeSystem(GameObject player)
        {
            GameObject vfxPrefab = CreatePerfectDodgeVfxPrefab();
            PerfectDodge dodge = player.AddComponent<PerfectDodge>();
            SetField(dodge, "vfxPrefab", vfxPrefab);
            SetField(dodge, "perfectDodgeDistance", 0.5f);
            SetField(dodge, "perfectDodgeCooldown", 0.75f);
            SetField(dodge, "whooshClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LaneSwitch.mp3"));
        }

        /// <summary>
        /// Pooled near-miss burst: thin stretched white streaks racing past on
        /// both sides plus one soft low-opacity centre flash. Materials are
        /// unlit placeholders — PerfectDodgeVFX swaps them for the shared
        /// additive glow at runtime (the glow texture is runtime-generated).
        /// </summary>
        static GameObject CreatePerfectDodgeVfxPrefab()
        {
            Material streakWhite = UnlitColorMaterial("PerfectDodgeStreak", Color.white);

            GameObject root = new GameObject("PerfectDodgeVFX");
            AddDodgeStreaks(root.transform, "LeftStreaks", new Vector3(-0.75f, 0f, 0f), streakWhite);
            AddDodgeStreaks(root.transform, "RightStreaks", new Vector3(0.75f, 0f, 0f), streakWhite);
            AddDodgeCentreFlash(root.transform, streakWhite);
            root.AddComponent<PerfectDodgeVFX>();
            root.SetActive(false);
            return SavePrefab(root, "Assets/Prefabs/PerfectDodgeVFX.prefab");
        }

        static void AddDodgeStreaks(Transform parent, string name, Vector3 localPosition, Material material)
        {
            GameObject streaks = new GameObject(name);
            streaks.transform.SetParent(parent, false);
            streaks.transform.localPosition = localPosition;
            // Cone emits along local +Z; face it backwards so the streaks
            // race past the player toward the camera.
            streaks.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            ParticleSystem system = streaks.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(18f, 25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.07f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            // White core with a slight blue tint mixed in at random.
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.72f, 0.86f, 1f));

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.18f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = fade;

            ParticleSystemRenderer streakRenderer = streaks.GetComponent<ParticleSystemRenderer>();
            streakRenderer.sharedMaterial = material;
            // Velocity-stretched billboards turn the round glow sprite into
            // thin motion streaks; no geometry or lights involved.
            streakRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            streakRenderer.lengthScale = 8f;
            streakRenderer.velocityScale = 0f;
            streakRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            streakRenderer.receiveShadows = false;
        }

        static void AddDodgeCentreFlash(Transform parent, Material material)
        {
            GameObject flash = new GameObject("CenterFlash");
            flash.transform.SetParent(parent, false);

            ParticleSystem system = flash.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.15f;
            main.startLifetime = 0.1f;
            main.startSpeed = 0f;
            main.startSize = 0.9f;
            main.maxParticles = 1;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startColor = new Color(1f, 1f, 1f, 0.28f);

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = fade;

            ParticleSystemRenderer flashRenderer = flash.GetComponent<ParticleSystemRenderer>();
            flashRenderer.sharedMaterial = material;
            flashRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flashRenderer.receiveShadows = false;
        }

        /// <summary>
        /// Builds the always-present, initially-hidden Ubuntu Pulse effect rig
        /// on the player: shield sphere, ambient particles, a forward-pointing
        /// arrow, ground glow, pulse light, and the looping shield-hum
        /// AudioSource. UbuntuPulseVisual (added here) turns all of it on/off
        /// by polling PowerUpManager each frame — see that script for why.
        /// </summary>
        static void AddUbuntuPulseRig(GameObject player)
        {
            GameObject rig = new GameObject("UbuntuPulseRig");
            rig.transform.SetParent(player.transform, false);
            rig.transform.localPosition = new Vector3(0f, 1f, 0f);

            GameObject shieldSphereObject = Sphere("ShieldSphere", rig.transform, Vector3.zero, Vector3.one,
                UnlitTransparentMaterial("UbuntuShieldSphere", new Color(0.35f, 0.75f, 1f, 0.26f)));
            StripColliders(shieldSphereObject);
            Renderer shieldRenderer = shieldSphereObject.GetComponent<Renderer>();
            shieldSphereObject.SetActive(false);

            Material groundGlowMaterial = UnlitTransparentMaterial("UbuntuGroundGlow", new Color(0.35f, 0.75f, 1f, 0.32f));
            GameObject groundGlowObject = Cylinder("GroundGlow", rig.transform, new Vector3(0f, -0.95f, 0f), new Vector3(1.6f, 0.02f, 1.6f),
                groundGlowMaterial);
            StripColliders(groundGlowObject);
            Renderer groundGlowRenderer = groundGlowObject.GetComponent<Renderer>();
            groundGlowObject.SetActive(false);

            GameObject particlesObject = new GameObject("ShieldParticles");
            particlesObject.transform.SetParent(rig.transform, false);
            ParticleSystem shieldParticles = particlesObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule particleMain = shieldParticles.main;
            particleMain.startLifetime = 1.2f;
            particleMain.startSpeed = 0.15f;
            particleMain.startSize = 0.05f;
            particleMain.maxParticles = 22;
            particleMain.loop = true;
            particleMain.playOnAwake = false;
            particleMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 0.85f, 1f), Color.white);
            ParticleSystem.EmissionModule particleEmission = shieldParticles.emission;
            particleEmission.rateOverTime = 6f;
            ParticleSystem.ShapeModule particleShape = shieldParticles.shape;
            particleShape.shapeType = ParticleSystemShapeType.Sphere;
            particleShape.radius = 0.65f;
            Material particleMaterial = UnlitColorMaterial("UbuntuShieldParticleGlow", new Color(0.5f, 0.85f, 1f));
            particlesObject.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;

            // Forward arrow: a chevron pointing down local +Z (the direction
            // the player always travels — the Player root never rotates), set
            // ahead of the player rather than trailing behind like a
            // TrailRenderer would (a TrailRenderer only ever shows where the
            // object already was, so it structurally cannot point forward).
            Material arrowMaterial = UnlitColorMaterial("UbuntuArrowGlow", new Color(0.45f, 0.85f, 1f));
            GameObject arrowPivot = new GameObject("ForwardArrow");
            arrowPivot.transform.SetParent(rig.transform, false);
            arrowPivot.transform.localPosition = new Vector3(0f, -0.4f, 1.8f);
            BuildForwardChevron(arrowPivot.transform, "ChevronNear", 0.7f, 0.5f, arrowMaterial);
            BuildForwardChevron(arrowPivot.transform, "ChevronFar", 0.55f, 0.4f, arrowMaterial).localPosition = new Vector3(0f, 0f, 0.5f);

            GameObject lightObject = new GameObject("PulseLight");
            lightObject.transform.SetParent(rig.transform, false);
            Light pulseLight = lightObject.AddComponent<Light>();
            pulseLight.type = LightType.Point;
            pulseLight.color = new Color(0.5f, 0.85f, 1f);
            pulseLight.range = 6f;
            pulseLight.intensity = 0f;
            pulseLight.shadows = LightShadows.None;
            pulseLight.enabled = false;

            AudioSource shieldHum = rig.AddComponent<AudioSource>();
            shieldHum.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(UbuntuPulseHumClipPath);
            shieldHum.loop = true;
            shieldHum.playOnAwake = false;
            shieldHum.volume = 0.35f;

            UbuntuPulseVisual ubuntuVisual = player.AddComponent<UbuntuPulseVisual>();
            SetField(ubuntuVisual, "shieldSphere", shieldSphereObject.transform);
            SetField(ubuntuVisual, "shieldRenderer", shieldRenderer);
            SetField(ubuntuVisual, "shieldParticles", shieldParticles);
            SetField(ubuntuVisual, "forwardArrow", arrowPivot.transform);
            SetField(ubuntuVisual, "groundGlow", groundGlowRenderer);
            SetField(ubuntuVisual, "pulseLight", pulseLight);
            SetField(ubuntuVisual, "shieldHum", shieldHum);
            SetField(ubuntuVisual, "shieldMaterial", shieldRenderer.sharedMaterial);
            SetField(ubuntuVisual, "vfxMaterial", particleMaterial);
            SetField(ubuntuVisual, "trailMaterial", UnlitTransparentMaterial("UbuntuTrailGlow", new Color(0.5f, 0.85f, 1f, 0.6f)));
            SetField(ubuntuVisual, "groundGlowMaterial", groundGlowMaterial);
            SetField(ubuntuVisual, "impactClip", AssetDatabase.LoadAssetAtPath<AudioClip>(UbuntuPulseImpactClipPath));
            SetField(ubuntuVisual, "powerDownClip", AssetDatabase.LoadAssetAtPath<AudioClip>(UbuntuPulsePowerDownClipPath));
            SetField(ubuntuVisual, "shieldScale", 1.12f);
            SetField(ubuntuVisual, "bloomIntensity", 1.8f);
            SetField(ubuntuVisual, "shieldOpacity", 0.24f);
            SetField(ubuntuVisual, "ringHeight", 0.78f);
            SetField(ubuntuVisual, "ringVerticalSeparation", 0.14f);
            SetField(ubuntuVisual, "ringRadiusMultiplier", 0.62f);
            SetField(ubuntuVisual, "particleCount", 28);
            SetField(ubuntuVisual, "trailWidth", 0.09f);
            SetField(ubuntuVisual, "lightningFrequency", 4f);
            SetField(ubuntuVisual, "maxCoinTrails", 8);
            SetField(ubuntuVisual, "lightBaseIntensity", 1.4f);
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

            if (BuildRunnerVisual(modelPath, playerRoot, "RunnerVisual_Fbx", RunnerPbrMaterialPath, RunnerAnimatorPath) == null)
            {
                return false;
            }

            // Second selectable character for the ME page. The game stays fully
            // playable without it — the ME page then offers a single choice.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SecondRunnerModelPath) != null &&
                BuildRunnerVisual(SecondRunnerModelPath, playerRoot, "RunnerVisual_Char1", SecondRunnerPbrMaterialPath, SecondRunnerAnimatorPath) == null)
            {
                Debug.LogWarning($"Second runner model at {SecondRunnerModelPath} could not be built; ME page offers only the default character.");
            }

            return true;
        }

        /// <summary>
        /// Builds one playable character visual under the player root from a
        /// rigged humanoid FBX: humanoid import, its own animator controller
        /// around its run clip (idle/jump/roll clips retarget across avatars),
        /// normalized scale, textures, and the per-visual presentation stack.
        /// Returns null when the model or its run clip cannot be loaded.
        /// </summary>
        static GameObject BuildRunnerVisual(string modelPath, Transform playerRoot, string visualName, string materialPath, string animatorPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                return null;
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

            // Pick the run take. A single-animation export has one clip, but a
            // merged-animation export bundles several (run, walk, idle, and a
            // few-frame base pose); grabbing the first would sometimes land on
            // the base pose or a walk. Prefer a clip that reads as a run, else
            // the longest meaningful clip.
            var animClips = new List<AnimationClip>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview"))
                {
                    animClips.Add(clip);
                }
            }

            AnimationClip runClip = null;
            foreach (AnimationClip clip in animClips)
            {
                if (clip.length > 0.2f && clip.name.ToLowerInvariant().Contains("run"))
                {
                    runClip = clip;
                    break;
                }
            }

            if (runClip == null)
            {
                foreach (AnimationClip clip in animClips)
                {
                    if (clip.length > 0.2f && (runClip == null || clip.length > runClip.length))
                    {
                        runClip = clip;
                    }
                }
            }

            if (runClip == null && animClips.Count > 0)
            {
                runClip = animClips[0];
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null || runClip == null)
            {
                Debug.LogWarning($"Found runner model at {modelPath} but could not load a run clip; using primitive runner.");
                return null;
            }

            GameObject visual = Object.Instantiate(model, playerRoot);
            visual.name = visualName;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = CreateRunnerAnimatorController(runClip, animatorPath);
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
                && !TryApplyExternalPbrTextures(visual, modelPath, materialPath))
            {
                RemapUntexturedRunnerMaterials(visual);
            }

            AddRunnerPresentation(visual, null);

            Debug.Log($"Player visual using rigged model '{modelPath}' with clip '{runClip.name}' (textured: {hasTextures}, triangles: {CountTriangles(visual)}).");
            return visual;
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
        /// Builds the JRPD traffic officer NPC: a rigged Meshy biped looping its
        /// run clip, hidden until TrafficOfficerChase shows him after a taxi
        /// side-swipe. Returns null when the model is missing so side bumps
        /// simply stay fatal.
        /// </summary>
        static TrafficOfficerChase CreateTrafficOfficer(Transform player, GameManager gameManager)
        {
            AnimationClip runClip = LoadRunnerClipFromModel(OfficerModelPath, loop: true);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(OfficerModelPath);
            if (model == null || runClip == null)
            {
                Debug.LogWarning($"Traffic officer model or clip missing at {OfficerModelPath}; taxi side bumps stay fatal.");
                return null;
            }

            GameObject officerObject = new GameObject("TrafficOfficer");
            // Parked behind the run start until a chase teleports him into view.
            officerObject.transform.position = new Vector3(0f, 0f, -40f);

            TrafficOfficerChase chase = officerObject.AddComponent<TrafficOfficerChase>();
            SetField(chase, "player", player);
            SetField(chase, "gameManager", gameManager);

            GameObject visual = Object.Instantiate(model, officerObject.transform);
            visual.name = "OfficerVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = CreateOfficerAnimatorController(runClip);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.updateWhenOffscreen = true;
            }

            NormalizeRunnerScale(visual, officerObject.transform);
            SimplifyMeshes(visual, 50000);

            ModelImporter importer = AssetImporter.GetAtPath(OfficerModelPath) as ModelImporter;
            bool hasTextures = importer != null && ExtractEmbeddedTextures(importer, OfficerModelPath);
            if (!hasTextures)
            {
                TryApplyExternalPbrTextures(visual, OfficerModelPath, OfficerPbrMaterialPath);
            }

            foreach (Collider collider in officerObject.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            SetField(chase, "officerVisual", visual);
            Debug.Log($"Traffic officer built from '{OfficerModelPath}' with clip '{runClip.name}' (triangles: {CountTriangles(visual)}).");
            return chase;
        }

        static RuntimeAnimatorController CreateOfficerAnimatorController(AnimationClip runClip)
        {
            Directory.CreateDirectory("Assets/Animations");
            AssetDatabase.DeleteAsset(OfficerAnimatorPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(OfficerAnimatorPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState runState = stateMachine.AddState("Run", new Vector3(260f, 100f, 0f));
            runState.motion = runClip;
            stateMachine.defaultState = runState;
            return controller;
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
        static bool TryApplyExternalPbrTextures(GameObject visual, string modelPath, string materialPath = RunnerPbrMaterialPath)
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

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
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

                // Vehicle models are scenery/obstacles and the traffic officer
                // is an NPC: none of them may become the playable character.
                string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (fileName.Contains("taxi") || fileName.Contains("bus") || fileName.Contains("car")
                    || fileName.Contains("vehicle") || fileName.Contains("officer"))
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

        static RuntimeAnimatorController CreateRunnerAnimatorController(AnimationClip runClip, string controllerPath)
        {
            Directory.CreateDirectory("Assets/Animations");
            AssetDatabase.DeleteAsset(controllerPath);
            // Idle plays the real Meshy dance clip (looping) as the pre-run showcase.
            AnimationClip idleClip = LoadRunnerClipFromModel(RunnerIdleModelPath, loop: true)
                ?? CreateGeneratedClip(RunnerIdleClipPath, 1f);
            // Jump, roll and air-roll come from the combined jumpandroll.fbx take,
            // carved into a takeoff→landing Jump clip, a landing→recovery Roll clip
            // and the whole dodge for AirRoll (same rig, retargeted). Each falls
            // back to the previous per-clip source (or a generated stub) if the
            // combined model or a sub-clip is missing.
            LoadRunnerJumpRollClips(out AnimationClip jumpRollJump, out AnimationClip jumpRollRoll, out AnimationClip jumpRollFull);
            // Plain jump (swipe up) uses the dedicated clean jump-over clip — NOT
            // the jumpandroll aerial flip, whose mid-air somersault reads as a roll.
            // The combined jump+roll take is reserved for the AirRoll state (swipe
            // down while airborne), so a plain hop shows a plain jump and only a
            // deliberate jump-and-roll shows the somersault.
            AnimationClip jumpClip = LoadRunnerClipFromModel(RunnerJumpModelPath, loop: false)
                ?? jumpRollJump
                ?? CreateGeneratedClip(RunnerJumpClipPath, 0.45f);
            AnimationClip airRollClip = jumpRollFull
                ?? LoadRunnerClipFromModel(RunnerAirRollModelPath, loop: false)
                ?? CreateGeneratedClip(RunnerRollClipPath, 0.7f);
            // Swipe-down roll: the dedicated forward-roll clip (its 0→360°
            // somersault baked into the pose). Falls back to the jumpandroll roll
            // portion, then a generated stub.
            AnimationClip rollClip = LoadRunnerBakedRootClip(RunnerRollForwardModelPath, loop: false, bakeForwardTravel: false)
                ?? jumpRollRoll
                ?? CreateGeneratedClip(RunnerRollClipPath, 0.7f);
            // Looping horizontal fly clip held while the Drone Boost is active
            // (the runner soars superman-style). Falls back to the old Falling
            // clip, then the jump pose, if the source model is unavailable.
            AnimationClip fallClip = LoadRunnerFlyClip()
                ?? LoadRunnerClipFromModel(RunnerFallModelPath, loop: true)
                ?? jumpClip;

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
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
            // Island state entered/exited only by DroneFlightVisual via a direct
            // Animator.Play — no parameter transitions so it loops undisturbed
            // while the drone carries the runner.
            AnimatorState fallState = stateMachine.AddState("Fall", new Vector3(780f, 50f, 0f));

            idleState.motion = idleClip;
            runState.motion = runClip;
            jumpState.motion = jumpClip;
            rollState.motion = rollClip;
            airRollState.motion = airRollClip;
            fallState.motion = fallClip;

            // Sync the Jump clip's playback to the physics airtime so its landing
            // pose arrives exactly at touchdown. The hop keeps the runner airborne
            // ~0.79s (PlayerController jumpHeight 2.2, gravity -28:
            // airtime = 2·√(h·2·|g|)/|g|); at state speed 1 a clip longer than that
            // has the runner touch down mid-air-pose and snap into the run — a hitch
            // on landing. Scale so clip length maps onto the airtime. Clamp so a
            // short clip is never slowed below 1×.
            const float jumpHeight = 2.2f; // mirrors PlayerController.jumpHeight
            const float jumpGravity = 28f; // |PlayerController.gravity|
            float jumpAirtime = 2f * Mathf.Sqrt(jumpHeight * 2f * jumpGravity) / jumpGravity;
            if (jumpClip != null && jumpClip.length > 0.01f && jumpAirtime > 0.01f)
            {
                jumpState.speed = Mathf.Clamp(jumpClip.length / jumpAirtime, 1f, 2f);
            }

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

            // Return to the run as the roll's recovery finishes, not after the
            // whole clip: Roll_Forward's last frames settle into a near-standing
            // recovery pose, so the old exitTime 0.95 + 0.05s snap left the runner
            // briefly static after the somersault before the run picked back up —
            // the pause felt after a roll. RollController's gameplay roll lasts
            // 0.7s, so begin the cross-fade to Run at that point and blend over a
            // wider window so the run overlaps the recovery tail instead of
            // waiting it out.
            const float rollGameplayDuration = 0.7f; // mirrors RollController.rollDuration
            AnimatorStateTransition rollToRun = rollState.AddTransition(runState);
            rollToRun.hasExitTime = true;
            rollToRun.exitTime = (rollClip != null && rollClip.length > 0.01f)
                ? Mathf.Clamp01(rollGameplayDuration / rollClip.length)
                : 0.75f;
            rollToRun.duration = 0.18f;

            AnimatorStateTransition airRollToRun = airRollState.AddTransition(runState);
            airRollToRun.hasExitTime = true;
            airRollToRun.exitTime = 0.9f;
            airRollToRun.duration = 0.08f;

            Debug.Log($"Runner animator clips — idle: '{idleClip.name}' ({idleClip.length:0.00}s), " +
                $"jump: '{jumpClip.name}' ({jumpClip.length:0.00}s), " +
                $"roll: '{rollClip.name}' ({rollClip.length:0.00}s), " +
                $"airRoll: '{airRollClip.name}' ({airRollClip.length:0.00}s), " +
                $"fall: '{fallClip.name}' ({fallClip.length:0.00}s).");
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

        /// <summary>
        /// Imports the horizontal fly loop (Humanoid, looping) for the Drone Boost.
        /// The clip authors its flat body orientation into the skeleton (Hips
        /// pitched 90°), so its root rotation must be baked into the pose to survive
        /// the runner's root-motion-off Animator. It hovers in place, so forward
        /// travel is baked too (there is none to speak of).
        /// </summary>
        static AnimationClip LoadRunnerFlyClip()
        {
            return LoadRunnerBakedRootClip(RunnerFlyModelPath, loop: true, bakeForwardTravel: true);
        }

        /// <summary>
        /// Imports a Humanoid clip whose body rotation is authored into the Hips
        /// (the humanoid root), baking that root rotation into the pose so it shows
        /// on the runner's Animator (which has no applyRootMotion — without the bake
        /// the root rotation is discarded and the body snaps upright). Used for the
        /// fly loop (constant horizontal pose) and the forward roll (an animated
        /// 0→360° somersault). Root Y is baked so height stays put; forward XZ
        /// travel is baked only when <paramref name="bakeForwardTravel"/> is set —
        /// the roll leaves it as discarded root motion so it somersaults in place
        /// while the world scrolls under it. Returns null when the model is missing.
        /// </summary>
        static AnimationClip LoadRunnerBakedRootClip(string modelPath, bool loop, bool bakeForwardTravel)
        {
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Baked-root clip model not found at {modelPath}; caller will fall back.");
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
                clip.lockRootRotation = true;        // Bake Into Pose (rotation)
                clip.keepOriginalOrientation = true; // Based Upon: Original
                clip.lockRootHeightY = true;         // Bake Into Pose (position Y)
                clip.keepOriginalPositionY = true;
                clip.lockRootPositionXZ = bakeForwardTravel; // Bake Into Pose (position XZ)
                clip.keepOriginalPositionXZ = bakeForwardTravel;
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

            Debug.LogWarning($"No animation clip found in {modelPath}; caller will fall back.");
            return null;
        }

        /// <summary>
        /// Imports the combined jump-into-roll FBX (Humanoid, so it retargets onto
        /// the runner avatar) as three sub-clips carved from the single take by
        /// frame range: "Jump" (takeoff→landing), "Roll" (landing→recovery), and
        /// "JumpRollFull" (the whole dodge, for the AirRoll state). The Blender
        /// authoring range is remapped proportionally onto whatever frame range
        /// Unity assigns the imported take, so the split tracks the real IMPACT
        /// frame regardless of how the FBX is re-based on import. Returns false
        /// (leaving the out clips null) when the model or its take is missing.
        /// </summary>
        static bool LoadRunnerJumpRollClips(out AnimationClip jumpClip, out AnimationClip rollClip, out AnimationClip fullClip)
        {
            jumpClip = null;
            rollClip = null;
            fullClip = null;

            ModelImporter importer = AssetImporter.GetAtPath(RunnerJumpRollModelPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Jump/roll model not found at {RunnerJumpRollModelPath}; using per-clip fallbacks.");
                return false;
            }

            // Read the take's actual imported frame range from the default (take)
            // clip; this is the frame span the sub-clip ranges must live within.
            ModelImporterClipAnimation[] takes = importer.defaultClipAnimations;
            if (takes == null || takes.Length == 0)
            {
                Debug.LogWarning($"No animation take found in {RunnerJumpRollModelPath}; using per-clip fallbacks.");
                return false;
            }

            float takeStart = takes[0].firstFrame;
            float takeEnd = takes[0].lastFrame;
            float authorSpan = JumpRollAuthorEndFrame - JumpRollAuthorStartFrame;
            float impactNorm = (JumpRollAuthorImpactFrame - JumpRollAuthorStartFrame) / authorSpan;
            float splitFrame = Mathf.Lerp(takeStart, takeEnd, impactNorm);

            ModelImporterClipAnimation full = MakeClip("JumpRollFull", takeStart, takeEnd);
            ModelImporterClipAnimation jump = MakeClip("Jump", takeStart, splitFrame);
            ModelImporterClipAnimation roll = MakeClip("Roll", splitFrame, takeEnd);

            importer.animationType = ModelImporterAnimationType.Human;
            importer.clipAnimations = new[] { full, jump, roll };
            importer.SaveAndReimport();

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(RunnerJumpRollModelPath))
            {
                if (!(asset is AnimationClip clip) || clip.name.StartsWith("__preview"))
                {
                    continue;
                }

                if (clip.name == "Jump") jumpClip = clip;
                else if (clip.name == "Roll") rollClip = clip;
                else if (clip.name == "JumpRollFull") fullClip = clip;
            }

            if (jumpClip == null || rollClip == null || fullClip == null)
            {
                Debug.LogWarning($"Jump/roll sub-clips missing after reimport of {RunnerJumpRollModelPath} " +
                    $"(jump={jumpClip != null}, roll={rollClip != null}, full={fullClip != null}); using fallbacks for any missing.");
            }
            return true;
        }

        static ModelImporterClipAnimation MakeClip(string name, float firstFrame, float lastFrame)
        {
            return new ModelImporterClipAnimation
            {
                name = name,
                firstFrame = firstFrame,
                lastFrame = lastFrame,
                loopTime = false,
                lockRootRotation = false,
                keepOriginalOrientation = false,
                keepOriginalPositionY = false
            };
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
            Vector3 offset = new Vector3(0f, 2.25f, -4.9f);
            const float lookHeight = 1.05f;
            Vector3 idleOffset = new Vector3(0f, 1.2f, -4.95f);
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

        static void CreateSystems(
            Transform player, GameObject roadPrefab, TrackChunk[] chunkPrefabs,
            JunctionSpawnSettings junctionSettings)
        {
            GameObject gameManagerObject = new GameObject("GameManager");
            GameManager gameManager = gameManagerObject.AddComponent<GameManager>();

            AudioClip backgroundMusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BackgroundMusicClipPath);
            if (backgroundMusicClip != null)
            {
                GameObject backgroundMusicObject = new GameObject("BackgroundMusic");
                AudioSource backgroundMusicSource = backgroundMusicObject.AddComponent<AudioSource>();
                backgroundMusicSource.clip = backgroundMusicClip;
                backgroundMusicSource.loop = true;
                backgroundMusicSource.playOnAwake = false;
                // Well under the coin-collect (0.8) and lane-switch (0.6) SFX
                // volumes — Remix.mp3 is mastered louder than those short
                // clips, so 0.45 still read as competing with them in play.
                backgroundMusicSource.volume = 0.2f;
                backgroundMusicObject.AddComponent<BackgroundMusic>();
            }

            GameObject scoreManagerObject = new GameObject("ScoreManager");
            ScoreManager scoreManager = scoreManagerObject.AddComponent<ScoreManager>();
            SetField(scoreManager, "player", player);

            GameObject powerUpManagerObject = new GameObject("PowerUpManager");
            PowerUpManager powerUpManager = powerUpManagerObject.AddComponent<PowerUpManager>();
            SetField(powerUpManager, "player", player);
            SetField(powerUpManager, "gameManager", gameManager);
            SetField(scoreManager, "powerUpManager", powerUpManager);

            DroneFlightVisual droneVisual = player.GetComponent<DroneFlightVisual>();
            if (droneVisual != null)
            {
                SetField(droneVisual, "powerUpManager", powerUpManager);
            }

            UbuntuPulseVisual ubuntuVisual = player.GetComponent<UbuntuPulseVisual>();
            if (ubuntuVisual != null)
            {
                SetField(ubuntuVisual, "powerUpManager", powerUpManager);
            }

            HoverboardVisual hoverboardVisual = player.GetComponent<HoverboardVisual>();
            if (hoverboardVisual != null)
            {
                SetField(hoverboardVisual, "powerUpManager", powerUpManager);
            }

            GameObject roadSpawnerObject = new GameObject("Road");
            roadSpawnerObject.transform.SetParent(EnvironmentCategory("Road"));
            RoadSegmentSpawner roadSpawner = roadSpawnerObject.AddComponent<RoadSegmentSpawner>();
            SetField(roadSpawner, "player", player);
            SetField(roadSpawner, "roadSegmentPrefab", roadPrefab);
            SetField(roadSpawner, "junctionSettings", junctionSettings);
            // Recycle as soon as the tile's far edge is safely behind the trailing
            // camera. Its replacement is staged ~167 m ahead, behind the 123 m haze.
            SetField(roadSpawner, "recycleBehindDistance", 12f);
            // One segment sits behind the origin so the cameras (which trail the
            // player) never see past the road's near edge on tall screens.
            int[] openingDistricts = { 0, 0, 2, 1, 3, 0 };
            for (int i = 0; i < 7; i++)
            {
                GameObject segment = Object.Instantiate(roadPrefab, new Vector3(0f, 0f, (i - 1) * 30f), Quaternion.identity, roadSpawnerObject.transform);
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
            // Activate pooled situations well behind the 123 m roadside haze,
            // giving renderers/animators time to settle before they become visible.
            SetField(chunkManager, "spawnDistanceAhead", 155f);

            CameraFollow cameraFollow = Object.FindAnyObjectByType<CameraFollow>();
            if (cameraFollow != null)
            {
                SetField(cameraFollow, "gameManager", gameManager);
            }

            // Every character visual has its own IdleFacing (inactive ones
            // included), so wire them all, not just the first.
            foreach (IdleFacing idleFacing in player.GetComponentsInChildren<IdleFacing>(true))
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

            TrafficOfficerChase officerChase = CreateTrafficOfficer(player, gameManager);
            if (officerChase != null)
            {
                SetField(controller, "officerChase", officerChase);
            }
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
            SetField(traffic, "oncomingSpawnDistance", 155f);
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
            // Match width only: phones taller than 16:9 (20:9 flagships) must keep
            // the full 1080-unit layout width or the title and pickup strip crop
            // at the screen edges; extra height just adds breathing room.
            scaler.matchWidthOrHeight = 0f;
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

            TextMeshProUGUI scoreCaption = Text(scorePill.transform, "ScoreCaption", "SCORE", 20, TextAlignmentOptions.Left);
            scoreCaption.color = new Color(.72f, .78f, .88f);
            scoreCaption.characterSpacing = 3f;
            Anchor(scoreCaption.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -10f), new Vector2(180f, 30f));
            scoreCaption.rectTransform.pivot = new Vector2(0f, 1f);

            TextMeshProUGUI scoreText = Text(scorePill.transform, "ScoreText", "0", 50, TextAlignmentOptions.Left);
            scoreText.fontStyle = FontStyles.Bold;
            // Auto-size down for six-figure scores with a double-digit
            // multiplier, which otherwise wrap onto a second line.
            scoreText.enableAutoSizing = true;
            scoreText.fontSizeMax = 48f;
            scoreText.fontSizeMin = 24f;
            Anchor(scoreText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scoreText.rectTransform.offsetMin = new Vector2(34f, 4f);
            scoreText.rectTransform.offsetMax = new Vector2(-122f, -27f);

            GameObject multiplierBadge = RoundedPanel(scorePill.transform, "MultiplierBadge", new Color(1f, .58f, .08f, .95f), rounded, .3f);
            Anchor(multiplierBadge.GetComponent<RectTransform>(), new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-18f, 0f), new Vector2(88f, 62f));
            TextMeshProUGUI multiplierText = Text(multiplierBadge.transform, "MultiplierText", "×1", 31, TextAlignmentOptions.Center);
            multiplierText.fontStyle = FontStyles.Bold;
            Anchor(multiplierText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

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
            // Collected coins fly into this icon (CoinCollectPop reads the anchor)
            // and set off a sparkle on the counter as they land.
            coinIcon.gameObject.AddComponent<CoinHudAnchor>();
            coinIcon.gameObject.AddComponent<CoinCounterSparkle>();

            TextMeshProUGUI coinText = Text(coinPill.transform, "CoinText", "0", 48, TextAlignmentOptions.Right);
            coinText.fontStyle = FontStyles.Bold;
            coinText.color = gold;
            Anchor(coinText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            coinText.rectTransform.offsetMin = new Vector2(92f, 0f);
            coinText.rectTransform.offsetMax = new Vector2(-32f, 0f);

            TextMeshProUGUI powerUpStatus = Text(safeArea.transform, "PowerUpStatusText", "", 40, TextAlignmentOptions.Center);
            powerUpStatus.fontStyle = FontStyles.Bold;
            // Below the pause button (which spans roughly -32..-136) so active
            // booster labels never crowd or overlap it.
            Anchor(powerUpStatus.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -208f), new Vector2(960f, 56f));

            // Perfect Dodge label: hidden until PerfectDodge pops it on a
            // near miss; that component drives the scale/alpha animation.
            TextMeshProUGUI dodgeLabel = Text(safeArea.transform, "PerfectDodgeLabel", "PERFECT DODGE!", 66, TextAlignmentOptions.Center);
            dodgeLabel.fontStyle = FontStyles.Bold;
            dodgeLabel.color = new Color(1f, 0.84f, 0.35f, 1f);
            dodgeLabel.characterSpacing = 4f;
            Anchor(dodgeLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(900f, 90f));
            dodgeLabel.gameObject.SetActive(false);

            GameObject dodgePlayer = GameObject.Find("Player");
            PerfectDodge perfectDodge = dodgePlayer != null ? dodgePlayer.GetComponent<PerfectDodge>() : null;
            if (perfectDodge != null)
            {
                SetField(perfectDodge, "dodgeLabel", dodgeLabel);
            }

            // Zone/level banner ("LEVEL 1 · JOBURG CBD") intentionally not
            // created — route legs still change the environment, just without
            // the on-screen announcement.

            // Ubuntu Pulse HUD icon: hidden until active, then shows a
            // pulsing blue glow and a radial countdown ring. UbuntuPulseUI
            // lives on the always-active root and toggles the "Visuals"
            // child — Update() never runs on a disabled GameObject, so the
            // script itself must not be the thing that gets deactivated.
            GameObject ubuntuIconRoot = new GameObject("UbuntuPulseIcon");
            ubuntuIconRoot.transform.SetParent(safeArea.transform, false);
            RectTransform ubuntuIconRect = ubuntuIconRoot.AddComponent<RectTransform>();
            Anchor(ubuntuIconRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -152f), new Vector2(112f, 112f));

            GameObject ubuntuVisuals = new GameObject("Visuals");
            ubuntuVisuals.transform.SetParent(ubuntuIconRoot.transform, false);
            RectTransform ubuntuVisualsRect = ubuntuVisuals.AddComponent<RectTransform>();
            Anchor(ubuntuVisualsRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Centered soft halo behind the tile. Note the offsetMin/Max
            // assignments: passing expansion values through Anchor's
            // position/size parameters shifts a stretched rect off-centre —
            // that was the stray blue "crescent" poking out of the icon.
            Image ubuntuGlow = new GameObject("Glow").AddComponent<Image>();
            ubuntuGlow.transform.SetParent(ubuntuVisuals.transform, false);
            ubuntuGlow.sprite = knob;
            ubuntuGlow.color = new Color(0.35f, 0.75f, 1f, 0.4f);
            Anchor(ubuntuGlow.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ubuntuGlow.rectTransform.offsetMin = new Vector2(-10f, -10f);
            ubuntuGlow.rectTransform.offsetMax = new Vector2(10f, 10f);

            // Countdown reads as a thin horizontal bar under the tile — the
            // old radial pie sat behind the square icon sprite and only its
            // corners showed, which looked like a rendering glitch.
            Image ubuntuRingBackground = new GameObject("RingBackground").AddComponent<Image>();
            ubuntuRingBackground.transform.SetParent(ubuntuVisuals.transform, false);
            ubuntuRingBackground.sprite = knob;
            ubuntuRingBackground.color = new Color(0f, 0f, 0f, 0.5f);
            Anchor(ubuntuRingBackground.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -12f), new Vector2(0f, 10f));

            Image ubuntuRingFill = new GameObject("RingFill").AddComponent<Image>();
            ubuntuRingFill.transform.SetParent(ubuntuVisuals.transform, false);
            ubuntuRingFill.sprite = knob;
            ubuntuRingFill.color = new Color(0.45f, 0.85f, 1f, 1f);
            ubuntuRingFill.type = Image.Type.Filled;
            ubuntuRingFill.fillMethod = Image.FillMethod.Horizontal;
            ubuntuRingFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            ubuntuRingFill.fillAmount = 1f;
            Anchor(ubuntuRingFill.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -12f), new Vector2(0f, 10f));

            Image ubuntuIconImage = new GameObject("Icon").AddComponent<Image>();
            ubuntuIconImage.transform.SetParent(ubuntuVisuals.transform, false);
            ubuntuIconImage.preserveAspect = true;
            Anchor(ubuntuIconImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ubuntuIconImage.rectTransform.offsetMin = new Vector2(6f, 6f);
            ubuntuIconImage.rectTransform.offsetMax = new Vector2(-6f, -6f);
            Sprite ubuntuIconSprite = RenderPrefabIcon(UbuntuPulsePrefabPath, "IconUbuntuPulse", new Vector3(-16f, 28f, 0f));
            if (ubuntuIconSprite != null)
            {
                ubuntuIconImage.sprite = ubuntuIconSprite;
            }

            UbuntuPulseUI ubuntuPulseUi = ubuntuIconRoot.AddComponent<UbuntuPulseUI>();
            SetField(ubuntuPulseUi, "root", ubuntuVisuals);
            SetField(ubuntuPulseUi, "fillImage", ubuntuRingFill);
            SetField(ubuntuPulseUi, "glow", ubuntuGlow);
            ubuntuVisuals.SetActive(false);

            // Pause sits level with the score and coin pills, forming one top
            // row: same top (y -32) and height (104), centred in the gap between
            // them. The score pill (470) is wider than the coin pill (340), so
            // the gap centre is 65px right of the safe-area centre; because both
            // pills use fixed edge insets that offset holds at any safe-area
            // width. Taps do nothing until a run is underway (GameManager guards it).
            Button pauseUiButton = UiButton(safeArea.transform, "PauseButton", "II", 44, new Color(0f, 0f, 0f, 0.45f), rounded,
                new Vector2(0.5f, 1f), new Vector2(65f, -32f), new Vector2(110f, 104f));
            pauseUiButton.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            pauseUiButton.gameObject.AddComponent<PauseButton>();

            GameObject pausePanel = Panel(canvasObject.transform, "PausePanel", new Color(0f, 0f, 0f, 0.75f));
            Anchor(pausePanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject pauseCard = RoundedPanel(pausePanel.transform, "PauseCard", new Color(0.07f, 0.08f, 0.12f, 0.98f), rounded, 0.3f);
            Anchor(pauseCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 640f));

            TextMeshProUGUI pauseTitle = Text(pauseCard.transform, "PauseTitle", "PAUSED", 84, TextAlignmentOptions.Center);
            pauseTitle.fontStyle = FontStyles.Bold;
            pauseTitle.color = gold;
            pauseTitle.characterSpacing = 8f;
            Anchor(pauseTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(680f, 110f));

            Button resumeUiButton = UiButton(pauseCard.transform, "ResumeButton", "RESUME", 60, new Color(1f, 0.6f, 0.05f, 1f), rounded,
                new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(560f, 150f));
            resumeUiButton.gameObject.AddComponent<ResumeButton>();

            Button pauseMenuUiButton = UiButton(pauseCard.transform, "PauseMenuButton", "MENU", 50, new Color(0.25f, 0.28f, 0.35f, 1f), rounded,
                new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(560f, 115f));
            pauseMenuUiButton.gameObject.AddComponent<MenuButton>();

            // Game-over uses the same visual language as the power-up HUD:
            // smoky navy glass, electric-cyan progress accents and warm gold
            // reserved for rewards / primary actions.
            Color powerCyan = new Color(0.25f, 0.78f, 1f, 1f);
            Color powerGlass = new Color(0.025f, 0.04f, 0.075f, 0.97f);
            Color powerRow = new Color(0.065f, 0.09f, 0.14f, 0.98f);
            GameObject gameOverPanel = Panel(canvasObject.transform, "GameOverPanel", new Color(0.005f, 0.012f, 0.025f, 0.86f));
            Anchor(gameOverPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject cardFrame = RoundedPanel(gameOverPanel.transform, "CardFrame", new Color(.25f, .78f, 1f, .72f), rounded, 0.3f);
            Anchor(cardFrame.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(884f, 1164f));

            GameObject card = RoundedPanel(cardFrame.transform, "Card", powerGlass, rounded, 0.3f);
            Anchor(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(866f, 1146f));

            Image headerRing = new GameObject("StatusRing").AddComponent<Image>();
            headerRing.transform.SetParent(card.transform, false);
            headerRing.sprite = knob;
            headerRing.color = powerCyan;
            headerRing.type = Image.Type.Filled;
            headerRing.fillMethod = Image.FillMethod.Radial360;
            headerRing.fillAmount = 0.82f;
            Anchor(headerRing.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -92f), new Vector2(92f, 92f));

            TextMeshProUGUI statusIcon = Text(headerRing.transform, "StatusIcon", "!", 52, TextAlignmentOptions.Center);
            statusIcon.fontStyle = FontStyles.Bold;
            statusIcon.color = Color.white;
            Anchor(statusIcon.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TextMeshProUGUI gameOverTitle = Text(card.transform, "GameOverTitle", "RUN COMPLETE", 70, TextAlignmentOptions.Center);
            gameOverTitle.fontStyle = FontStyles.Bold;
            gameOverTitle.color = Color.white;
            gameOverTitle.characterSpacing = 6f;
            Anchor(gameOverTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(720f, 90f));

            Image titleUnderline = new GameObject("CyanAccent").AddComponent<Image>();
            titleUnderline.transform.SetParent(card.transform, false);
            titleUnderline.sprite = rounded;
            titleUnderline.color = powerCyan;
            Anchor(titleUnderline.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -280f), new Vector2(190f, 8f));

            Button closeGameOverButton = UiButton(card.transform, "CloseButton", "×", 54,
                powerRow, rounded, new Vector2(1f, 1f), new Vector2(-26f, -26f), new Vector2(96f, 96f));
            closeGameOverButton.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            closeGameOverButton.GetComponentInChildren<TextMeshProUGUI>().color = powerCyan;
            closeGameOverButton.gameObject.AddComponent<GameOverCloseButton>();

            GameObject scoreCard = RoundedPanel(card.transform, "ScoreCard", powerRow, rounded, 0.25f);
            Anchor(scoreCard.GetComponent<RectTransform>(), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -445f), new Vector2(726f, 380f));
            TextMeshProUGUI finalScore = Text(scoreCard.transform, "FinalScoreText", "Final score: 0", 44, TextAlignmentOptions.Center);
            Anchor(finalScore.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Button continueUiButton = UiButton(card.transform, "ContinueButton", "CONTINUE  ·  1 R5", 50, gold, rounded,
                new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(650f, 126f));
            TextMeshProUGUI continueLabel = continueUiButton.GetComponentInChildren<TextMeshProUGUI>();
            continueLabel.color = new Color(0.16f, 0.12f, 0.03f);
            ContinueButton continueButton = continueUiButton.gameObject.AddComponent<ContinueButton>();

            Button restartUiButton = UiButton(card.transform, "RestartButton", "RESTART", 52, powerCyan, rounded,
                new Vector2(0.5f, 0f), new Vector2(-167f, 42f), new Vector2(316f, 104f));
            restartUiButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(.02f, .08f, .13f, 1f);
            RestartButton restartButton = restartUiButton.gameObject.AddComponent<RestartButton>();

            Button menuUiButton = UiButton(card.transform, "MenuButton", "MENU", 46, powerRow, rounded,
                new Vector2(0.5f, 0f), new Vector2(167f, 42f), new Vector2(316f, 104f));
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
                LoadHiggsfieldSprite("PU_DoubleCoins") ?? RenderPrefabIcon("Assets/Prefabs/PowerUpDoubleCoins.prefab", "IconDoubleCoins", new Vector3(0f, 25f, 0f)),
                ubuntuIconSprite ?? RenderPrefabIcon(UbuntuPulsePrefabPath, "IconUbuntuPulse", new Vector3(-16f, 28f, 0f)),
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

            // Showcase strip mirrors the in-run HUD language: compact dark
            // tiles, circular cyan timer rings and centred recognisable icons.
            Sprite[] showcase = { pickupIcons[0], pickupIcons[1], pickupIcons[2], pickupIcons[3], pickupIcons[4], pickupIcons[5], pickupIcons[6], r5Icon };
            for (int i = 0; i < showcase.Length; i++)
            {
                GameObject chip = RoundedPanel(menuPanel.transform, $"ShowcaseChip{i}", new Color(.025f,.04f,.075f,.88f), rounded, 0.25f);
                Anchor(chip.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2((i - (showcase.Length - 1) * 0.5f) * 118f, -590f), new Vector2(104f, 96f));
                Image ring = new GameObject("PowerRing").AddComponent<Image>();
                ring.transform.SetParent(chip.transform, false);
                ring.sprite = knob;
                ring.color = i == showcase.Length - 1 ? gold : new Color(.25f,.78f,1f,.8f);
                ring.type = Image.Type.Filled;
                ring.fillMethod = Image.FillMethod.Radial360;
                ring.fillAmount = 0.82f;
                Anchor(ring.rectTransform, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(82f,82f));
                IconImage(chip.transform, "Icon", showcase[i], new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62f, 62f));
            }

            Button playButton = UiButton(menuPanel.transform, "PlayButton", "PLAY", 74, buttonOrange, rounded,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(640f, 180f));
            Button storeButton = UiButton(menuPanel.transform, "StoreButton", "STORE", 52, rowDark, rounded,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(640f, 140f));
            Button collectablesButton = UiButton(menuPanel.transform, "CollectablesButton", "COLLECTABLES", 52, rowDark, rounded,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -430f), new Vector2(640f, 140f));
            Button meButton = UiButton(menuPanel.transform, "MeButton", "ME", 52, rowDark, rounded,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -600f), new Vector2(640f, 140f));
            Button boardsButton = UiButton(menuPanel.transform, "BoardsButton", "BOARDS", 52, rowDark, rounded,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -770f), new Vector2(640f, 140f));
            Button missionsButton = UiButton(menuPanel.transform, "MissionsButton", "MISSIONS", 52, rowDark, rounded,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -935f), new Vector2(640f, 140f));
            Button soundButton = UiButton(menuPanel.transform, "SoundButton", "SOUND: ON", 44, rowDark, rounded,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -1090f), new Vector2(640f, 110f));

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
                PowerUpType.DoubleCoins,
                PowerUpType.UbuntuPulse,
            };

            var storeItemLabels = new TextMeshProUGUI[storeOrder.Length];
            var storeUpgradeButtons = new Button[storeOrder.Length];
            var storeUpgradeLabels = new TextMeshProUGUI[storeOrder.Length];
            for (int i = 0; i < storeOrder.Length; i++)
            {
                GameObject row = RoundedPanel(storePanel.transform, $"StoreRow{i}", rowDark, rounded, 0.25f);
                Anchor(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f - i * 170f), new Vector2(960f, 165f));

                IconImage(row.transform, "ItemIcon", pickupIcons[i], new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(132f, 132f));

                TextMeshProUGUI itemLabel = Text(row.transform, "ItemLabel",
                    $"<b>{PowerUpManager.DisplayName(storeOrder[i])}</b>  <color=#FFC845>Lv 1/{PowerUpManager.MaxUpgradeLevel}</color>\n" +
                    $"<size=30><color=#AEB4C2>Active {PowerUpManager.Duration(storeOrder[i], 1):0}s -> {PowerUpManager.Duration(storeOrder[i], 2):0}s · Upgrade {BoosterUpgradeConfig.UpgradeCost(storeOrder[i], 1)} coins</color></size>",
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
            // Shifted down one row-height (170) from the original 6-row layout
            // to make room for the 7th store row (Ubuntu Pulse).
            Anchor(specialsHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1500f), new Vector2(700f, 50f));

            string[] specialNames = { "Head Start", "Shield Start", "Pulse Start" };
            string[] specialDescriptions = { "Begin your next run flying the Drone", "Begin your next run with a Hoverboard shield", "Begin your next run with Ubuntu Pulse active" };
            int[] specialCosts = { 1, 2, 3 };
            Sprite[] specialIcons = { pickupIcons[2], pickupIcons[4], pickupIcons[6] };

            var specialItemLabels = new TextMeshProUGUI[specialNames.Length];
            var specialBuyButtons = new Button[specialNames.Length];
            var specialBuyLabels = new TextMeshProUGUI[specialNames.Length];
            for (int i = 0; i < specialNames.Length; i++)
            {
                GameObject row = RoundedPanel(storePanel.transform, $"SpecialRow{i}", rowDark, rounded, 0.25f);
                Anchor(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1590f - i * 168f), new Vector2(960f, 160f));

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
                new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(400f, 110f));

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

            // ---------------- Me (character select) ----------------
            GameObject mePanel = Panel(canvasObject.transform, "MePanel", panelDark);
            Anchor(mePanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TextMeshProUGUI meTitle = Text(mePanel.transform, "MeTitle", "ME", 84, TextAlignmentOptions.Center);
            meTitle.fontStyle = FontStyles.Bold;
            meTitle.color = gold;
            meTitle.characterSpacing = 8f;
            Anchor(meTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(600f, 110f));

            TextMeshProUGUI meSubtitle = Text(mePanel.transform, "MeSubtitle", "CHOOSE YOUR RUNNER", 40, TextAlignmentOptions.Center);
            meSubtitle.color = new Color(0.85f, 0.87f, 0.92f);
            meSubtitle.characterSpacing = 4f;
            Anchor(meSubtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(700f, 60f));

            // One card per character visual actually built on the player.
            CharacterSelector characterSelector = Object.FindAnyObjectByType<CharacterSelector>();
            string[] characterNames = { "Mgijimi", "Themba" };
            string[] characterTaglines =
            {
                "The original Jozi street runner",
                "Soweto marathon breakaway",
            };
            // Portraits are rendered from the scene visuals — the raw FBX assets
            // have Meshy's blank materials; only the built visuals carry the real
            // textures. Non-default visuals are saved inactive, so search the
            // player hierarchy including inactive objects.
            var characterIconSources = new GameObject[characterNames.Length];
            PlayerController playerForIcons = Object.FindAnyObjectByType<PlayerController>();
            if (playerForIcons != null)
            {
                foreach (Transform child in playerForIcons.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "RunnerVisual_Fbx")
                    {
                        characterIconSources[0] = child.gameObject;
                    }
                    else if (child.name == "RunnerVisual_Char1")
                    {
                        characterIconSources[1] = child.gameObject;
                    }
                }
            }
            int characterCount = Mathf.Clamp(
                characterSelector != null ? characterSelector.CharacterCount : 1, 1, characterNames.Length);

            var characterSelectButtons = new Button[characterCount];
            var characterSelectLabels = new TextMeshProUGUI[characterCount];
            for (int i = 0; i < characterCount; i++)
            {
                GameObject row = RoundedPanel(mePanel.transform, $"CharacterRow{i}", rowDark, rounded, 0.25f);
                Anchor(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -450f - i * 320f), new Vector2(960f, 300f));

                Sprite portrait = characterIconSources[i] != null
                    ? RenderInstanceIcon(Object.Instantiate(characterIconSources[i]), $"IconRunner{i}", new Vector3(0f, 180f, 0f))
                    : null;
                IconImage(row.transform, "Portrait", portrait, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(244f, 244f));

                TextMeshProUGUI cardLabel = Text(row.transform, "CardLabel",
                    $"<b>{characterNames[i].ToUpperInvariant()}</b>\n<size=32><color=#AEB4C2>{characterTaglines[i]}</color></size>",
                    46, TextAlignmentOptions.Left);
                Anchor(cardLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                cardLabel.rectTransform.offsetMin = new Vector2(310f, 0f);
                cardLabel.rectTransform.offsetMax = new Vector2(-270f, 0f);

                Button selectButton = UiButton(row.transform, "SelectButton", "SELECT", 40, buttonOrange, rounded,
                    new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(210f, 112f));
                characterSelectButtons[i] = selectButton;
                characterSelectLabels[i] = selectButton.GetComponentInChildren<TextMeshProUGUI>();
            }

            Button meBackButton = UiButton(mePanel.transform, "MeBackButton", "BACK", 48, buttonGrey, rounded,
                new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(400f, 120f));

            // ---------------- Boards (hoverboard boosters + skin select) ----------------
            GameObject boardsPanel = Panel(canvasObject.transform, "BoardsPanel", panelDark);
            Anchor(boardsPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TextMeshProUGUI boardsTitle = Text(boardsPanel.transform, "BoardsTitle", "BOARDS", 84, TextAlignmentOptions.Center);
            boardsTitle.fontStyle = FontStyles.Bold;
            boardsTitle.color = gold;
            boardsTitle.characterSpacing = 8f;
            Anchor(boardsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(600f, 110f));

            TextMeshProUGUI boardsSubtitle = Text(boardsPanel.transform, "BoardsSubtitle", "DOUBLE-TAP DURING A RUN TO RIDE", 40, TextAlignmentOptions.Center);
            boardsSubtitle.color = new Color(0.85f, 0.87f, 0.92f);
            boardsSubtitle.characterSpacing = 4f;
            Anchor(boardsSubtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(900f, 60f));

            // Booster wallet row: owned count plus the coin-priced BUY button.
            GameObject boosterRow = RoundedPanel(boardsPanel.transform, "BoosterRow", rowDark, rounded, 0.25f);
            Anchor(boosterRow.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(960f, 165f));

            IconImage(boosterRow.transform, "ItemIcon", pickupIcons[4], new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(132f, 132f));

            TextMeshProUGUI boardsOwnedLabel = Text(boosterRow.transform, "OwnedLabel",
                "<b>Hoverboard Boosters</b>  <color=#FFC845>Owned x0</color>\n" +
                "<size=30><color=#AEB4C2>Double-tap during a run to ride · absorbs one crash</color></size>",
                40, TextAlignmentOptions.Left);
            Anchor(boardsOwnedLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            boardsOwnedLabel.rectTransform.offsetMin = new Vector2(180f, 0f);
            boardsOwnedLabel.rectTransform.offsetMax = new Vector2(-270f, 0f);

            Button boardBuyButton = UiButton(boosterRow.transform, "BuyButton", $"{MenuController.BoardBoosterCost}", 46, buttonOrange, rounded,
                new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(210f, 112f));

            // One selectable card per board built on the player's mount, in the
            // same index order as BoardNames. Portraits render from the scene
            // visual (the raw Meshy FBX would render untextured); the visuals
            // are saved inactive, so search including inactive objects.
            Transform boardsMount = null;
            PlayerController playerForBoards = Object.FindAnyObjectByType<PlayerController>();
            if (playerForBoards != null)
            {
                foreach (Transform child in playerForBoards.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "HoverboardMount")
                    {
                        boardsMount = child;
                        break;
                    }
                }
            }

            int boardCount = Mathf.Clamp(boardsMount != null ? boardsMount.childCount : 0, 0, BoardNames.Length);
            var boardSelectButtons = new Button[boardCount];
            var boardSelectLabels = new TextMeshProUGUI[boardCount];
            for (int i = 0; i < boardCount; i++)
            {
                GameObject row = RoundedPanel(boardsPanel.transform, $"BoardRow{i}", rowDark, rounded, 0.25f);
                Anchor(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -580f - i * 320f), new Vector2(960f, 300f));

                Sprite portrait = RenderInstanceIcon(
                    Object.Instantiate(boardsMount.GetChild(i).gameObject), $"IconBoard{i}", new Vector3(25f, 40f, 0f));
                IconImage(row.transform, "Portrait", portrait, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(244f, 244f));

                TextMeshProUGUI cardLabel = Text(row.transform, "CardLabel",
                    $"<b>{BoardNames[i].ToUpperInvariant()}</b>\n<size=32><color=#AEB4C2>{BoardTaglines[i]}</color></size>",
                    46, TextAlignmentOptions.Left);
                Anchor(cardLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                cardLabel.rectTransform.offsetMin = new Vector2(310f, 0f);
                cardLabel.rectTransform.offsetMax = new Vector2(-270f, 0f);

                Button selectButton = UiButton(row.transform, "SelectButton", "SELECT", 40, buttonOrange, rounded,
                    new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(210f, 112f));
                boardSelectButtons[i] = selectButton;
                boardSelectLabels[i] = selectButton.GetComponentInChildren<TextMeshProUGUI>();
            }

            Button boardsBackButton = UiButton(boardsPanel.transform, "BoardsBackButton", "BACK", 48, buttonGrey, rounded,
                new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(400f, 120f));

            // ---------------- Missions & daily rewards ----------------
            GameObject missionsPanel = Panel(canvasObject.transform, "MissionsPanel", panelDark);
            Anchor(missionsPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TextMeshProUGUI missionsTitle = Text(missionsPanel.transform, "MissionsTitle", "MISSIONS", 84, TextAlignmentOptions.Center);
            missionsTitle.fontStyle = FontStyles.Bold;
            missionsTitle.color = gold;
            missionsTitle.characterSpacing = 8f;
            Anchor(missionsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(700f, 110f));

            // Daily-reward claim button up top (retention hook).
            Button claimRewardButton = UiButton(missionsPanel.transform, "ClaimRewardButton", "DAILY REWARD", 44, buttonOrange, rounded,
                new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(820f, 120f));
            TextMeshProUGUI claimRewardLabel = claimRewardButton.GetComponentInChildren<TextMeshProUGUI>();

            GameObject missionsCard = RoundedPanel(missionsPanel.transform, "MissionsCard", rowDark, rounded, 0.3f);
            Anchor(missionsCard.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -370f), new Vector2(920f, 900f));

            TextMeshProUGUI missionsText = Text(missionsCard.transform, "MissionsText",
                "<size=40><color=#AEB4C2>TODAY'S MISSIONS</color></size>", 40, TextAlignmentOptions.Top);
            Anchor(missionsText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            missionsText.rectTransform.offsetMin = new Vector2(48f, 40f);
            missionsText.rectTransform.offsetMax = new Vector2(-48f, -40f);

            Button missionsBackButton = UiButton(missionsPanel.transform, "MissionsBackButton", "BACK", 48, buttonGrey, rounded,
                new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(400f, 120f));

            // ---------------- Wiring ----------------
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            ScoreManager scoreManager = Object.FindAnyObjectByType<ScoreManager>();
            PowerUpManager powerUpManager = Object.FindAnyObjectByType<PowerUpManager>();
            SetField(gameManager, "gameOverPanel", gameOverPanel);
            SetField(gameManager, "pausePanel", pausePanel);
            SetField(gameManager, "finalScoreText", finalScore);
            SetField(scoreManager, "scoreText", scoreText);
            SetField(scoreManager, "multiplierText", multiplierText);
            SetField(scoreManager, "coinText", coinText);
            powerUpStatus.gameObject.SetActive(false);
            PowerUpHudController compactPowerHud = safeArea.AddComponent<PowerUpHudController>();
            compactPowerHud.Configure(powerUpManager, pickupIcons, rounded);
            SetField(powerUpManager, "coinAttractionClip", AssetDatabase.LoadAssetAtPath<AudioClip>(UbuntuPulseCoinAttractionClipPath));
            SetField(ubuntuPulseUi, "powerUpManager", powerUpManager);
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
            SetField(menuController, "mePanel", mePanel);
            SetField(menuController, "boardsPanel", boardsPanel);
            SetField(menuController, "playButton", playButton);
            SetField(menuController, "storeButton", storeButton);
            SetField(menuController, "collectablesButton", collectablesButton);
            SetField(menuController, "meButton", meButton);
            SetField(menuController, "boardsButton", boardsButton);
            SetField(menuController, "missionsButton", missionsButton);
            SetField(menuController, "soundButton", soundButton);
            SetField(menuController, "storeBackButton", storeBackButton);
            SetField(menuController, "collectablesBackButton", collectablesBackButton);
            SetField(menuController, "meBackButton", meBackButton);
            SetField(menuController, "boardsBackButton", boardsBackButton);
            SetField(menuController, "missionsBackButton", missionsBackButton);
            SetField(menuController, "missionsPanel", missionsPanel);
            SetField(menuController, "missionsText", missionsText);
            SetField(menuController, "claimRewardButton", claimRewardButton);
            SetField(menuController, "claimRewardLabel", claimRewardLabel);
            SetField(menuController, "characterSelectButtons", characterSelectButtons);
            SetField(menuController, "characterSelectLabels", characterSelectLabels);
            SetField(menuController, "boardsOwnedText", boardsOwnedLabel);
            SetField(menuController, "boardBuyButton", boardBuyButton);
            SetField(menuController, "boardSelectButtons", boardSelectButtons);
            SetField(menuController, "boardSelectLabels", boardSelectLabels);
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
            pausePanel.SetActive(false);
            menuPanel.SetActive(false);
            storePanel.SetActive(false);
            collectablesPanel.SetActive(false);
            mePanel.SetActive(false);
            boardsPanel.SetActive(false);
            missionsPanel.SetActive(false);
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

            return RenderInstanceIcon((GameObject)PrefabUtility.InstantiatePrefab(prefab), iconName, modelEuler);
        }

        /// <summary>
        /// Renders (and consumes) a staged instance to an icon sprite. Use with
        /// a clone of a scene object when the raw asset would render untextured
        /// — e.g. character FBXes, whose materials are only built on the scene
        /// visual — otherwise prefer RenderPrefabIcon.
        /// </summary>
        static Sprite RenderInstanceIcon(GameObject instance, string iconName, Vector3 modelEuler)
        {
            bool previousAsyncCompilation = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;

            // Stage the model far above the scene so nothing else is in frame.
            // Clones of disabled scene objects (e.g. non-default characters)
            // arrive inactive and would render an empty icon.
            instance.SetActive(true);
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
            // Camera.Render() produces a flat placeholder when Unity is run
            // with -nographics. Never let an automated headless build replace
            // a previously valid UI icon with that blank image.
            bool hasVisibleContent = HasVisibleIconContent(texture);
            if (hasVisibleContent)
            {
                File.WriteAllBytes(pngPath, texture.EncodeToPNG());
            }
            else
            {
                Debug.LogWarning($"Icon '{iconName}' rendered blank; preserving the existing icon asset.");
            }

            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(instance);

            if (!File.Exists(pngPath))
            {
                return null;
            }

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

        static bool HasVisibleIconContent(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            if (pixels.Length == 0)
            {
                return false;
            }

            Color32 background = pixels[0];
            int differentPixels = 0;
            foreach (Color32 pixel in pixels)
            {
                int difference = Mathf.Abs(pixel.r - background.r)
                    + Mathf.Abs(pixel.g - background.g)
                    + Mathf.Abs(pixel.b - background.b);
                if (difference > 18)
                {
                    differentPixels++;
                }
            }

            // Ignore tiny render artifacts; a real model occupies a meaningful
            // portion of the 256px card image.
            return differentPixels > pixels.Length / 200;
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
