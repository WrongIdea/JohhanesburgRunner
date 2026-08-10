using JoburgRunner.Environment.Pigeons;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Attaches pigeon landing points and jacaranda interest areas to the shared
    /// <c>RoadSegment.prefab</c> (Parts 3–5 of the integration spec). Placement is
    /// bounds-driven — read from the baked props' real geometry, not guessed — so
    /// perches sit on the pavement surface and the traffic-light arms, and the
    /// jacaranda marker wires the tile's existing <c>Falling_Blossom_Petals</c>
    /// particle system. Idempotent: everything lives under one "PigeonPoints" child
    /// that is cleared and rebuilt each run. Points self-register with pooling, so
    /// no road-segment lifecycle code is needed.
    /// </summary>
    public static class PigeonSegmentIntegration
    {
        const string RoadSegmentPath = "Assets/Prefabs/RoadSegment.prefab";
        const string Container = "PigeonPoints";

        [MenuItem("Jozi Runner/Pigeons/Integrate Into Road Segment")]
        public static void Integrate()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RoadSegmentPath);
            int perches = 0, interest = 0;
            try
            {
                Transform old = root.transform.Find(Container);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }
                GameObject holder = new GameObject(Container);
                holder.transform.SetParent(root.transform, false);

                // The tile's Z span comes from the road strip (a real spanning
                // renderer); pavement X is deterministic (the spawner's proven
                // pavement line) because the sidewalk/tree nodes are runtime-placed
                // templates sitting at the origin in the prefab.
                GetTileZRange(root, out float zMin, out float zMax);

                // Pavement perches: always valid ground, no float risk.
                perches += AddPavementPerches(holder, zMin, zMax);

                // Traffic-light arm perches from the baked light geometry.
                perches += AddPropPerches(root, holder, "TrafficLight_FBX",
                    PigeonLandingPoint.LandingType.TrafficLightArm, topInset: 0.15f, maxPerTile: 4);

                // Jacaranda interest area(s) + a feeding perch beneath.
                interest += AddJacaranda(holder, zMin, zMax);

                PrefabUtility.SaveAsPrefabAsset(root, RoadSegmentPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[PigeonIntegration] RoadSegment.prefab: {perches} landing points, " +
                      $"{interest} interest areas attached (under '{Container}').");
        }

        const float PavementX = 6.2f; // matches PigeonSpawner.pavementX

        static int AddPavementPerches(GameObject holder, float zMin, float zMax)
        {
            int added = 0;
            // Two perches per pavement, spread along the tile and inset from the seams.
            foreach (float side in new[] { -PavementX, PavementX })
            {
                added += MakePerch(holder, new Vector3(side, 0f, Mathf.Lerp(zMin, zMax, 0.3f)),
                    PigeonLandingPoint.LandingType.Pavement, 2);
                added += MakePerch(holder, new Vector3(side, 0f, Mathf.Lerp(zMin, zMax, 0.7f)),
                    PigeonLandingPoint.LandingType.Pavement, 2);
            }
            return added;
        }

        static int AddPropPerches(GameObject root, GameObject holder, string nameContains,
            PigeonLandingPoint.LandingType type, float topInset, int maxPerTile)
        {
            int added = 0;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (added >= maxPerTile || !t.name.Contains(nameContains))
                {
                    continue;
                }
                if (!TryBounds(t, out Bounds b))
                {
                    continue;
                }
                Vector3 pos = new Vector3(b.center.x, b.max.y - topInset, b.center.z);
                added += MakePerch(holder, pos, type, 1);
            }
            return added;
        }

        static int AddJacaranda(GameObject holder, float zMin, float zMax)
        {
            // Roadside trees stand on the pavement line, so anchor a jacaranda
            // interest area (one per side) there — guaranteed off-road. Petals are
            // left unwired (their baked template sits at road-centre); the interest
            // area's takeoff event remains for per-scene wiring.
            int added = 0;
            foreach (float side in new[] { -PavementX, PavementX })
            {
                Vector3 pos = new Vector3(side, 0f, Mathf.Lerp(zMin, zMax, 0.5f));
                GameObject go = new GameObject("Pigeon_Interest_Jacaranda");
                go.transform.SetParent(holder.transform, false);
                go.transform.position = pos;
                var area = go.AddComponent<PigeonInterestArea>();
                var so = new SerializedObject(area);
                so.FindProperty("type").enumValueIndex = (int)PigeonInterestArea.InterestType.Jacaranda;
                so.FindProperty("radius").floatValue = 3.5f;
                so.FindProperty("spawnMultiplier").floatValue = 1.4f;
                so.FindProperty("preferFeeding").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();

                MakePerch(holder, pos, PigeonLandingPoint.LandingType.Pavement, 2);
                added++;
            }
            return added;
        }

        // Tile Z span from the widest spanning renderer (the asphalt road strip).
        static void GetTileZRange(GameObject root, out float zMin, out float zMax)
        {
            zMin = -15f; zMax = 15f; // safe default if the road can't be found
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if ((t.name.Contains("Asphalt") || t.name == "Road") && TryBounds(t, out Bounds b))
                {
                    zMin = b.min.z;
                    zMax = b.max.z;
                    return;
                }
            }
        }

        static int MakePerch(GameObject holder, Vector3 pos, PigeonLandingPoint.LandingType type, int maxOcc)
        {
            GameObject go = new GameObject($"Pigeon_Landing_{type}");
            go.transform.SetParent(holder.transform, false);
            go.transform.position = pos;
            var lp = go.AddComponent<PigeonLandingPoint>();
            var so = new SerializedObject(lp);
            so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("maxOccupants").intValue = maxOcc;
            so.ApplyModifiedPropertiesWithoutUndo();
            return 1;
        }

        static bool TryBounds(Transform t, out Bounds b)
        {
            var renderers = t.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                b = default;
                return false;
            }
            b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            return true;
        }
    }
}
