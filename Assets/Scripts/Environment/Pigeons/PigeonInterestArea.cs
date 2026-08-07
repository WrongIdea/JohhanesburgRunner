using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// An attractor that raises spawn probability and biases behaviour: jacaranda
    /// trees, food, vendors, taxi ranks, parks, shelter (Part 14). Self-registers
    /// so it works with pooled segments. Exposes an optional petal
    /// <see cref="ParticleSystem"/> and a takeoff <see cref="UnityEvent"/> so the
    /// jacaranda "purple petals on takeoff" hook exists without editing any tree.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PigeonInterestArea : MonoBehaviour
    {
        public enum InterestType { Jacaranda, Food, Vendor, TaxiRank, Park, Shelter }

        [SerializeField] InterestType type = InterestType.Food;
        [SerializeField] float radius = 4f;
        [SerializeField] float spawnMultiplier = 1.4f;
        [Tooltip("Bias ground behaviour toward Idle/Peck/Walk (feeding) inside this area.")]
        [SerializeField] bool preferFeeding = true;
        [SerializeField] ParticleSystem petals;
        [SerializeField] UnityEvent onFlockTakeoff;

        static readonly List<PigeonInterestArea> active = new List<PigeonInterestArea>(16);

        public InterestType Type => type;
        public bool PreferFeeding => preferFeeding;
        public float Radius => radius;

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        /// <summary>
        /// Strongest interest multiplier overlapping a world position (1 if none).
        /// Max rather than product so many baked roadside trees can't stack into a
        /// runaway spawn boost.
        /// </summary>
        public static float MultiplierAt(Vector3 worldPos)
        {
            float m = 1f;
            for (int i = 0; i < active.Count; i++)
            {
                PigeonInterestArea a = active[i];
                if (a == null)
                {
                    continue;
                }
                float dx = a.transform.position.x - worldPos.x;
                float dz = a.transform.position.z - worldPos.z;
                if (dx * dx + dz * dz <= a.radius * a.radius && a.spawnMultiplier > m)
                {
                    m = a.spawnMultiplier;
                }
            }
            return m;
        }

        /// <summary>The feeding-preference area covering a position, or null.</summary>
        public static PigeonInterestArea FeedingAreaAt(Vector3 worldPos)
        {
            for (int i = 0; i < active.Count; i++)
            {
                PigeonInterestArea a = active[i];
                if (a == null || !a.preferFeeding)
                {
                    continue;
                }
                float dx = a.transform.position.x - worldPos.x;
                float dz = a.transform.position.z - worldPos.z;
                if (dx * dx + dz * dz <= a.radius * a.radius)
                {
                    return a;
                }
            }
            return null;
        }

        /// <summary>Called by a flock as it lifts off — drops petals if wired.</summary>
        public void NotifyTakeoff()
        {
            if (petals != null)
            {
                petals.Play();
            }
            onFlockTakeoff?.Invoke();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Color c = type switch
            {
                InterestType.Jacaranda => new Color(0.6f, 0.4f, 0.9f, 0.5f),
                InterestType.TaxiRank => new Color(0.95f, 0.75f, 0.2f, 0.5f),
                InterestType.Park => new Color(0.3f, 0.85f, 0.4f, 0.5f),
                _ => new Color(0.9f, 0.6f, 0.3f, 0.5f),
            };
            Gizmos.color = c;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }
}
