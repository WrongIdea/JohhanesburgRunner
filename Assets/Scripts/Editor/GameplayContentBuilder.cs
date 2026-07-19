using JoburgRunner.Obstacles;
using JoburgRunner.VFX;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// One-shot generator for example Priority-2 (VFX) and Priority-3
    /// (obstacle) ScriptableObjects, built from prefabs/audio the scene builder
    /// already produces. Additive and idempotent — overwrites the example
    /// assets in place and touches nothing in the live scene.
    /// </summary>
    public static class GameplayContentBuilder
    {
        const string VfxFolder = "Assets/GameplayContent/VFX";
        const string ObstacleFolder = "Assets/GameplayContent/Obstacles";

        [MenuItem("Joburg Runner/Generate Example Gameplay Content")]
        public static void Generate()
        {
            EnsureFolder("Assets/GameplayContent");
            EnsureFolder(VfxFolder);
            EnsureFolder(ObstacleFolder);

            GenerateVfxDefinitions();
            GenerateObstacleContent();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated example VFX definitions + obstacle catalog under Assets/GameplayContent.");
        }

        static void GenerateVfxDefinitions()
        {
            VFXDefinition coin = LoadOrCreate<VFXDefinition>($"{VfxFolder}/VFX_CoinCollect.asset");
            coin.trigger = GameplayVFXTrigger.CoinCollected;
            coin.prefab = LoadPrefab("Assets/Prefabs/CoinCollectPop.prefab");
            coin.lifetime = 0.45f;      // fast — coins are collected constantly
            coin.prewarm = 8;
            coin.maxInstances = 24;
            coin.sound = LoadAudio("Assets/Audio/CoinCollect.wav");
            coin.volume = 0.9f;
            coin.pitchJitter = 0.06f;
            coin.cameraFeedback = false; // no shake on coins — must stay readable
            EditorUtility.SetDirty(coin);

            VFXDefinition dodge = LoadOrCreate<VFXDefinition>($"{VfxFolder}/VFX_PerfectDodge.asset");
            dodge.trigger = GameplayVFXTrigger.PerfectDodge;
            dodge.prefab = LoadPrefab("Assets/Prefabs/PerfectDodgeVFX.prefab");
            dodge.lifetime = 0.5f;
            dodge.prewarm = 3;
            dodge.maxInstances = 4;
            dodge.cameraFeedback = true;
            dodge.fovPulse = 2.5f;       // brief punch-in
            dodge.positionalImpulse = new Vector3(0f, 0f, -0.05f);
            dodge.feedbackDuration = 0.12f;
            EditorUtility.SetDirty(dodge);

            VFXDefinition laneSwitch = LoadOrCreate<VFXDefinition>($"{VfxFolder}/VFX_LaneSwitch.asset");
            laneSwitch.trigger = GameplayVFXTrigger.LaneSwitch;
            laneSwitch.prefab = null;    // reuses the existing UbuntuLaneShift ribbon; sound-only example here
            laneSwitch.lifetime = 0.25f;
            laneSwitch.sound = LoadAudio("Assets/Audio/LaneSwitch.mp3");
            laneSwitch.volume = 0.7f;
            laneSwitch.pitchJitter = 0.08f;
            laneSwitch.cameraFeedback = false; // no camera tilt on lane switch
            EditorUtility.SetDirty(laneSwitch);
        }

        static void GenerateObstacleContent()
        {
            ObstacleDefinition barrier = LoadOrCreate<ObstacleDefinition>($"{ObstacleFolder}/Obstacle_ConstructionBarricade.asset");
            barrier.displayName = "Construction Barricade";
            barrier.category = ObstacleCategory.ConstructionBarricade;
            barrier.prefab = LoadPrefab("Assets/Prefabs/ConstructionBarrier.prefab");
            barrier.allowedLanes = 0b111;
            barrier.blockedLanes = 0b010; // occupies one lane
            barrier.requiredAction = RequiredAction.Avoid;
            barrier.minPlayerProgression = 0f;
            barrier.spawnWeight = 1.5f;
            barrier.safeGap = 12f;
            barrier.width = 2.4f;
            barrier.height = 1.4f;
            barrier.movement = MovementBehaviour.Static;
            barrier.perfectDodgeEligible = true;
            EditorUtility.SetDirty(barrier);

            ObstacleDefinition pothole = LoadOrCreate<ObstacleDefinition>($"{ObstacleFolder}/Obstacle_Pothole.asset");
            pothole.displayName = "Pothole";
            pothole.category = ObstacleCategory.Pothole;
            pothole.prefab = LoadPrefab("Assets/Prefabs/PotholeObstacle.prefab");
            pothole.allowedLanes = 0b111;
            pothole.blockedLanes = 0b100;
            pothole.requiredAction = RequiredAction.Avoid;
            pothole.minPlayerProgression = 150f; // eased in a little later
            pothole.spawnWeight = 1f;
            pothole.safeGap = 10f;
            pothole.width = 2.2f;
            pothole.height = 0.3f;
            pothole.movement = MovementBehaviour.Static;
            EditorUtility.SetDirty(pothole);

            ObstacleDefinition movingTaxi = LoadOrCreate<ObstacleDefinition>($"{ObstacleFolder}/Obstacle_MovingTaxi.asset");
            movingTaxi.displayName = "Moving Minibus Taxi";
            movingTaxi.category = ObstacleCategory.MovingTaxi;
            movingTaxi.prefab = null; // taxis currently spawn via chunk prefabs; definition documents the tuning
            movingTaxi.allowedLanes = 0b111;
            movingTaxi.blockedLanes = 0b010;
            movingTaxi.requiredAction = RequiredAction.Avoid;
            movingTaxi.minPlayerProgression = 400f; // moving traffic only after basics are learned
            movingTaxi.minSpawnSpeed = 4f;
            movingTaxi.maxSpawnSpeed = 8f;
            movingTaxi.spawnWeight = 1.2f;
            movingTaxi.safeGap = 16f;
            movingTaxi.width = 2.6f;
            movingTaxi.height = 2.4f;
            movingTaxi.movement = MovementBehaviour.ChangeLanesPeriodically;
            movingTaxi.indicatorBlink = true;
            movingTaxi.steeringAnimation = true;
            EditorUtility.SetDirty(movingTaxi);

            // Example authored pattern: a barrier gate that forces a lane move,
            // then a staggered pothole — validated survivable before shipping.
            ObstaclePattern gate = LoadOrCreate<ObstaclePattern>($"{ObstacleFolder}/Pattern_BarrierGate.asset");
            gate.elements = new[]
            {
                new ObstaclePattern.Element
                {
                    relativeDistance = 0f, laneMask = 0b011, // block left+centre
                    requiredAction = RequiredAction.Avoid, obstacle = barrier, categoryHint = ObstacleCategory.ConstructionBarricade,
                },
                new ObstaclePattern.Element
                {
                    relativeDistance = 14f, laneMask = 0b100, // then block right
                    requiredAction = RequiredAction.Avoid, obstacle = pothole, categoryHint = ObstacleCategory.Pothole,
                },
            };
            gate.difficultyRating = 0.5f;
            gate.minPlayerProgression = 200f;
            gate.spawnProbability = 1f;
            gate.length = 26f;
            if (!gate.IsSurvivable())
            {
                Debug.LogError("Pattern_BarrierGate is not survivable — check lane masks.");
            }
            EditorUtility.SetDirty(gate);

            ObstacleCatalog catalog = LoadOrCreate<ObstacleCatalog>($"{ObstacleFolder}/ObstacleCatalog.asset");
            catalog.obstacles = new[] { barrier, pothole, movingTaxi };
            catalog.patterns = new[] { gate };
            catalog.avoidRecentObstacles = 2;
            EditorUtility.SetDirty(catalog);
        }

        static GameObject LoadPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"Gameplay content: prefab not found at {path}; leaving reference empty.");
            }

            return prefab;
        }

        static AudioClip LoadAudio(string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"Gameplay content: audio not found at {path}; leaving reference empty.");
            }

            return clip;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
                string leaf = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }
    }
}
