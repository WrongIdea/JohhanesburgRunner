using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// JRPD traffic officer summoned when the runner scrapes a taxi's side
    /// (a survivable offence). He runs a few metres behind the player and
    /// gives up after a few seconds; if the player scrapes another taxi while
    /// he is still chasing, the run ends and the officer sprints up to stand
    /// next to the fallen runner.
    /// </summary>
    public class TrafficOfficerChase : MonoBehaviour
    {
        [SerializeField] GameManager gameManager;
        [SerializeField] Transform player;
        [SerializeField] GameObject officerVisual;
        [Tooltip("Seconds the officer keeps chasing after a side bump.")]
        [SerializeField] float chaseSeconds = 5f;
        [SerializeField] float followDistance = 2.8f;
        [Tooltip("Seconds to settle sideways onto the player's lane.")]
        [SerializeField] float lateralSmoothTime = 0.18f;
        [Tooltip("Speed of the final sprint to the fallen player's side.")]
        [SerializeField] float catchUpSpeed = 24f;
        [Tooltip("How far behind the player the officer despawns when he gives up.")]
        [SerializeField] float giveUpDropBack = 16f;

        enum State { Hidden, Chasing, GivingUp, Catching, Caught }

        State state = State.Hidden;
        Animator animator;
        float chaseEndTime;
        float lateralVelocity;
        float caughtSideOffset;

        public bool IsChasing => state == State.Chasing;

        void Awake()
        {
            animator = GetComponentInChildren<Animator>(true);
            if (officerVisual != null)
            {
                officerVisual.SetActive(false);
            }
        }

        /// <summary>First side bump: appear behind the runner and give chase.</summary>
        public void StartChase()
        {
            if (player == null || officerVisual == null)
            {
                return;
            }

            chaseEndTime = Time.time + chaseSeconds;
            if (state == State.Chasing)
            {
                return; // already on the player's tail: just extend the chase
            }

            state = State.Chasing;
            lateralVelocity = 0f;
            transform.position = new Vector3(player.position.x, 0f, player.position.z - followDistance * 2.2f);
            transform.rotation = Quaternion.identity;
            officerVisual.SetActive(true);
            if (animator != null)
            {
                animator.speed = 1f;
            }
        }

        /// <summary>Second side bump: run up beside the fallen player and stop.</summary>
        public void CatchPlayer()
        {
            if (state == State.Hidden || player == null)
            {
                return;
            }

            state = State.Catching;
            // Stand on whichever side of the player has road left.
            caughtSideOffset = player.position.x > 0f ? -1.5f : 1.5f;
        }

        /// <summary>Hides the officer instantly (used by continue/revive).</summary>
        public void Dismiss()
        {
            state = State.Hidden;
            if (officerVisual != null)
            {
                officerVisual.SetActive(false);
            }
        }

        void Update()
        {
            if (player == null)
            {
                return;
            }

            switch (state)
            {
                case State.Chasing:
                    if (gameManager == null || gameManager.IsRunning)
                    {
                        ChaseUpdate();
                    }

                    break;
                case State.GivingUp:
                    GiveUpUpdate();
                    break;
                case State.Catching:
                    CatchingUpdate();
                    break;
            }
        }

        void ChaseUpdate()
        {
            Vector3 position = transform.position;
            position.z = player.position.z - followDistance;
            position.x = Mathf.SmoothDamp(position.x, player.position.x, ref lateralVelocity, lateralSmoothTime);
            position.y = 0f;
            transform.position = position;

            if (Time.time >= chaseEndTime)
            {
                state = State.GivingUp;
            }
        }

        void GiveUpUpdate()
        {
            // He stops running forward and the road leaves him behind.
            if (player.position.z - transform.position.z > giveUpDropBack)
            {
                Dismiss();
            }
        }

        void CatchingUpdate()
        {
            Vector3 target = new Vector3(player.position.x + caughtSideOffset, 0f, player.position.z);
            transform.position = Vector3.MoveTowards(transform.position, target, catchUpSpeed * Time.deltaTime);
            if ((transform.position - target).sqrMagnitude >= 0.01f)
            {
                return;
            }

            state = State.Caught;
            // Face the fallen runner and hold the pose.
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(toPlayer);
            }

            if (animator != null)
            {
                animator.speed = 0f;
            }
        }
    }
}
