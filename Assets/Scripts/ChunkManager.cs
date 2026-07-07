using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Chunk-based track spawner, Subway-Surfers style. The road ahead is
    /// assembled from pooled, hand-designed TrackChunk prefabs instead of
    /// timer- or row-based random spawning.
    ///
    /// Responsibilities:
    ///  - keep the track filled ahead of the player and recycle chunks behind,
    ///  - weighted-random selection with distance-based difficulty bands,
    ///  - never chain chunks whose safe lanes are unreachable from the
    ///    previous chunk's, so a survivable path always exists,
    ///  - avoid immediate chunk repeats,
    ///  - zero GameObject.Instantiate during gameplay: the pool is filled at
    ///    startup and only grows in the rare case a tier runs dry.
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform player;
        [SerializeField] GameManager gameManager;
        [SerializeField] TrackChunk[] chunkPrefabs;

        [Header("Streaming")]
        [SerializeField] float spawnDistanceAhead = 130f;
        [SerializeField] float despawnDistanceBehind = 25f;
        [SerializeField] float firstChunkDistance = 25f;
        [SerializeField] int openingSafeChunkCount = 2;
        [SerializeField] int prewarmInstancesPerChunk = 2;

        struct ActiveChunk
        {
            public TrackChunk Instance;
            public TrackChunk Prefab;
            public float EndZ;
        }

        readonly Dictionary<TrackChunk, Stack<TrackChunk>> pool = new Dictionary<TrackChunk, Stack<TrackChunk>>();
        readonly Queue<ActiveChunk> activeChunks = new Queue<ActiveChunk>();
        readonly List<TrackChunk> candidates = new List<TrackChunk>(16);

        TrackChunk lastPrefab;
        TrackChunk secondLastPrefab;
        int lastExitLanes = 0b111;
        int spawnedChunkCount;
        float nextChunkZ;
        float difficultyStartZ;
        bool initialised;

        void Awake()
        {
            foreach (TrackChunk prefab in chunkPrefabs)
            {
                var stack = new Stack<TrackChunk>(prewarmInstancesPerChunk);
                for (int i = 0; i < prewarmInstancesPerChunk; i++)
                {
                    stack.Push(CreateInstance(prefab));
                }

                pool[prefab] = stack;
            }
        }

        void Update()
        {
            if (player == null || chunkPrefabs == null || chunkPrefabs.Length == 0)
            {
                return;
            }

            if (gameManager != null && !gameManager.IsRunning)
            {
                return;
            }

            if (!initialised)
            {
                initialised = true;
                difficultyStartZ = player.position.z;
                nextChunkZ = player.position.z + firstChunkDistance;
            }

            while (nextChunkZ < player.position.z + spawnDistanceAhead)
            {
                SpawnNextChunk();
            }

            RecycleChunksBehind();
        }

        /// <summary>
        /// After a paid continue: disarm obstacles around the player and
        /// restart the difficulty ramp from the current position.
        /// </summary>
        public void ResetForContinue()
        {
            if (player == null)
            {
                return;
            }

            difficultyStartZ = player.position.z;
            foreach (ActiveChunk chunk in activeChunks)
            {
                if (chunk.EndZ > player.position.z - 5f &&
                    chunk.EndZ - chunk.Prefab.Length < player.position.z + 60f)
                {
                    chunk.Instance.DisarmObstacles();
                }
            }
        }

        void SpawnNextChunk()
        {
            TrackChunk prefab = SelectNextPrefab();
            TrackChunk instance = TakeFromPool(prefab);
            instance.transform.position = new Vector3(0f, 0f, nextChunkZ);
            instance.gameObject.SetActive(true);
            instance.OnSpawned();

            activeChunks.Enqueue(new ActiveChunk
            {
                Instance = instance,
                Prefab = prefab,
                EndZ = nextChunkZ + prefab.Length,
            });

            spawnedChunkCount++;
            nextChunkZ += prefab.Length;
            secondLastPrefab = lastPrefab;
            lastPrefab = prefab;
            lastExitLanes = prefab.ExitSafeLanes;
        }

        void RecycleChunksBehind()
        {
            while (activeChunks.Count > 0)
            {
                ActiveChunk oldest = activeChunks.Peek();
                if (oldest.EndZ >= player.position.z - despawnDistanceBehind)
                {
                    break;
                }

                activeChunks.Dequeue();
                oldest.Instance.gameObject.SetActive(false);
                pool[oldest.Prefab].Push(oldest.Instance);
            }
        }

        TrackChunk SelectNextPrefab()
        {
            if (spawnedChunkCount < openingSafeChunkCount && chunkPrefabs.Length > 0)
            {
                return chunkPrefabs[0];
            }

            ChunkDifficulty tier = SelectTier(DistanceTravelled());

            candidates.Clear();
            float totalWeight = 0f;
            foreach (TrackChunk prefab in chunkPrefabs)
            {
                if (prefab.Difficulty != tier ||
                    prefab == lastPrefab ||
                    prefab == secondLastPrefab ||
                    !TrackChunk.Reachable(lastExitLanes, prefab.EntrySafeLanes))
                {
                    continue;
                }

                candidates.Add(prefab);
                totalWeight += prefab.Weight;
            }

            // Fall back to any reachable chunk, then to the first prefab
            // (kept as an always-safe empty stretch) so the track can never
            // dead-end or turn unfair.
            if (candidates.Count == 0)
            {
                foreach (TrackChunk prefab in chunkPrefabs)
                {
                    if (prefab != lastPrefab && TrackChunk.Reachable(lastExitLanes, prefab.EntrySafeLanes))
                    {
                        candidates.Add(prefab);
                        totalWeight += prefab.Weight;
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return chunkPrefabs[0];
            }

            float pick = Random.value * totalWeight;
            foreach (TrackChunk prefab in candidates)
            {
                pick -= prefab.Weight;
                if (pick <= 0f)
                {
                    return prefab;
                }
            }

            return candidates[candidates.Count - 1];
        }

        float DistanceTravelled() => Mathf.Max(0f, player.position.z - difficultyStartZ);

        // Difficulty comes from richer chunk design, not raw obstacle count:
        // later bands favour chunks with combinations, forced lane switches
        // and moving traffic.
        static ChunkDifficulty SelectTier(float distance)
        {
            float easy, medium, hard, special;
            if (distance < 500f)
            {
                easy = 78f; medium = 16f; hard = 0f; special = 6f;
            }
            else if (distance < 1500f)
            {
                easy = 52f; medium = 36f; hard = 6f; special = 6f;
            }
            else if (distance < 3000f)
            {
                easy = 28f; medium = 50f; hard = 15f; special = 7f;
            }
            else if (distance < 6000f)
            {
                easy = 16f; medium = 42f; hard = 34f; special = 8f;
            }
            else
            {
                easy = 10f; medium = 33f; hard = 45f; special = 12f;
            }

            float pick = Random.value * (easy + medium + hard + special);
            if ((pick -= easy) <= 0f)
            {
                return ChunkDifficulty.Easy;
            }

            if ((pick -= medium) <= 0f)
            {
                return ChunkDifficulty.Medium;
            }

            return pick - hard <= 0f ? ChunkDifficulty.Hard : ChunkDifficulty.Special;
        }

        TrackChunk TakeFromPool(TrackChunk prefab)
        {
            Stack<TrackChunk> stack = pool[prefab];
            return stack.Count > 0 ? stack.Pop() : CreateInstance(prefab);
        }

        TrackChunk CreateInstance(TrackChunk prefab)
        {
            TrackChunk instance = Instantiate(prefab, transform);
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}
