using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Global game-audio mute switch, persisted across sessions. Backed by
    /// AudioListener.volume rather than gating each sound individually, so it
    /// covers every current and future sound (coin pickups, any later music)
    /// with one flag. AudioListener.volume is a runtime-only static that
    /// always resets to 1 on process start, so the saved preference is
    /// re-applied before the first frame on every launch.
    /// </summary>
    public static class SoundSettings
    {
        const string EnabledKey = "JoburgRunner.SoundEnabled";

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Apply();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            AudioListener.volume = Enabled ? 1f : 0f;
        }
    }
}
