using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Ubuntu Pulse Lane Shift — Jozi Runner's signature lane-change
    /// feedback. The runner briefly bends the flow of Ubuntu energy: a
    /// blue-white energy ribbon (plus a softer, wider glow ribbon) traces
    /// the dash, small sparks peel away, a tiny radial burst fires under the
    /// shoes when grounded, the camera FOV kicks out two degrees, and the
    /// swipe whoosh plays with a randomized pitch.
    ///
    /// Everything lives permanently on the player rig and is Emit()- or
    /// emitting-gated — no Instantiate/Destroy, no allocations during
    /// gameplay. Trails render the actual path traveled, so rapid
    /// consecutive lane changes just extend the ribbon and old segments
    /// fade through their own lifetime. Ribbons are kept short because the
    /// camera sits directly behind the runner, where a long trail reads as
    /// a beam splitting the screen. The additive glow materials are built at
    /// runtime from the builder's unlit placeholders (the pipeline has no
    /// HDR/bloom, so all glow is faked additively).
    /// </summary>
    public class UbuntuLaneShift : MonoBehaviour
    {
        [Header("Feature Toggles")]
        [SerializeField] bool enableUbuntuRibbon = true;
        [SerializeField] bool enableGlowRibbon = true;
        [SerializeField] bool enableEnergySparks = true;
        [SerializeField] bool enableGroundBurst = true;
        [SerializeField] bool enableCameraFovPulse = true;
        [SerializeField] bool enableAudio = true;

        [Header("Ribbons")]
        [Tooltip("How long the ribbons keep emitting after a lane switch; roughly the lane-change settle time.")]
        [SerializeField] float ribbonSeconds = 0.24f;
        [SerializeField] float ribbonWidth = 0.16f;
        [SerializeField] float glowWidth = 0.26f;
        [Tooltip("Seconds to ease the ribbon width down once emission time is up. The glow fades a little slower.")]
        [SerializeField] float fadeOutSeconds = 0.1f;
        [Tooltip("Gentle brightness/width breathing of the main ribbon while it is alive.")]
        [SerializeField] float pulseSpeed = 24f;
        [SerializeField, Range(0f, 0.5f)] float pulseAmount = 0.18f;

        [Header("Energy Sparks")]
        [SerializeField, Range(1, 24)] int sparkCount = 10;

        [Header("Ground Energy Burst")]
        [SerializeField, Range(1, 6)] int groundBurstCount = 6;

        [Header("Camera")]
        [SerializeField] float fovIncrease = 2f;
        [Tooltip("Whole FOV envelope; the attack is the first ~35% (≈0.08 s), the rest eases back.")]
        [SerializeField] float fovKickSeconds = 0.22f;

        [Header("Audio")]
        [SerializeField] AudioClip swipeClip;
        [SerializeField] float swipeVolume = 0.85f;
        [SerializeField] float pitchMin = 0.97f;
        [SerializeField] float pitchMax = 1.03f;

        [Header("References")]
        [SerializeField] TrailRenderer ubuntuRibbon;
        [SerializeField] TrailRenderer glowRibbon;
        [SerializeField] ParticleSystem energySparks;
        [SerializeField] ParticleSystem groundBurst;
        [SerializeField] AudioSource swipeSource;
        [SerializeField] PlayerController playerController;

        // Placeholder → additive-glow material, built once per source. The
        // built material also maps to itself so a second pass (edit-mode
        // capture after Awake, or scene reloads) never stacks conversions.
        static readonly Dictionary<Material, Material> RuntimeMaterialCache = new Dictionary<Material, Material>();

        Camera followCamera;
        float baseFov;
        Color ribbonBaseColor = Color.white;
        float emitUntil;
        float fovTimer;

        void Awake()
        {
            ApplyRuntimeMaterials(gameObject);

            if (playerController == null)
            {
                playerController = GetComponentInParent<PlayerController>();
            }

            PrepareRibbon(ubuntuRibbon);
            PrepareRibbon(glowRibbon);
            if (ubuntuRibbon != null && ubuntuRibbon.sharedMaterial != null)
            {
                ribbonBaseColor = ubuntuRibbon.sharedMaterial.color;
            }
        }

        static void PrepareRibbon(TrailRenderer ribbon)
        {
            if (ribbon != null)
            {
                ribbon.emitting = false;
                ribbon.widthMultiplier = 0f;
            }
        }

        /// <summary>
        /// Public and static so edit-mode capture code can re-apply the glow
        /// for a faithful preview (Awake does not run in edit mode). Trails
        /// get a plain additive conversion of their own placeholder;
        /// particles additionally get the runtime-generated soft sprite.
        /// </summary>
        public static void ApplyRuntimeMaterials(GameObject root)
        {
            foreach (TrailRenderer trail in root.GetComponentsInChildren<TrailRenderer>(true))
            {
                trail.sharedMaterial = GetOrBuildGlowMaterial(trail.sharedMaterial, null);
            }

            foreach (ParticleSystemRenderer particleRenderer in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                particleRenderer.sharedMaterial = GetOrBuildGlowMaterial(
                    particleRenderer.sharedMaterial, UbuntuPulseVisual.SoftGlowTexture());
            }
        }

        static Material GetOrBuildGlowMaterial(Material source, Texture2D sprite)
        {
            if (source == null)
            {
                return null;
            }

            if (RuntimeMaterialCache.TryGetValue(source, out Material built) && built != null)
            {
                return built;
            }

            built = UbuntuPulseVisual.MakeAdditiveGlowMaterial(source, sprite);
            RuntimeMaterialCache[source] = built;
            RuntimeMaterialCache[built] = built;
            return built;
        }

        /// <summary>
        /// Fires the full signature effect. Direction is the lane step just
        /// taken (+1 right, -1 left); the ribbons follow the real motion, so
        /// the parameter is unused today but kept so callers stay
        /// direction-aware. Only called for successful, player-initiated
        /// lane changes — PlayerController gates dead/paused (IsRunning),
        /// blocked edge steps, and the taxi-bounce auto-correction.
        /// </summary>
        public void Play(int direction)
        {
            emitUntil = Time.time + ribbonSeconds;

            if (enableUbuntuRibbon && ubuntuRibbon != null)
            {
                ubuntuRibbon.emitting = true;
                ubuntuRibbon.widthMultiplier = ribbonWidth;
            }

            if (enableGlowRibbon && glowRibbon != null)
            {
                glowRibbon.emitting = true;
                glowRibbon.widthMultiplier = glowWidth;
            }

            if (enableEnergySparks && energySparks != null)
            {
                energySparks.Emit(sparkCount);
            }

            if (enableGroundBurst && groundBurst != null
                && playerController != null && playerController.GroundedStable)
            {
                groundBurst.Emit(groundBurstCount);
            }

            if (enableCameraFovPulse)
            {
                fovTimer = fovKickSeconds;
            }

            PlaySwipeAudio();
        }

        /// <summary>
        /// The audio cue alone — used by the taxi side-bounce, which shoves
        /// the runner into the next lane without the signature dash visuals.
        /// </summary>
        public void PlaySwipeAudio()
        {
            if (!enableAudio || swipeSource == null || swipeClip == null)
            {
                return;
            }

            swipeSource.pitch = Random.Range(pitchMin, pitchMax);
            swipeSource.PlayOneShot(swipeClip, swipeVolume);
        }

        void Update()
        {
            AnimateRibbons();
            AnimateCameraKick();
        }

        void AnimateRibbons()
        {
            float now = Time.time;
            if (now < emitUntil)
            {
                // Alive: gentle life pulse — width and brightness breathe
                // together on the main ribbon; the glow stays steady.
                float pulse = 1f - pulseAmount + pulseAmount * 0.5f * (1f + Mathf.Sin(now * pulseSpeed));
                if (ubuntuRibbon != null && ubuntuRibbon.emitting)
                {
                    ubuntuRibbon.widthMultiplier = ribbonWidth * pulse;
                    if (ubuntuRibbon.sharedMaterial != null)
                    {
                        Color breathing = ribbonBaseColor * (0.85f + 0.3f * pulse);
                        breathing.a = ribbonBaseColor.a;
                        ubuntuRibbon.sharedMaterial.color = breathing;
                    }
                }

                return;
            }

            // Settled: ease widths down, then stop emitting. Already-laid
            // ribbon segments keep fading through their own trail lifetime.
            FadeRibbon(ubuntuRibbon, ribbonWidth, now, fadeOutSeconds);
            FadeRibbon(glowRibbon, glowWidth, now, fadeOutSeconds * 1.8f);
            if (ubuntuRibbon != null && !ubuntuRibbon.emitting && ubuntuRibbon.sharedMaterial != null)
            {
                ubuntuRibbon.sharedMaterial.color = ribbonBaseColor;
            }
        }

        void FadeRibbon(TrailRenderer ribbon, float fullWidth, float now, float fadeSeconds)
        {
            if (ribbon == null || !ribbon.emitting)
            {
                return;
            }

            float remaining = 1f - Mathf.Clamp01((now - emitUntil) / Mathf.Max(0.01f, fadeSeconds));
            ribbon.widthMultiplier = fullWidth * remaining;
            if (remaining <= 0f)
            {
                ribbon.emitting = false;
            }
        }

        // Same envelope PerfectDodge uses (fast attack, smooth release back
        // to the exact base FOV); both cache the base before ever kicking,
        // so overlapping kicks cannot drift the camera.
        void AnimateCameraKick()
        {
            if (followCamera == null)
            {
                followCamera = Camera.main;
                if (followCamera != null)
                {
                    baseFov = followCamera.fieldOfView;
                }

                return;
            }

            if (fovTimer <= 0f)
            {
                return;
            }

            fovTimer -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(fovTimer / Mathf.Max(0.01f, fovKickSeconds));
            float envelope = progress < 0.35f ? progress / 0.35f : 1f - (progress - 0.35f) / 0.65f;
            followCamera.fieldOfView = baseFov + fovIncrease * envelope;
            if (fovTimer <= 0f)
            {
                followCamera.fieldOfView = baseFov;
            }
        }
    }
}
