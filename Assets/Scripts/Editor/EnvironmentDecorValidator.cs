using System.Collections.Generic;
using System.Text;
using JoburgRunner;
using JoburgRunner.Environment.Decor;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Read-only auditor for the socket decoration system. Reports problems to the
    /// Console; it never moves, deletes or edits gameplay-critical objects. Run it
    /// from "Jozi Runner/Validate Environment Decor".
    /// </summary>
    public static class EnvironmentDecorValidator
    {
        const string RoadPrefabPath = "Assets/Prefabs/RoadSegment.prefab";
        // Half-width of the gameplay keep-out (must match SegmentDecorator.LaneKeepOut).
        const float LaneKeepOut = 4.6f;

        [MenuItem("Jozi Runner/Validate Environment Decor")]
        public static void Validate()
        {
            var report = new StringBuilder();
            int errors = 0, warnings = 0;

            void Error(string m) { report.AppendLine("  ERROR   " + m); errors++; }
            void Warn(string m) { report.AppendLine("  WARNING " + m); warnings++; }
            void Ok(string m) { report.AppendLine("  OK      " + m); }

            report.AppendLine("=== Environment Decor Validation ===");

            // --- Road tile sockets ---
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoadPrefabPath);
            if (prefab == null)
            {
                Error($"RoadSegment prefab not found at {RoadPrefabPath}.");
            }
            else
            {
                DecorSocket[] sockets = prefab.GetComponentsInChildren<DecorSocket>(true);
                if (prefab.GetComponentInChildren<SegmentDecorator>(true) == null)
                {
                    Error("RoadSegment prefab has no SegmentDecorator component.");
                }

                if (sockets.Length == 0)
                {
                    Error("RoadSegment prefab has no DecorSockets at all.");
                }
                else
                {
                    Ok($"Found {sockets.Length} sockets on the road tile.");
                }

                var byType = new Dictionary<DecorSocketType, int>();
                foreach (DecorSocket s in sockets)
                {
                    byType[s.SocketType] = byType.TryGetValue(s.SocketType, out int c) ? c + 1 : 1;

                    // Trees/props must never sit within the three gameplay lanes.
                    float localX = prefab.transform.InverseTransformPoint(s.transform.position).x;
                    if (Mathf.Abs(localX) < LaneKeepOut)
                    {
                        Error($"Socket '{s.name}' ({s.SocketType}) at local x={localX:F2} is inside the " +
                              $"gameplay keep-out (|x|<{LaneKeepOut}). It could intrude on a lane.");
                    }
                }

                foreach (DecorSocketType required in new[] { DecorSocketType.Tree, DecorSocketType.Lamp, DecorSocketType.TrafficLight })
                {
                    if (!byType.ContainsKey(required))
                    {
                        Warn($"No '{required}' sockets on the road tile – that decoration can never appear.");
                    }
                }

                // Static overlap check: sockets whose clearance circles already intersect.
                for (int i = 0; i < sockets.Length; i++)
                {
                    for (int j = i + 1; j < sockets.Length; j++)
                    {
                        float min = sockets[i].ClearanceRadius + sockets[j].ClearanceRadius;
                        float d = Vector3.Distance(sockets[i].transform.position, sockets[j].transform.position);
                        if (d < min * 0.5f)
                        {
                            Warn($"Sockets '{sockets[i].name}' and '{sockets[j].name}' are {d:F2} m apart – " +
                                 "props here may overlap if both spawn.");
                        }
                    }
                }
            }

            // --- District profiles ---
            string[] guids = AssetDatabase.FindAssets("t:DistrictDecorProfile");
            if (guids.Length == 0)
            {
                Warn("No DistrictDecorProfile assets found.");
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>(path);
                if (profile == null)
                {
                    continue;
                }

                bool canSpawnSomething = false;
                if (profile.rules != null)
                {
                    foreach (DecorRule rule in profile.rules)
                    {
                        if (rule == null)
                        {
                            continue;
                        }

                        if (rule.allowedPrefabs != null && rule.allowedPrefabs.Length > 0 && rule.spawnProbability > 0f)
                        {
                            canSpawnSomething = true;
                        }

                        // Traffic lights MUST be gated to intersections.
                        if (rule.socketType == DecorSocketType.TrafficLight && !rule.requiresIntersection)
                        {
                            Error($"Profile '{profile.districtName}': TrafficLight rule is not gated to " +
                                  "intersections (requiresIntersection = false). It would appear on straight tiles.");
                        }

                        if (rule.allowedPrefabs == null || rule.allowedPrefabs.Length == 0)
                        {
                            Warn($"Profile '{profile.districtName}': {rule.socketType} rule has no prefabs.");
                        }
                    }
                }

                if (!canSpawnSomething)
                {
                    Warn($"Profile '{profile.districtName}' can never spawn anything – its tiles would render empty.");
                }
                else
                {
                    Ok($"Profile '{profile.districtName}' is populated.");
                }
            }

            report.AppendLine($"=== Done: {errors} error(s), {warnings} warning(s) ===");
            report.AppendLine("Note: duplicate-consecutive-buildings and empty-tile checks are runtime-only " +
                              "(district assignment happens as tiles are placed); this audit covers static layout.");

            if (errors > 0)
            {
                Debug.LogError(report.ToString());
            }
            else if (warnings > 0)
            {
                Debug.LogWarning(report.ToString());
            }
            else
            {
                Debug.Log(report.ToString());
            }
        }
    }
}
