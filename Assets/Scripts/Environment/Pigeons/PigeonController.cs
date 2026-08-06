using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// One pooled pigeon. Owns its animation state machine (ground behaviours +
    /// take-off / fly / land) and per-instance flight parameters. Ticked by its
    /// <see cref="PigeonFlock"/> (no per-pigeon Update, so a full flock costs one
    /// Update callback). Drives clips directly via Animator.CrossFade — the
    /// controller only needs the six states to exist, no parameter wiring.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PigeonController : MonoBehaviour
    {
        enum Phase { Ground, TakeOff, Fly, Land, Done }
        enum Ground { Idle, Walk, Peck }

        [Header("Ground behaviour")]
        [SerializeField] float minGroundSwitch = 1.5f;
        [SerializeField] float maxGroundSwitch = 4.0f;
        [SerializeField] float walkSpeed = 0.45f;

        [Header("Flight (set by the flock/spawner, exposed for tuning)")]
        [SerializeField] float flightHeight = 4f;
        [SerializeField] float flightSpeed = 6f;
        [SerializeField] float flightDuration = 5f;
        [SerializeField] float landingChance = 0.4f;

        [Header("Audio clips (Inspector only)")]
        [SerializeField] AudioClip softCoo;
        [SerializeField] AudioClip wingFlap;
        [SerializeField] AudioClip takeOffFlutter;
        [SerializeField] AudioClip landingFlutter;

        static readonly int HashIdle = Animator.StringToHash("Idle");
        static readonly int HashWalk = Animator.StringToHash("Walk");
        static readonly int HashPeck = Animator.StringToHash("Peck");
        static readonly int HashTakeOff = Animator.StringToHash("TakeOff");
        static readonly int HashFly = Animator.StringToHash("Fly");
        static readonly int HashLand = Animator.StringToHash("Land");

        Animator animator;
        Phase phase;
        Ground ground;
        float groundTimer;
        float scareDelay = -1f;   // >=0 while a scare is pending.
        float phaseTimer;
        Vector3 flightVelocity;
        float cooTimer;
        bool audioReady;
        AudioSource audioSource;

        // Editor gizmo state.
        Vector3 gizmoFlightDir;
        bool gizmoFlying;

        public bool IsFinished => phase == Phase.Done;
        public bool IsFlying => phase == Phase.TakeOff || phase == Phase.Fly;

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            audioSource = GetComponent<AudioSource>();
            audioReady = audioSource != null;
        }

        /// <summary>Called by the flock when this pigeon is rented from the pool.</summary>
        public void OnSpawned(float height, float speed, float duration, float landChance)
        {
            flightHeight = height;
            flightSpeed = speed;
            flightDuration = duration;
            landingChance = landChance;

            phase = Phase.Ground;
            scareDelay = -1f;
            gizmoFlying = false;
            PickGroundBehaviour(true);
            cooTimer = Random.Range(2f, 8f);

            if (animator != null)
            {
                // Desync flocks: start each clip at a random normalised time.
                animator.Play(HashIdle, 0, Random.value);
                animator.speed = Random.Range(0.9f, 1.1f);
            }
        }

        /// <summary>Begin the scatter. Delay staggers the flock's take-off.</summary>
        public void Scare(float delay)
        {
            if (phase != Phase.Ground)
            {
                return;
            }
            scareDelay = delay;
        }

        /// <summary>Advanced by the owning flock once per frame.</summary>
        public void Tick(float dt)
        {
            switch (phase)
            {
                case Phase.Ground:
                    TickGround(dt);
                    break;
                case Phase.TakeOff:
                    phaseTimer -= dt;
                    ClimbTowardFlight(dt);
                    if (phaseTimer <= 0f)
                    {
                        EnterFly();
                    }
                    break;
                case Phase.Fly:
                    phaseTimer -= dt;
                    transform.position += flightVelocity * dt;
                    ClimbTowardFlight(dt);
                    if (phaseTimer <= 0f)
                    {
                        EnterLandOrDespawn();
                    }
                    break;
                case Phase.Land:
                    phaseTimer -= dt;
                    // Ease down to the ground during the land clip.
                    Vector3 p = transform.position;
                    p.y = Mathf.MoveTowards(p.y, 0f, flightHeight / 0.5f * dt);
                    transform.position = p;
                    if (phaseTimer <= 0f)
                    {
                        phase = Phase.Ground;
                        PickGroundBehaviour(true);
                    }
                    break;
            }
        }

        void TickGround(float dt)
        {
            if (scareDelay >= 0f)
            {
                scareDelay -= dt;
                if (scareDelay <= 0f)
                {
                    EnterTakeOff();
                    return;
                }
            }

            groundTimer -= dt;
            if (groundTimer <= 0f)
            {
                PickGroundBehaviour(false);
            }

            if (ground == Ground.Walk)
            {
                transform.position += transform.forward * (walkSpeed * dt);
            }

            if (softCoo != null && audioReady)
            {
                cooTimer -= dt;
                if (cooTimer <= 0f)
                {
                    audioSource.PlayOneShot(softCoo, 0.35f);
                    cooTimer = Random.Range(4f, 11f);
                }
            }
        }

        void PickGroundBehaviour(bool forceIdle)
        {
            groundTimer = Random.Range(minGroundSwitch, maxGroundSwitch);
            float r = forceIdle ? 0f : Random.value;
            if (r < 0.5f)
            {
                ground = Ground.Idle;
                CrossFade(HashIdle);
            }
            else if (r < 0.8f)
            {
                ground = Ground.Peck;
                CrossFade(HashPeck);
            }
            else
            {
                ground = Ground.Walk;
                CrossFade(HashWalk);
            }
        }

        void EnterTakeOff()
        {
            phase = Phase.TakeOff;
            scareDelay = -1f;
            phaseTimer = 0.6f;
            CrossFade(HashTakeOff, 0.05f);
            Play(takeOffFlutter, 0.6f);

            // Choose a flight path: left / right / forward / diagonals.
            float[] yaws = { -90f, 90f, 0f, -45f, 45f };
            float yaw = yaws[Random.Range(0, yaws.Length)];
            Vector3 dir = Quaternion.Euler(0f, transform.eulerAngles.y + yaw, 0f) * Vector3.forward;
            dir.y = 0f;
            flightVelocity = dir.normalized * flightSpeed;
            gizmoFlightDir = flightVelocity;
            gizmoFlying = true;
            // Face the flight direction.
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        void EnterFly()
        {
            phase = Phase.Fly;
            phaseTimer = flightDuration;
            CrossFade(HashFly, 0.15f);
            Play(wingFlap, 0.4f);
        }

        void EnterLandOrDespawn()
        {
            if (Random.value < landingChance)
            {
                phase = Phase.Land;
                phaseTimer = 0.5f;
                flightVelocity = Vector3.zero;
                gizmoFlying = false;
                CrossFade(HashLand, 0.1f);
                Play(landingFlutter, 0.5f);
            }
            else
            {
                phase = Phase.Done; // Flock returns us to the pool.
                gizmoFlying = false;
            }
        }

        void ClimbTowardFlight(float dt)
        {
            Vector3 p = transform.position;
            p.y = Mathf.MoveTowards(p.y, flightHeight, flightSpeed * dt);
            transform.position = p;
        }

        void CrossFade(int hash, float dur = 0.12f)
        {
            if (animator != null)
            {
                animator.CrossFadeInFixedTime(hash, dur);
            }
        }

        void Play(AudioClip clip, float volume)
        {
            if (clip != null && audioReady)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!gizmoFlying)
            {
                return;
            }
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Vector3 from = transform.position;
            Vector3 to = from + gizmoFlightDir.normalized * (flightSpeed * flightDuration);
            to.y = flightHeight;
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireSphere(to, 0.3f);
        }
#endif
    }
}
