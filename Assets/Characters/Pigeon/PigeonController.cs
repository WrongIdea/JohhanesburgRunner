using System.Collections;
using UnityEngine;

namespace JoburgRunner.Characters.Pigeon
{
    /// <summary>
    /// Pool-ready controller for the rigged/animated Meshy pigeon (v2). Drives the
    /// <see cref="Animator"/> via cached parameter hashes only (no per-frame
    /// StringToHash / allocations), runs autonomous random ground behaviour on a
    /// coroutine, and exposes a small imperative API the flock/pool can call to
    /// take off, fly, bank, land and react. Root motion stays baked-into-pose in
    /// the clips; world movement is owned by the caller. This is the v2 asset in
    /// <c>Assets/Characters/Pigeon</c> — distinct from the shipping flock bird in
    /// <c>JoburgRunner.Environment.Pigeons</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PigeonController : MonoBehaviour
    {
        public enum Ground { Idle = 0, Walk = 1, Peck = 2 }
        public enum Flight { Fly = 0, Glide = 1, BankLeft = 2, BankRight = 3 }

        [Header("References")]
        [SerializeField] Animator animator;
        [SerializeField] Transform groundCheck;
        [SerializeField] AudioSource audioSource;
        [SerializeField] Collider bodyCollider;

        [Header("Autonomous ground behaviour")]
        [SerializeField] bool autoBehaviour = true;
        [SerializeField] Vector2 idleDuration = new Vector2(2f, 5f);
        [SerializeField] Vector2 walkDuration = new Vector2(1f, 3f);
        [SerializeField] Vector2 peckDuration = new Vector2(1.5f, 3f);
        [SerializeField] Vector2 alertDuration = new Vector2(0.5f, 1.5f);
        [Range(0f, 1f)] [SerializeField] float walkWeight = 0.42f;
        [Range(0f, 1f)] [SerializeField] float peckWeight = 0.30f;
        [Range(0f, 1f)] [SerializeField] float alertWeight = 0.10f;

        [Header("Local wander (stays off the road)")]
        [SerializeField] float wanderRadius = 1.2f;
        [SerializeField] float walkSpeed = 0.35f;
        [Tooltip("The pigeon is never allowed to wander past this X (the kerb). " +
                 "Sign is chosen from its spawn side automatically.")]
        [SerializeField] float roadBoundaryX = 5.6f;
        [SerializeField] float turnSpeedDeg = 220f;

        [Header("Audio")]
        [SerializeField] AudioClip cooClip;
        [SerializeField] AudioClip wingFlapClip;
        [SerializeField] AudioClip takeoffClip;
        [SerializeField] AudioClip landingClip;
        [SerializeField] AudioClip alertClip;
        [SerializeField] Vector2 cooInterval = new Vector2(4f, 11f);
        [SerializeField] [Range(0f, 1f)] float cooVolume = 0.35f;

        [Header("Flight timings (clip lengths, seconds)")]
        [SerializeField] float landingLength = 1.0f;

        // ---- cached animator parameter hashes ----
        static readonly int PSpeed = Animator.StringToHash("Speed");
        static readonly int PIsAlert = Animator.StringToHash("IsAlert");
        static readonly int PTakeOff = Animator.StringToHash("TakeOff");
        static readonly int PLand = Animator.StringToHash("Land");
        static readonly int PPeck = Animator.StringToHash("Peck");
        static readonly int PShortHop = Animator.StringToHash("ShortHop");
        static readonly int PFrightened = Animator.StringToHash("Frightened");
        static readonly int PFlightMode = Animator.StringToHash("FlightMode");
        static readonly int PGroundState = Animator.StringToHash("GroundState");

        Vector3 home;
        float homeSideSign = 1f;
        bool grounded = true;
        Coroutine behaviourRoutine;
        Coroutine flightRoutine;
        float cooTimer;

        public bool IsGrounded => grounded;

        void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        // ---------------------------------------------------------------- pooling
        /// <summary>Activate from the pool at a home position. Randomises the clip
        /// phase so a flock never moves in lock-step.</summary>
        public void Initialize(Vector3 homePosition, float animStartOffset = -1f)
        {
            home = homePosition;
            homeSideSign = homePosition.x >= 0f ? 1f : -1f;
            transform.position = homePosition;
            ResetPigeon(animStartOffset);
        }

        /// <summary>Full reset for reuse: animator, triggers, collider, renderers,
        /// behaviour. Safe to call on spawn.</summary>
        public void ResetPigeon(float animStartOffset = -1f)
        {
            StopAllPigeonCoroutines();
            grounded = true;

            if (bodyCollider != null) bodyCollider.enabled = true;

            if (animator != null)
            {
                animator.Rebind();               // clears state + parameters
                animator.ResetTrigger(PTakeOff);
                animator.ResetTrigger(PLand);
                animator.ResetTrigger(PPeck);
                animator.ResetTrigger(PShortHop);
                animator.ResetTrigger(PFrightened);
                animator.SetBool(PIsAlert, false);
                animator.SetInteger(PGroundState, (int)Ground.Idle);
                animator.SetInteger(PFlightMode, (int)Flight.Fly);
                animator.SetFloat(PSpeed, 0f);
                float offset = animStartOffset >= 0f ? animStartOffset : Random.value;
                animator.Play("Idle", 0, offset);   // desync the flock
                animator.speed = Random.Range(0.94f, 1.06f);
            }

            cooTimer = Random.Range(cooInterval.x, cooInterval.y);
            if (autoBehaviour && isActiveAndEnabled)
                behaviourRoutine = StartCoroutine(GroundBehaviour());
        }

        /// <summary>Stop everything and hand back to the pool (never Destroy).</summary>
        public void ReturnToPool()
        {
            StopAllPigeonCoroutines();
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
            grounded = true;
            if (animator != null) animator.speed = 1f;
            gameObject.SetActive(false);
        }

        void StopAllPigeonCoroutines()
        {
            if (behaviourRoutine != null) { StopCoroutine(behaviourRoutine); behaviourRoutine = null; }
            if (flightRoutine != null) { StopCoroutine(flightRoutine); flightRoutine = null; }
        }

        // ---------------------------------------------------------------- ground API
        public void SetGroundState(Ground state)
        {
            if (animator != null) animator.SetInteger(PGroundState, (int)state);
            if (animator != null) animator.SetFloat(PSpeed, state == Ground.Walk ? 1f : 0f);
        }

        public void SetAlert(bool value)
        {
            if (animator != null) animator.SetBool(PIsAlert, value);
            if (value) PlayOneShot(alertClip, 0.5f);
        }

        public void TriggerShortHop()
        {
            if (animator != null) animator.SetTrigger(PShortHop);
            PlayOneShot(wingFlapClip, 0.4f);
        }

        // ---------------------------------------------------------------- flight API
        public void TriggerTakeoff(Flight initialMode = Flight.Fly)
        {
            grounded = false;
            StopBehaviourOnly();
            if (bodyCollider != null) bodyCollider.enabled = false; // no collisions airborne
            if (animator != null)
            {
                animator.SetInteger(PFlightMode, (int)initialMode);
                animator.SetTrigger(PTakeOff);
            }
            PlayOneShot(takeoffClip, 0.7f);
        }

        public void SetFlightMode(Flight mode)
        {
            if (animator != null) animator.SetInteger(PFlightMode, (int)mode);
        }

        public void TriggerLanding()
        {
            if (animator != null) animator.SetTrigger(PLand);
            PlayOneShot(landingClip, 0.6f);
            flightRoutine = StartCoroutine(ResumeGroundAfter(landingLength + 0.1f));
        }

        public void TriggerFrightened()
        {
            grounded = false;
            StopBehaviourOnly();
            if (bodyCollider != null) bodyCollider.enabled = false;
            if (animator != null) animator.SetTrigger(PFrightened);
            PlayOneShot(wingFlapClip, 0.8f);
        }

        void StopBehaviourOnly()
        {
            if (behaviourRoutine != null) { StopCoroutine(behaviourRoutine); behaviourRoutine = null; }
        }

        IEnumerator ResumeGroundAfter(float seconds)
        {
            float t = 0f;
            while (t < seconds) { t += Time.deltaTime; yield return null; }
            grounded = true;
            if (bodyCollider != null) bodyCollider.enabled = true;
            SetGroundState(Ground.Idle);
            if (autoBehaviour) behaviourRoutine = StartCoroutine(GroundBehaviour());
            flightRoutine = null;
        }

        // ---------------------------------------------------------------- autonomous
        IEnumerator GroundBehaviour()
        {
            // small random settle so a freshly-spawned flock is out of phase
            yield return WaitSeconds(Random.value * 0.6f);

            while (grounded)
            {
                float roll = Random.value;
                if (roll < alertWeight)
                {
                    SetAlert(true);
                    yield return WaitSeconds(RangeOf(alertDuration));
                    SetAlert(false);
                }
                else if (roll < alertWeight + peckWeight)
                {
                    SetGroundState(Ground.Peck);
                    yield return WaitSeconds(RangeOf(peckDuration));
                }
                else if (roll < alertWeight + peckWeight + walkWeight)
                {
                    SetGroundState(Ground.Walk);
                    yield return Wander(RangeOf(walkDuration));
                }
                else
                {
                    SetGroundState(Ground.Idle);
                    yield return WaitSeconds(RangeOf(idleDuration));
                }
            }
        }

        // Walk toward a random point inside the wander disc, never crossing the kerb.
        IEnumerator Wander(float seconds)
        {
            Vector3 target = PickWanderTarget();
            float t = 0f;
            while (t < seconds && grounded)
            {
                t += Time.deltaTime;
                Vector3 to = target - transform.position; to.y = 0f;
                float d = to.magnitude;
                if (d > 0.02f)
                {
                    Vector3 dir = to / d;
                    Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, want, turnSpeedDeg * Time.deltaTime);
                    transform.position += dir * (walkSpeed * Time.deltaTime);
                }
                else
                {
                    target = PickWanderTarget();
                }
                TickCoo();
                yield return null;
            }
        }

