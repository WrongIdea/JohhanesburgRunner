using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// Anything that can spook a flock: a minibus taxi, car, bus or police
    /// vehicle. A vehicle raises itself through <see cref="PigeonThreatBus"/>
    /// rather than every flock polling the scene, so the reaction stays O(active
    /// flocks) — never a scene-wide search.
    /// </summary>
    public interface IPigeonThreat
    {
        /// <summary>World position the threat radiates from.</summary>
        Vector3 ThreatPosition { get; }

        /// <summary>Ground speed in world units/second.</summary>
        float ThreatSpeed { get; }

        /// <summary>How close the threat must pass to spook a flock.</summary>
        float ThreatRadius { get; }

        /// <summary>False while parked / crawling so slow traffic is ignored.</summary>
        bool IsScary { get; }
    }

    /// <summary>
    /// Central, allocation-free scatter hub. Deployed flocks register themselves
    /// (never more than the ordinary cap + one cinematic), so a vehicle-approach
    /// or horn event reaches nearby flocks by walking a tiny list. No
    /// <c>FindObjectsOfType</c>, no per-frame scene scan.
    /// </summary>
    public static class PigeonThreatBus
    {
        static readonly List<PigeonFlock> flocks = new List<PigeonFlock>(4);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Clear() => flocks.Clear();

        public static void Register(PigeonFlock flock)
        {
            if (flock != null && !flocks.Contains(flock))
            {
                flocks.Add(flock);
            }
        }

        public static void Unregister(PigeonFlock flock) => flocks.Remove(flock);

        /// <summary>A vehicle passed close and fast — scare flocks in its radius.</summary>
        public static void NotifyVehicleApproach(IPigeonThreat threat)
        {
            if (threat == null || !threat.IsScary)
            {
                return;
            }
            Vector3 p = threat.ThreatPosition;
            float rSqr = threat.ThreatRadius * threat.ThreatRadius;
            for (int i = 0; i < flocks.Count; i++)
            {
                PigeonFlock f = flocks[i];
                if (f == null)
                {
                    continue;
                }
                if (HorizontalSqr(f.Center, p) <= rSqr)
                {
                    f.ReactToVehicle(p, threat.ThreatSpeed);
                }
            }
        }

        /// <summary>Explicit approach without an interface (test buttons, ad-hoc callers).</summary>
        public static void NotifyVehicleApproach(Vector3 worldPos, float speed, float radius)
        {
            float rSqr = radius * radius;
            for (int i = 0; i < flocks.Count; i++)
            {
                PigeonFlock f = flocks[i];
                if (f != null && HorizontalSqr(f.Center, worldPos) <= rSqr)
                {
                    f.ReactToVehicle(worldPos, speed);
                }
            }
        }

        /// <summary>A horn sounded — scare flocks inside the horn radius.</summary>
        public static void NotifyHorn(Vector3 worldPos, float radius)
        {
            float rSqr = radius * radius;
            for (int i = 0; i < flocks.Count; i++)
            {
                PigeonFlock f = flocks[i];
                if (f != null && HorizontalSqr(f.Center, worldPos) <= rSqr)
                {
                    f.ReactToHorn(worldPos);
                }
            }
        }

        static float HorizontalSqr(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
