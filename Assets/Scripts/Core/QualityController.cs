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

        /// <summary>When true, the frame rate is forced to 30 FPS to save battery/heat.</summary>
        public static bool BatterySaver { get; private set; }

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

        const string BatterySaverKey = "BatterySaver";

        void Awake()
        {
            Instance = this;
            BatterySaver = PlayerPrefs.GetInt(BatterySaverKey, 0) == 1;
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
            ApplyFrameRate();
        }

        /// <summary>
        /// Toggle the 30 FPS battery-saving mode (Part 1). Opt-in; halving the cap
        /// roughly halves GPU+CPU work and device heat with no visual change.
        /// </summary>
        public void SetBatterySaver(bool on)
        {
            BatterySaver = on;
            PlayerPrefs.SetInt(BatterySaverKey, on ? 1 : 0);
            ApplyFrameRate();
        }

        /// <summary>
        /// Single owner of the mobile frame cap. Battery mode wins; otherwise the
        /// active tier's target (default 60 for assets predating the field).
        /// </summary>
        void ApplyFrameRate()
        {
            int tierFps = Active != null && Active.targetFrameRate > 0 ? Active.targetFrameRate : 60;
            Application.targetFrameRate = BatterySaver ? 30 : tierFps;
            QualitySettings.vSyncCount = 0; // vSync would re-pin us to the display's 120 Hz
        }

        // Some Android ROMs reset targetFrameRate when the app regains focus; re-assert it.
        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ApplyFrameRate();
            }
        }
    }
}
