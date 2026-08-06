using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// A cluster of 3–8 pigeons rented from the shared pool. Scatters them with
    /// jittered polar offsets (never a straight line), gives each its own facing
    /// and animation offset, and on player approach staggers the whole flock into
    /// take-off. Ticked by <see cref="PigeonSpawner"/> so a flock adds no Update.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PigeonFlock : MonoBehaviour
    {
        readonly List<PigeonController> pigeons = new List<PigeonController>(8);

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
        bool scared;

        public bool IsComplete => pigeons.Count == 0;
        public Vector3 Center => center;
        public float SpawnRadius { get; private set; }
        public float TriggerDistance => triggerDistance;

        public void Init(PigeonPool sharedPool, Transform playerTransform,
            float reclaimBehindDistance, float vehicleTrigger)
        {
            pool = sharedPool;
            player = playerTransform;
            reclaimBehind = reclaimBehindDistance;
            vehicleTriggerDistance = vehicleTrigger;
        }

        public void Deploy(Vector3 worldCenter, int count, float spacing, float trigger,
            float height, float speed, float duration, float landChance)
        {
            center = worldCenter;
            SpawnRadius = spacing;
            triggerDistance = trigger;
            flightHeight = height;
            flightSpeed = speed;
            flightDuration = duration;
            landingChance = landChance;
            scared = false;
            transform.position = worldCenter;

            for (int i = 0; i < count; i++)
            {
                PigeonController pigeon = pool.Get(transform);
                if (pigeon == null)
                {
                    break;
                }
                // Jittered polar placement so no two line up perfectly.
                float ang = Random.value * Mathf.PI * 2f;
                float rad = spacing * Mathf.Sqrt(Random.value); // uniform disc
                Vector3 offset = new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
                pigeon.transform.position = worldCenter + offset;
                pigeon.transform.rotation = Quaternion.Euler(0f, Random.value * 360f, 0f);
                pigeon.OnSpawned(height, speed, duration, landChance);
                pigeons.Add(pigeon);
            }
        }

        public void Tick(float dt)
        {
            if (pigeons.Count == 0)
            {
                return;
            }

            if (!scared && player != null)
            {
                float dx = player.position.x - center.x;
                float dz = player.position.z - center.z;
                if (dx * dx + dz * dz <= triggerDistance * triggerDistance)
                {
                    ScatterAll();
                }
            }

            // Independent of the player: a nearby vehicle (taxi) also spooks them.
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
                    if (vdx * vdx + vdz * vdz <= rSqr)
                    {
                        ScatterAll();
                        break;
                    }
                }
            }

            // Reclaim whole flock once it is well behind the player (off-screen).
            bool forceRecall = player != null && (player.position.z - center.z) > reclaimBehind;

            for (int i = pigeons.Count - 1; i >= 0; i--)
            {
                PigeonController pigeon = pigeons[i];
                pigeon.Tick(dt);
                if (forceRecall || pigeon.IsFinished)
                {
                    pool.Return(pigeon);
                    pigeons.RemoveAt(i);
                }
            }
        }

        /// <summary>Independent scare (e.g. a passing vehicle) — used by later stages.</summary>
        public void ScatterAll()
        {
            if (scared)
            {
                return;
            }
            scared = true;
            for (int i = 0; i < pigeons.Count; i++)
            {
                pigeons[i].Scare(Random.Range(0f, 0.25f));
            }
        }

        public void RecallAll()
        {
            for (int i = 0; i < pigeons.Count; i++)
            {
                pool.Return(pigeons[i]);
            }
            pigeons.Clear();
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
