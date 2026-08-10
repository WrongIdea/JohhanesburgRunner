using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// A perch a flying pigeon can descend onto instead of despawning (Part 9).
    /// Self-registers to a static list on enable, so a pooled road segment that
    /// activates/deactivates its children registers and releases its perches with
    /// no manager bookkeeping. Reservation is single-threaded gameplay code, so a
    /// plain occupant count is enough.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PigeonLandingPoint : MonoBehaviour
    {
        public enum LandingType
        {
            Pavement, Bench, BusStopRoof, TrafficLightArm,
            PlanterEdge, BuildingLedge, LowWall, TreeBranch
        }

        [SerializeField] LandingType type = LandingType.Pavement;
        [SerializeField] Transform landingTransform;
        [SerializeField] int maxOccupants = 1;
        [Tooltip("-1 = any district; otherwise only pigeons whose flock is in this game-district index.")]
        [SerializeField] int districtRestriction = -1;
        [Tooltip("Approach cone half-angle (deg) around this transform's forward. 180 = any direction.")]
        [SerializeField, Range(0f, 180f)] float approachHalfAngle = 180f;

        static readonly List<PigeonLandingPoint> active = new List<PigeonLandingPoint>(32);

        int occupants;

        public LandingType Type => type;
        public int District => districtRestriction;
        public bool IsFull => occupants >= maxOccupants;
        public Transform Perch => landingTransform != null ? landingTransform : transform;
        public Vector3 Position => Perch.position;

        void OnEnable()
        {
            occupants = 0;
            active.Add(this);
        }

        void OnDisable() => active.Remove(this);

        /// <summary>Take a slot if one is free. Returns false if full.</summary>
        public bool TryReserve()
        {
            if (occupants >= maxOccupants)
            {
                return false;
            }
            occupants++;
            return true;
        }

        public void Release()
        {
            if (occupants > 0)
            {
                occupants--;
            }
        }

        /// <summary>
        /// Reserve the nearest free perch within range that accepts this district
        /// and approach direction, or null if none. Reserves before returning so
        /// two pigeons can't grab the same narrow perch in one frame.
        /// </summary>
        public static PigeonLandingPoint ReserveNearest(Vector3 from, float maxDistance, int district,
            Vector3 approachDir)
        {
            PigeonLandingPoint best = null;
            float bestSqr = maxDistance * maxDistance;
            for (int i = 0; i < active.Count; i++)
            {
                PigeonLandingPoint p = active[i];
                if (p == null || p.IsFull)
                {
                    continue;
                }
                if (p.districtRestriction >= 0 && district >= 0 && p.districtRestriction != district)
                {
                    continue;
                }
                Vector3 d = p.Position - from;
                d.y = 0f;
                float sqr = d.sqrMagnitude;
                if (sqr > bestSqr)
                {
                    continue;
                }
                if (p.approachHalfAngle < 180f && approachDir.sqrMagnitude > 0.0001f)
                {
                    Vector3 face = p.Perch.forward;
                    face.y = 0f;
                    if (Vector3.Angle(face, -approachDir) > p.approachHalfAngle)
                    {
                        continue;
                    }
                }
                bestSqr = sqr;
                best = p;
            }
            if (best != null)
            {
                best.TryReserve();
            }
            return best;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Transform t = landingTransform != null ? landingTransform : transform;
            Gizmos.color = IsFull ? new Color(1f, 0.25f, 0.2f, 0.9f) : new Color(0.3f, 0.9f, 1f, 0.9f);
            Gizmos.DrawWireCube(t.position, new Vector3(0.25f, 0.05f, 0.25f));
            Gizmos.DrawLine(t.position, t.position + Vector3.up * 0.2f);
            if (approachHalfAngle < 180f)
            {
                Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.6f);
                Gizmos.DrawRay(t.position, t.forward * 0.4f);
            }
        }
#endif
    }
}