        Vector3 PickWanderTarget()
        {
            Vector2 disc = Random.insideUnitCircle * wanderRadius;
            Vector3 p = home + new Vector3(disc.x, 0f, disc.y);
            // clamp X so the bird never wanders into the road: keep it on its side,
            // no closer to the road than the kerb (roadBoundaryX magnitude).
            if (homeSideSign >= 0f) p.x = Mathf.Min(p.x, roadBoundaryX);
            else p.x = Mathf.Max(p.x, -roadBoundaryX);
            return p;
        }

        void TickCoo()
        {
            if (cooClip == null || audioSource == null) return;
            cooTimer -= Time.deltaTime;
            if (cooTimer <= 0f)
            {
                audioSource.PlayOneShot(cooClip, cooVolume);
                cooTimer = Random.Range(cooInterval.x, cooInterval.y);
            }
        }

        // ---------------------------------------------------------------- helpers
        static float RangeOf(Vector2 r) => Random.Range(r.x, r.y);

        // Time-sliced wait that also services the coo timer, no WaitForSeconds alloc.
        IEnumerator WaitSeconds(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                TickCoo();
                yield return null;
            }
        }

        void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, volume);
        }

        void OnDisable()
        {
            // if the GameObject is pooled-off mid-flight, leave clean state
            behaviourRoutine = null;
            flightRoutine = null;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.6f);
            Vector3 c = Application.isPlaying ? home : transform.position;
            Gizmos.DrawWireSphere(c, wanderRadius);
        }
#endif
    }
}
