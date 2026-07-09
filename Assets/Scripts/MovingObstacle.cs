using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Oncoming minibus-taxi behaviour. Each taxi picks its own cruising speed,
    /// pulls off from a standstill when it activates, may brake to a sudden stop
    /// once per pass (the signature Joburg pull-over), and — when a faster taxi
    /// catches a slower one in the same lane — changes into a clear adjacent lane
    /// to overtake, then returns to its designed lane before it nears the player.
    ///
    /// Fairness: overtakes only start well ahead of the player and the taxi is
    /// steered back into its original (chunk-designed) lane before it gets close,
    /// so the survivable path the chunk laid out is restored near the player.
    /// </summary>
    public class MovingObstacle : MonoBehaviour
    {
        static readonly float[] LaneCenters = { -2.7f, 0f, 2.7f };
        static readonly List<MovingObstacle> Active = new List<MovingObstacle>();

        [Header("Speed")]
        [SerializeField] float minSpeed = 4f;
        [SerializeField] float maxSpeed = 7.5f;
        [Tooltip("How quickly a taxi pulls off from a stop toward its cruising speed.")]
        [SerializeField] float acceleration = 9f;
        [Tooltip("How hard a taxi brakes when it pulls over.")]
        [SerializeField] float braking = 16f;

        [Header("Pull-over")]
        [Tooltip("Chance (0-1) that a taxi brakes to a stop once during its approach.")]
        [SerializeField] float brakeChance = 0.3f;
        [SerializeField] Vector2 brakeHoldSeconds = new Vector2(0.5f, 1.3f);

        [Header("Overtaking")]
        [Tooltip("Sideways lane-change speed in units/second.")]
        [SerializeField] float laneChangeSpeed = 3.2f;
        [Tooltip("Overtakes only start while the taxi is inside this ahead-of-player band (metres).")]
        [SerializeField] float overtakeMinAhead = 30f;
        [SerializeField] float overtakeMaxAhead = 78f;
        [Tooltip("Below this distance ahead of the player the taxi steers back to its designed lane and makes no new lane changes.")]
        [SerializeField] float returnAhead = 26f;
        [Tooltip("A slower taxi this close ahead in the same lane triggers an overtake.")]
        [SerializeField] float overtakeGap = 10f;
        [Tooltip("An adjacent lane is clear to move into if no taxi sits within this Z window of us.")]
        [SerializeField] float laneClearWindow = 7f;

        [Tooltip("Stays parked until the player is this close, so chunk taxis hold their designed lane instead of drifting into earlier chunks.")]
        [SerializeField] float activationDistance = 70f;

        enum Drive { Cruise, Braking, Stopped }

        bool isStopped;
        GameManager gameManager;
        Transform player;

        int originalLane;
        int targetLane;
        float targetSpeed;
        float currentSpeed;
        Drive drive;
        bool brakeSpent;
        float brakeTriggerAhead;
        float brakeHoldRemaining;

        public bool IsStopped => isStopped;
        float Speed => currentSpeed;
        int ClaimedLane => targetLane;

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        /// <summary>Freezes the taxi after a crash until its chunk is recycled.</summary>
        public void StopMoving()
        {
            isStopped = true;
        }

        /// <summary>
        /// Clears the crash-stop and re-rolls this taxi's driving personality when
        /// a pooled chunk is recycled. Called by TrackChunk after the transform has
        /// been restored to its designed lane.
        /// </summary>
        public void ResetMotion()
        {
            isStopped = false;
            currentSpeed = 0f;
            drive = Drive.Cruise;
            originalLane = NearestLane(transform.position.x);
            targetLane = originalLane;
            targetSpeed = Random.Range(minSpeed, maxSpeed);

            brakeSpent = Random.value >= brakeChance;
            brakeTriggerAhead = Random.Range(18f, 34f);
            brakeHoldRemaining = 0f;
        }

        void Start()
        {
            gameManager = FindAnyObjectByType<GameManager>();
            PlayerController controller = FindAnyObjectByType<PlayerController>();
            player = controller != null ? controller.transform : null;

            if (targetSpeed <= 0f)
            {
                ResetMotion();
            }
        }

        void Update()
        {
            if (isStopped || (gameManager != null && !gameManager.IsRunning))
            {
                return;
            }

            float dt = Time.deltaTime;
            float z = transform.position.z;

            if (player != null)
            {
                float aheadOfPlayer = z - player.position.z;
                if (aheadOfPlayer > activationDistance)
                {
                    return; // parked in its designed lane until the player is close
                }

                if (!brakeSpent && drive == Drive.Cruise &&
                    aheadOfPlayer <= brakeTriggerAhead && aheadOfPlayer > 10f)
                {
                    drive = Drive.Braking;
                    brakeSpent = true;
                }

                UpdateLaneChoice(aheadOfPlayer, z);
            }

            switch (drive)
            {
                case Drive.Cruise:
                    currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * dt);
                    break;
                case Drive.Braking:
                    currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, braking * dt);
                    if (currentSpeed <= 0.01f)
                    {
                        drive = Drive.Stopped;
                        brakeHoldRemaining = Random.Range(brakeHoldSeconds.x, brakeHoldSeconds.y);
                    }
                    break;
                case Drive.Stopped:
                    brakeHoldRemaining -= dt;
                    if (brakeHoldRemaining <= 0f)
                    {
                        drive = Drive.Cruise;
                    }
                    break;
            }

            Vector3 position = transform.position;
            position.z -= currentSpeed * dt;                                  // travel toward the player
            position.x = Mathf.MoveTowards(position.x, LaneCenters[targetLane], laneChangeSpeed * dt); // steer to lane
            transform.position = position;
        }

        // Decide which lane to aim for: overtake a slower taxi ahead while far
        // from the player, then return to the designed lane as the player nears.
        void UpdateLaneChoice(float aheadOfPlayer, float z)
        {
            if (drive != Drive.Cruise)
            {
                return; // never change lanes while braking or stopped
            }

            if (targetLane != originalLane)
            {
                // Already overtaking: rejoin the designed lane once close, so the
                // configuration the player meets matches the chunk's safe layout.
                if (aheadOfPlayer < returnAhead && LaneIsClear(z, originalLane))
                {
                    targetLane = originalLane;
                }
                return;
            }

            if (aheadOfPlayer < overtakeMinAhead || aheadOfPlayer > overtakeMaxAhead)
            {
                return;
            }

            if (!BlockedBySlowerAhead(z))
            {
                return;
            }

            int lane = PickClearAdjacentLane(z, originalLane);
            if (lane >= 0)
            {
                targetLane = lane;
            }
        }

        bool BlockedBySlowerAhead(float z)
        {
            foreach (MovingObstacle other in Active)
            {
                if (other == this || other.isStopped || other.ClaimedLane != originalLane)
                {
                    continue;
                }

                float gap = z - other.transform.position.z; // >0 means other is ahead in travel dir
                if (gap > 0f && gap < overtakeGap && other.Speed < currentSpeed - 0.3f)
                {
                    return true;
                }
            }

            return false;
        }

        int PickClearAdjacentLane(float z, int lane)
        {
            // Prefer moving toward the centre lane, then the other side.
            int first = lane == 2 ? 1 : lane + 1;
            int second = lane == 0 ? 1 : lane - 1;
            if (first >= 0 && first <= 2 && first != lane && LaneIsClear(z, first))
            {
                return first;
            }

            if (second >= 0 && second <= 2 && second != lane && LaneIsClear(z, second))
            {
                return second;
            }

            return -1;
        }

        bool LaneIsClear(float z, int lane)
        {
            foreach (MovingObstacle other in Active)
            {
                if (other == this)
                {
                    continue;
                }

                if (other.ClaimedLane == lane &&
                    Mathf.Abs(other.transform.position.z - z) < laneClearWindow)
                {
                    return false;
                }
            }

            return true;
        }

        static int NearestLane(float x)
        {
            int best = 0;
            float bestDelta = Mathf.Abs(x - LaneCenters[0]);
            for (int i = 1; i < LaneCenters.Length; i++)
            {
                float delta = Mathf.Abs(x - LaneCenters[i]);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    best = i;
                }
            }

            return best;
        }
    }
}
