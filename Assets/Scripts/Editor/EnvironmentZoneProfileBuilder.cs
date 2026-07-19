using System.Collections.Generic;
using JoburgRunner.Environment;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// One-shot generator for example <see cref="EnvironmentZoneProfile"/>
    /// assets and a <see cref="ZoneCatalog"/>, built from the environment
    /// library prefabs the main scene builder already produces
    /// (Assets/Prefabs/Environment). Additive and idempotent: re-running
    /// overwrites the example assets in place and touches nothing else, so it
    /// never affects the working generated scene.
    ///
    /// This is the shipped "at least two example profiles using existing
    /// assets" for Priority 1; designers author further zones in the Inspector
    /// via the CreateAssetMenu entries.
    /// </summary>
    public static class EnvironmentZoneProfileBuilder
    {
        const string ZoneFolder = "Assets/Environment/Zones";
        const string EnvironmentPrefabFolder = "Assets/Prefabs/Environment";

        [MenuItem("Joburg Runner/Generate Example Zone Profiles")]
        public static void GenerateExampleZoneProfiles()
        {
            Directory("Assets/Environment");
            Directory(ZoneFolder);

            EnvironmentZoneProfile cbd = BuildCbdProfile();
            EnvironmentZoneProfile soweto = BuildSowetoProfile();

            ZoneCatalog catalog = LoadOrCreate<ZoneCatalog>($"{ZoneFolder}/ZoneCatalog.asset");
            catalog.zones = new[] { cbd, soweto };
            catalog.openingZone = cbd;
            catalog.metresPerZone = 800f;
            catalog.avoidRecentZones = 1;
            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated example zone profiles: {cbd.displayName}, {soweto.displayName} + ZoneCatalog at {ZoneFolder}.");
        }

        static EnvironmentZoneProfile BuildCbdProfile()
        {
            EnvironmentZoneProfile p = LoadOrCreate<EnvironmentZoneProfile>($"{ZoneFolder}/Zone_JoburgCBD.asset");
            p.displayName = "Joburg CBD";
            p.zoneId = EnvironmentZoneId.JoburgCBD;

            p.palette = new EnvironmentZoneProfile.ColorPalette
            {
                buildingColors = new[]
                {
                    new Color(0.60f, 0.58f, 0.52f), // weathered concrete
                    new Color(0.70f, 0.45f, 0.30f), // brick
                    new Color(0.45f, 0.50f, 0.55f), // grey office
                },
                accentColors = new[] { new Color(0.95f, 0.62f, 0.12f), new Color(0.15f, 0.45f, 0.30f) },
                skyTint = new Color(0.66f, 0.72f, 0.80f),
                hazeColor = new Color(0.74f, 0.72f, 0.66f),   // warm CBD dust
                roadWearTint = new Color(0.85f, 0.84f, 0.82f),
                weatherTint = new Color(1f, 0.97f, 0.90f),
            };

            p.difficulty = new EnvironmentZoneProfile.DifficultyModifiers
            {
                forwardSpeedMultiplier = 1f,
                easyWeightScale = 1f,
                mediumWeightScale = 1.1f,
                hardWeightScale = 1f,
                specialWeightScale = 1f,
            };

            p.buildings = WeightedPrefabs(
                ("BuildingTall", 2f), ("BuildingMedium", 2f), ("BuildingSmall", 1f), ("Shop", 1.5f));
            p.props = WeightedPrefabs(
                ("StreetLight", 2f), ("TrafficLight", 1.5f), ("RoadSign", 1f),
                ("Bin", 1f), ("Bench", 0.7f), ("BusStop", 0.6f));

            p.variation = DefaultVariation(
                buildingColors: p.palette.buildingColors,
                shopSigns: new[] { "JOZI ELECTRONICS", "CBD CASH STORE", "MZANSI PHONES", "GOLDEN SPUR CAFE", "EGOLI FASHION" });

            p.routeWeight = 1.5f;
            p.minRunDistance = 0f;
            EditorUtility.SetDirty(p);
            return p;
        }

        static EnvironmentZoneProfile BuildSowetoProfile()
        {
            EnvironmentZoneProfile p = LoadOrCreate<EnvironmentZoneProfile>($"{ZoneFolder}/Zone_Soweto.asset");
            p.displayName = "Soweto";
            p.zoneId = EnvironmentZoneId.Soweto;

            p.palette = new EnvironmentZoneProfile.ColorPalette
            {
                buildingColors = new[]
                {
                    new Color(0.85f, 0.72f, 0.50f), // painted plaster
                    new Color(0.78f, 0.40f, 0.32f), // face brick
                    new Color(0.55f, 0.62f, 0.68f), // corrugated
                },
                accentColors = new[] { new Color(0.95f, 0.75f, 0.15f), new Color(0.20f, 0.55f, 0.75f) },
                skyTint = new Color(0.78f, 0.72f, 0.58f),      // golden-hour township light
                hazeColor = new Color(0.85f, 0.72f, 0.55f),
                roadWearTint = new Color(0.80f, 0.78f, 0.74f), // more worn tar
                weatherTint = new Color(1f, 0.92f, 0.78f),
            };

            p.difficulty = new EnvironmentZoneProfile.DifficultyModifiers
            {
                forwardSpeedMultiplier = 1.05f,
                easyWeightScale = 0.9f,
                mediumWeightScale = 1.1f,
                hardWeightScale = 1.15f,
                specialWeightScale = 1f,
            };

            p.buildings = WeightedPrefabs(
                ("BuildingSmall", 2.5f), ("Shop", 2f), ("BuildingMedium", 1f));
            p.props = WeightedPrefabs(
                ("StreetLight", 1.5f), ("RoadSign", 1f), ("Bin", 1f),
                ("Tree01", 1.5f), ("Tree02", 1.5f), ("BusStop", 0.8f));

            p.variation = DefaultVariation(
                buildingColors: p.palette.buildingColors,
                shopSigns: new[] { "VILAKAZI SPAZA", "SOWETO KOTA KING", "KASI STYLE", "UBUNTU BUTCHERY", "ORLANDO EATS" });

            p.routeWeight = 1f;
            p.minRunDistance = 800f; // unlocks after the opening CBD leg
            EditorUtility.SetDirty(p);
            return p;
        }

        static EnvironmentVariation DefaultVariation(Color[] buildingColors, string[] shopSigns)
        {
            EnvironmentVariation v = EnvironmentVariation.Default;
            v.buildingColors = buildingColors;
            v.shopSigns = shopSigns;
            v.vehicleColors = new[]
            {
                new Color(0.85f, 0.85f, 0.85f), // the ubiquitous white minibus taxi
                new Color(0.15f, 0.30f, 0.55f),
                new Color(0.70f, 0.15f, 0.15f),
                new Color(0.90f, 0.80f, 0.10f),
            };
            return v;
        }

        static EnvironmentZoneProfile.WeightedPrefab[] WeightedPrefabs(params (string name, float weight)[] entries)
        {
            var list = new List<EnvironmentZoneProfile.WeightedPrefab>(entries.Length);
            foreach ((string name, float weight) in entries)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnvironmentPrefabFolder}/{name}.prefab");
                if (prefab == null)
                {
                    Debug.LogWarning($"Zone profile: environment prefab '{name}' not found at {EnvironmentPrefabFolder}; skipping.");
                    continue;
                }

                list.Add(new EnvironmentZoneProfile.WeightedPrefab { prefab = prefab, weight = weight });
            }

            return list.ToArray();
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

        static void Directory(string path)
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
