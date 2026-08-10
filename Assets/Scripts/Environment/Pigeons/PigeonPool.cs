using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// Fixed pool of pigeons, prewarmed once so no Instantiate/Destroy happens
    /// during gameplay. Idle pigeons are deactivated under a hidden root; the
    /// flock rents and returns them. It only ever grows past the prewarm count in
    /// the editor (or when expansion is explicitly enabled) and never past
    /// <see cref="maxSize"/>; in a player build a request past the warmed size
    /// returns null so the caller shrinks the event instead of allocating.
    /// </summary>
    public sealed class PigeonPool
    {
        readonly GameObject prefab;
        readonly Transform idleRoot;
        readonly Stack<PigeonController> free = new Stack<PigeonController>();
        readonly HashSet<PigeonController> active = new HashSet<PigeonController>();
        readonly int maxSize;
        readonly bool allowExpansion;

        public int AvailableCount => free.Count;
        public int ActiveCount => active.Count;
        public int TotalCount => free.Count + active.Count;

        public PigeonPool(GameObject prefab, Transform idleRoot, int prewarm,
            int maxSize = 30, bool? allowExpansion = null)
        {
            this.prefab = prefab;
            this.idleRoot = idleRoot;
            this.maxSize = Mathf.Max(prewarm, maxSize);
            // Growth is an editor / authoring affordance; a shipped build stays fixed.
            this.allowExpansion = allowExpansion ?? Application.isEditor;
            InitializePool(prewarm);
        }

        public void InitializePool(int prewarm)
        {
            for (int i = 0; i < prewarm; i++)
            {
                PigeonController p = Create();
                if (p != null)
                {
                    free.Push(p);
                }
            }
        }

        PigeonController Create()
        {
            GameObject go = Object.Instantiate(prefab, idleRoot);
            go.SetActive(false);
            return go.GetComponent<PigeonController>();
        }

        /// <summary>Rent a pigeon under <paramref name="parent"/>, or null if exhausted.</summary>
        public PigeonController GetPigeon(Transform parent)
        {
            PigeonController pigeon;
            if (free.Count > 0)
            {
                pigeon = free.Pop();
            }
            else if (allowExpansion && TotalCount < maxSize)
            {
                pigeon = Create();
            }
            else
            {
                return null; // fixed pool exhausted — caller must degrade gracefully
            }

            if (pigeon == null)
            {
                return null;
            }
            pigeon.transform.SetParent(parent, false);
            pigeon.gameObject.SetActive(true);
            active.Add(pigeon);
            return pigeon;
        }

        /// <summary>Return a rented pigeon. Safe to call twice — the second is a no-op.</summary>
        public void ReturnPigeon(PigeonController pigeon)
        {
            if (pigeon == null || !active.Remove(pigeon))
            {
                return;
            }
            pigeon.SilenceAudio();
            pigeon.ReleaseLandingPoint();
            pigeon.gameObject.SetActive(false);
            pigeon.transform.SetParent(idleRoot, false);
            free.Push(pigeon);
        }

        /// <summary>Return every rented pigeon (teardown / hard reset).</summary>
        public void ReturnAllPigeons()
        {
            if (active.Count == 0)
            {
                return;
            }
            var snapshot = new List<PigeonController>(active);
            for (int i = 0; i < snapshot.Count; i++)
            {
                ReturnPigeon(snapshot[i]);
            }
        }

        // --- Back-compat aliases (original API) ---
        public PigeonController Get(Transform parent) => GetPigeon(parent);
        public void Return(PigeonController pigeon) => ReturnPigeon(pigeon);
    }
}
