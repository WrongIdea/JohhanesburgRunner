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
    /// Tuning comes from an optional <see cref="PigeonFlockSettings"/> asset; with
    /// none assigned it uses the serialized defaults below (unchanged behaviour).
    /// A separate cinematic flock slot lets set-pieces run without eating the
    /// ordinary two-flock budget.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PigeonSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] GameObject pigeonPrefab;
        [SerializeField] GameObject flockPrefab;
        [SerializeField] Transform player;

        [Header("Data-driven settings (optional; overrides the fields below)")]
        [SerializeField] PigeonFlockSettings settings;

        [Header("Trigger & flight")]
        [SerializeField] float triggerDistance = 15f;
        [SerializeField] float flightHeightMin = 3f;
        [SerializeField] float flightHeightMax = 5f;
        [SerializeField] float flightSpeed = 6f;
        [SerializeField] float flightDurationMin = 4f;
        [SerializeField] float flightDurationMax = 6f;
        [SerializeField, Range(0f, 1f)] float landingChance = 0.4f;
        [SerializeField] float maxBankAngle = 28f;
        [SerializeField] float pathVariation = 1f;

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
        [SerializeField] int poolMaxSize = 30;
        [SerializeField] float landingSearchRadius = 12f;
        [Tooltip("Suppress spawning until the player has run this far (tutorial guard).")]
        [SerializeField] float warmupDistance = 60f;

        [Header("Cinematic bursts (inline rare big flock)")]
        [SerializeField, Range(0f, 1f)] float cinematicChance = 0.06f;
        [SerializeField] int cinematicMinSize = 12;
        [SerializeField] int cinematicMaxSize = 15;

        [Header("Environment multipliers (manual overrides; weather/district are automatic)")]
        [SerializeField, Range(0f, 1f)] float rainSpawnMultiplier = 1f;
        [SerializeField] float districtSpawnMultiplier = 1f;
        [SerializeField] float vehicleTriggerDistance = 8f;
        [SerializeField] float hornReactionRadius = 22f;

        PigeonPool pool;
        readonly Stack<PigeonFlock> freeFlocks = new Stack<PigeonFlock>();
        readonly List<PigeonFlock> activeFlocks = new List<PigeonFlock>(4);
        PigeonFlock cinematicFlock;
        float nextSpawnZ;
        float startZ;
        bool ready;

        // Quality + weather + cinematic pacing (applied at Start).
        IPigeonWeatherProvider weather;
        int qualityMaxActive = 20;
        float cinematicMinSpacing = 500f;
        float cinematicMaxSpacing = 800f;
        float lastCinematicZ = float.NegativeInfinity;

        public float VehicleTriggerDistance => vehicleTriggerDistance;
        public float HornReactionRadius => hornReactionRadius;
        public int ActivePigeonCount => pool != null ? pool.ActiveCount : 0;
        public int AvailablePigeonCount => pool != null ? pool.AvailableCount : 0;
        public int ActiveFlockCount => activeFlocks.Count;
        public bool CinematicActive => cinematicFlock != null && !cinematicFlock.IsComplete;

        void Start()
        {
            ApplySettings();
            ApplyQuality();

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
            pool = new PigeonPool(pigeonPrefab, idle.transform, poolSize, poolMaxSize);

            // One flock per ordinary slot plus a dedicated cinematic slot.
            for (int i = 0; i < Mathf.Max(1, maxActiveFlocks); i++)
            {
                freeFlocks.Push(CreateFlock($"PigeonFlock_{i}"));
            }
            cinematicFlock = CreateFlock("PigeonFlock_Cinematic");

            startZ = player != null ? player.position.z : 0f;
            nextSpawnZ = startZ + spawnAheadDistance;
            ready = true;
        }

        PigeonFlock CreateFlock(string flockName)
        {
            GameObject go = Instantiate(flockPrefab, transform);
            go.name = flockName;
            PigeonFlock flock = go.GetComponent<PigeonFlock>();
            flock.Init(pool, player, reclaimBehindDistance, vehicleTriggerDistance);
            return flock;
        }

        void ApplySettings()
        {
            if (settings == null)
            {
                return;
            }
            triggerDistance = settings.triggerDistance;
            flightHeightMin = settings.minHeightGain;
            flightHeightMax = settings.maxHeightGain;
            flightSpeed = Mathf.Lerp(settings.minSpeed, settings.maxSpeed, 0.5f);
            flightDurationMin = settings.minFlightDuration;
            flightDurationMax = settings.maxFlightDuration;
            landingChance = settings.landingChance;
            maxBankAngle = settings.maxBankAngle;
            pathVariation = settings.flightPathVariation;
            spawnChance = settings.spawnChance;
            minFlockSize = settings.minFlockSize;
            maxFlockSize = settings.maxFlockSize;
            minFlockSpacing = settings.minEventSpacing;
            maxFlockSpacing = settings.maxEventSpacing;
            maxActiveFlocks = settings.maxActiveFlocks;
            reclaimBehindDistance = settings.cullDistance;
            flockRadius = settings.groundRoamRadius;
            vehicleTriggerDistance = settings.vehicleTriggerDistance;
            hornReactionRadius = settings.hornReactionRadius;
            cinematicChance = settings.cinematicChance;
            cinematicMinSize = settings.cinematicMinSize;
            cinematicMaxSize = settings.cinematicMaxSize;
            cinematicMinSpacing = settings.cinematicMinSpacing;
            cinematicMaxSpacing = settings.cinematicMaxSpacing;
        }

        /// <summary>
        /// Fold the active graphics tier into the pigeon caps (Part 14) and resolve
        /// the weather provider (Part 9). Safe when no QualityController exists — the
        /// statics return sensible High-tier defaults.
        /// </summary>
        public void ApplyQuality()
        {
            weather = PigeonWeatherService.Provider;
            qualityMaxActive = Mathf.Max(0, JoburgRunner.Core.QualityController.PigeonMaxActive);
            maxActiveFlocks = Mathf.Clamp(JoburgRunner.Core.QualityController.PigeonMaxFlocks, 0, maxActiveFlocks);
            reclaimBehindDistance = JoburgRunner.Core.QualityController.PigeonCullDistance;
            PigeonController.AudioEnabled = JoburgRunner.Core.QualityController.PigeonAudio;
        }

        float DistrictWeightResolved(int district) =>
            settings != null ? settings.DistrictMultiplier(district) : DistrictWeight(district);

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
                    flock.ResetFlock();
                    activeFlocks.RemoveAt(i);
                    freeFlocks.Push(flock);
                }
            }

            if (cinematicFlock != null && !cinematicFlock.IsComplete)
            {
                cinematicFlock.Tick(dt);
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
            // Don't spawn in the opening tutorial stretch.
            if (z - startZ < warmupDistance)
            {
                return;
            }
            // Heavy rain clears the ground entirely (via the weather adapter).
            if (weather != null && !weather.AllowGroundFlocks)
            {
                return;
            }

            int district = RoadSegmentVisuals.DistrictAt(z);

            // Prefer a hand-placed spawn point ahead if one is registered; else fall
            // back to procedural pavement placement.
            PigeonSpawnPoint point = PigeonSpawnPoint.NearestAhead(
                z - 10f, z - 30f, z + 30f, district);

            Vector3 center;
            float radius = flockRadius;
            float chance = spawnChance;
            int minSize = minFlockSize;
            int maxSize = maxFlockSize;
            bool allowCinematic = true;

            if (point != null)
            {
                center = point.Position;
                radius = point.Radius;
                chance = point.ResolveChance(spawnChance);
                minSize = point.ResolveMinFlock(minFlockSize);
                maxSize = point.ResolveMaxFlock(maxFlockSize);
                allowCinematic = point.AllowCinematic;
            }
            else
            {
                float side = Random.value < 0.5f ? -pavementX : pavementX;
                center = new Vector3(side, 0f, z);
            }

            float weatherMul = weather != null ? weather.GroundSpawnMultiplier : 1f;
            chance *= weatherMul                                        // rain thins flocks
                    * DistrictWeightResolved(district)                  // busier districts denser
                    * PigeonInterestArea.MultiplierAt(center)           // jacaranda/food/vendor pull
                    * rainSpawnMultiplier * districtSpawnMultiplier;    // manual overrides

            // Rare cinematic set-piece: dedicated slot, min-spacing, one-at-a-time,
            // quality-gated. Doesn't consume the ordinary flock budget.
            if (allowCinematic
                && JoburgRunner.Core.QualityController.PigeonCinematics
                && !CinematicActive
                && z - lastCinematicZ >= Random.Range(cinematicMinSpacing, cinematicMaxSpacing)
                && Random.value < cinematicChance)
            {
                int cCount = Random.Range(cinematicMinSize, cinematicMaxSize + 1);
                if (SpawnCinematic(center, cCount, radius * 2.2f))
                {
                    lastCinematicZ = z;
                    if (point != null)
                    {
                        point.MarkConsumed();
                    }
                    return;
                }
            }

            if (Random.value > chance)
            {
                return;
            }

            // Respect the quality active-pigeon cap: skip if there's no headroom for
            // even a small flock, else trim the flock to the remaining budget.
            int budget = qualityMaxActive - pool.ActiveCount;
            if (budget < minSize)
            {
                return;
            }
            if (point != null)
            {
                point.MarkConsumed();
            }

            PigeonFlock flock = freeFlocks.Pop();
            int count = Mathf.Min(Random.Range(minSize, maxSize + 1), budget);
            float height = Random.Range(flightHeightMin, flightHeightMax);
            float duration = Random.Range(flightDurationMin, flightDurationMax);
            flock.Deploy(center, count, radius, triggerDistance,
                height, flightSpeed, duration, landingChance,
                maxBankAngle, pathVariation,
                JoburgRunner.Core.QualityController.PigeonLanding && (point == null || point.HasLandingPoints),
                district, landingSearchRadius);
            activeFlocks.Add(flock);
        }

        /// <summary>
        /// Deploy a large set-piece on the dedicated cinematic slot (separate from
        /// the ordinary two-flock cap). Shrinks to fit the pool; returns false if it
        /// can't run safely. Used by <see cref="PigeonCinematicEvent"/>.
        /// </summary>
        public bool SpawnCinematic(Vector3 center, int requestedCount, float radius)
        {
            if (!ready || cinematicFlock == null || !cinematicFlock.IsComplete)
            {
                return false;
            }
            if (weather != null && !weather.AllowGroundFlocks)
            {
                return false; // heavy rain: no ground set-pieces
            }
            // Bounded by both the free pool and the quality active cap.
            int budget = Mathf.Min(pool.AvailableCount, qualityMaxActive - pool.ActiveCount);
            if (budget < 3)
            {
                return false; // not enough birds to read as an event
            }
            int count = Mathf.Min(requestedCount, budget);
            int district = RoadSegmentVisuals.DistrictAt(center.z);
            float height = Random.Range(flightHeightMin, flightHeightMax);
            float duration = Random.Range(flightDurationMin, flightDurationMax);
            cinematicFlock.Deploy(center, count, radius, triggerDistance,
                height, flightSpeed, duration, landingChance,
                maxBankAngle, pathVariation, true, district, landingSearchRadius);
            return true;
        }

        /// <summary>Raise a horn event through the threat bus (vehicles / test button).</summary>
        public void NotifyHorn(Vector3 worldPos) => PigeonThreatBus.NotifyHorn(worldPos, hornReactionRadius);

        /// <summary>Return every active pigeon to the pool (hard reset).</summary>
        public void ReturnAllPigeons()
        {
            for (int i = activeFlocks.Count - 1; i >= 0; i--)
            {
                activeFlocks[i].RecallAll();
                freeFlocks.Push(activeFlocks[i]);
            }
            activeFlocks.Clear();
            if (cinematicFlock != null)
            {
                cinematicFlock.RecallAll();
            }
        }

        static float DistrictWeight(int district) => district switch
        {
            0 => 1.4f,
            1 => 0.9f,
            2 => 1.3f,
            3 => 0.85f,
            4 => 0.7f,
            _ => 1f,
        };

        public void SetRainMultiplier(float value) => rainSpawnMultiplier = Mathf.Clamp01(value);
        public void SetDistrictMultiplier(float value) => districtSpawnMultiplier = Mathf.Max(0f, value);

        // --- Editor / debug helpers (Part 19). Safe to call in play mode. ---

        public bool SpawnTestFlockAtPlayer()
        {
            if (!ready || player == null || freeFlocks.Count == 0)
            {
                return false;
            }
            Vector3 c = new Vector3(pavementX, 0f, player.position.z + 12f);
            PigeonFlock flock = freeFlocks.Pop();
            flock.Deploy(c, Random.Range(minFlockSize, maxFlockSize + 1), flockRadius, triggerDistance,
                Random.Range(flightHeightMin, flightHeightMax), flightSpeed,
                Random.Range(flightDurationMin, flightDurationMax), landingChance,
                maxBankAngle, pathVariation, true, RoadSegmentVisuals.DistrictAt(c.z), landingSearchRadius);
            activeFlocks.Add(flock);
            return true;
        }

        public void TriggerPlayerReactionAll()
        {
            for (int i = 0; i < activeFlocks.Count; i++)
            {
                activeFlocks[i].ReactToPlayer();
            }
        }

        public void TriggerVehicleReactionAll()
        {
            for (int i = 0; i < activeFlocks.Count; i++)
            {
                activeFlocks[i].ReactToVehicle(activeFlocks[i].Center, minFlockSpacing);
            }
        }

        public void TriggerHornAll()
        {
            for (int i = 0; i < activeFlocks.Count; i++)
            {
                PigeonThreatBus.NotifyHorn(activeFlocks[i].Center, hornReactionRadius);
            }
        }

        public void ForceLandingAll() => TriggerPlayerReactionAll();

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
            Gizmos.color = new Color(0.9f, 0.3f, 0.9f, 0.4f);
            Gizmos.DrawWireSphere(l, hornReactionRadius);
        }
#endif
    }
}
