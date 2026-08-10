using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// A hand-placed hint that "a ground flock belongs here" — taxi ranks, bus
    /// stops, benches, plazas, pavement corners (Part 10). Self-registers on
    /// enable so road segments carrying spawn points contribute them for the
    /// duration they're live, and withdraw them when pooled. The spawner prefers
    /// registered points ahead of the runner; when none are near it falls back to
    /// its own procedural pavement placement, so this component is purely additive.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PigeonSpawnPoint : MonoBehaviour
    {
        [SerializeField] float spawnRadius = 1.6f;
        [Tooltip("-1 = use spawner/settings default.")]
        [SerializeField] int minFlockSizeOverride = -1;
        [SerializeField] int maxFlockSizeOverride = -1;
        [Tooltip("<0 = use spawner/settings default.")]
        [SerializeField, Range(-1f, 1f)] float spawnChanceOverride = -1f;
        [Tooltip("Empty = any district.")]
        [SerializeField] int[] allowedDistricts;
        [SerializeField] Transform groundSurface;
        [SerializeField] Vector3 safeAreaSize = new Vector3(3f, 2f, 3f);
        [SerializeField] bool hasLandingPoints = true;
        [SerializeField] bool allowCinematic = true;
        [SerializeField] bool drawGizmos = true;

        static readonly List<PigeonSpawnPoint> active = new List<PigeonSpawnPoint>(32);

        bool consumedThisPass;

        public float Radius => spawnRadius;
        public bool HasLandingPoints => hasLandingPoints;
        public bool AllowCinematic => allowCinematic;
        public Vector3 Position => groundSurface != null ? groundSurface.position : transform.position;

        void OnEnable()
        {
            consumedThisPass = false;
            active.Add(this);
        }

        void OnDisable() => active.Remove(this);

        public bool AcceptsDistrict(int district)
        {
            if (allowedDistricts == null || allowedDistricts.Length == 0 || district < 0)
            {
                return true;
            }
            for (int i = 0; i < allowedDistricts.Length; i++)
            {
                if (allowedDistricts[i] == district)
                {
                    return true;
                }
            }
            return false;
        }

        public int ResolveMinFlock(int fallback) => minFlockSizeOverride >= 0 ? minFlockSizeOverride : fallback;
        public int ResolveMaxFlock(int fallback) => maxFlockSizeOverride >= 0 ? maxFlockSizeOverride : fallback;
        public float ResolveChance(float fallback) => spawnChanceOverride >= 0f ? spawnChanceOverride : fallback;

        /// <summary>Mark used so the same corner doesn't fire twice per pass.</summary>
        public void MarkConsumed() => consumedThisPass = true;

        /// <summary>
        /// Nearest unused registered point in the Z window ahead of the runner that
        /// accepts the local district, or null. Cheap: walks the small active list.
        /// </summary>
        public static PigeonSpawnPoint NearestAhead(float fromZ, float windowStart, float windowEnd, int district)
        {
            PigeonSpawnPoint best = null;
            float bestDz = float.MaxValue;
            for (int i = 0; i < active.Count; i++)
            {
                PigeonSpawnPoint p = active[i];
                if (p == null || p.consumedThisPass || !p.AcceptsDistrict(district))
                {
                    continue;
                }
                float z = p.Position.z;
                if (z < windowStart || z > windowEnd)
                {
                    continue;
                }
                float dz = z - fromZ;
                if (dz < bestDz)
                {
                    bestDz = dz;
                    best = p;
                }
            }
            return best;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }
            Vector3 c = Position;
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(c, spawnRadius);
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.3f);
            Gizmos.DrawWireCube(c, safeAreaSize);
        }
#endif
    }
}
