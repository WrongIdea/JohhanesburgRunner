using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Scene-level owner of the socket decoration system. Holds the district
    /// profiles, the shared <see cref="DecorPool"/> and the feature toggle that
    /// switches between the new socket placement and the legacy baked decoration.
    /// The road-segment spawner asks it to dress each tile as it is (re)assigned a
    /// district, so there is no per-frame work here.
    /// </summary>
    public sealed class EnvironmentDecorDirector : MonoBehaviour
    {
        public static EnvironmentDecorDirector Instance { get; private set; }

        [Header("Feature toggle")]
        [Tooltip("ON = new socket-based, district-weighted decoration. " +
                 "OFF = legacy hand-baked trees/lights on every tile. Flip to A/B compare.")]
        [SerializeField] bool useSocketDecoration = true;

        [Tooltip("Suppress district buildings and all street furniture except Jacaranda trees.")]
        [SerializeField] bool heroOnlyMode = false;

        [Header("District profiles")]
        [Tooltip("One profile per RoadSegmentVisuals district index (0..N). " +
                 "Index maps to the same district the building groups use, so decoration " +
                 "density follows the district. A null entry leaves that district undecorated.")]
        [SerializeField] DistrictDecorProfile[] profilesByDistrict;

        [Tooltip("Dedicated five-tile Jacaranda Avenue recipe. Kept outside the normal " +
                 "district rotation so the canopy always appears as one continuous section.")]
        [SerializeField] DistrictDecorProfile jacarandaAvenueProfile;

        [Header("Intersections")]
        [Tooltip("Every Nth tile is flagged an intersection (the only tiles that may " +
                 "spawn traffic lights). 1 = every tile, 3 = every third. Districts whose " +
                 "profile has no traffic-light rule stay light-free even on intersection tiles.")]
        [SerializeField, Min(1)] int intersectionEvery = 3;

        [Header("Pooling")]
        [Tooltip("Instances pre-created per distinct decoration prefab at startup. " +
                 "Sized so no Instantiate happens during play; raise if you see pool-growth warnings.")]
        [SerializeField, Min(1)] int prewarmPerPrefab = 12;

        [Header("Hero buildings")]
        [Tooltip("Catalogue of roadside hero buildings. Null = hero spawning disabled.")]
        [SerializeField] HeroBuildingSet heroSet;

        [Tooltip("Allow the reusable Johannesburg mid-rise pool in every district. " +
                 "Disable to restrict hero buildings to CBD segments only.")]
        [SerializeField] bool heroAllDistricts = true;

        [Tooltip("Instances pre-created per hero prefab. Kept low (heroes are large and rare); " +
                 "the pool grows lazily with a warning if a tier runs dry.")]
        [SerializeField, Min(0)] int heroPrewarm = 1;

        [Tooltip("Minimum CBD appearances between hero buildings (inclusive).")]
        [SerializeField, Min(1)] int heroMinGap = 2;

        [Tooltip("Maximum CBD appearances between hero buildings (inclusive).")]
        [SerializeField, Min(1)] int heroMaxGap = 4;

        [Tooltip("Guarantee a hero within this many of the run's first CBD appearances.")]
        [SerializeField, Min(1)] int heroEarlyGuaranteeWithin = 2;

        [Tooltip("How many recently-used hero prefabs to avoid re-picking (prevents back-to-back repeats).")]
        [SerializeField, Min(0)] int heroAvoidRecent = 1;

        [Tooltip("When ON, a hero block spawns a (different) building on BOTH sides. " +
                 "When OFF, one hero per block, alternating sides.")]
        [SerializeField] bool heroBothSides = true;

        public bool UseSocketDecoration => useSocketDecoration;
        public bool IsReady { get; private set; }

        // CBD is district index 0 (CBDOfficeStreet building group + District_CBD profile).
        const int CbdDistrictIndex = 0;

        DecorPool pool;
        DecorPool heroPool;
        int placedSegmentCount;

        // Hero cadence state (cross-tile; the director is the only place with it).
        int cbdAppearances;        // eligible CBD dresses seen this run
        int nextHeroAtCbd = -1;    // cbdAppearances value at which the next hero spawns (-1 = uninitialised)
        HeroSide lastHeroSide = HeroSide.None;
        readonly Queue<GameObject> recentHeroPrefabs = new Queue<GameObject>(); // avoid the same source design
        readonly List<int> heroCandidates = new List<int>(16);      // reusable pick scratch
        readonly List<float> heroWeights = new List<float>(16);

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Start()
        {
            BuildPool();
            IsReady = true;
        }

        void BuildPool()
        {
            var idle = new GameObject("DecorPool (idle)").transform;
            idle.SetParent(transform, false);
            idle.gameObject.SetActive(false);
            pool = new DecorPool(idle);

            BuildHeroPool();

            if (!useSocketDecoration || profilesByDistrict == null)
            {
                return;
            }

            // Warm one bucket per distinct prefab referenced by any profile.
            var seen = new HashSet<GameObject>();
            foreach (DistrictDecorProfile profile in profilesByDistrict)
            {
                PrewarmProfile(profile, seen);
            }
            PrewarmProfile(jacarandaAvenueProfile, seen);
        }

        void PrewarmProfile(DistrictDecorProfile profile, HashSet<GameObject> seen)
        {
            if (profile == null || profile.rules == null)
            {
                return;
            }

            foreach (DecorRule rule in profile.rules)
            {
                if (rule?.allowedPrefabs == null)
                {
                    continue;
                }

                foreach (GameObject prefab in rule.allowedPrefabs)
                {
                    if (prefab != null && seen.Add(prefab))
                    {
                        pool.Prewarm(prefab, prewarmPerPrefab);
                    }
                }
            }
        }

        /// <summary>
        /// Dresses a tile for the given district. Called by the spawner each time a
        /// segment is placed or recycled. Generates a stable per-tile seed and the
        /// intersection flag, then defers to the tile's <see cref="SegmentDecorator"/>.
        /// </summary>
        public void DecorateSegment(
            SegmentDecorator decorator, int districtIndex,
            JunctionType junctionType = JunctionType.None,
            bool faceHeroBuildingsTowardRunner = false)
        {
            if (decorator == null || pool == null)
            {
                return;
            }

            if (!useSocketDecoration)
            {
                decorator.ApplyLegacyMode(pool);
                decorator.ClearHeroes(heroPool); // clear any hero from a prior dress
                return;
            }

            DistrictDecorProfile profile = ProfileFor(districtIndex);
            int seed = unchecked(placedSegmentCount * 73856093 + districtIndex * 19349663 + 12345);
            bool isIntersection = junctionType != JunctionType.None ||
                                  placedSegmentCount % intersectionEvery == 0;
            bool hasTrafficLights = profile?.GetRule(DecorSocketType.TrafficLight) != null;
            // Only alternating eligible intersections receive zebra markings, so
            // traffic lights remain common without every robot looking identical.
            bool showPedestrianCrossing = isIntersection && hasTrafficLights &&
                                          (placedSegmentCount / intersectionEvery) % 2 == 0;
            int decorationSequence = placedSegmentCount;
            placedSegmentCount++;

            decorator.Decorate(
                profile, seed, isIntersection, pool,
                heroOnlyMode ? DecorSocketType.Tree : (DecorSocketType?)null,
                showPedestrianCrossing, decorationSequence, junctionType);
            if (junctionType == JunctionType.None)
            {
                ResolveHero(decorator, districtIndex, faceHeroBuildingsTowardRunner);
            }
            else
            {
                decorator.ClearHeroes(heroPool);
            }
        }

        public void DecorateJacarandaSegment(SegmentDecorator decorator)
        {
            if (decorator == null || pool == null)
            {
                return;
            }

            int seed = unchecked(placedSegmentCount * 73856093 + 0x4A4341);
            placedSegmentCount++;
            decorator.Decorate(jacarandaAvenueProfile, seed, false, pool);
            decorator.ClearHeroes(heroPool);
        }

        // ---------------------------------------------------------------- hero buildings
        void BuildHeroPool()
        {
            var heroIdle = new GameObject("HeroPool (idle)").transform;
            heroIdle.SetParent(transform, false);
            heroIdle.gameObject.SetActive(false);
            heroPool = new DecorPool(heroIdle);

            if (heroSet == null || heroSet.entries == null)
            {
                return;
            }

            // Large buildings must never be instantiated during a run. Four per
            // design covers the two-sided seven-tile ring with recent-item avoidance.
            int safeHeroPrewarm = Mathf.Max(4, heroPrewarm);
            var seen = new HashSet<GameObject>();
            foreach (HeroEntry e in heroSet.entries)
            {
                if (e != null && e.enabled && e.prefab != null && seen.Add(e.prefab))
                {
                    heroPool.Prewarm(e.prefab, safeHeroPrewarm);
                }
            }
        }

        bool IsCbd(int districtIndex)
        {
            int mod = profilesByDistrict != null && profilesByDistrict.Length > 0
                ? profilesByDistrict.Length : 5;
            return Mathf.Abs(districtIndex) % mod == CbdDistrictIndex;
        }

        /// <summary>
        /// Decides whether the just-dressed tile gets a hero building and, if so, spawns
        /// one; otherwise clears any hero the recycled tile carried.
        ///
        /// Cadence counts only eligible CBD appearances (never ordinary segments). A hero
        /// is guaranteed within the first <see cref="heroEarlyGuaranteeWithin"/> CBD blocks
        /// of the run; thereafter the gap to the next hero is drawn inclusively from
        /// [<see cref="heroMinGap"/>, <see cref="heroMaxGap"/>] CBD appearances. Because the
        /// spawner calls DecorateSegment exactly once per newly-dressed tile, the counter
        /// advances at most once per real CBD block (recycling cannot double-count).
        /// </summary>
        void ResolveHero(
            SegmentDecorator decorator, int districtIndex,
            bool faceBuildingsTowardRunner = false)
        {
            if (heroPool == null)
            {
                return;
            }

            bool eligible = heroSet != null && heroSet.Count > 0 &&
                            (heroAllDistricts || IsCbd(districtIndex));
            if (!eligible)
            {
                decorator.ClearHeroes(heroPool); // non-CBD tile: clear any hero
                return;
            }

            cbdAppearances++;

            // First target: guarantee a hero within the first N CBD appearances of the run.
            if (nextHeroAtCbd < 0)
            {
                nextHeroAtCbd = Random.Range(1, Mathf.Max(1, heroEarlyGuaranteeWithin) + 1);
            }

            if (cbdAppearances < nextHeroAtCbd)
            {
                decorator.ClearHeroes(heroPool); // this CBD block stays hero-free
                return;
            }

            decorator.ClearHeroes(heroPool); // reset both slots, then place this block's heroes

            if (heroBothSides)
            {
                // A (different) building on each side. Two picks in a row are distinct
                // while avoidRecent >= 1, so the two sides never show the same prefab.
                HeroEntry el = PickEntry(HeroSide.Left);
                HeroEntry er = PickEntry(HeroSide.Right);
                if (el != null) decorator.SetHero(el, HeroSide.Left, heroPool, faceBuildingsTowardRunner);
                if (er != null) decorator.SetHero(er, HeroSide.Right, heroPool, faceBuildingsTowardRunner);
            }
            else
            {
                HeroSide side = NextSide();
                HeroEntry entry = PickEntry(side);
                if (entry == null)
                {
                    side = Opposite(side);
                    entry = PickEntry(side);
                }
                if (entry != null)
                {
                    decorator.SetHero(entry, side, heroPool, faceBuildingsTowardRunner);
                    lastHeroSide = side;
                }
            }

            // Schedule the next hero: a gap of [min,max] CBD appearances from now.
            int lo = Mathf.Max(1, heroMinGap);
            int hi = Mathf.Max(lo, heroMaxGap);
            nextHeroAtCbd = cbdAppearances + Random.Range(lo, hi + 1);
        }

        static HeroSide Opposite(HeroSide side) =>
            side == HeroSide.Left ? HeroSide.Right : HeroSide.Left;

        HeroSide NextSide()
        {
            if (lastHeroSide == HeroSide.Left) return HeroSide.Right;
            if (lastHeroSide == HeroSide.Right) return HeroSide.Left;
            return Random.value < 0.5f ? HeroSide.Left : HeroSide.Right; // first hero: random side
        }

        /// <summary>Weighted pick among enabled entries that support the side, avoiding the
        /// most-recent <see cref="heroAvoidRecent"/> entries so the same building never repeats
        /// back-to-back. Falls back to ignoring the recent filter if it excludes everything.</summary>
        HeroEntry PickEntry(HeroSide side)
        {
            HeroEntry[] entries = heroSet.entries;

            int chosen = PickWeighted(entries, side, respectRecent: true);
            if (chosen < 0)
            {
                chosen = PickWeighted(entries, side, respectRecent: false);
            }

            if (chosen < 0)
            {
                return null;
            }

            recentHeroPrefabs.Enqueue(entries[chosen].prefab);
            while (recentHeroPrefabs.Count > heroAvoidRecent)
            {
                recentHeroPrefabs.Dequeue();
            }

            return entries[chosen];
        }

        int PickWeighted(HeroEntry[] entries, HeroSide side, bool respectRecent)
        {
            heroCandidates.Clear();
            heroWeights.Clear();
            float total = 0f;

            for (int i = 0; i < entries.Length; i++)
            {
                HeroEntry e = entries[i];
                if (e == null || !e.enabled || e.prefab == null || !e.SupportsSide(side))
                {
                    continue;
                }

                if (respectRecent && recentHeroPrefabs.Contains(e.prefab))
                {
                    continue;
                }

                float w = Mathf.Max(0.0001f, e.weight);
                heroCandidates.Add(i);
                heroWeights.Add(w);
                total += w;
            }

            if (heroCandidates.Count == 0)
            {
                return -1;
            }

            float r = Random.value * total;
            for (int i = 0; i < heroCandidates.Count; i++)
            {
                r -= heroWeights[i];
                if (r <= 0f)
                {
                    return heroCandidates[i];
                }
            }

            return heroCandidates[heroCandidates.Count - 1];
        }

        DistrictDecorProfile ProfileFor(int districtIndex)
        {
            if (profilesByDistrict == null || profilesByDistrict.Length == 0)
            {
                return null;
            }

            int index = Mathf.Abs(districtIndex) % profilesByDistrict.Length;
            return profilesByDistrict[index];
        }
    }
}
