using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Drives the running-dust rig built under the player: continuous foot
    /// dust whose emission follows ground contact and forward speed, plus a
    /// one-shot landing burst scaled by fall speed. Polls PlayerController
    /// state each frame — this codebase has no player state events, polling
    /// is its idiom — and never restarts the particle systems: only the
    /// emission rate moves, and the landing burst is Emit()-driven so its
    /// count can scale with impact without reconfiguring the module.
    /// </summary>
    public class PlayerRunningDustController : MonoBehaviour
    {
        [Header("Particle Systems")]
        [SerializeField] ParticleSystem continuousDust;
        [SerializeField] ParticleSystem landingDust;

        [Header("Speed To Emission")]
        [SerializeField] float minimumRunningSpeed = 2f;
        [SerializeField] float maximumRunningSpeed = 26f;
        [SerializeField] float minimumEmissionRate = 8f;
        [SerializeField] float maximumEmissionRate = 15f;

        [Header("Landing Burst")]
        [SerializeField] float minimumLandingVelocity = 6f;
        [SerializeField] float maximumLandingVelocity = 14f;
        [SerializeField, Range(1, 20)] int minimumBurstCount = 8;
        [SerializeField, Range(1, 20)] int maximumBurstCount = 14;

        [Header("Look")]
        [Tooltip("Johannesburg street dust: warm grey for asphalt. Future surfaces can recolour via SetDustColour().")]
        [SerializeField] Color dustColour = new Color(0.62f, 0.55f, 0.47f, 0.6f);

        [Header("Toggles")]
        [SerializeField] bool enableContinuousDust = true;
        [SerializeField] bool enableLandingBurst = true;

        [Header("References")]
        [SerializeField] PlayerController groundedReference;
        [SerializeField] RollController rollController;
        [SerializeField] GameManager gameManager;

        // One alpha-blended dust material shared by both systems (and any
        // future surface variants); built once from the prefab placeholder
        // because the soft sprite texture is runtime-generated.
        static Material sharedDustMaterial;

        CharacterController characterController;
        float currentRate = -1f;
        float peakFallSpeed;
        bool wasGrounded = true;

        void Awake()
        {
            if (groundedReference == null)
            {
                groundedReference = GetComponent<PlayerController>();
            }

            if (rollController == null)
            {
                rollController = GetComponent<RollController>();
            }

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }

            characterController = GetComponent<CharacterController>();

            ApplySharedMaterial(continuousDust);
            ApplySharedMaterial(landingDust);
            SetDustColour(dustColour);

            if (continuousDust != null)
            {
                SetRate(0f);
                continuousDust.Play();
            }
        }

        void Update()
        {
            bool running = gameManager != null && gameManager.IsRunning;
            bool grounded = groundedReference != null && groundedReference.GroundedStable;
            float speed = groundedReference != null ? groundedReference.CurrentForwardSpeed : 0f;
            bool rolling = rollController != null && rollController.IsRolling;

            float targetRate = 0f;
            if (enableContinuousDust && running && grounded && !rolling && speed >= minimumRunningSpeed)
            {
                float speedBlend = Mathf.InverseLerp(minimumRunningSpeed, maximumRunningSpeed, speed);
                targetRate = Mathf.Lerp(minimumEmissionRate, maximumEmissionRate, speedBlend);
            }

            SetRate(targetRate);

            if (!grounded && characterController != null)
            {
                peakFallSpeed = Mathf.Min(peakFallSpeed, characterController.velocity.y);
            }

            if (grounded && !wasGrounded)
            {
                TryPlayLandingBurst(running);
                peakFallSpeed = 0f;
            }

            wasGrounded = grounded;
        }

        void SetRate(float rate)
        {
            if (continuousDust == null || Mathf.Abs(rate - currentRate) < 0.25f)
            {
                return;
            }

            currentRate = rate;
            ParticleSystem.EmissionModule emission = continuousDust.emission;
            emission.rateOverTime = rate;
        }

        void TryPlayLandingBurst(bool running)
        {
            if (!enableLandingBurst || !running || landingDust == null)
            {
                return;
            }

            float fallSpeed = -peakFallSpeed;
            if (fallSpeed < minimumLandingVelocity)
            {
                return;
            }

            float blend = Mathf.InverseLerp(minimumLandingVelocity, maximumLandingVelocity, fallSpeed);
            landingDust.Emit(Mathf.RoundToInt(Mathf.Lerp(minimumBurstCount, maximumBurstCount, blend)));
        }

        /// <summary>
        /// Surface hook: future track sections can recolour the dust here
        /// (warm grey asphalt, light-brown dirt road, pale pavement, near
        /// transparent for wet road) without touching the particle rig.
        /// </summary>
        public void SetDustColour(Color colour)
        {
            dustColour = colour;
            ApplyStartColour(continuousDust, colour);
            ApplyStartColour(landingDust, colour);
        }

        static void ApplyStartColour(ParticleSystem system, Color colour)
        {
            if (system == null)
            {
                return;
            }

            Color faint = colour;
            faint.a *= 0.75f;
            ParticleSystem.MainModule main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(colour, faint);
        }

        static void ApplySharedMaterial(ParticleSystem system)
        {
            if (system == null)
            {
                return;
            }

            ParticleSystemRenderer dustRenderer = system.GetComponent<ParticleSystemRenderer>();
            if (dustRenderer == null)
            {
                return;
            }

            if (sharedDustMaterial == null)
            {
                // Alpha-blended on purpose — dust is an occluding haze, not a
                // glow, so the additive helper the other effects use would
                // read wrong here. The builder material is already the URP
                // Unlit transparent variant; it only lacks the soft sprite.
                sharedDustMaterial = new Material(dustRenderer.sharedMaterial);
                sharedDustMaterial.SetTexture("_BaseMap", UbuntuPulseVisual.SoftGlowTexture());
                sharedDustMaterial.SetTexture("_MainTex", UbuntuPulseVisual.SoftGlowTexture());
            }

            dustRenderer.sharedMaterial = sharedDustMaterial;
        }
    }
}
