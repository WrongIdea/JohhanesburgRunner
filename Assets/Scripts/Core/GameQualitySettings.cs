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
    }
}
