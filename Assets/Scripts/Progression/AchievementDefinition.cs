using UnityEngine;

namespace JoburgRunner.Progression
{
    /// <summary>The measurable behind an achievement (read from stats/events).</summary>
    public enum AchievementStat
    {
        RunsPlayed,             // First Run, Commuter King
        LifetimePerfectDodges,  // Taxi Dodger
        LifetimeCoins,          // Coin Collector
        LifetimeUbuntuPulseUses,// Ubuntu Master
        BestScore,              // Jozi Legend
        PerfectDodgesInRun,     // Perfect Timing
        LongestDistance,        // Marathon Runner
        NightRuns,              // Night Runner
    }

    /// <summary>
    /// Inspector-authored achievement. Unlocks once when its stat reaches the
    /// threshold; <see cref="AchievementManager"/> handles tracking, one-time
    /// unlock, save, popup event, and optional reward.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Achievement Definition", fileName = "Achievement")]
    public sealed class AchievementDefinition : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public AchievementStat stat;
        [Min(1)] public int threshold = 1;

        [Header("Reward (optional)")]
        [Min(0)] public int coinReward = 0;
        [Min(0)] public int rareCoinReward = 0;

        [Header("Presentation")]
        public Sprite icon;
    }
}
