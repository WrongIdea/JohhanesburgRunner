using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.VFX
{
    /// <summary>
    /// Central, pooled player for gameplay effects. Subscribes to
    /// <see cref="GameplayVFXEvents"/>, and for each trigger plays every
    /// matching <see cref="VFXDefinition"/>: spawns its pooled visual, fires its
    /// one-shot sound (with pitch jitter), and applies its camera impulse via
    /// <see cref="CameraFeedbackController"/>. Recalls all live effects on crash
    /// or when disabled, so nothing lingers across a restart or scene change.
    ///
    /// Inert until given definitions, so it is safe to add to the scene ahead
    /// of the gameplay scripts learning to raise events.
    /// </summary>
    public sealed class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Tooltip("All effects this manager can play. Multiple definitions may share a trigger.")]
        [SerializeField] List<VFXDefinition> definitions = new List<VFXDefinition>();
        [Tooltip("Concurrent one-shot audio voices for effect sounds.")]
        [SerializeField] int audioVoices = 6;

        readonly Dictionary<GameplayVFXTrigger, List<Entry>> byTrigger = new Dictionary<GameplayVFXTrigger, List<Entry>>();
        readonly List<VFXPool> allPools = new List<VFXPool>();
        AudioSource[] voices;
        int nextVoice;
        Transform poolRoot;

        struct Entry
        {
            public VFXDefinition definition;
            public VFXPool pool;
        }

        void Awake()
        {
            Instance = this;

            poolRoot = new GameObject("VFXPools").transform;
            poolRoot.SetParent(transform, false);

            foreach (VFXDefinition definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                VFXPool pool = new VFXPool(definition, poolRoot);
                allPools.Add(pool);

                if (!byTrigger.TryGetValue(definition.trigger, out List<Entry> list))
                {
                    list = new List<Entry>();
                    byTrigger[definition.trigger] = list;
                }

                list.Add(new Entry { definition = definition, pool = pool });
            }

            voices = new AudioSource[Mathf.Max(1, audioVoices)];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
            }
        }

        void OnEnable()
        {
            GameEvents.CoinCollected += OnCoin;
            GameEvents.PlayerJumped += OnJump;
            GameEvents.PlayerLanded += OnLand;
            GameEvents.PlayerRolled += OnRoll;
            GameEvents.LaneChanged += OnLane;
            GameEvents.PerfectDodge += OnDodge;
            GameEvents.PowerUpStarted += OnPowerUpStart;
            GameEvents.PowerUpWarning += OnPowerUpWarning;
            GameEvents.PowerUpEnded += OnPowerUpEnd;
            GameEvents.PlayerCrashed += OnCrash;
        }

        void OnDisable()
        {
            GameEvents.CoinCollected -= OnCoin;
            GameEvents.PlayerJumped -= OnJump;
            GameEvents.PlayerLanded -= OnLand;
            GameEvents.PlayerRolled -= OnRoll;
            GameEvents.LaneChanged -= OnLane;
            GameEvents.PerfectDodge -= OnDodge;
            GameEvents.PowerUpStarted -= OnPowerUpStart;
            GameEvents.PowerUpWarning -= OnPowerUpWarning;
            GameEvents.PowerUpEnded -= OnPowerUpEnd;
            GameEvents.PlayerCrashed -= OnCrash;
            ClearAll();
        }

        void OnCoin(int value, bool rare, Vector3 pos) => Dispatch(
            rare ? GameplayVFXTrigger.RareCoinCollected : GameplayVFXTrigger.CoinCollected,
            new GameplayVFXContext { position = pos, amount = value });
        void OnJump(Vector3 p) => Dispatch(GameplayVFXTrigger.Jump, new GameplayVFXContext { position = p });
        void OnLand(Vector3 p) => Dispatch(GameplayVFXTrigger.Land, new GameplayVFXContext { position = p });
        void OnRoll(Transform t) => Dispatch(GameplayVFXTrigger.Roll, new GameplayVFXContext { position = t != null ? t.position : Vector3.zero, follow = t });
        void OnLane(int dir) => Dispatch(GameplayVFXTrigger.LaneSwitch, new GameplayVFXContext { direction = dir });
        void OnDodge(Vector3 p) => Dispatch(GameplayVFXTrigger.PerfectDodge, new GameplayVFXContext { position = p });
        void OnPowerUpStart(PowerUpType t) => Dispatch(GameplayVFXTrigger.PowerUpStart, new GameplayVFXContext { powerUp = t });
        void OnPowerUpWarning(PowerUpType t) => Dispatch(GameplayVFXTrigger.PowerUpWarning, new GameplayVFXContext { powerUp = t });
        void OnPowerUpEnd(PowerUpType t) => Dispatch(GameplayVFXTrigger.PowerUpEnd, new GameplayVFXContext { powerUp = t });
        void OnCrash()
        {
            ClearAll();
            Dispatch(GameplayVFXTrigger.Crash, default);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Update()
        {
            for (int i = 0; i < allPools.Count; i++)
            {
                allPools[i].Tick();
            }
        }

        void Dispatch(GameplayVFXTrigger trigger, GameplayVFXContext context)
        {
            if (!byTrigger.TryGetValue(trigger, out List<Entry> list))
            {
                return;
            }

            foreach (Entry entry in list)
            {
                VFXDefinition def = entry.definition;
                if (def.filterByPowerUp && def.powerUp != context.powerUp)
                {
                    continue;
                }

                entry.pool.Play(context);
                PlaySound(def, context.position);

                if (def.cameraFeedback && CameraFeedbackController.Instance != null)
                {
                    CameraFeedbackController.Instance.AddImpulse(def);
                }
            }
        }

        void PlaySound(VFXDefinition def, Vector3 position)
        {
            if (def.sound == null || voices == null)
            {
                return;
            }

            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.pitch = 1f + Random.Range(-def.pitchJitter, def.pitchJitter);
            voice.PlayOneShot(def.sound, def.volume);
        }

        void ClearAll()
        {
            for (int i = 0; i < allPools.Count; i++)
            {
                allPools[i].Clear();
            }
        }
    }
}
