using UnityEngine;

namespace JoburgRunner.VFX
{
    /// <summary>
    /// Inspector-authored description of one pooled gameplay effect: which
    /// trigger fires it, the prefab to spawn, how long it lives, its pool
    /// sizing, an optional one-shot sound, and an optional camera-feedback
    /// impulse. Pure data — <see cref="VFXManager"/> owns the pools and plays
    /// definitions; <see cref="CameraFeedbackController"/> reads the impulse.
    ///
    /// Keeping effects as data (not hard-coded into gameplay scripts) is the
    /// core of Priority 2: a designer tunes duration/particle count/impulse in
    /// the Inspector and adds new effects without new code.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/VFX Definition", fileName = "VFX")]
    public sealed class VFXDefinition : ScriptableObject
    {
        [Header("Trigger")]
        public GameplayVFXTrigger trigger = GameplayVFXTrigger.CoinCollected;
        [Tooltip("When set, only fires for this power-up (for the PowerUp* triggers).")]
        public bool filterByPowerUp;
        public PowerUpType powerUp;

        [Header("Spawn")]
        [Tooltip("Pooled root to spawn — a ParticleSystem, trail, or small hierarchy. Leave empty for a sound/camera-only effect.")]
        public GameObject prefab;
        [Tooltip("Seconds before the instance returns to the pool. Keep coin/dodge effects short — they fire constantly.")]
        [Min(0.05f)] public float lifetime = 0.6f;
        [Tooltip("Parent the instance to the trigger's follow transform so it tracks the player.")]
        public bool followTarget;
        public Vector3 spawnOffset = Vector3.zero;
        [Min(0.01f)] public float scale = 1f;

        [Header("Pooling")]
        [Min(0)] public int prewarm = 4;
        [Tooltip("Hard cap on live instances; extra triggers are dropped rather than allocating. 0 = unlimited.")]
        [Min(0)] public int maxInstances = 16;

        [Header("Audio")]
        public AudioClip sound;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Random pitch spread so repeated coins/steps don't sound machine-gun identical.")]
        [Range(0f, 0.5f)] public float pitchJitter = 0.05f;

        [Header("Camera Feedback (optional)")]
        public bool cameraFeedback;
        [Tooltip("Transient positional kick, in camera-local metres. Keep tiny for coins.")]
        public Vector3 positionalImpulse = Vector3.zero;
        [Tooltip("Transient rotational kick in degrees. Keep near zero — the follow camera is deliberately tilt-free.")]
        public Vector3 rotationalImpulse = Vector3.zero;
        [Tooltip("Transient field-of-view pulse in degrees (e.g. +3 punch on power-up start).")]
        public float fovPulse = 0f;
        [Min(0.02f)] public float feedbackDuration = 0.15f;
    }
}
