using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// One pooled pigeon. Owns its animation state machine (ground behaviours +
    /// take-off / fly / land) and per-instance flight parameters. Ticked by its
    /// <see cref="PigeonFlock"/> (no per-pigeon Update, so a full flock costs one
    /// Update callback). Drives clips directly via Animator.CrossFade — the
    /// controller only needs the six states to exist, no parameter wiring.
    ///
    /// Flight follows a per-pigeon quadratic bezier arc (no two identical) and the
    /// body banks into lateral movement by rolling around its forward axis, so
    /// banking needs no extra clips. A pigeon may be given a reserved
    /// <see cref="PigeonLandingPoint"/> to descend onto instead of despawning.
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
        [SerializeField] float maxBankAngle = 28f;
        [SerializeField] float pathVariation = 1f;

        [Header("Audio clips (Inspector only)")]
        [SerializeField] AudioClip softCoo;
        [SerializeField] AudioClip wingFlap;
        [SerializeField] AudioClip takeOffFlutter;
        [SerializeField] AudioClip landingFlutter;

        const float TakeOffTime = 0.6f;

        /// <summary>Global mute (set from quality settings). Cheap gate on all pigeon SFX.</summary>
        public static bool AudioEnabled = true;

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
        float cooTimer;
        bool audioReady;
        AudioSource audioSource;

        // Per-pigeon flight plan (varied by the flock so no two paths match).
        float yawBias;
        bool feeding;
        bool canCoo = true;
        PigeonLandingPoint landingPoint;

        // Bezier arc state.
        Vector3 p0, p1, p2;
        float flightElapsed, flightTotal;
        Vector3 prevHeading;
        float bankAngle, bankVel;
        float roamRadius = 1.6f;
        Vector3 homePos;

        // Idle micro-motion — a per-instance breathing / weight-shift sway so a
        // resting flock is never statue-still or perfectly in sync (Part 4). Driven
        // procedurally on the root while grounded (the in-place clips don't touch it),
        // so it costs a sine per bird and needs no extra animation states.
        float swayPhase;
        float swaySpeed;
        float groundBaseYaw;
        Ground lastGround = Ground.Idle;

        // Editor gizmo state.
        bool gizmoFlying;

        public bool IsFinished => phase == Phase.Done;
        public bool IsFlying => phase == Phase.TakeOff || phase == Phase.Fly;
        public bool IsLanding => phase == Phase.Land;

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

            ReleaseLandingPoint();
            phase = Phase.Ground;
            scareDelay = -1f;
            gizmoFlying = false;
            bankAngle = 0f;
            bankVel = 0f;
            yawBias = 0f;
            feeding = false;
            canCoo = true;
            swayPhase = Random.value * Mathf.PI * 2f;
            swaySpeed = Random.Range(0.8f, 1.3f);
            homePos = transform.position;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            PickGroundBehaviour(true);
            cooTimer = Random.Range(2f, 8f);

            if (animator != null)
            {
                // Desync flocks: start each clip at a random normalised time.
                animator.Play(HashIdle, 0, Random.value);
                animator.speed = Random.Range(0.9f, 1.1f);
            }
        }

        /// <summary>Extra per-pigeon variation applied by the flock after spawn.</summary>
        public void SetFlightPlan(float yawBiasDeg, float bankMax, float variation, float roam)
        {
            yawBias = yawBiasDeg;
            maxBankAngle = bankMax;
            pathVariation = variation;
            roamRadius = Mathf.Max(0.2f, roam);
        }

        /// <summary>Bias ground behaviour toward feeding (Idle/Peck) under interest areas.</summary>
        public void SetFeeding(bool value) => feeding = value;

        /// <summary>Flock designates only one or two birds as coo emitters (Part 15).</summary>
        public void SetCanCoo(bool value) => canCoo = value;

        /// <summary>Give this pigeon a reserved perch to descend onto after flight.</summary>
        public void SetLandingTarget(PigeonLandingPoint point) => landingPoint = point;

        /// <summary>Free any perch we hold (on scatter re-plan or return to pool).</summary>
        public void ReleaseLandingPoint()
        {
            if (landingPoint != null)
            {
                landingPoint.Release();
                landingPoint = null;
            }
        }

        /// <summary>
        /// Begin the scatter. <paramref name="delay"/> staggers the flock's take-off
        /// as the alarm ripples out from the leader. The bird snaps to an alert idle
        /// (head up, stops pecking / wandering) the instant the alarm reaches it, even
        /// though lift-off is still <paramref name="delay"/> seconds away (Part 6).
        /// </summary>
        public void Scare(float delay)
        {
            if (phase != Phase.Ground || scareDelay >= 0f)
            {
                return;
            }
            scareDelay = delay;
            if (ground != Ground.Idle)
            {
                ground = Ground.Idle;
                CrossFade(HashIdle, 0.06f);
            }
            groundBaseYaw = transform.eulerAngles.y;
            groundTimer = Mathf.Max(groundTimer, delay + 0.1f); // don't re-pick mid-alert
        }

        /// <summary>Force this pigeon straight into a despawning flight (segment recycle).</summary>
        public void ForceDespawn()
        {
            ReleaseLandingPoint();
            if (phase == Phase.Ground)
            {
                EnterTakeOff();
            }
            landingChance = 0f; // ensure it exits rather than perches
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
                    AdvanceFlight(dt);
                    if (phaseTimer <= 0f)
                    {
                        EnterFly();
                    }
                    break;
                case Phase.Fly:
                    phaseTimer -= dt;
                    AdvanceFlight(dt);
                    if (phaseTimer <= 0f)
                    {
                        EnterLandOrDespawn();
                    }
                    break;
                case Phase.Land:
                    phaseTimer -= dt;
                    Vector3 lp = transform.position;
                    Vector3 target = landingPoint != null ? landingPoint.Position : new Vector3(lp.x, 0f, lp.z);
                    transform.position = Vector3.MoveTowards(lp, target, flightSpeed * dt);
                    if (phaseTimer <= 0f)
                    {
                        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                        homePos = transform.position;
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
                // Small local roaming — turn back before leaving the roam radius so
                // pigeons never wander into the road or through each other's home.
                Vector3 next = transform.position + transform.forward * (walkSpeed * dt);
                Vector3 fromHome = next - homePos;
                fromHome.y = 0f;
                if (fromHome.magnitude > roamRadius)
                {
                    transform.rotation = Quaternion.LookRotation(-fromHome.normalized, Vector3.up);
                    groundBaseYaw = transform.eulerAngles.y;
                }
                else
                {
                    transform.position = next;
                }
            }
            else if (scareDelay < 0f)
            {
                // Calm idle/peck: gentle procedural breathing + weight shift so the bird
                // reads as alive rather than a frozen prop. Held still while alert.
                swayPhase += dt * swaySpeed;
                float pitch = Mathf.Sin(swayPhase) * 1.4f;
                float roll = Mathf.Sin(swayPhase * 0.5f) * 1.1f;
                transform.rotation = Quaternion.Euler(pitch, groundBaseYaw, roll);
            }

            if (softCoo != null && audioReady && canCoo && AudioEnabled)
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

            if (forceIdle)
            {
                ground = Ground.Idle;
            }
            else
            {
                // Weighted pick, re-rolled once if it repeats the previous behaviour,
                // so a bird never plays the same thing twice running — the main source
                // of visible repetition. Feeding areas bias toward Idle/Peck (Part 3).
                float peckCut = feeding ? 0.9f : 0.8f;
                Ground next = ground;
                for (int attempt = 0; attempt < 2 && next == lastGround; attempt++)
                {
                    float r = Random.value;
                    next = r < 0.5f ? Ground.Idle : (r < peckCut ? Ground.Peck : Ground.Walk);
                }
                ground = next;
            }

            lastGround = ground;
            // The idle sway rotates the body around whatever yaw it settled on; walking
            // drives its own facing, so only re-anchor when we're staying put.
            if (ground != Ground.Walk)
            {
                groundBaseYaw = transform.eulerAngles.y;
            }

            switch (ground)
            {
                case Ground.Idle: CrossFade(HashIdle); break;
                case Ground.Peck: CrossFade(HashPeck); break;
                case Ground.Walk: CrossFade(HashWalk); break;
            }
        }

        void EnterTakeOff()
        {
            phase = Phase.TakeOff;
            scareDelay = -1f;
            phaseTimer = TakeOffTime;
            CrossFade(HashTakeOff, 0.05f);
            Play(takeOffFlutter, 0.6f);
            BuildFlightArc();
            flightElapsed = 0f;
            flightTotal = TakeOffTime + flightDuration;
            gizmoFlying = true;
            bankAngle = 0f;
            bankVel = 0f;
        }

        void BuildFlightArc()
        {
            p0 = transform.position;

            Vector3 dir;
            if (landingPoint != null)
            {
                // Fly to the reserved perch.
                p2 = landingPoint.Position;
                Vector3 flat = p2 - p0;
                flat.y = 0f;
                dir = flat.sqrMagnitude > 0.01f ? flat.normalized : transform.forward;
            }
            else
            {
                // Pick a scatter heading: left / right / forward / diagonals, plus a
                // per-pigeon bias and jitter so the flock fans out, never a line.
                float[] yaws = { -90f, 90f, 0f, -45f, 45f };
                float yaw = yaws[Random.Range(0, yaws.Length)] + yawBias + Random.Range(-12f, 12f);
                dir = Quaternion.Euler(0f, transform.eulerAngles.y + yaw, 0f) * Vector3.forward;
                dir.y = 0f;
                dir.Normalize();
                float dist = flightSpeed * flightDuration;
                p2 = p0 + dir * dist;
                p2.y = flightHeight;
            }

            // Control point: raised above the midpoint and pushed sideways so the arc
            // curves instead of running straight. Sign/scale vary per pigeon.
            Vector3 mid = (p0 + p2) * 0.5f;
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            float lateral = Random.Range(-1f, 1f) * pathVariation * 2f;
            float lift = Mathf.Max(flightHeight, p2.y) + Random.Range(0.5f, 1.5f) * pathVariation;
            p1 = mid + side * lateral;
            p1.y = lift;

            prevHeading = dir;
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        void EnterFly()
        {
            phase = Phase.Fly;
            phaseTimer = flightDuration;
            CrossFade(HashFly, 0.15f);
            Play(wingFlap, 0.4f);
        }

        void AdvanceFlight(float dt)
        {
            flightElapsed += dt;
            float t = flightTotal > 0f ? Mathf.Clamp01(flightElapsed / flightTotal) : 1f;
            Vector3 pos = Bezier(p0, p1, p2, t);
            Vector3 delta = pos - transform.position;
            transform.position = pos;

            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            if (flat.sqrMagnitude > 1e-6f)
            {
                Vector3 heading = flat.normalized;
                // Bank into the turn: this frame's yaw change → roll, smoothed.
                float turn = Vector3.SignedAngle(prevHeading, heading, Vector3.up);
                float target = Mathf.Clamp(-turn / Mathf.Max(dt, 1e-4f) * 0.08f, -maxBankAngle, maxBankAngle);
                bankAngle = Mathf.SmoothDamp(bankAngle, target, ref bankVel, 0.15f);
                prevHeading = heading;
                transform.rotation = Quaternion.LookRotation(heading, Vector3.up) * Quaternion.Euler(0f, 0f, bankAngle);
            }
        }

        void EnterLandOrDespawn()
        {
            bool land = landingPoint != null || Random.value < landingChance;
            if (land)
            {
                phase = Phase.Land;
                phaseTimer = 0.5f;
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

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
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
            if (clip != null && audioReady && AudioEnabled)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        /// <summary>Stop and mute audio (called by the flock on return to pool).</summary>
        public void SilenceAudio()
        {
            if (audioReady)
            {
                audioSource.Stop();
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
            Vector3 prev = p0;
            for (int i = 1; i <= 16; i++)
            {
                Vector3 cur = Bezier(p0, p1, p2, i / 16f);
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
            Gizmos.DrawWireSphere(p2, 0.3f);
        }
#endif
    }
}
