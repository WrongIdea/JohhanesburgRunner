using UnityEngine;

namespace JoburgRunner.Core
{
    public enum QualityLevel
    {
        Low,
        Medium,
        High,
    }

    /// <summary>
    /// Inspector-authored quality preset. Other systems read these scales
    /// (particles, trails, density, camera feedback, audio voices) so one
    /// asset tunes the whole game for a device tier. Ship one asset per level.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Quality Settings", fileName = "Quality")]
    public sealed class GameQualitySettings : ScriptableObject
    {
        public QualityLevel level = QualityLevel.Medium;

        [Header("VFX")]
        [Range(0f, 1f)] public float particleCountScale = 1f;
        [Range(0f, 1f)] public float trailDurationScale = 1f;
        public bool cameraFeedback = true;
        public bool postProcessing = false;

        [Header("Environment Density")]
        [Range(0f, 1f)] public float propDensity = 1f;
        [Range(0f, 1f)] public float pedestrianDensity = 1f;
        [Range(0f, 1f)] public float vehicleDensity = 1f;

        [Header("Rendering")]
        [Min(0f)] public float shadowDistance = 40f;
        [Min(0)] public int shadowCascades = 2;

        [Header("Audio")]
        [Min(1)] public int audioVoiceLimit = 12;

        [Header("Pigeons")]
        [Tooltip("Hard cap on simultaneously active pigeons for this tier.")]
        [Min(0)] public int pigeonMaxActive = 20;
        [Tooltip("Max ordinary (non-cinematic) flocks alive at once.")]
        [Min(0)] public int pigeonMaxFlocks = 2;
        [Tooltip("Allow rare large cinematic pigeon set-pieces.")]
        public bool pigeonCinematics = true;
        [Tooltip("Distance behind the player at which flocks are reclaimed / culled.")]
        [Min(5f)] public float pigeonCullDistance = 50f;
        [Tooltip("Allow pigeons to perch on landing points (vs. always fly off).")]
        public bool pigeonLanding = true;
        [Tooltip("Cast shadows from pigeons.")]
        public bool pigeonShadows = true;
        [Tooltip("Play pigeon audio (coo / wing flutter).")]
        public bool pigeonAudio = true;
    }
}
