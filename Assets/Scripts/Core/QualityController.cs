using UnityEngine;

namespace JoburgRunner.Core
{
    /// <summary>
    /// Applies the active <see cref="GameQualitySettings"/> and exposes its
    /// scales statically so pooled VFX, environment density, and audio can
    /// consult one source. Safe when unassigned (leaves engine defaults).
    /// </summary>
    public sealed class QualityController : MonoBehaviour
    {
        public static QualityController Instance { get; private set; }

        [SerializeField] GameQualitySettings low;
        [SerializeField] GameQualitySettings medium;
        [SerializeField] GameQualitySettings high;
        [SerializeField] QualityLevel startLevel = QualityLevel.Medium;

        public GameQualitySettings Active { get; private set; }

        public static float ParticleScale => Instance != null && Instance.Active != null ? Instance.Active.particleCountScale : 1f;
        public static float TrailScale => Instance != null && Instance.Active != null ? Instance.Active.trailDurationScale : 1f;
        public static bool CameraFeedbackEnabled => Instance == null || Instance.Active == null || Instance.Active.cameraFeedback;
        public static float PropDensity => Instance != null && Instance.Active != null ? Instance.Active.propDensity : 1f;
        public static float VehicleDensity => Instance != null && Instance.Active != null ? Instance.Active.vehicleDensity : 1f;

        // Pigeon caps (consumed by PigeonSpawner at Start / on quality change).
        public static int PigeonMaxActive => Instance != null && Instance.Active != null ? Instance.Active.pigeonMaxActive : 20;
        public static int PigeonMaxFlocks => Instance != null && Instance.Active != null ? Instance.Active.pigeonMaxFlocks : 2;
        public static bool PigeonCinematics => Instance == null || Instance.Active == null || Instance.Active.pigeonCinematics;
        public static float PigeonCullDistance => Instance != null && Instance.Active != null ? Instance.Active.pigeonCullDistance : 50f;
        public static bool PigeonLanding => Instance == null || Instance.Active == null || Instance.Active.pigeonLanding;
        public static bool PigeonShadows => Instance == null || Instance.Active == null || Instance.Active.pigeonShadows;
        public static bool PigeonAudio => Instance == null || Instance.Active == null || Instance.Active.pigeonAudio;

        void Awake()
        {
            Instance = this;
            Apply(startLevel);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Apply(QualityLevel level)
        {
            Active = level switch
            {
                QualityLevel.Low => low,
                QualityLevel.High => high,
                _ => medium,
            };

            if (Active == null)
            {
                return;
            }

            QualitySettings.shadowDistance = Active.shadowDistance;
        }
    }
}
