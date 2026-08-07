using JoburgRunner.Environment.Pigeons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Builds <c>Assets/Scenes/PigeonFlockTest.unity</c> (Part 20): a mock runner,
    /// a wired <see cref="PigeonSpawner"/>, a pavement flock spawn point, a scatter
    /// of landing points, taxi-rank and jacaranda interest areas + cinematic events,
    /// and a mock vehicle. Keyboard controls and an on-screen stats overlay come
    /// from <see cref="PigeonFlockTestRig"/>; the scene reproduces all 10 test
    /// scenarios without the full game.
    /// </summary>
    public static class PigeonFlockTestSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/PigeonFlockTest.unity";

        [MenuItem("Joburg Runner/Build Pigeon Flock Test Scene")]
        public static void Build()
        {
            // Make sure the prefabs + settings exist first.
            PigeonBuilder.BuildAll();
            GameObject pigeonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PigeonBuilder.PigeonPrefabPath);
            GameObject flockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PigeonBuilder.FlockPrefabPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Ground.
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 40f);
            ground.transform.position = new Vector3(0f, 0f, 180f);

            // Mock runner (tagged Player so the spawner / cinematic events find it).
            GameObject runner = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            runner.name = "MockRunner";
            runner.transform.position = new Vector3(0f, 1f, 0f);
            TrySetTag(runner, "Player");

            // Camera chase.
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.SetParent(runner.transform, false);
                cam.transform.localPosition = new Vector3(0f, 4f, -8f);
                cam.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
                cam.farClipPlane = 300f;
            }

            // Spawner — no settings asset here so the tuned test fields below apply
            // (tighter spacing / no warm-up) and flocks appear quickly.
            GameObject spawnerGo = new GameObject("PigeonSpawner");
            var spawner = spawnerGo.AddComponent<PigeonSpawner>();
            var so = new SerializedObject(spawner);
            SetObj(so, "pigeonPrefab", pigeonPrefab);
            SetObj(so, "flockPrefab", flockPrefab);
            SetObj(so, "player", runner.transform);
            SetFloat(so, "minFlockSpacing", 22f);
            SetFloat(so, "maxFlockSpacing", 34f);
            SetFloat(so, "spawnAheadDistance", 45f);
            SetFloat(so, "warmupDistance", 0f);
            so.FindProperty("spawnChance").floatValue = 1f;
            SetFloat(so, "cinematicChance", 0f); // cinematics come from the placed events
            so.ApplyModifiedPropertiesWithoutUndo();

            // Ground pavement spawn point.
            GameObject sp = new GameObject("SpawnPoint_Pavement");
            sp.transform.position = new Vector3(6.2f, 0f, 40f);
            sp.AddComponent<PigeonSpawnPoint>();

            // Landing points scattered near the spawn areas.
            GameObject lpRoot = new GameObject("LandingPoints");
            AddLanding(lpRoot, new Vector3(6.2f, 0.0f, 44f), PigeonLandingPoint.LandingType.Pavement);
            AddLanding(lpRoot, new Vector3(7.0f, 0.5f, 46f), PigeonLandingPoint.LandingType.LowWall);
            AddLanding(lpRoot, new Vector3(5.6f, 2.6f, 48f), PigeonLandingPoint.LandingType.TrafficLightArm);
            AddLanding(lpRoot, new Vector3(6.4f, 1.2f, 50f), PigeonLandingPoint.LandingType.PlanterEdge);
            AddLanding(lpRoot, new Vector3(-6.2f, 0.0f, 92f), PigeonLandingPoint.LandingType.Pavement);
            AddLanding(lpRoot, new Vector3(-6.8f, 3.0f, 96f), PigeonLandingPoint.LandingType.BusStopRoof);

            // Taxi-rank interest area + cinematic event.
            GameObject taxi = new GameObject("InterestArea_TaxiRank");
            taxi.transform.position = new Vector3(-6.2f, 0f, 94f);
            var taxiArea = taxi.AddComponent<PigeonInterestArea>();
            SetEnum(new SerializedObject(taxiArea), "type", (int)PigeonInterestArea.InterestType.TaxiRank);
            AddCinematic(taxi.transform.position + Vector3.up * 0.01f, PigeonCinematicEvent.EventType.TaxiRank,
                spawner, runner.transform, 12, 3.2f);

            // Jacaranda feeding area + cinematic (petal hook left unassigned).
            GameObject jac = new GameObject("InterestArea_Jacaranda");
            jac.transform.position = new Vector3(6.2f, 0f, 140f);
            var jacArea = jac.AddComponent<PigeonInterestArea>();
            SetEnum(new SerializedObject(jacArea), "type", (int)PigeonInterestArea.InterestType.Jacaranda);
            AddLanding(jac, new Vector3(6.2f, 0f, 140f), PigeonLandingPoint.LandingType.TreeBranch);
            AddCinematic(jac.transform.position, PigeonCinematicEvent.EventType.Jacaranda,
                spawner, runner.transform, 6, 2.4f);

            // Mock vehicle the rig sweeps forward.
            GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicle.name = "MockVehicle";
            vehicle.transform.localScale = new Vector3(1.8f, 1.4f, 4f);
            vehicle.transform.position = new Vector3(2.5f, 0.7f, -20f);

            // Test rig.
            GameObject rigGo = new GameObject("PigeonFlockTestRig");
            var rig = rigGo.AddComponent<PigeonFlockTestRig>();
            var rigSo = new SerializedObject(rig);
            SetObj(rigSo, "spawner", spawner);
            SetObj(rigSo, "runner", runner.transform);
            SetObj(rigSo, "mockVehicle", vehicle.transform);
            rigSo.ApplyModifiedPropertiesWithoutUndo();

            EnsureScenesFolder();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Built pigeon flock test scene at {ScenePath}");
        }

        static void AddLanding(GameObject parent, Vector3 pos, PigeonLandingPoint.LandingType type)
        {
            GameObject go = new GameObject($"Landing_{type}");
            go.transform.SetParent(parent.transform, true);
            go.transform.position = pos;
            var lp = go.AddComponent<PigeonLandingPoint>();
            SetEnum(new SerializedObject(lp), "type", (int)type);
        }

        static void AddCinematic(Vector3 pos, PigeonCinematicEvent.EventType type,
            PigeonSpawner spawner, Transform runner, int size, float radius)
        {
            GameObject go = new GameObject($"Cinematic_{type}");
            go.transform.position = pos;
            var ev = go.AddComponent<PigeonCinematicEvent>();
            var so = new SerializedObject(ev);
            so.FindProperty("type").enumValueIndex = (int)type;
            SetObj(so, "spawner", spawner);
            SetObj(so, "player", runner);
            so.FindProperty("flockSize").intValue = size;
            so.FindProperty("radius").floatValue = radius;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetObj(SerializedObject so, string prop, Object value)
        {
            SerializedProperty p = so.FindProperty(prop);
            if (p != null)
            {
                p.objectReferenceValue = value;
            }
        }

        static void SetFloat(SerializedObject so, string prop, float value)
        {
            SerializedProperty p = so.FindProperty(prop);
            if (p != null)
            {
                p.floatValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void SetEnum(SerializedObject so, string prop, int value)
        {
            SerializedProperty p = so.FindProperty(prop);
            if (p != null)
            {
                p.enumValueIndex = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void TrySetTag(GameObject go, string tag)
        {
            try { go.tag = tag; }
            catch { Debug.LogWarning($"Tag '{tag}' not defined; MockRunner left Untagged."); }
        }

        static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
        }
    }
}
