using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// A rare, hand-placed set-piece (Part 16): a big feeding flock at a zebra
    /// crossing, taxi rank or jacaranda that lifts off as the runner arrives. Runs
    /// on the spawner's dedicated cinematic slot, so it never eats the ordinary
    /// two-flock budget, and only one cinematic can be live at a time. If the pool
    /// can't supply it safely the spawner shrinks or refuses the event, so it never
    /// starves ordinary flocks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PigeonCinematicEvent : MonoBehaviour
    {
        public enum EventType { ZebraCrossing, TaxiRank, Jacaranda }

        [SerializeField] EventType type = EventType.ZebraCrossing;
        [SerializeField] PigeonSpawner spawner;
        [SerializeField] Transform player;
        [SerializeField] int flockSize = 8;
        [SerializeField] float radius = 3f;
        [SerializeField] float triggerDistance = 15f;
        [Tooltip("Only arm once the runner is within this far ahead, so it can't fire from across the map.")]
        [SerializeField] float armDistance = 60f;

        bool triggered;
        float triggerSqr;
        float armSqr;

        void Start()
        {
            triggerSqr = triggerDistance * triggerDistance;
            armSqr = armDistance * armDistance;
            if (spawner == null)
            {
                spawner = FindFirstObjectByType<PigeonSpawner>();
            }
            if (player == null)
            {
                GameObject tagged = GameObject.FindGameObjectWithTag("Player");
                player = tagged != null ? tagged.transform : null;
            }
        }

        void Update()
        {
            if (triggered || spawner == null || player == null)
            {
                return;
            }
            float dx = player.position.x - transform.position.x;
            float dz = player.position.z - transform.position.z;
            float sqr = dx * dx + dz * dz;
            if (sqr > armSqr)
            {
                return; // still too far ahead to bother
            }
            if (sqr <= triggerSqr)
            {
                // The runner has arrived: deploy pre-scared so it lifts off on cue.
                if (spawner.SpawnCinematic(transform.position, flockSize, radius))
                {
                    triggered = true;
                    // A frame later the flock is grounded; the player's own proximity
                    // (already inside triggerDistance) scatters it via the flock's
                    // player check on its first tick — a coordinated, on-cue takeoff.
                }
            }
        }

        /// <summary>Re-arm this event (e.g. when a pooled segment carrying it recycles).</summary>
        public void Rearm() => triggered = false;

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = type switch
            {
                EventType.Jacaranda => new Color(0.6f, 0.4f, 0.9f, 0.7f),
                EventType.TaxiRank => new Color(0.95f, 0.75f, 0.2f, 0.7f),
                _ => new Color(0.3f, 0.8f, 1f, 0.7f),
            };
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, triggerDistance);
        }
#endif
    }
}
