using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Progression
{
    /// <summary>
    /// Evaluates achievements against lifetime/run stats after relevant events,
    /// unlocking each exactly once, banking any reward, raising the unlock
    /// event (for a popup), and saving. Re-checking all definitions per event
    /// is cheap at these counts and keeps the logic declarative.
    /// </summary>
    public sealed class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [SerializeField] List<AchievementDefinition> achievements = new List<AchievementDefinition>();

        public int UnlockedCount => SaveManager.Profile.unlockedAchievements.Count;
        public int TotalCount => achievements != null ? achievements.Count : 0;

        void Awake() => Instance = this;

        void OnEnable()
        {
            GameEvents.RunEnded += OnRunEnded;
            GameEvents.PerfectDodge += OnAny3;
            GameEvents.CoinCollected += OnAny2;
            GameEvents.PowerUpStarted += OnPowerUp;
            GameEvents.ScoreChanged += OnScore;
            Evaluate();
        }

        void OnDisable()
        {
            GameEvents.RunEnded -= OnRunEnded;
            GameEvents.PerfectDodge -= OnAny3;
            GameEvents.CoinCollected -= OnAny2;
            GameEvents.PowerUpStarted -= OnPowerUp;
            GameEvents.ScoreChanged -= OnScore;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void OnRunEnded(RunSummary s) => Evaluate();
        void OnAny3(Vector3 p) => Evaluate();
        void OnAny2(int a, bool b, Vector3 c) => Evaluate();
        void OnPowerUp(PowerUpType t) => Evaluate();
        void OnScore(int s) => Evaluate();

        void Evaluate()
        {
            PlayerProfile profile = SaveManager.Profile;
            bool dirty = false;

            foreach (AchievementDefinition def in achievements)
            {
                if (def == null || string.IsNullOrEmpty(def.id) || profile.HasAchievement(def.id))
                {
                    continue;
                }

                if (CurrentValue(def.stat, profile) >= def.threshold)
                {
                    profile.unlockedAchievements.Add(def.id);
                    ScoreManager.GrantCoins(def.coinReward);
                    ScoreManager.GrantRareCoins(def.rareCoinReward);
                    GameEvents.RaiseAchievementUnlocked(def.id);
                    dirty = true;
                }
            }

            if (dirty)
            {
                SaveManager.Save();
            }
        }

        static int CurrentValue(AchievementStat stat, PlayerProfile profile) => stat switch
        {
            AchievementStat.RunsPlayed => profile.totalRuns,
            AchievementStat.LifetimePerfectDodges => profile.totalPerfectDodges,
            AchievementStat.LifetimeCoins => (int)Mathf.Min(profile.lifetimeCoins, int.MaxValue),
            AchievementStat.LifetimeUbuntuPulseUses => PowerUpManager.PickupCount(PowerUpType.UbuntuPulse),
            AchievementStat.BestScore => ScoreManager.BestScore,
            AchievementStat.PerfectDodgesInRun => StatsRecorder.Instance != null ? StatsRecorder.Instance.RunPerfectDodges : 0,
            AchievementStat.LongestDistance => Mathf.FloorToInt(profile.longestDistance),
            _ => 0, // NightRuns: reserved (not yet tracked) — placeholder
        };
    }
}
