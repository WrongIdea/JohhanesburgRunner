using UnityEngine;

namespace JoburgRunner.Progression
{
    /// <summary>What a mission measures. Each maps to central GameEvents.</summary>
    public enum MissionType
    {
        CollectCoins,
        ReachScore,
        TravelDistance,
        Jump,
        Roll,
        DodgeTaxis,
        PerfectDodges,
        UsePowerUp,
        SurviveDuration,   // seconds without crashing in one run
        CollectPowerUps,
        PlayRuns,
    }

    public enum MissionDuration
    {
        Daily,
        Weekly,
        LongTerm,
    }

    /// <summary>
    /// Inspector-authored mission. Pure data; <see cref="MissionManager"/>
    /// tracks progress from central events, awards the reward once, and rotates
    /// daily missions. IDs must be unique — the validation pass flags clashes.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Mission Definition", fileName = "Mission")]
    public sealed class MissionDefinition : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public MissionType type;
        [Min(1)] public int targetAmount = 1;

        [Header("Reward")]
        [Min(0)] public int coinReward = 100;
        [Min(0)] public int rareCoinReward = 0;

        [Header("Gating")]
        public MissionDuration duration = MissionDuration.Daily;
        [Min(0)] public int minPlayerLevel = 0;
        public bool hasZoneRequirement;
        public JoburgRunner.Environment.EnvironmentZoneId zoneRequirement;

        [Header("Presentation")]
        public Sprite icon;
        [Tooltip("Format string for progress, e.g. \"{0}/{1} coins\". {0}=current, {1}=target.")]
        public string progressFormat = "{0}/{1}";

        public string FormatProgress(int current) =>
            string.Format(string.IsNullOrEmpty(progressFormat) ? "{0}/{1}" : progressFormat, current, targetAmount);
    }
}
