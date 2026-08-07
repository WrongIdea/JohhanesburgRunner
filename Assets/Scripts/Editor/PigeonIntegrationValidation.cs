using System.Collections.Generic;
using System.Linq;
using System.Text;
using JoburgRunner.Environment.Pigeons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// One-shot integration validator (Part 20): "Jozi Runner ▸ Pigeons ▸ Validate
    /// Integration". Checks the shared prefabs and the open scene for the common
    /// integration mistakes and prints a readable PASS / WARN / FAIL summary,
    /// selecting the first offending object where possible.
    /// </summary>
    public static class PigeonIntegrationValidation
    {
        const string PigeonPrefab = "Assets/Prefabs/Pigeons/Pigeon.prefab";
        const string FlockPrefab = "Assets/Prefabs/Pigeons/PigeonFlock.prefab";
        const string SettingsPath = "Assets/Environment/Pigeons/PigeonFlockSettings.asset";
        const string RoadSegmentPath = "Assets/Prefabs/RoadSegment.prefab";
        const float RoadHalfWidth = 4f;

        static readonly string[] RequiredStates = { "Idle", "Walk", "Peck", "TakeOff", "Fly", "Land" };

        [MenuItem("Jozi Runner/Pigeons/Validate Integration")]
        public static void Validate()
        {
            var sb = new StringBuilder();
            int fails = 0, warns = 0;
            Object selectTarget = null;

            void Pass(string m) => sb.AppendLine($"  PASS  {m}");
            void Warn(string m) { sb.AppendLine($"  WARN  {m}"); warns++; }
            void Fail(string m, Object o = null) { sb.AppendLine($"  FAIL  {m}"); fails++; if (selectTarget == null) selectTarget = o; }

            sb.AppendLine("=== Pigeon Integration Validation ===");

            // --- Pigeon prefab ---
            GameObject pigeon = AssetDatabase.LoadAssetAtPath<GameObject>(PigeonPrefab);
            if (pigeon == null)
            {
                Fail($"Pigeon prefab missing at {PigeonPrefab}");
            }
            else
            {
                if (pigeon.GetComponent<PigeonController>() == null) Fail("Pigeon prefab has no PigeonController", pigeon);
                if (pigeon.GetComponentInChildren<LODGroup>(true) == null) Warn("Pigeon prefab has no LODGroup");
                if (pigeon.GetComponent<AudioSource>() == null) Warn("Pigeon prefab has no AudioSource");

                Animator anim = pigeon.GetComponentInChildren<Animator>(true);
                if (anim == null || anim.runtimeAnimatorController == null)
                {
                    Fail("Pigeon prefab has no Animator/controller", pigeon);
                }
                else
                {
                    var clips = anim.runtimeAnimatorController.animationClips.Select(c => c.name).ToArray();
                    // States drive clips; check the clip set covers the required motions.
                    foreach (string s in RequiredStates)
                    {
                        string wanted = s switch { "TakeOff" => "Takeoff", "Fly" => "Glide", "Land" => "Landing", _ => s };
                        if (!clips.Any(c => c.Contains(wanted))) Warn($"Animator missing a clip for state '{s}' (~{wanted})");
                    }
                }

                // Shared material across all LOD renderers + instancing on.
                var rends = pigeon.GetComponentsInChildren<Renderer>(true);
                var mats = new HashSet<Material>();
                foreach (var r in rends) foreach (var m in r.sharedMaterials) if (m != null) mats.Add(m);
                if (mats.Count == 0) Fail("Pigeon prefab has no material", pigeon);
                else if (mats.Count > 1) Warn($"Pigeon uses {mats.Count} materials (expected 1 shared) — check LOD material reuse");
                else Pass("Pigeon uses a single shared material");
                foreach (var m in mats) if (m != null && !m.enableInstancing) Warn($"Material '{m.name}' has GPU instancing OFF");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(FlockPrefab) == null) Fail($"Flock prefab missing at {FlockPrefab}");

            // --- Settings asset ---
            var settings = AssetDatabase.LoadAssetAtPath<PigeonFlockSettings>(SettingsPath);
            if (settings == null)
            {
                Warn($"No settings asset at {SettingsPath} (spawner will use serialized defaults)");
            }
            else
            {
                if (settings.minFlockSize <= 0 || settings.minFlockSize > settings.maxFlockSize)
                    Fail($"Invalid flock size range {settings.minFlockSize}-{settings.maxFlockSize}", settings);
                else Pass($"Flock size range {settings.minFlockSize}-{settings.maxFlockSize}");
                if (settings.cinematicMaxSize > 20)
                    Warn($"Cinematic max ({settings.cinematicMaxSize}) exceeds default pool (20) — may shrink events");
            }

            // --- Road segment integration ---
            GameObject seg = AssetDatabase.LoadAssetAtPath<GameObject>(RoadSegmentPath);
            if (seg == null)
            {
                Warn($"RoadSegment.prefab not found at {RoadSegmentPath}");
            }
            else
            {
                var perches = seg.GetComponentsInChildren<PigeonLandingPoint>(true);
                var areas = seg.GetComponentsInChildren<PigeonInterestArea>(true);
                if (perches.Length == 0) Warn("RoadSegment has no landing points — run 'Integrate Into Road Segment'");
                else Pass($"RoadSegment has {perches.Length} landing points");
                if (areas.Length == 0) Warn("RoadSegment has no interest areas");
                else Pass($"RoadSegment has {areas.Length} interest areas");

                foreach (var p in perches)
                {
                    Vector3 lp = p.transform.position;
                    if (lp.y < -0.1f) Fail($"Landing point '{p.name}' is below ground (y={lp.y:F2})", p.gameObject);
                    if (Mathf.Abs(lp.x) < RoadHalfWidth) Warn($"Landing point '{p.name}' is inside the road (|x|={Mathf.Abs(lp.x):F2})");
                }
            }

            // --- Open scene spawner wiring ---
            var spawner = Object.FindAnyObjectByType<PigeonSpawner>();
            if (spawner == null)
            {
                sb.AppendLine("  INFO  No PigeonSpawner in the open scene (fine if this isn't the gameplay scene).");
            }
            else
            {
                var so = new SerializedObject(spawner);
                if (so.FindProperty("pigeonPrefab").objectReferenceValue == null) Fail("Scene PigeonSpawner has no pigeonPrefab", spawner);
                if (so.FindProperty("flockPrefab").objectReferenceValue == null) Fail("Scene PigeonSpawner has no flockPrefab", spawner);
                if (so.FindProperty("player").objectReferenceValue == null) Warn("Scene PigeonSpawner has no player (will search by tag at runtime)");
                if (so.FindProperty("settings").objectReferenceValue == null) Warn("Scene PigeonSpawner has no settings asset");
                if (fails == 0) Pass("Scene PigeonSpawner wired");
            }

            sb.AppendLine($"=== {fails} FAIL, {warns} WARN ===");
            if (fails > 0) Debug.LogError(sb.ToString());
            else if (warns > 0) Debug.LogWarning(sb.ToString());
            else Debug.Log(sb.ToString());

            if (selectTarget != null)
            {
                Selection.activeObject = selectTarget;
            }
        }
    }
}
