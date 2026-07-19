using UnityEngine;

namespace JoburgRunner.Environment
{
    /// <summary>
    /// The ordered set of zones a route can travel through, plus how far the
    /// player runs in each before the route advances to the next. Authored in
    /// the Inspector so designers can add, reorder, or reweight destinations
    /// without code. Pure data; <see cref="EnvironmentDirector"/> consumes it.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Zone Catalog", fileName = "ZoneCatalog")]
    public sealed class ZoneCatalog : ScriptableObject
    {
        [Tooltip("All zones eligible for a route. Order is only a default; selection is weighted + distance-gated.")]
        public EnvironmentZoneProfile[] zones = new EnvironmentZoneProfile[0];

        [Tooltip("Zone used at the very start of every run (falls back to the first entry if unset).")]
        public EnvironmentZoneProfile openingZone;

        [Tooltip("Metres travelled in one zone before the route advances to another.")]
        [Min(50f)] public float metresPerZone = 800f;

        [Tooltip("Avoid re-selecting this many of the most-recent zones when advancing.")]
        [Min(0)] public int avoidRecentZones = 1;

        public EnvironmentZoneProfile OpeningZone =>
            openingZone != null ? openingZone : (zones.Length > 0 ? zones[0] : null);

        /// <summary>
        /// Weighted, distance-gated, no-repeat zone pick. Returns null only
        /// when the catalog is empty.
        /// </summary>
        public EnvironmentZoneProfile Pick(float runDistance, NoRepeatPicker state)
        {
            if (zones == null || zones.Length == 0)
            {
                return null;
            }

            // Only zones unlocked by distance are eligible; if none are yet,
            // fall back to the opening zone so a run always has a valid look.
            float totalWeight = 0f;
            int eligibleCount = 0;
            foreach (EnvironmentZoneProfile zone in zones)
            {
                if (zone != null && runDistance >= zone.minRunDistance)
                {
                    totalWeight += Mathf.Max(0f, zone.routeWeight);
                    eligibleCount++;
                }
            }

            if (eligibleCount == 0 || totalWeight <= 0f)
            {
                return OpeningZone;
            }

            // The no-repeat picker indexes the full zone array; retry a few
            // times so a just-used zone is skipped without starving selection.
            for (int attempt = 0; attempt < 8; attempt++)
            {
                int index = state.Next(zones.Length);
                EnvironmentZoneProfile candidate = index >= 0 ? zones[index] : null;
                if (candidate != null && runDistance >= candidate.minRunDistance && candidate.routeWeight > 0f)
                {
                    return candidate;
                }
            }

            return OpeningZone;
        }
    }
}
