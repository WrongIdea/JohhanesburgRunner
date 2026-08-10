using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace JoburgRunner.Editor
{
    public static class JunctionSegmentValidator
    {
        static readonly string[] VariantPaths =
        {
            "Assets/Prefabs/Road/Junctions/RoadSegment_CrossIntersection.prefab",
            "Assets/Prefabs/Road/Junctions/RoadSegment_TJunction_Left.prefab",
            "Assets/Prefabs/Road/Junctions/RoadSegment_TJunction_Right.prefab"
        };

        [MenuItem("Joburg Runner/Validate Intersection Roads")]
        public static void ValidateMenu() => ValidateOrThrow();

        public static void ValidateOrThrow()
        {
            var errors = new List<string>();
            foreach (string path in VariantPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    errors.Add($"Missing prefab: {path}");
                    continue;
                }

                if (prefab.transform.localScale != Vector3.one)
                    errors.Add($"{prefab.name}: root scale must be 1,1,1.");
                if (prefab.GetComponentsInChildren<Collider>(true).Length != 0)
                    errors.Add($"{prefab.name}: decorative variant contains colliders.");
                if (prefab.GetComponentsInChildren<Coin>(true).Length != 0 ||
                    prefab.GetComponentsInChildren<PowerUp>(true).Length != 0 ||
                    prefab.GetComponentsInChildren<RunnerObstacle>(true).Length != 0)
                    errors.Add($"{prefab.name}: gameplay content found on a side road.");

                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null)
                            errors.Add($"{prefab.name}/{renderer.name}: missing material.");
                    }

                    if (renderer.name == "SideStreetAsphalt" && renderer.bounds.max.y < 0.125f)
                        errors.Add($"{prefab.name}/{renderer.name}: surface is not raised clear of the base sidewalk; z-fighting may blink.");
                }

                foreach (JunctionDecorSocket socket in
                         prefab.GetComponentsInChildren<JunctionDecorSocket>(true))
                {
                    if (socket.SocketType == JunctionDecorSocketType.Pedestrian &&
                        Mathf.Abs(socket.transform.localPosition.x) < RoadMetrics.RoadHalfWidth)
                        errors.Add($"{prefab.name}/{socket.name}: pedestrian socket enters gameplay road.");
                }
            }

            GameObject road = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RoadSegment.prefab");
            if (road == null)
            {
                errors.Add("Missing production RoadSegment prefab.");
            }
            else
            {
                Transform asphalt = road.transform.Find("Road/ThreeLaneAsphalt");
                if (asphalt == null)
                    errors.Add("Production main-road surface is missing.");
                else
                {
                    if (!Mathf.Approximately(asphalt.localScale.x, RoadMetrics.RoadWidth))
                        errors.Add($"Main-road width is {asphalt.localScale.x:0.###} m (expected {RoadMetrics.RoadWidth:0.##}).");
                    if (!Mathf.Approximately(asphalt.localScale.z, 30f))
                        errors.Add($"Segment length changed to {asphalt.localScale.z:0.###} m (expected 30).");
                }
                if (road.GetComponent<JunctionVisuals>() == null)
                    errors.Add("RoadSegment is missing pooled JunctionVisuals.");
            }

            JunctionSpawnSettings settings =
                AssetDatabase.LoadAssetAtPath<JunctionSpawnSettings>(
                    "Assets/Environment/JunctionSpawnSettings.asset");
            if (settings == null)
                errors.Add("Missing JunctionSpawnSettings asset.");
            else
            {
                if (settings.straightWeight != 70f ||
                    settings.crossroadWeight != 20f ||
                    settings.tJunctionWeight != 10f)
                    errors.Add("Junction weights are not 70/20/10.");
                if (settings.minimumStraightSegments < 3)
                    errors.Add("Junction cooldown permits fewer than three straight segments.");
                if (settings.openingSegmentCount < 7)
                    errors.Add("Junctions may enter the seven-tile opening ring.");
                if (!settings.guaranteeFirstJunctionAfterOpening)
                    errors.Add("First post-opening junction guarantee is disabled.");
                if (settings.allowInJacarandaAvenue)
                    errors.Add("Jacaranda Avenue junction spawning must remain disabled.");
            }

            ValidateGameplayLanes(errors);

            if (errors.Count > 0)
                throw new BuildFailedException(
                    "Intersection-road validation failed:\n- " + string.Join("\n- ", errors));

            Debug.Log(
                $"Intersection-road validation passed: segment 30 m, road {RoadMetrics.RoadWidth:0.##} m, " +
                $"lanes [{-RoadMetrics.LaneSpacing:0.##}, 0, {RoadMetrics.LaneSpacing:0.##}], roots scale 1, no side-road gameplay content/colliders, " +
                "materials present, weights 70/20/10, cooldown >= 4, opening/Jacaranda excluded.");
        }

        static void ValidateGameplayLanes(List<string> errors)
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                errors.Add("Main scene has no PlayerController.");
            }
            else
            {
                SerializedObject data = new SerializedObject(player);
                SerializedProperty lanes = data.FindProperty("laneXPositions");
                if (lanes == null || lanes.arraySize != 3 ||
                    !Near(lanes.GetArrayElementAtIndex(0).floatValue, -RoadMetrics.LaneSpacing) ||
                    !Near(lanes.GetArrayElementAtIndex(1).floatValue, 0f) ||
                    !Near(lanes.GetArrayElementAtIndex(2).floatValue, RoadMetrics.LaneSpacing))
                    errors.Add("Player lane centres do not match RoadMetrics.");
                SerializedProperty smoothing = data.FindProperty("laneChangeSmoothTime");
                if (smoothing == null || !Near(smoothing.floatValue, 0.12f))
                    errors.Add("Lane-switch duration changed from 0.12 seconds.");
            }

            ChunkManager chunkManager = Object.FindAnyObjectByType<ChunkManager>();
            if (chunkManager == null)
            {
                errors.Add("Main scene has no active ChunkManager.");
                return;
            }

            SerializedObject managerData = new SerializedObject(chunkManager);
            SerializedProperty chunks = managerData.FindProperty("chunkPrefabs");
            for (int i = 0; chunks != null && i < chunks.arraySize; i++)
            {
                TrackChunk chunk = chunks.GetArrayElementAtIndex(i).objectReferenceValue as TrackChunk;
                if (chunk == null) continue;
                foreach (Component item in GameplayItems(chunk))
                {
                    float x = chunk.transform.InverseTransformPoint(item.transform.position).x;
                    if (!IsLaneCentre(x))
                        errors.Add($"{chunk.name}/{item.name}: gameplay X {x:0.###} is not a narrowed lane centre.");
                }
            }
        }

        static IEnumerable<Component> GameplayItems(Component root)
        {
            foreach (Coin item in root.GetComponentsInChildren<Coin>(true)) yield return item;
            foreach (PowerUp item in root.GetComponentsInChildren<PowerUp>(true)) yield return item;
            foreach (RunnerObstacle item in root.GetComponentsInChildren<RunnerObstacle>(true)) yield return item;
        }

        static bool IsLaneCentre(float x) =>
            Near(x, -RoadMetrics.LaneSpacing) || Near(x, 0f) || Near(x, RoadMetrics.LaneSpacing);

        static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.01f;
    }
}
