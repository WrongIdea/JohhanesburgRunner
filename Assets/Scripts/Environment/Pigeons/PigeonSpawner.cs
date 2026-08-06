using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// Scene-level pigeon director. Owns the shared <see cref="PigeonPool"/> and a
    /// fixed set of pooled flock objects, and spawns flocks ahead of the player on
    /// the pavement at a randomised spacing, gated by spawn chance and a cap on
    /// active flocks. It is the only pigeon component with an Update — it ticks all
    /// live flocks, so the whole system costs one Update callback.
    ///
    /// Weather / district multipliers and richer furniture-anchored placement are
    /// wired in later stages; the hooks (multipliers, spawn gating) already exist.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PigeonSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] GameObject pigeonPrefab;
        [SerializeField] GameObject flockPrefab;
        [SerializeField] Transform player;

        [Header("Trigger & flight")]
        [SerializeField] float triggerDistance = 15f;
        [SerializeField] float flightHeightMin = 3f;
        [SerializeField] float flightHeightMax = 5f;
        [SerializeField] float flightSpeed = 6f;
        [SerializeField] float flightDurationMin = 4f;
        [SerializeField] float flightDurationMax = 6f;
        [SerializeField, Range(0f, 1f)] float landingChance = 0.4f;

        [Header("Spawning")]
        [SerializeField, Range(0f, 1f)] float spawnChance = 0.3f;
        [SerializeField] int minFlockSize = 3;
        [SerializeField] int maxFlockSize = 8;
        [SerializeField] float minFlockSpacing = 80f;
        [SerializeField] float maxFlockSpacing = 120f;
        [SerializeField] int maxActiveFlocks = 2;
        [SerializeField] float spawnAheadDistance = 55f;
        [SerializeField] float flockRadius = 1.6f;
        [SerializeField] float pavementX = 6.2f;
        [SerializeField] float reclaimBehindDistance = 45f;
        [SerializeField] int poolSize = 20;

        [Header("Cinematic bursts")]
        [SerializeField, Range(0f, 1f)] float cinematicChance = 0.06f;
        [SerializeField] int cinematicMinSize = 12;
        [SerializeField] int cinematicMaxSize = 15;

        [Header("Environment multipliers (manual overrides; weather/district are automatic)")]
        [SerializeField, Range(0f, 1f)] float rainSpawnMultiplier = 1f;
        [SerializeField] float districtSpawnMultiplier = 1f;
        [SerializeField] float vehicleTriggerDistance = 8f;

        PigeonPool pool;
        readonly Stack<PigeonFlock> freeFlocks = new Stack<PigeonFlock>();
        readonly List<PigeonFlock> activeFlocks = new List<PigeonFlock>(4);
        float nextSpawnZ;
        bool ready;

        public float VehicleTriggerDistance => vehicleTriggerDistance;

        void Start()
        {
            if (pigeonPrefab == null || flockPrefab == null)
            {
                enabled = false;
                return;
            }
            if (player == null)
            {
                GameObject tagged = GameObject.FindGameObjectWithTag("Player");
                player = tagged != null ? tagged.transform : null;
            }

            GameObject idle = new GameObject("PigeonIdle");
            idle.transform.SetParent(transform, false);
            idle.SetActive(false);
            pool = new PigeonPool(pigeonPrefab, idle.transform, poolSize);

            for (int i = 0; i < Mathf.Max(1, maxActiveFlocks); i++)
            {
                GameObject go = Instantiate(flockPrefab, transform);
                go.name = $"PigeonFlock_{i}";
                PigeonFlock flock = go.GetComponent<PigeonFlock>();
                flock.Init(pool, player, reclaimBehindDistance, vehicleTriggerDistance);
                freeFlocks.Push(flock);
            }

            nextSpawnZ = (player != null ? player.position.z : 0f) + spawnAheadDistance;
            ready = true;
        }

        void Update()
        {
            if (!ready)
            {
                return;
            }
            float dt = Time.deltaTime;

            for (int i = activeFlocks.Count - 1; i >= 0; i--)
            {
                PigeonFlock flock = activeFlocks[i];
                flock.Tick(dt);
                if (flock.IsComplete)
                {
                    activeFlocks.RemoveAt(i);
                    freeFlocks.Push(flock);
                }
            }

            if (player == null)
            {
                return;
            }

            // Walk past each candidate slot the player has reached.
            while (player.position.z + spawnAheadDistance >= nextSpawnZ)
            {
                TryDeployAt(nextSpawnZ);
                nextSpawnZ += Random.Range(minFlockSpacing, maxFlockSpacing);
            }
        }

        void TryDeployAt(float z)
        {
            if (freeFlocks.Count == 0 || activeFlocks.Count >= maxActiveFlocks)
            {
                return;
            }
            // Heavy rain clears the ground entirely.
            if (!JoburgRunner.Environment.WeatherState.AllowsGroundFlocks)
            {
                return;
            }

            int district = RoadSegmentVisuals.DistrictAt(z);
            float chance = spawnChance
                * JoburgRunner.Environment.WeatherState.GroundFlockMultiplier   // rain thins flocks
                * DistrictWeight(district)                          // busier districts denser
                * rainSpawnMultiplier * districtSpawnMultiplier;    // manual overrides
            if (Random.value > chance)
            {
                return;
            }

            PigeonFlock flock = freeFlocks.Pop();
            float side = Random.value < 0.5f ? -pavementX : pavementX;
            Vector3 center = new Vector3(side, 0f, z);

            // Rare cinematic burst: a big flock scattered over a wider area.
            bool cinematic = Random.value < cinematicChance;
            int count = cinematic
                ? Random.Range(cinematicMinSize, cinematicMaxSize + 1)
                : Random.Range(minFlockSize, maxFlockSize + 1);
            float radius = cinematic ? flockRadius * 2.2f : flockRadius;

            float height = Random.Range(flightHeightMin, flightHeightMax);
            float duration = Random.Range(flightDurationMin, flightDurationMax);
            flock.Deploy(center, count, radius, triggerDistance,
                height, flightSpeed, duration, landingChance);
            activeFlocks.Add(flock);
        }

        /// <summary>
        /// Per-district spawn weight, mapping the spec's density guidance onto the
        /// game's districts: 0 CBD (high), 1 Commissioner (medium), 2 Park (high),
        /// 3 Business (lower), 4 Mandela Bridge (medium). -1 (no segment) = neutral.
        /// </summary>
        static float DistrictWeight(int district) => district switch
        {
            0 => 1.4f,
            1 => 0.9f,
            2 => 1.3f,
            3 => 0.85f,
            4 => 0.7f,
            _ => 1f,
        };

        /// <summary>Runtime hook for the weather system (later stage).</summary>
        public void SetRainMultiplier(float value) => rainSpawnMultiplier = Mathf.Clamp01(value);

        /// <summary>Runtime hook for district weighting (later stage).</summary>
        public void SetDistrictMultiplier(float value) => districtSpawnMultiplier = Mathf.Max(0f, value);

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.5f);
            float z = Application.isPlaying ? nextSpawnZ
                : (player != null ? player.position.z : 0f) + spawnAheadDistance;
            Vector3 l = new Vector3(-pavementX, 0f, z);
            Vector3 r = new Vector3(pavementX, 0f, z);
            Gizmos.DrawWireSphere(l, flockRadius);
            Gizmos.DrawWireSphere(r, flockRadius);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(l, triggerDistance);
            Gizmos.DrawWireSphere(r, triggerDistance);
        }
#endif
    }
}
