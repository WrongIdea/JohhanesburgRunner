using UnityEngine;

namespace JoburgRunner.Audio
{
    /// <summary>Mixer routing category for a sound.</summary>
    public enum AudioCategory
    {
        Music,
        Ambience,
        UI,
        Player,
        Vehicle,
        Effects,
    }

    /// <summary>Gameplay moment that fires a sound, mapped from central GameEvents.</summary>
    public enum AudioTrigger
    {
        None,
        Coin,
        Jump,
        Land,
        Roll,
        LaneSwitch,
        PerfectDodge,
        PowerUpStart,
        PowerUpWarning,
        PowerUpEnd,
        ShieldBreak,
        Crash,
        ButtonClick,
        MissionComplete,
        AchievementUnlock,
        RewardClaim,
    }

    /// <summary>
    /// Inspector-authored sound. Supports multiple interchangeable samples
    /// (picked at random) and random pitch, so repeated coins/steps don't
    /// machine-gun; a minimum interval throttles spammy sounds (horns, coins);
    /// a concurrency cap prevents voice floods and clipping.
    /// Pure data — <see cref="AudioManager"/> owns pooling and mixer routing.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Audio Definition", fileName = "Audio")]
    public sealed class AudioDefinition : ScriptableObject
    {
        public string id;
        public AudioCategory category = AudioCategory.Effects;
        [Tooltip("Gameplay trigger that plays this sound (None = played manually, e.g. music/ambience).")]
        public AudioTrigger trigger = AudioTrigger.None;

        [Tooltip("One or more interchangeable samples; a random one plays each time.")]
        public AudioClip[] clips = new AudioClip[0];

        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = new Vector2(0.97f, 1.03f);
        public bool loop;

        [Header("Anti-Spam")]
        [Tooltip("Minimum seconds between plays of this sound (0 = no throttle).")]
        [Min(0f)] public float minInterval = 0f;
        [Tooltip("Max concurrent voices of this sound (0 = unlimited).")]
        [Min(0)] public int maxConcurrent = 4;

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            return clips[Random.Range(0, clips.Length)];
        }

        public float PickPitch() => Random.Range(Mathf.Min(pitchRange.x, pitchRange.y), Mathf.Max(pitchRange.x, pitchRange.y));
    }
}
