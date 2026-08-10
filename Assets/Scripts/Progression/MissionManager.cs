using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Progression
{
    /// <summary>
    /// Tracks mission progress from central <see cref="GameEvents"/>, persists
    /// it to the player profile, rotates the daily slate once per calendar day,
    /// and awards each reward exactly once. Exposes the active slate for UI.
    ///
    /// Fully event-driven and pooled-free: no per-frame work beyond the cheap
    /// distance/score handlers, and it can be disabled wholesale by leaving the
    /// mission pool empty.
    /// </summary>
    public sealed class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Tooltip("All authorable missions. Daily ones are rotated onto the slate; weekly/long-term persist.")]
        [SerializeField] List<MissionDefinition> missionPool = new List<MissionDefinition>();
        [Tooltip("How many daily missions are active at once.")]
        [SerializeField] int dailySlots = 3;

        readonly Dictionary<string, MissionDefinition> byId = new Dictionary<string, MissionDefinition>();
        readonly List<MissionDefinition> active = new List<MissionDefinition>();

        // Per-run counters for run-scoped mission types.
        int runSurviveSeconds;
        float runTimer;
        bool runActive;

        public IReadOnlyList<MissionDefinition> ActiveMissions => active;

        void Awake()
        {
            Instance = this;
            foreach (MissionDefinition m in missionPool)
            {
                if (m != null && !string.IsNullOrEmpty(m.id))
                {
                    byId[m.id] = m;
                }
            }
        }

        void OnEnable()
        {
            RollDailyIfNeeded();

            GameEvents.RunStarted += OnRunStarted;
            GameEvents.RunEnded += OnRunEnded;
            GameEvents.CoinCollected += OnCoin;
            GameEvents.ScoreChanged += OnScore;
            GameEvents.DistanceChanged += OnDistance;
            GameEvents.PlayerJumped += OnJump;
            GameEvents.PlayerRolled += OnRoll;
            GameEvents.PerfectDodge += OnPerfectDodge;
            GameEvents.PowerUpStarted += OnPowerUp;
        }

        void OnDisable()
        {
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.RunEnded -= OnRunEnded;
            GameEvents.CoinCollected -= OnCoin;
            GameEvents.ScoreChanged -= OnScore;
            GameEvents.DistanceChanged -= OnDistance;
            GameEvents.PlayerJumped -= OnJump;
            GameEvents.PlayerRolled -= OnRoll;
            GameEvents.PerfectDodge -= OnPerfectDodge;
            GameEvents.PowerUpStarted -= OnPowerUp;
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
            if (!runActive)
            {
                return;
            }

            runTimer += Time.deltaTime;
            int seconds = Mathf.FloorToInt(runTimer);
            if (seconds > runSurviveSeconds)
            {
                runSurviveSeconds = seconds;
                Report(MissionType.SurviveDuration, seconds, absolute: true);
            }
        }

        // ---- event handlers ----
        void OnRunStarted() { runActive = true; runTimer = 0f; runSurviveSeconds = 0; Report(MissionType.PlayRuns, 1); }
        void OnRunEnded(RunSummary s) { runActive = false; }
        void OnCoin(int value, bool rare, Vector3 pos) => Report(MissionType.CollectCoins, value);
        void OnScore(int score) => Report(MissionType.ReachScore, score, absolute: true);
        void OnDistance(float metres) => Report(MissionType.TravelDistance, Mathf.FloorToInt(metres), absolute: true);
        void OnJump(Vector3 p) => Report(MissionType.Jump, 1);
        void OnRoll(Transform t) => Report(MissionType.Roll, 1);
        void OnPerfectDodge(Vector3 p) { Report(MissionType.PerfectDodges, 1); Report(MissionType.DodgeTaxis, 1); }
        void OnPowerUp(PowerUpType t) { Report(MissionType.UsePowerUp, 1); Report(MissionType.CollectPowerUps, 1); }

        /// <summary>
        /// Advances every active mission of this type. <paramref name="absolute"/>
        /// sets progress to a max-with rather than incrementing (score/distance/
        /// survive-time are "reach a value", not "accumulate").
        /// </summary>
        void Report(MissionType type, int amount, bool absolute = false)
        {
            PlayerProfile profile = SaveManager.Profile;
            bool dirty = false;

            foreach (MissionDefinition def in active)
            {
                if (def.type != type)
                {
                    continue;
                }

                int idx = profile.FindMission(def.id);
                if (idx < 0)
                {
                    continue;
                }

                MissionState state = profile.missions[idx];
                if (state.completed)
                {
                    continue;
                }

                int newProgress = absolute ? Mathf.Max(state.progress, amount) : state.progress + amount;
                newProgress = Mathf.Min(newProgress, def.targetAmount);
                if (newProgress == state.progress)
                {
                    continue;
                }

                state.progress = newProgress;
                GameEvents.RaiseMissionProgressed(def.id, newProgress, def.targetAmount);

                if (newProgress >= def.targetAmount)
                {
                    state.completed = true;
                    Award(def);
                    GameEvents.RaiseMissionCompleted(def.id);
                }

                profile.missions[idx] = state;
                dirty = true;
            }

            if (dirty)
            {
                SaveManager.Save();
            }
        }

        static void Award(MissionDefinition def)
        {
            ScoreManager.GrantCoins(def.coinReward);
            ScoreManager.GrantRareCoins(def.rareCoinReward);
        }

        // Rotate daily missions when the calendar day changes; weekly/long-term
        // are added once and persist. Duplicates are prevented by id.
        void RollDailyIfNeeded()
        {
            PlayerProfile profile = SaveManager.Profile;
            long today = System.DateTime.UtcNow.Date.Ticks;

            active.Clear();

            // Keep any non-daily missions already assigned.
            foreach (MissionDefinition def in missionPool)
            {
                if (def == null || def.duration == MissionDuration.Daily)
                {
                    continue;
                }

                EnsureAssigned(profile, def, today);
                active.Add(def);
            }

            // Are today's daily missions already assigned?
            var todaysDailies = new List<MissionDefinition>();
            foreach (MissionDefinition def in missionPool)
            {
                if (def == null || def.duration != MissionDuration.Daily)
                {
                    continue;
                }

                int idx = profile.FindMission(def.id);
                if (idx >= 0 && profile.missions[idx].assignedDayEpoch == today && !profile.missions[idx].completed)
                {
                    todaysDailies.Add(def);
                }
            }

            if (todaysDailies.Count == 0)
            {
                // Roll a fresh slate: reset daily states and pick N.
                var pool = new List<MissionDefinition>();
                foreach (MissionDefinition def in missionPool)
                {
                    if (def != null && def.duration == MissionDuration.Daily)
                    {
                        pool.Add(def);
                    }
                }

                Shuffle(pool);
                int count = Mathf.Min(dailySlots, pool.Count);
                for (int i = 0; i < count; i++)
                {
                    ResetMission(profile, pool[i], today);
                    todaysDailies.Add(pool[i]);
                }

                SaveManager.Save();
            }

            active.AddRange(todaysDailies);
        }

        static void EnsureAssigned(PlayerProfile profile, MissionDefinition def, long today)
        {
            if (profile.FindMission(def.id) < 0)
            {
                profile.missions.Add(new MissionState { id = def.id, assignedDayEpoch = today });
            }
        }

        static void ResetMission(PlayerProfile profile, MissionDefinition def, long today)
        {
            int idx = profile.FindMission(def.id);
            var state = new MissionState { id = def.id, progress = 0, completed = false, claimed = false, assignedDayEpoch = today };
            if (idx >= 0)
            {
                profile.missions[idx] = state;
            }
            else
            {
                profile.missions.Add(state);
            }
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
