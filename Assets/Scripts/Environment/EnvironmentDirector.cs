using UnityEngine;

namespace JoburgRunner.Environment
{
    /// <summary>
    /// Runtime owner of the active Johannesburg zone. Advances the route by
    /// distance, applies the zone's ambience (and optional lighting mood), and
    /// exposes read-only tuning that other systems can consult without being
    /// coupled to it:
    ///   - <see cref="ForwardSpeedMultiplier"/> for the player controller,
    ///   - <see cref="SpawnWeightScale"/> for the chunk spawner,
    ///   - <see cref="NextVariation"/> for the road-segment dressing layer.
    ///
    /// Fully inert until given a <see cref="ZoneCatalog"/> and a player, so it
    /// is safe to add to the scene before the rest of the integration lands —
    /// no catalog means no behaviour change. It never fights DayNightCycle
    /// unless a zone explicitly opts into lighting override.
    /// </summary>
    public sealed class EnvironmentDirector : MonoBehaviour
    {
        public static EnvironmentDirector Instance { get; private set; }

        [Header("References")]
        [SerializeField] ZoneCatalog catalog;
        [SerializeField] Transform player;
        [Tooltip("Optional: only needed if any zone opts into lighting override.")]
        [SerializeField] Light sun;
        [Tooltip("Optional looping ambience source; auto-created if left empty and a zone has ambience.")]
        [SerializeField] AudioSource ambienceSource;

        public EnvironmentZoneProfile ActiveZone { get; private set; }

        public float ForwardSpeedMultiplier =>
            ActiveZone != null ? ActiveZone.difficulty.forwardSpeedMultiplier : 1f;

        /// <summary>Fired when the active zone changes (arg = the new zone).</summary>
        public event System.Action<EnvironmentZoneProfile> ZoneChanged;

        float zoneStartZ;
        NoRepeatPicker zonePicker;
        EnvironmentVariation.VariationState variationState;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Start()
        {
            if (catalog == null)
            {
                return;
            }

            zonePicker = new NoRepeatPicker(catalog.avoidRecentZones);
            zoneStartZ = player != null ? player.position.z : 0f;
            SetZone(catalog.OpeningZone);
        }

        void Update()
        {
            if (catalog == null || player == null || ActiveZone == null)
            {
                return;
            }

            if (player.position.z - zoneStartZ >= catalog.metresPerZone)
            {
                zoneStartZ = player.position.z;
                float runDistance = player.position.z;
                SetZone(catalog.Pick(runDistance, zonePicker));
            }
        }

        /// <summary>Scales a chunk tier's spawn weight for the active zone.</summary>
        public float SpawnWeightScale(ChunkDifficulty tier) =>
            ActiveZone != null ? ActiveZone.difficulty.WeightScale(tier) : 1f;

        /// <summary>
        /// Resolves the next per-segment dressing combination for the active
        /// zone, honouring its anti-repetition memory. Returns a default
        /// selection (all -1 / neutral) when no zone is active.
        /// </summary>
        public EnvironmentVariation.Selection NextVariation()
        {
            if (ActiveZone == null)
            {
                return default;
            }

            variationState ??= new EnvironmentVariation.VariationState(ActiveZone.variation.avoidRecentCount);
            return ActiveZone.variation.Pick(variationState);
        }

        void SetZone(EnvironmentZoneProfile zone)
        {
            if (zone == null || zone == ActiveZone)
            {
                return;
            }

            ActiveZone = zone;
            // Reset variation memory so a new zone's pools are picked fresh.
            variationState = new EnvironmentVariation.VariationState(zone.variation.avoidRecentCount);

            ApplyAmbience(zone.audio);
            ApplyLighting(zone.lighting);

            ZoneChanged?.Invoke(zone);
        }

        void ApplyAmbience(EnvironmentZoneProfile.AudioProfile audio)
        {
            if (audio.ambienceClip == null)
            {
                if (ambienceSource != null)
                {
                    ambienceSource.Stop();
                }

                return;
            }

            if (ambienceSource == null)
            {
                ambienceSource = gameObject.AddComponent<AudioSource>();
                ambienceSource.loop = true;
                ambienceSource.playOnAwake = false;
                ambienceSource.spatialBlend = 0f;
            }

            ambienceSource.clip = audio.ambienceClip;
            ambienceSource.volume = audio.ambienceVolume;
            ambienceSource.Play();
        }

        void ApplyLighting(EnvironmentZoneProfile.LightingProfile lighting)
        {
            if (!lighting.overrideLighting)
            {
                return;
            }

            if (sun != null)
            {
                sun.color = lighting.sunColor;
                sun.intensity = lighting.sunIntensity;
            }

            RenderSettings.ambientLight = lighting.ambientTint;
            RenderSettings.fog = lighting.enableFog;
            if (lighting.enableFog)
            {
                RenderSettings.fogColor = lighting.fogColor;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = lighting.fogDensity;
            }
        }
    }
}
