using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Visual-only controller for Ubuntu Pulse. It polls PowerUpManager state and
    /// drives prebuilt/reused URP VFX objects: shield, Tswana pattern lines,
    /// counter-rotating rings, orbit particles, lightning arcs, ground glow,
    /// player trail, coin-attraction trails, activation burst and impact burst.
    /// Gameplay timing, magnet movement, scoring and collision decisions remain
    /// in their existing gameplay scripts.
    /// </summary>
    public class UbuntuPulseVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PowerUpManager powerUpManager;
        [SerializeField] Transform shieldSphere;
        [SerializeField] Renderer shieldRenderer;
        [SerializeField] ParticleSystem shieldParticles;
        [SerializeField] Transform forwardArrow;
        [SerializeField] Renderer groundGlow;
        [SerializeField] Light pulseLight;
        [SerializeField] AudioSource shieldHum;

        [Header("Shared URP Materials")]
        [SerializeField] Material shieldMaterial;
        [SerializeField] Material vfxMaterial;
        [SerializeField] Material trailMaterial;
        [SerializeField] Material groundGlowMaterial;

        [Header("Audio Clips")]
        [SerializeField] AudioClip impactClip;
        [SerializeField] AudioClip powerDownClip;
        [SerializeField] float sfxVolume = 0.7f;

        [Header("Look")]
        [SerializeField] Color shieldColor = new Color(0.25f, 0.72f, 1f, 0.32f);
        [SerializeField] Color secondaryColor = Color.white;
        [SerializeField] float shieldScale = 1.12f;
        [SerializeField] float bloomIntensity = 2.4f;
        [SerializeField] float shieldOpacity = 0.32f;

        [Header("Timing")]
        [SerializeField] float fadeInSeconds = 0.35f;
        [SerializeField] float fadeOutSeconds = 0.45f;
        [SerializeField] float pulseSpeed = 2f;
        [SerializeField] float finalFlashPulseSpeed = 8f;
        [SerializeField] float shieldImpactFlashSeconds = 0.24f;

        [Header("Motion")]
        [SerializeField] float shieldRotationSpeed = 20f;
        [SerializeField] float ringSpeed = 90f;
        [SerializeField] float ringHeight = 0.78f;
        [SerializeField] float ringVerticalSeparation = 0.14f;
        [SerializeField] float ringRadiusMultiplier = 0.62f;
        [SerializeField] float patternScrollSpeed = 24f;
        [SerializeField, Range(8, 96)] int particleCount = 42;
        [SerializeField] float trailWidth = 0.12f;
        [SerializeField] float lightBaseIntensity = 2.2f;

        [Header("Lightning")]
        [SerializeField, Range(0f, 20f)] float lightningFrequency = 7f;
        [SerializeField, Range(1, 8)] int lightningArcCount = 4;
        [SerializeField] float lightningArcSeconds = 0.08f;

        [Header("Coin Trails")]
        [SerializeField] float coinTrailRadius = 8f;
        [SerializeField, Range(0, 24)] int maxCoinTrails = 12;
        [SerializeField] float coinTrailLength = 1.1f;

        [Header("Impact Camera")]
        [SerializeField] float cameraShakeSeconds = 0.16f;
        [SerializeField] float cameraShakeStrength = 0.12f;

        [Header("Shockwave")]
        [SerializeField] float shockwaveSeconds = 0.45f;
        [SerializeField] float activationShockwaveRadius = 2.2f;
        [SerializeField] float impactShockwaveRadius = 1.8f;

        [Header("Shield Surface")]
        [SerializeField, Range(0.5f, 8f)] float fresnelPower = 2.6f;
        [SerializeField, Range(2f, 12f)] float patternBandCount = 4f;
        [SerializeField, Range(4f, 40f)] float patternRepeat = 12f;
        [SerializeField, Range(-4f, 4f)] float patternUvScrollSpeed = 0.6f;

        float blend;
        bool wasActive;
        bool visualsInitialized;
        float impactFlash;
        float nextLightningTime;
        float cameraShakeTimer;
        Vector3 cameraShakeOffset;

        Transform effectRoot;
        Transform patternRoot;
        Transform ringA;
        Transform ringB;
        TrailRenderer bodyTrail;
        ParticleSystem activationBurst;
        ParticleSystem orbitParticles;
        ParticleSystem impactParticles;
        Camera followCamera;

        Material runtimeShieldMaterial;
        bool shieldUsesCustomShader;
        Material runtimeVfxMaterial;
        Material runtimeTrailMaterial;
        Material runtimeGroundMaterial;
        Material runtimeWaveMaterial;
        Transform activationWave;
        Transform impactWave;
        Renderer activationWaveRenderer;
        Renderer impactWaveRenderer;
        float activationWaveStart = float.NegativeInfinity;
        float impactWaveStart = float.NegativeInfinity;
        static Texture2D sharedSoftGlowTexture;
        static Texture2D sharedRingTexture;

        MaterialPropertyBlock shieldBlock;
        MaterialPropertyBlock glowBlock;
        MaterialPropertyBlock lineBlock;
        MaterialPropertyBlock waveBlock;
        readonly List<LineRenderer> coinTrailPool = new List<LineRenderer>();
        readonly List<LineRenderer> lightningPool = new List<LineRenderer>();
        readonly List<float> lightningEndTimes = new List<float>();

        void Awake()
        {
            shieldBlock = new MaterialPropertyBlock();
            glowBlock = new MaterialPropertyBlock();
            lineBlock = new MaterialPropertyBlock();
            waveBlock = new MaterialPropertyBlock();
            BuildReusableVisuals();
            SetVisualsActive(false);
        }

        void Update()
        {
            bool active = powerUpManager != null && powerUpManager.UbuntuPulseActive;
            HandleStateChange(active);

            float target = active ? 1f : 0f;
            if (Mathf.Approximately(blend, target) && blend <= 0f)
            {
                ClearTransientParticles();
                DisableCoinTrails();
                HideShockwaves();
                return;
            }

            float fadeSeconds = active ? fadeInSeconds : fadeOutSeconds;
            blend = Mathf.MoveTowards(blend, target, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
            impactFlash = Mathf.MoveTowards(impactFlash, 0f, Time.deltaTime / Mathf.Max(0.01f, shieldImpactFlashSeconds));

            if (blend <= 0f)
            {
                SetVisualsActive(false);
                ClearTransientParticles();
                DisableCoinTrails();
                HideShockwaves();
                return;
            }

            AnimateActiveVisuals(active);
            UpdateCoinTrailPool(active);
            UpdateLightning(active);
            UpdateShockwaves();
        }

        void LateUpdate()
        {
            UpdateCameraShake();
        }

        void HandleStateChange(bool active)
        {
            if (active == wasActive)
            {
                return;
            }

            wasActive = active;
            if (active)
            {
                SetVisualsActive(true);
                shieldParticles?.Play();
                orbitParticles?.Play();
                activationBurst?.Emit(48);
                activationWaveStart = Time.time;
                if (shieldHum != null)
                {
                    shieldHum.loop = true;
                    shieldHum.Play();
                }
            }
            else
            {
                shieldParticles?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                orbitParticles?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                shieldHum?.Stop();
                if (powerDownClip != null)
                {
                    AudioSource.PlayClipAtPoint(powerDownClip, transform.position, sfxVolume);
                }
            }
        }

        void BuildReusableVisuals()
        {
            if (visualsInitialized)
            {
                return;
            }

            visualsInitialized = true;
            effectRoot = shieldSphere != null && shieldSphere.parent != null ? shieldSphere.parent : transform;
            CacheSharedMaterials();
            CreateRuntimeMaterials();

            if (shieldRenderer != null && runtimeShieldMaterial != null)
            {
                shieldRenderer.sharedMaterial = runtimeShieldMaterial;
            }

            ReplaceGroundGlowWithSoftQuad();

            if (shieldParticles != null && runtimeVfxMaterial != null)
            {
                ParticleSystemRenderer shieldParticleRenderer = shieldParticles.GetComponent<ParticleSystemRenderer>();
                if (shieldParticleRenderer != null)
                {
                    shieldParticleRenderer.sharedMaterial = runtimeVfxMaterial;
                }
            }

            if (shieldSphere != null)
            {
                patternRoot = CreateChild("TswanaPatternBands", shieldSphere);
                BuildPatternBand("DiamondBand_Low", -0.28f, 0.82f, 24, 0.07f);
                BuildPatternBand("TriangleBand_Mid", 0.02f, 0.94f, 30, 0.085f);
                BuildPatternBand("DiamondBand_High", 0.32f, 0.76f, 22, 0.065f);
            }

            Transform ringParent = transform;
            ringA = BuildEnergyRing("CounterRingA", ringParent, 0.64f, 14f, 36);
            ringB = BuildEnergyRing("CounterRingB", ringParent, 0.78f, -14f, 40);
            if (ringA != null)
            {
                ringA.localPosition = Vector3.up * ringHeight;
            }

            if (ringB != null)
            {
                ringB.localPosition = Vector3.up * (ringHeight + ringVerticalSeparation);
            }

            activationBurst = CreatePooledParticles("ActivationBurst", false, 64, 0.55f, 2.4f, 0.09f, 0f);
            orbitParticles = CreatePooledParticles("OrbitingBlueWhiteParticles", true, particleCount, 1.25f, 0.18f, 0.035f, 16f);
            impactParticles = CreatePooledParticles("ShieldImpactFlash", false, 72, 0.32f, 3.1f, 0.11f, 0f);
            // Kept short: the camera sits directly behind the runner, so a
            // long trail points straight at the lens and reads as a giant
            // beam splitting the screen (seen in device testing).
            bodyTrail = CreateTrail("UbuntuMotionTrail", transform, 0.14f, trailWidth, 0.03f);
            if (bodyTrail != null)
            {
                bodyTrail.transform.localPosition = new Vector3(0f, 0.75f, -0.12f);
                Gradient trailFade = new Gradient();
                trailFade.SetKeys(
                    new[] { new GradientColorKey(secondaryColor, 0f), new GradientColorKey(shieldColor, 1f) },
                    new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) });
                bodyTrail.colorGradient = trailFade;
            }

            activationWave = CreateShockwaveQuad("ActivationShockwave", out activationWaveRenderer);
            impactWave = CreateShockwaveQuad("ImpactShockwave", out impactWaveRenderer);

            BuildLinePool("UbuntuCoinAttractionTrail", coinTrailPool, maxCoinTrails, effectRoot);
            BuildLinePool("UbuntuLightningArc", lightningPool, lightningArcCount, effectRoot);
            while (lightningEndTimes.Count < lightningPool.Count)
            {
                lightningEndTimes.Add(0f);
            }

            followCamera = Camera.main;
        }

        void CacheSharedMaterials()
        {
            if (shieldMaterial == null && shieldRenderer != null)
            {
                shieldMaterial = shieldRenderer.sharedMaterial;
            }

            if (groundGlowMaterial == null && groundGlow != null)
            {
                groundGlowMaterial = groundGlow.sharedMaterial;
            }

            if (vfxMaterial == null && shieldParticles != null)
            {
                ParticleSystemRenderer particleRenderer = shieldParticles.GetComponent<ParticleSystemRenderer>();
                if (particleRenderer != null)
                {
                    vfxMaterial = particleRenderer.sharedMaterial;
                }
            }

            if (trailMaterial == null)
            {
                trailMaterial = vfxMaterial != null ? vfxMaterial : shieldMaterial;
            }
        }

        void AnimateActiveVisuals(bool active)
        {
            float remaining = powerUpManager != null ? powerUpManager.TimeRemaining(PowerUpType.UbuntuPulse) : 999f;
            float currentPulseSpeed = active && remaining <= 2f ? finalFlashPulseSpeed : pulseSpeed;
            float pulse = 0.72f + 0.28f * Mathf.Sin(Time.time * currentPulseSpeed * Mathf.PI);
            float flash = impactFlash * impactFlash;
            float alphaBoost = 1f + flash * 1.8f;
            float brightBoost = 1f + flash * 2.8f;

            if (shieldSphere != null)
            {
                shieldSphere.localScale = Vector3.one * (shieldScale * blend * (1f + flash * 0.14f));
                shieldSphere.Rotate(Vector3.up, shieldRotationSpeed * Time.deltaTime, Space.Self);
            }

            if (shieldRenderer != null)
            {
                shieldRenderer.GetPropertyBlock(shieldBlock);
                if (shieldUsesCustomShader)
                {
                    Color rim = shieldColor;
                    rim.a = Mathf.Clamp01(0.35f + shieldOpacity);
                    shieldBlock.SetColor("_RimColor", rim);
                    shieldBlock.SetColor("_PatternColor", secondaryColor);
                    shieldBlock.SetFloat("_GlowIntensity", bloomIntensity);
                    shieldBlock.SetFloat("_Pulse", pulse);
                    shieldBlock.SetFloat("_ImpactFlash", Mathf.Clamp01(impactFlash));
                    shieldBlock.SetFloat("_Alpha", blend);
                }
                else
                {
                    // Fallback when the custom shield shader is missing: plain
                    // URP/Unlit alpha drive (URP Unlit has no emission).
                    Color color = shieldColor;
                    color.a = Mathf.Clamp01(shieldOpacity * blend * pulse * alphaBoost);
                    shieldBlock.SetColor("_BaseColor", color);
                    shieldBlock.SetColor("_Color", color);
                }

                shieldRenderer.SetPropertyBlock(shieldBlock);
            }

            if (patternRoot != null)
            {
                patternRoot.localRotation *= Quaternion.Euler(0f, patternScrollSpeed * Time.deltaTime, 0f);
                // Kept subtle so the shader's surface pattern reads through
                // instead of a solid white cage.
                SetLineRenderers(patternRoot, blend * (0.28f + 0.5f * flash), trailWidth * 0.13f);
            }

            if (ringA != null)
            {
                ringA.localRotation *= Quaternion.Euler(0f, ringSpeed * Time.deltaTime, 0f);
                ringA.localPosition = Vector3.up * ringHeight;
                ringA.localScale = Vector3.one * RingScale(ringA, blend, flash, 0.2f);
                SetLineRenderers(ringA, blend * (0.95f + flash), trailWidth * 0.22f);
            }

            if (ringB != null)
            {
                ringB.localRotation *= Quaternion.Euler(0f, -ringSpeed * 1.25f * Time.deltaTime, 0f);
                ringB.localPosition = Vector3.up * (ringHeight + ringVerticalSeparation);
                ringB.localScale = Vector3.one * RingScale(ringB, blend, flash, 0.25f);
                SetLineRenderers(ringB, blend * (0.95f + flash), trailWidth * 0.18f);
            }

            if (groundGlow != null)
            {
                groundGlow.GetPropertyBlock(glowBlock);
                Color color = shieldColor * brightBoost;
                color.a = Mathf.Clamp01(0.38f * blend * pulse * alphaBoost);
                glowBlock.SetColor("_BaseColor", color);
                glowBlock.SetColor("_Color", color);
                groundGlow.SetPropertyBlock(glowBlock);
            }

            if (pulseLight != null)
            {
                pulseLight.intensity = lightBaseIntensity * blend * pulse * brightBoost;
            }

            if (forwardArrow != null)
            {
                forwardArrow.localScale = Vector3.one * (blend * (0.85f + 0.15f * pulse));
            }

            if (bodyTrail != null)
            {
                bodyTrail.emitting = blend > 0.02f;
                bodyTrail.widthMultiplier = trailWidth * 0.6f * blend * (0.8f + 0.2f * pulse);
            }
        }

        void SetVisualsActive(bool active)
        {
            BuildReusableVisuals();

            if (shieldSphere != null)
            {
                shieldSphere.gameObject.SetActive(active);
            }

            if (groundGlow != null)
            {
                groundGlow.gameObject.SetActive(active);
            }

            if (forwardArrow != null)
            {
                forwardArrow.gameObject.SetActive(active);
            }

            if (pulseLight != null)
            {
                pulseLight.enabled = active;
            }

            SetObjectActive(patternRoot, active);
            SetObjectActive(ringA, active);
            SetObjectActive(ringB, active);

            if (bodyTrail != null)
            {
                bodyTrail.emitting = active;
                bodyTrail.Clear();
            }

            if (!active)
            {
                DisableExpiredLightning(true);
            }
        }

        public void PlayShieldImpact(Vector3 point)
        {
            if (impactClip != null)
            {
                AudioSource.PlayClipAtPoint(impactClip, point, sfxVolume);
            }

            impactFlash = 1f;
            cameraShakeTimer = cameraShakeSeconds;
            shieldParticles?.Emit(36);
            if (impactParticles != null)
            {
                impactParticles.transform.position = point;
                impactParticles.Emit(64);
            }

            if (impactWave != null)
            {
                impactWave.position = point;
                impactWaveStart = Time.time;
            }

            FireLightningArc(point);
        }

        void UpdateCoinTrailPool(bool active)
        {
            if (!active || coinTrailPool.Count == 0)
            {
                DisableCoinTrails();
                return;
            }

            int used = 0;
            float radiusSqr = coinTrailRadius * coinTrailRadius;
            Vector3 target = transform.position + Vector3.up * 1f;

            foreach (Coin coin in Coin.ActiveCoins)
            {
                if (coin == null || used >= coinTrailPool.Count)
                {
                    continue;
                }

                Vector3 coinPosition = coin.transform.position;
                if ((coinPosition - transform.position).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                // A short comet tail trailing the coin, not a full beam to the
                // player — a dozen coin-to-player beams overlap into a jagged
                // web across the whole road (seen in device testing).
                LineRenderer line = coinTrailPool[used++];
                line.gameObject.SetActive(true);
                line.widthMultiplier = trailWidth * 0.14f * blend;
                line.positionCount = 4;
                Vector3 away = (coinPosition - target).normalized;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (float)(line.positionCount - 1);
                    Vector3 position = coinPosition + away * (coinTrailLength * t);
                    position.y += 0.12f * t * t;
                    line.SetPosition(i, position);
                }

                SetLineColor(line, blend * 0.85f);
            }

            for (int i = used; i < coinTrailPool.Count; i++)
            {
                coinTrailPool[i].gameObject.SetActive(false);
            }
        }

        void DisableCoinTrails()
        {
            for (int i = 0; i < coinTrailPool.Count; i++)
            {
                coinTrailPool[i].gameObject.SetActive(false);
            }
        }

        void ClearTransientParticles()
        {
            StopAndClear(shieldParticles);
            StopAndClear(activationBurst);
            StopAndClear(orbitParticles);
            StopAndClear(impactParticles);
        }

        static void StopAndClear(ParticleSystem particles)
        {
            if (particles == null)
            {
                return;
            }

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void UpdateLightning(bool active)
        {
            if (!active || lightningPool.Count == 0 || lightningFrequency <= 0f)
            {
                DisableExpiredLightning(true);
                return;
            }

            DisableExpiredLightning(false);
            if (Time.time < nextLightningTime)
            {
                return;
            }

            nextLightningTime = Time.time + 1f / Mathf.Max(0.01f, lightningFrequency);
            FireLightningArc(transform.position + Vector3.up * 1f);
        }

        void FireLightningArc(Vector3 centre)
        {
            for (int i = 0; i < lightningPool.Count; i++)
            {
                if (Time.time < lightningEndTimes[i])
                {
                    continue;
                }

                LineRenderer line = lightningPool[i];
                Vector3 start = Random.onUnitSphere * shieldScale;
                Vector3 end = Random.onUnitSphere * shieldScale;
                start.y = Mathf.Clamp(start.y, -0.6f, 0.75f);
                end.y = Mathf.Clamp(end.y, -0.6f, 0.75f);

                line.gameObject.SetActive(true);
                line.widthMultiplier = trailWidth * 0.12f;
                line.positionCount = 4;
                line.SetPosition(0, effectRoot.TransformPoint(start));
                line.SetPosition(1, centre + Vector3.Lerp(start, end, 0.33f) + Random.insideUnitSphere * 0.18f);
                line.SetPosition(2, centre + Vector3.Lerp(start, end, 0.66f) + Random.insideUnitSphere * 0.18f);
                line.SetPosition(3, effectRoot.TransformPoint(end));
                SetLineColor(line, 1f);
                lightningEndTimes[i] = Time.time + lightningArcSeconds;
                return;
            }
        }

        void DisableExpiredLightning(bool force)
        {
            for (int i = 0; i < lightningPool.Count; i++)
            {
                if (force || Time.time >= lightningEndTimes[i])
                {
                    lightningPool[i].gameObject.SetActive(false);
                }
            }
        }

        void UpdateCameraShake()
        {
            if (cameraShakeOffset != Vector3.zero && followCamera != null)
            {
                followCamera.transform.localPosition -= cameraShakeOffset;
                cameraShakeOffset = Vector3.zero;
            }

            if (cameraShakeTimer <= 0f || followCamera == null)
            {
                return;
            }

            cameraShakeTimer -= Time.deltaTime;
            float strength = cameraShakeStrength * Mathf.Clamp01(cameraShakeTimer / Mathf.Max(0.01f, cameraShakeSeconds));
            cameraShakeOffset = Random.insideUnitSphere * strength;
            cameraShakeOffset.z = 0f;
            followCamera.transform.localPosition += cameraShakeOffset;
        }

        float RingScale(Transform ring, float currentBlend, float flash, float flashScale)
        {
            float fadeScale = ring.parent == shieldSphere ? 1f : currentBlend;
            return ringRadiusMultiplier * fadeScale * (1f + flash * flashScale);
        }

        Transform BuildEnergyRing(string name, Transform parent, float radius, float tiltDegrees, int segments)
        {
            Transform ring = CreateChild(name, parent);
            ring.localRotation = Quaternion.Euler(tiltDegrees, 0f, 0f);
            LineRenderer line = ConfigureLineRenderer(ring.gameObject, false, trailWidth * 0.18f);
            line.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            return ring;
        }

        void BuildPatternBand(string name, float y, float radius, int segments, float spike)
        {
            Transform band = CreateChild(name, patternRoot);
            LineRenderer line = ConfigureLineRenderer(band.gameObject, false, trailWidth * 0.12f);
            line.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float r = radius + (i % 2 == 0 ? spike : -spike);
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r));
            }
        }

        void BuildLinePool(string baseName, List<LineRenderer> pool, int count, Transform parent)
        {
            pool.Clear();
            for (int i = 0; i < count; i++)
            {
                Transform item = CreateChild($"{baseName}_{i:00}", parent);
                LineRenderer line = ConfigureLineRenderer(item.gameObject, true, trailWidth * 0.12f);
                line.gameObject.SetActive(false);
                pool.Add(line);
            }
        }

        ParticleSystem CreatePooledParticles(string name, bool looping, int maxParticles, float lifetime, float speed, float size, float rate)
        {
            Transform item = CreateChild(name, effectRoot);
            // Explicit null check instead of ??, which bypasses Unity's
            // overloaded == and breaks on the editor's fake-null components.
            ParticleSystem particles = item.GetComponent<ParticleSystem>();
            if (particles == null)
            {
                particles = item.gameObject.AddComponent<ParticleSystem>();
            }
            ParticleSystem.MainModule main = particles.main;
            main.loop = looping;
            main.playOnAwake = false;
            main.maxParticles = maxParticles;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(shieldColor, secondaryColor);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient lifetimeFade = new Gradient();
            lifetimeFade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = lifetimeFade;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = rate;
            if (!looping)
            {
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(maxParticles, 1, 120)) });
            }

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = looping ? shieldScale * 0.75f : 0.18f;

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = looping;
            velocity.orbitalY = looping ? 0.75f : 0f;
            velocity.speedModifier = looping ? 0.35f : 1f;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = vfxMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.2f;
            return particles;
        }

        TrailRenderer CreateTrail(string name, Transform parent, float time, float width, float minVertexDistance)
        {
            Transform item = CreateChild(name, parent);
            TrailRenderer trail = item.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = item.gameObject.AddComponent<TrailRenderer>();
            }
            trail.sharedMaterial = trailMaterial;
            trail.time = time;
            trail.widthMultiplier = width;
            trail.minVertexDistance = minVertexDistance;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            trail.colorGradient = BuildGradient();
            return trail;
        }

        LineRenderer ConfigureLineRenderer(GameObject go, bool worldSpace, float width)
        {
            LineRenderer line = go.GetComponent<LineRenderer>();
            if (line == null)
            {
                line = go.AddComponent<LineRenderer>();
            }
            line.useWorldSpace = worldSpace;
            line.loop = !worldSpace;
            line.sharedMaterial = trailMaterial;
            line.widthMultiplier = width;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.colorGradient = BuildGradient();
            return line;
        }

        Gradient BuildGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(secondaryColor, 0f),
                    new GradientColorKey(shieldColor, 0.5f),
                    new GradientColorKey(new Color(0.08f, 0.35f, 1f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(0f, 1f),
                });
            return gradient;
        }

        void SetLineRenderers(Transform root, float alpha, float width)
        {
            foreach (LineRenderer line in root.GetComponentsInChildren<LineRenderer>(true))
            {
                line.widthMultiplier = width;
                SetLineColor(line, alpha);
            }
        }

        void SetLineColor(LineRenderer line, float alpha)
        {
            line.GetPropertyBlock(lineBlock);
            // Lines render additively, so pushing rgb with the glow intensity
            // reads as brightness (URP Unlit has no emission channel).
            Color color = shieldColor * (1f + 0.2f * bloomIntensity * Mathf.Clamp01(alpha));
            color.a = Mathf.Clamp01(alpha);
            lineBlock.SetColor("_BaseColor", color);
            lineBlock.SetColor("_Color", color);
            line.SetPropertyBlock(lineBlock);
        }

        Transform CreateChild(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static void SetObjectActive(Transform target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }

        // The builder wires plain URP/Unlit materials, and with HDR off there
        // is no bloom to sell "glow" (URP Unlit also has no emission channel).
        // So the glow is faked at runtime: additive-blend copies of those
        // materials with a generated soft radial sprite, plus the custom
        // fresnel/pattern shader for the shield itself. Built once; the
        // MaterialPropertyBlocks animate on top of these shared instances.
        void CreateRuntimeMaterials()
        {
            if (runtimeVfxMaterial != null)
            {
                return;
            }

            Shader shieldShader = Shader.Find("JoburgRunner/UbuntuPulseShield");
            shieldUsesCustomShader = shieldShader != null;
            if (shieldUsesCustomShader)
            {
                runtimeShieldMaterial = new Material(shieldShader);
                runtimeShieldMaterial.SetColor("_RimColor", new Color(shieldColor.r, shieldColor.g, shieldColor.b, 0.55f));
                runtimeShieldMaterial.SetColor("_PatternColor", secondaryColor);
                runtimeShieldMaterial.SetFloat("_FresnelPower", fresnelPower);
                runtimeShieldMaterial.SetFloat("_GlowIntensity", bloomIntensity);
                runtimeShieldMaterial.SetFloat("_PatternBands", patternBandCount);
                runtimeShieldMaterial.SetFloat("_PatternRepeat", patternRepeat);
                runtimeShieldMaterial.SetFloat("_ScrollSpeed", patternUvScrollSpeed);
            }
            else if (shieldMaterial != null)
            {
                runtimeShieldMaterial = new Material(shieldMaterial);
            }

            Material vfxSource = vfxMaterial != null ? vfxMaterial : shieldMaterial;
            runtimeVfxMaterial = MakeAdditiveGlowMaterial(vfxSource, SoftGlowTexture());
            runtimeTrailMaterial = MakeAdditiveGlowMaterial(trailMaterial != null ? trailMaterial : vfxSource, null);
            runtimeGroundMaterial = MakeAdditiveGlowMaterial(groundGlowMaterial != null ? groundGlowMaterial : vfxSource, SoftGlowTexture());
            runtimeWaveMaterial = MakeAdditiveGlowMaterial(vfxSource, RingTexture());

            if (runtimeVfxMaterial != null)
            {
                vfxMaterial = runtimeVfxMaterial;
            }

            if (runtimeTrailMaterial != null)
            {
                trailMaterial = runtimeTrailMaterial;
            }
        }

        public static Material MakeAdditiveGlowMaterial(Material source, Texture2D sprite)
        {
            if (source == null)
            {
                return null;
            }

            Material material = new Material(source);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = 3050;
            if (sprite != null)
            {
                material.SetTexture("_BaseMap", sprite);
                material.SetTexture("_MainTex", sprite);
            }

            return material;
        }

        public static Texture2D SoftGlowTexture()
        {
            if (sharedSoftGlowTexture == null)
            {
                sharedSoftGlowTexture = BuildRadialTexture(64,
                    d => Mathf.Pow(Mathf.Clamp01(1.1f * (1f - d)), 2.4f));
            }

            return sharedSoftGlowTexture;
        }

        internal static Texture2D RingTexture()
        {
            if (sharedRingTexture == null)
            {
                sharedRingTexture = BuildRadialTexture(64, d =>
                {
                    float band = Mathf.Clamp01(1f - Mathf.Abs(d - 0.72f) / 0.16f);
                    return band * band + 0.08f * Mathf.Clamp01(1f - d);
                });
            }

            return sharedRingTexture;
        }

        static Texture2D BuildRadialTexture(int size, System.Func<float, float> alphaByDistance)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
            };
            Color32[] pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = (byte)(255f * Mathf.Clamp01(alphaByDistance(d)));
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        // The builder's ground glow is a flat cylinder whose cap UVs cannot
        // take the radial sprite; swap it for an upward-facing quad so the
        // glow fades out softly instead of ending in a hard disc edge.
        void ReplaceGroundGlowWithSoftQuad()
        {
            if (groundGlow == null || runtimeGroundMaterial == null || groundGlow.name == "GroundGlowSoft")
            {
                return;
            }

            Transform parent = groundGlow.transform.parent != null ? groundGlow.transform.parent : effectRoot;
            Transform existing = parent.Find("GroundGlowSoft");
            GameObject quad = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "GroundGlowSoft";
            StripCollider(quad);
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = groundGlow.transform.localPosition + Vector3.up * 0.02f;
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
            Renderer quadRenderer = quad.GetComponent<Renderer>();
            quadRenderer.sharedMaterial = runtimeGroundMaterial;
            quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;

            groundGlow.gameObject.SetActive(false);
            groundGlow = quadRenderer;
            quad.SetActive(false);
        }

        Transform CreateShockwaveQuad(string name, out Renderer renderer)
        {
            Transform existing = effectRoot.Find(name);
            GameObject quad = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            StripCollider(quad);
            quad.transform.SetParent(effectRoot, false);
            renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = runtimeWaveMaterial != null ? runtimeWaveMaterial : trailMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            quad.SetActive(false);
            return quad.transform;
        }

        static void StripCollider(GameObject go)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        void UpdateShockwaves()
        {
            AnimateShockwave(activationWave, activationWaveRenderer, activationWaveStart, activationShockwaveRadius, true);
            AnimateShockwave(impactWave, impactWaveRenderer, impactWaveStart, impactShockwaveRadius, false);
        }

        void AnimateShockwave(Transform wave, Renderer renderer, float startTime, float maxRadius, bool flatOnGround)
        {
            if (wave == null || renderer == null)
            {
                return;
            }

            float progress = (Time.time - startTime) / Mathf.Max(0.05f, shockwaveSeconds);
            if (progress < 0f || progress >= 1f)
            {
                wave.gameObject.SetActive(false);
                return;
            }

            wave.gameObject.SetActive(true);
            float eased = 1f - (1f - progress) * (1f - progress) * (1f - progress);
            float diameter = Mathf.Lerp(0.6f, maxRadius * 2f, eased);
            wave.localScale = new Vector3(diameter, diameter, 1f);
            if (flatOnGround)
            {
                wave.localRotation = Quaternion.Euler(90f, 0f, 0f);
                float groundY = groundGlow != null ? groundGlow.transform.localPosition.y + 0.03f : -0.9f;
                wave.localPosition = new Vector3(0f, groundY, 0f);
            }
            else if (followCamera != null)
            {
                wave.rotation = Quaternion.LookRotation(wave.position - followCamera.transform.position);
            }

            renderer.GetPropertyBlock(waveBlock);
            Color color = shieldColor * (1f + 0.5f * bloomIntensity);
            color.a = Mathf.Clamp01(Mathf.Pow(1f - progress, 1.4f) * (0.3f + 0.3f * blend));
            waveBlock.SetColor("_BaseColor", color);
            waveBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(waveBlock);
        }

        void HideShockwaves()
        {
            activationWaveStart = float.NegativeInfinity;
            impactWaveStart = float.NegativeInfinity;
            SetObjectActive(activationWave, false);
            SetObjectActive(impactWave, false);
        }

        /// <summary>
        /// Editor-preview hook: forces the full active look without
        /// PowerUpManager running, so the scene builder's capture method can
        /// screenshot the effect. Safe to call in edit mode; never call it
        /// during a live run.
        /// </summary>
        public void ForcePreview(float previewBlend = 1f, float previewImpactFlash = 0.4f)
        {
            shieldBlock ??= new MaterialPropertyBlock();
            glowBlock ??= new MaterialPropertyBlock();
            lineBlock ??= new MaterialPropertyBlock();
            waveBlock ??= new MaterialPropertyBlock();
            BuildReusableVisuals();
            SetVisualsActive(true);
            blend = Mathf.Clamp01(previewBlend);
            impactFlash = Mathf.Clamp01(previewImpactFlash);
            wasActive = true;
            AnimateActiveVisuals(true);
            activationWaveStart = Time.time - shockwaveSeconds * 0.45f;
            UpdateShockwaves();
            FireLightningArc(transform.position + Vector3.up * 1f);
            if (shieldParticles != null)
            {
                shieldParticles.Simulate(1.2f, true, true);
            }

            if (orbitParticles != null)
            {
                orbitParticles.Simulate(1.5f, true, true);
            }

            if (activationBurst != null)
            {
                activationBurst.Emit(40);
                activationBurst.Simulate(0.12f, true, false);
            }
        }
    }
}
