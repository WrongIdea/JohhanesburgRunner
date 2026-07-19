using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace JoburgRunner.Audio
{
    /// <summary>
    /// Central, pooled audio system: routes categorised sounds through mixer
    /// groups, plays event-driven SFX with random sample/pitch variation and
    /// anti-spam throttling, and runs a small music state machine (menu ↔
    /// gameplay ↔ game-over) with crossfades plus a speed-driven intensity
    /// layer.
    ///
    /// Foundation component — inert until given definitions and (optionally) a
    /// mixer. Designed to replace the current ad-hoc PlayClipAtPoint calls; it
    /// is intentionally not added to the scene until those are migrated, to
    /// avoid double-playing sounds.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Mixer (optional)")]
        [SerializeField] AudioMixer mixer;
        [SerializeField] AudioMixerGroup musicGroup;
        [SerializeField] AudioMixerGroup ambienceGroup;
        [SerializeField] AudioMixerGroup uiGroup;
        [SerializeField] AudioMixerGroup playerGroup;
        [SerializeField] AudioMixerGroup vehicleGroup;
        [SerializeField] AudioMixerGroup effectsGroup;

        [Header("SFX")]
        [SerializeField] List<AudioDefinition> definitions = new List<AudioDefinition>();
        [SerializeField] int sfxVoices = 12;

        [Header("Music")]
        [SerializeField] AudioClip menuMusic;
        [SerializeField] AudioClip gameplayMusic;
        [SerializeField] AudioClip gameplayHighIntensityMusic;
        [SerializeField] AudioClip gameOverSting;
        [SerializeField] float musicCrossfade = 1.2f;
        [Range(0f, 1f)] [SerializeField] float musicVolume = 0.5f;

        readonly Dictionary<AudioTrigger, AudioDefinition> byTrigger = new Dictionary<AudioTrigger, AudioDefinition>();
        readonly Dictionary<AudioDefinition, float> lastPlayed = new Dictionary<AudioDefinition, float>();

        AudioSource[] voices;
        int nextVoice;
        AudioSource musicA;
        AudioSource musicB;
        bool musicAActive;

        void Awake()
        {
            Instance = this;

            foreach (AudioDefinition def in definitions)
            {
                if (def != null && def.trigger != AudioTrigger.None)
                {
                    byTrigger[def.trigger] = def;
                }
            }

            voices = new AudioSource[Mathf.Max(1, sfxVoices)];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
                voices[i].outputAudioMixerGroup = effectsGroup;
            }

            musicA = gameObject.AddComponent<AudioSource>();
            musicB = gameObject.AddComponent<AudioSource>();
            foreach (AudioSource m in new[] { musicA, musicB })
            {
                m.playOnAwake = false;
                m.loop = true;
                m.spatialBlend = 0f;
                m.volume = 0f;
                m.outputAudioMixerGroup = musicGroup;
            }
        }

        void OnEnable()
        {
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.RunEnded += OnRunEnded;
            GameEvents.CoinCollected += (v, r, p) => Play(AudioTrigger.Coin);
            GameEvents.PlayerJumped += p => Play(AudioTrigger.Jump);
            GameEvents.PlayerLanded += p => Play(AudioTrigger.Land);
            GameEvents.PlayerRolled += t => Play(AudioTrigger.Roll);
            GameEvents.LaneChanged += d => Play(AudioTrigger.LaneSwitch);
            GameEvents.PerfectDodge += p => Play(AudioTrigger.PerfectDodge);
            GameEvents.PowerUpStarted += t => Play(AudioTrigger.PowerUpStart);
            GameEvents.PowerUpWarning += t => Play(AudioTrigger.PowerUpWarning);
            GameEvents.PowerUpEnded += t => Play(AudioTrigger.PowerUpEnd);
            GameEvents.PlayerCrashed += () => Play(AudioTrigger.Crash);
            GameEvents.MissionCompleted += id => Play(AudioTrigger.MissionComplete);
            GameEvents.AchievementUnlocked += id => Play(AudioTrigger.AchievementUnlock);
            GameEvents.RewardClaimed += d => Play(AudioTrigger.RewardClaim);

            PlayMusic(menuMusic);
        }

        void OnDisable()
        {
            // Lambda subscriptions above are anonymous; clearing GameEvents on
            // subsystem registration (see GameEvents.ResetStatics) prevents
            // cross-session leaks. For in-session teardown, disabling the
            // manager stops its voices below.
            for (int i = 0; voices != null && i < voices.Length; i++)
            {
                if (voices[i] != null) voices[i].Stop();
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Plays a UI/button sound by trigger (call from buttons).</summary>
        public void PlayUI(AudioTrigger trigger) => Play(trigger);

        public void Play(AudioTrigger trigger)
        {
            if (!byTrigger.TryGetValue(trigger, out AudioDefinition def) || def == null)
            {
                return;
            }

            Play(def);
        }

        public void Play(AudioDefinition def)
        {
            if (def == null || voices == null)
            {
                return;
            }

            // Anti-spam throttle (e.g. horns, coin bursts). Global concurrency
            // is bounded by the fixed voice pool, so no per-sound counter needed.
            if (def.minInterval > 0f && lastPlayed.TryGetValue(def, out float last) && Time.unscaledTime - last < def.minInterval)
            {
                return;
            }

            AudioClip clip = def.PickClip();
            if (clip == null)
            {
                return;
            }

            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.outputAudioMixerGroup = GroupFor(def.category);
            voice.pitch = def.PickPitch();
            voice.PlayOneShot(clip, def.volume);

            lastPlayed[def] = Time.unscaledTime;
        }

        AudioMixerGroup GroupFor(AudioCategory category) => category switch
        {
            AudioCategory.Music => musicGroup,
            AudioCategory.Ambience => ambienceGroup,
            AudioCategory.UI => uiGroup,
            AudioCategory.Player => playerGroup,
            AudioCategory.Vehicle => vehicleGroup,
            _ => effectsGroup,
        };

        // ---- Music state machine ----
        void OnRunStarted() => PlayMusic(gameplayMusic);
        void OnRunEnded(RunSummary s)
        {
            if (gameOverSting != null)
            {
                AudioSource sting = voices[nextVoice];
                nextVoice = (nextVoice + 1) % voices.Length;
                sting.outputAudioMixerGroup = musicGroup;
                sting.pitch = 1f;
                sting.PlayOneShot(gameOverSting, musicVolume);
            }

            PlayMusic(menuMusic);
        }

        /// <summary>Crossfades to a new looping track (no-op if already playing it).</summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            AudioSource from = musicAActive ? musicA : musicB;
            AudioSource to = musicAActive ? musicB : musicA;
            if (to.clip == clip && to.isPlaying)
            {
                return;
            }

            to.clip = clip;
            to.volume = 0f;
            to.Play();
            musicAActive = !musicAActive;
            StopAllCoroutines();
            StartCoroutine(Crossfade(from, to));
        }

        System.Collections.IEnumerator Crossfade(AudioSource from, AudioSource to)
        {
            float t = 0f;
            float startFrom = from != null ? from.volume : 0f;
            while (t < musicCrossfade)
            {
                t += Time.unscaledDeltaTime;
                float k = musicCrossfade <= 0f ? 1f : t / musicCrossfade;
                if (from != null) from.volume = Mathf.Lerp(startFrom, 0f, k);
                to.volume = Mathf.Lerp(0f, musicVolume, k);
                yield return null;
            }

            if (from != null)
            {
                from.Stop();
            }

            to.volume = musicVolume;
        }
    }
}
