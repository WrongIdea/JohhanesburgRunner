using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// A cluster of pigeons rented from the shared pool — the "flock controller"
    /// of the spec (Part 4). Scatters birds with jittered polar offsets (never a
    /// straight line), gives each its own facing, animation offset and flight plan,
    /// and on player approach / vehicle / horn staggers the whole flock into
    /// take-off. Ticked by <see cref="PigeonSpawner"/> so a flock adds no Update.
    /// Registers with <see cref="PigeonThreatBus"/> while live so vehicle and horn
    /// events reach it without a scene search.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PigeonFlock : MonoBehaviour
    {
        public enum FlockState { Inactive, Grounded, Alerted, TakingOff, Flying, Landing, Completed }

        readonly List<PigeonController> pigeons = new List<PigeonController>(16);

        PigeonPool pool;
        Transform player;
        Vector3 center;
        float triggerDistance;
        float vehicleTriggerDistance;
        float flightHeight;
        float flightSpeed;
        float flightDuration;
        float landingChance;
        float reclaimBehind;
        float bankMax = 28f;
        float pathVariation = 1f;
        bool useLandingPoints = true;
        int district = -1;
        float landingSearchRadius = 12f;
        bool scared;
        PigeonInterestArea interestArea;

        public bool IsComplete => pigeons.Count == 0;
        public Vector3 Center => center;
        public float SpawnRadius { get; private set; }
        public float TriggerDistance => triggerDistance;
        public FlockState State { get; private set; } = FlockState.Inactive;

        public void Init(PigeonPool sharedPool, Transform playerTransform,
            float reclaimBehindDistance, float vehicleTrigger)
        {
            pool = sharedPool;
            player = playerTransform;
            reclaimBehind = reclaimBehindDistance;
            vehicleTriggerDistance = vehicleTrigger;
        }

        public void Deploy(Vector3 worldCenter, int count, float spacing, float trigger,
            float height, float speed, float duration, float landChance,
            float bankAngleMax = 28f, float pathVar = 1f, bool landingPointsEnabled = true,
            int districtIndex = -1, float landingRadius = 12f)
        {
            center = worldCenter;
            SpawnRadius = spacing;
            triggerDistance = trigger;
            flightHeight = height;
            flightSpeed = speed;
            flightDuration = duration;
            landingChance = landChance;
            bankMax = bankAngleMax;
            pathVariation = pathVar;
            useLandingPoints = landingPointsEnabled;
            district = districtIndex;
            landingSearchRadius = landingRadius;
            scared = false;
            transform.position = worldCenter;

            interestArea = PigeonInterestArea.FeedingAreaAt(worldCenter);
            bool feeding = interestArea != null;

            // Two designated coo emitters keep the flock from all cooing at once.
            int cooA = count > 0 ? Random.Range(0, count) : -1;
            int cooB = count > 1 ? (cooA + 1 + Random.Range(0, count - 1)) % count : -1;

            for (int i = 0; i < count; i++)
            {
                PigeonController pigeon = pool.GetPigeon(transform);
                if (pigeon == null)
                {
                    break; // pool exhausted — flock is simply smaller, never allocates
                }
                // Jittered polar placement so no two line up perfectly.
                float ang = Random.value * Mathf.PI * 2f;
                float rad = spacing * Mathf.Sqrt(Random.value); // uniform disc
                Vector3 offset = new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
                pigeon.transform.position = worldCenter + offset;
                pigeon.transform.rotation = Quaternion.Euler(0f, Random.value * 360f, 0f);
                pigeon.OnSpawned(height, speed, duration, landChance);

                // Fan the scatter headings out and vary bank/path per bird.
                float yawBias = count > 1 ? Mathf.Lerp(-35f, 35f, (float)i / (count - 1)) : 0f;
                pigeon.SetFlightPlan(yawBias + Random.Range(-8f, 8f), bankMax,
                    pathVariation * Random.Range(0.7f, 1.3f), spacing);
                pigeon.SetFeeding(feeding);
                pigeon.SetCanCoo(i == cooA || i == cooB);
                pigeons.Add(pigeon);
            }

            State = pigeons.Count > 0 ? FlockState.Grounded : FlockState.Completed;
            PigeonThreatBus.Register(this);
        }

        public void Tick(float dt)
        {
            if (pigeons.Count == 0)
            {
                if (State != FlockState.Completed)
                {
                    State = FlockState.Completed;
                    PigeonThreatBus.Unregister(this);
                }
                return;
            }

            // One distance check for the whole flock (never one per pigeon).
            if (!scared && player != null)
            {
                float dx = player.position.x - center.x;
                float dz = player.position.z - center.z;
                if (dx * dx + dz * dz <= triggerDistance * triggerDistance)
                {
                    ReactToPlayer();
                }
            }

            // Independent of the player: a nearby fast vehicle (taxi) also spooks
            // them. The threat bus handles horn/explicit approaches; this covers
            // vehicles that never call in, walking the maintained obstacle registry.
            // Only genuine, moving-fast threats flush the flock — a parked taxi or a
            // static barrier within range is ignored (IPigeonThreat.IsScary).
            if (!scared && vehicleTriggerDistance > 0f)
            {
                var vehicles = JoburgRunner.RunnerObstacle.ActiveObstacles;
                float rSqr = vehicleTriggerDistance * vehicleTriggerDistance;
                for (int i = 0; i < vehicles.Count; i++)
                {
                    var v = vehicles[i];
                    if (v == null)
                    {
                        continue;
                    }
                    Vector3 p = v.transform.position;
                    float vdx = p.x - center.x;
                    float vdz = p.z - center.z;
                    if (vdx * vdx + vdz * vdz > rSqr)
                    {
                        continue;
                    }
                    IPigeonThreat threat = v.GetComponent<IPigeonThreat>();
                    if (threat == null || !threat.IsScary)
                    {
                        continue;
                    }
                    ReactToVehicle(p, threat.ThreatSpeed);
                    break;
                }
            }

            bool forceRecall = player != null && (player.position.z - center.z) > reclaimBehind;

            bool anyFlying = false;
            bool anyLanding = false;
            for (int i = pigeons.Count - 1; i >= 0; i--)
            {
                PigeonController pigeon = pigeons[i];
                pigeon.Tick(dt);
                if (forceRecall || pigeon.IsFinished)
                {
                    pool.ReturnPigeon(pigeon);
                    pigeons.RemoveAt(i);
                    continue;
                }
                anyFlying |= pigeon.IsFlying;
                anyLanding |= pigeon.IsLanding;
            }

            if (pigeons.Count == 0)
            {
                State = FlockState.Completed;
                PigeonThreatBus.Unregister(this);
            }
            else if (anyFlying)
            {
                State = FlockState.Flying;
            }
            else if (scared)
            {
                State = anyLanding ? FlockState.Landing : FlockState.Alerted;
            }
        }

        // --- Reaction API (Part 4 named methods) ---

        public void ReactToPlayer() => Scatter();
        public void ReactToVehicle(Vector3 source, float speed) => Scatter();
        public void ReactToHorn(Vector3 source) => Scatter();
        public void TriggerTakeoff() => Scatter();

        /// <summary>Independent scare from any source — staggered take-off.</summary>
        void Scatter()
        {
            if (scared || pigeons.Count == 0)
            {
                return;
            }
            scared = true;
            State = FlockState.Alerted;

            for (int i = 0; i < pigeons.Count; i++)
            {
                PigeonController pigeon = pigeons[i];
                // A share of the flock reserves a perch to land on; the rest fly off
                // and despawn. Reserve before the delay so perches aren't double-taken.
                if (useLandingPoints && Random.value < landingChance)
                {
                    Vector3 approach = pigeon.transform.forward;
                    PigeonLandingPoint perch = PigeonLandingPoint.ReserveNearest(
                        pigeon.transform.position, landingSearchRadius, district, approach);
                    if (perch != null)
                    {
                        pigeon.SetLandingTarget(perch);
                    }
                }
                pigeon.Scare(Random.Range(0f, 0.25f));
            }

            if (interestArea != null)
            {
                interestArea.NotifyTakeoff(); // jacaranda petals etc.
            }
        }

        /// <summary>Force the whole flock to fly off and despawn (segment recycle).</summary>
        public void DespawnFlock()
        {
            for (int i = 0; i < pigeons.Count; i++)
            {
                pigeons[i].ForceDespawn();
            }
            scared = true;
        }

        /// <summary>Immediately return every pigeon to the pool.</summary>
        public void RecallAll()
        {
            for (int i = 0; i < pigeons.Count; i++)
            {
                pool.ReturnPigeon(pigeons[i]);
            }
            pigeons.Clear();
            ResetFlock();
        }

        /// <summary>Clear ownership and go inactive (after RecallAll / before reuse).</summary>
        public void ResetFlock()
        {
            scared = false;
            interestArea = null;
            State = FlockState.Inactive;
            PigeonThreatBus.Unregister(this);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(center, SpawnRadius);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.7f);
            Gizmos.DrawWireSphere(center, triggerDistance);
        }
#endif
    }
}
