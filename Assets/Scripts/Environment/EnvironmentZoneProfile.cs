using UnityEngine;

namespace JoburgRunner.Environment
{
    /// <summary>
    /// The seven Johannesburg destination routes a run can travel through.
    /// Appended-only: existing profiles reference these by value, so never
    /// reorder — add new zones at the end.
    /// </summary>
    public enum EnvironmentZoneId
    {
        JoburgCBD,
        Braamfontein,
        Maboneng,
        Soweto,
        Sandton,
        TaxiRankDistrict,
        IndustrialDistrict,
    }

    /// <summary>
    /// Inspector-authored description of one Johannesburg zone: its palette,
    /// lighting mood, ambience, difficulty feel, and the pooled content
    /// (buildings, props, obstacles) plus per-segment variation rules that
    /// dress it. Pure data — <see cref="EnvironmentDirector"/> reads a profile
    /// at runtime and applies it. Nothing here spawns or holds scene state, so
    /// a profile is safe to share across scenes and reload.
    ///
    /// A zone does not have to be fully populated to be valid: empty content
    /// arrays simply mean "reuse whatever the base road segment already
    /// provides", which is how the two shipped example profiles work off the
    /// existing generated environment library.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Environment Zone Profile", fileName = "ZoneProfile")]
    public sealed class EnvironmentZoneProfile : ScriptableObject
    {
        [Tooltip("Shown in debug overlays and future zone-select UI.")]
        public string displayName = "Johannesburg";
        public EnvironmentZoneId zoneId = EnvironmentZoneId.JoburgCBD;

        [Header("Colour Palette")]
        public ColorPalette palette = ColorPalette.Default;

        [Header("Lighting Mood")]
        public LightingProfile lighting = LightingProfile.Default;

        [Header("Audio Ambience")]
        public AudioProfile audio = AudioProfile.Default;

        [Header("Difficulty Feel")]
        public DifficultyModifiers difficulty = DifficultyModifiers.Default;

        [Header("Pooled Content (empty = reuse base segment)")]
        public WeightedPrefab[] buildings = new WeightedPrefab[0];
        public WeightedPrefab[] props = new WeightedPrefab[0];
        public WeightedPrefab[] obstacles = new WeightedPrefab[0];

        [Header("Per-Segment Variation")]
        public EnvironmentVariation variation = EnvironmentVariation.Default;

        [Header("Route Selection")]
        [Tooltip("Relative chance of this zone being chosen for a route leg.")]
        [Min(0f)] public float routeWeight = 1f;
        [Tooltip("Metres the player must have run before this zone can appear (0 = from the start).")]
        [Min(0f)] public float minRunDistance = 0f;

        /// <summary>Colours that dress buildings, roads, weather and haze.</summary>
        [System.Serializable]
        public struct ColorPalette
        {
            public Color[] buildingColors;
            public Color[] accentColors;
            public Color skyTint;
            public Color hazeColor;
            [Tooltip("Multiplied over the road/asphalt when this zone is active.")]
            public Color roadWearTint;
            [Tooltip("Subtle full-scene colour grade stand-in (applied to ambient), e.g. warm CBD dust vs cool Sandton glass.")]
            public Color weatherTint;

            public static ColorPalette Default => new ColorPalette
            {
                buildingColors = new[] { new Color(0.72f, 0.68f, 0.60f), new Color(0.55f, 0.58f, 0.62f) },
                accentColors = new[] { new Color(0.90f, 0.55f, 0.15f) },
                skyTint = new Color(0.58f, 0.78f, 0.95f),
                hazeColor = new Color(0.70f, 0.76f, 0.83f),
                roadWearTint = Color.white,
                weatherTint = Color.white,
            };
        }

        /// <summary>
        /// Optional lighting overrides. Left off by default so the existing
        /// DayNightCycle keeps ownership of the sun; turn on per zone to force
        /// a mood (e.g. golden-hour Soweto, cool Sandton morning).
        /// </summary>
        [System.Serializable]
        public struct LightingProfile
        {
            public bool overrideLighting;
            public Color sunColor;
            [Min(0f)] public float sunIntensity;
            public Color ambientTint;
            public bool enableFog;
            public Color fogColor;
            [Min(0f)] public float fogDensity;

            public static LightingProfile Default => new LightingProfile
            {
                overrideLighting = false,
                sunColor = Color.white,
                sunIntensity = 1f,
                ambientTint = new Color(0.5f, 0.5f, 0.5f),
                enableFog = false,
                fogColor = new Color(0.70f, 0.76f, 0.83f),
                fogDensity = 0.008f,
            };
        }

        [System.Serializable]
        public struct AudioProfile
        {
            [Tooltip("Looping ambience bed (traffic hum, taxi-rank chatter, township street life).")]
            public AudioClip ambienceClip;
            [Range(0f, 1f)] public float ambienceVolume;
            [Tooltip("Optional per-zone music; leave empty to keep the current track.")]
            public AudioClip musicClip;
            [Range(0f, 1f)] public float musicVolume;

            public static AudioProfile Default => new AudioProfile
            {
                ambienceClip = null,
                ambienceVolume = 0.5f,
                musicClip = null,
                musicVolume = 0.45f,
            };
        }

        /// <summary>
        /// Non-destructive difficulty tuning: multiplies the player's forward
        /// speed and scales the chunk-tier spawn weights the ChunkManager
        /// already computes, so a zone can feel faster/harder without touching
        /// the working difficulty ramp.
        /// </summary>
        [System.Serializable]
        public struct DifficultyModifiers
        {
            [Range(0.5f, 2f)] public float forwardSpeedMultiplier;
            [Range(0f, 3f)] public float easyWeightScale;
            [Range(0f, 3f)] public float mediumWeightScale;
            [Range(0f, 3f)] public float hardWeightScale;
            [Range(0f, 3f)] public float specialWeightScale;

            public static DifficultyModifiers Default => new DifficultyModifiers
            {
                forwardSpeedMultiplier = 1f,
                easyWeightScale = 1f,
                mediumWeightScale = 1f,
                hardWeightScale = 1f,
                specialWeightScale = 1f,
            };

            public float WeightScale(ChunkDifficulty tier) => tier switch
            {
                ChunkDifficulty.Easy => easyWeightScale,
                ChunkDifficulty.Medium => mediumWeightScale,
                ChunkDifficulty.Hard => hardWeightScale,
                _ => specialWeightScale,
            };
        }

        /// <summary>A pooled prefab with a relative spawn weight.</summary>
        [System.Serializable]
        public struct WeightedPrefab
        {
            public GameObject prefab;
            [Min(0f)] public float weight;
        }
    }
}
