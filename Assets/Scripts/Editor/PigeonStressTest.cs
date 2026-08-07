using System.Text;
using JoburgRunner.Environment.Pigeons;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Pool stress harness (Part 14): runs 100 / 500 / 1000 rent-and-return cycles
    /// against a real <see cref="PigeonPool"/> built from the Pigeon prefab and
    /// asserts no leaks, no runaway growth, and stable counts. Pure pool cycling
    /// needs no play mode (no Tick/Animator), so it runs headlessly and prints a
    /// pass/fail result with the numbers. Menu: Jozi Runner ▸ Pigeons ▸ Stress Test.
    /// </summary>
    public static class PigeonStressTest
    {
        const int Prewarm = 20;
        const int MaxSize = 30;

        [MenuItem("Jozi Runner/Pigeons/Stress Test")]
        public static void Run()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PigeonBuilder.PigeonPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[PigeonStress] Pigeon prefab missing — build it first.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== Pigeon Pool Stress Test ===");
            bool ok = true;
            ok &= RunCycles(prefab, 100, sb);
            ok &= RunCycles(prefab, 500, sb);
            ok &= RunCycles(prefab, 1000, sb);
            ok &= ExhaustionCheck(prefab, sb);
            sb.AppendLine(ok ? "=== RESULT: PASS ===" : "=== RESULT: FAIL ===");

            if (ok) Debug.Log(sb.ToString());
            else Debug.LogError(sb.ToString());
        }

        static bool RunCycles(GameObject prefab, int cycles, StringBuilder sb)
        {
            GameObject rootGo = new GameObject("StressIdleRoot");
            rootGo.SetActive(false);
            // Expansion disabled so a leak would surface as a null, not silent growth.
            var pool = new PigeonPool(prefab, rootGo.transform, Prewarm, MaxSize, allowExpansion: false);
            var parent = new GameObject("StressParent").transform;

            int startAvail = pool.AvailableCount;
            int maxTotal = pool.TotalCount;
            var rented = new System.Collections.Generic.List<PigeonController>(MaxSize);

            for (int c = 0; c < cycles; c++)
            {
                int want = Random.Range(3, 9);
                rented.Clear();
                for (int i = 0; i < want; i++)
                {
                    PigeonController p = pool.GetPigeon(parent);
                    if (p != null)
                    {
                        rented.Add(p);
                    }
                }
                maxTotal = Mathf.Max(maxTotal, pool.TotalCount);
                for (int i = 0; i < rented.Count; i++)
                {
                    pool.ReturnPigeon(rented[i]);
                }
                // Double-return must be a no-op (idempotent).
                if (rented.Count > 0)
                {
                    pool.ReturnPigeon(rented[0]);
                }
            }

            bool leak = pool.ActiveCount != 0;
            bool restored = pool.AvailableCount == startAvail;
            bool grew = pool.TotalCount > Prewarm;
            bool pass = !leak && restored && !grew;
            sb.AppendLine($"  {cycles,5} cycles: active={pool.ActiveCount} free={pool.AvailableCount} " +
                          $"total={pool.TotalCount} peakTotal={maxTotal}  {(pass ? "PASS" : "FAIL")}");

            Object.DestroyImmediate(parent.gameObject);
            Object.DestroyImmediate(rootGo);
            return pass;
        }

        static bool ExhaustionCheck(GameObject prefab, StringBuilder sb)
        {
            GameObject rootGo = new GameObject("StressIdleRoot2");
            rootGo.SetActive(false);
            var pool = new PigeonPool(prefab, rootGo.transform, Prewarm, MaxSize, allowExpansion: false);
            var parent = new GameObject("StressParent2").transform;

            int got = 0;
            bool nullReturnedSafely = true;
            for (int i = 0; i < Prewarm + 10; i++)
            {
                PigeonController p = pool.GetPigeon(parent);
                if (p != null) got++;
                else if (i < Prewarm) nullReturnedSafely = false; // ran dry too early
            }
            bool pass = got == Prewarm && nullReturnedSafely && pool.AvailableCount == 0;
            sb.AppendLine($"  exhaustion: rented={got}/{Prewarm}, extra requests returned null safely  " +
                          $"{(pass ? "PASS" : "FAIL")}");

            pool.ReturnAllPigeons();
            bool recovered = pool.ActiveCount == 0 && pool.AvailableCount == Prewarm;
            sb.AppendLine($"  ReturnAllPigeons: active={pool.ActiveCount} free={pool.AvailableCount}  " +
                          $"{(recovered ? "PASS" : "FAIL")}");

            Object.DestroyImmediate(parent.gameObject);
            Object.DestroyImmediate(rootGo);
            return pass && recovered;
        }
    }
}
