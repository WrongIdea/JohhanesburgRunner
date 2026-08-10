using UnityEngine;

namespace JoburgRunner.Progression
{
    /// <summary>Reward for one day of the cycle.</summary>
    [System.Serializable]
    public struct DailyReward
    {
        public int coins;
        public int rareCoins;
        public Sprite icon;
        public string label;
    }

    /// <summary>How a missed day is handled.</summary>
    public enum MissedDayPolicy
    {
        ResetToDayOne,   // break the streak
        KeepStreak,      // resume where they left off
    }

    /// <summary>
    /// Configurable seven-day daily-reward cycle. Pure data + date-safe claim
    /// logic. Uses local UTC date via <see cref="TimeProvider"/>, which is a
    /// single seam to swap for authoritative server time later — the spec's
    /// "do not trust device time for real-money rewards" caveat lives here.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Daily Reward Cycle", fileName = "DailyRewardCycle")]
    public sealed class DailyRewardCycle : ScriptableObject
    {
        [Tooltip("Seven days of rewards; index 0 = day 1.")]
        public DailyReward[] days = new DailyReward[7];
        public MissedDayPolicy missedDayPolicy = MissedDayPolicy.ResetToDayOne;

        public int CycleLength => days != null && days.Length > 0 ? days.Length : 7;

        public DailyReward RewardForDay(int day) // day is 1-based
        {
            int idx = Mathf.Clamp(day - 1, 0, CycleLength - 1);
            return days[idx];
        }
    }

    /// <summary>
    /// Swappable clock. Defaults to device UTC; replace <see cref="NowUtc"/>
    /// with a server call before shipping real-money rewards.
    /// </summary>
    public static class TimeProvider
    {
        public static System.Func<System.DateTime> NowUtc = () => System.DateTime.UtcNow;
        public static System.DateTime Today => NowUtc().Date;
    }

    /// <summary>
    /// Drives the daily-reward cycle against the saved profile: one claim per
    /// calendar day, streak advance, and missed-day handling. Grants coins and
    /// raises <see cref="GameEvents.RewardClaimed"/>. UI reads the claimable
    /// state and upcoming rewards from here.
    /// </summary>
    public sealed class DailyRewardManager : MonoBehaviour
    {
        public static DailyRewardManager Instance { get; private set; }

        [SerializeField] DailyRewardCycle cycle;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnEnable() => ApplyMissedDayPolicy();

        /// <summary>The day (1-based) the player would claim next.</summary>
        public int CurrentDay => Mathf.Clamp(SaveManager.Profile.dailyReward.streakDay, 1, Cycle().CycleLength);

        public DailyReward CurrentReward => Cycle().RewardForDay(CurrentDay);

        public DailyReward UpcomingReward(int offset) =>
            Cycle().RewardForDay(((CurrentDay - 1 + offset) % Cycle().CycleLength) + 1);

        public bool CanClaimToday()
        {
            if (cycle == null)
            {
                return false;
            }

            long today = TimeProvider.Today.Ticks;
            return SaveManager.Profile.dailyReward.lastClaimDayEpoch != today;
        }

        /// <summary>Claims today's reward. Returns false if already claimed today.</summary>
        public bool Claim()
        {
            if (!CanClaimToday())
            {
                return false;
            }

            PlayerProfile profile = SaveManager.Profile;
            int day = CurrentDay;
            DailyReward reward = Cycle().RewardForDay(day);

            ScoreManager.GrantCoins(reward.coins);
            ScoreManager.GrantRareCoins(reward.rareCoins);

            DailyRewardState state = profile.dailyReward;
            state.lastClaimDayEpoch = TimeProvider.Today.Ticks;
            state.lastClaimIso = TimeProvider.NowUtc().ToString("o");
            state.streakDay = day >= Cycle().CycleLength ? 1 : day + 1;
            profile.dailyReward = state;

            SaveManager.Save();
            GameEvents.RaiseRewardClaimed(day);
            return true;
        }

        // If the player skipped one or more days, reset or keep the streak
        // per policy. Only relevant when they didn't claim yesterday.
        void ApplyMissedDayPolicy()
        {
            if (cycle == null)
            {
                return;
            }

            PlayerProfile profile = SaveManager.Profile;
            if (profile.dailyReward.lastClaimDayEpoch == 0)
            {
                return; // never claimed
            }

            System.DateTime lastClaim = new System.DateTime(profile.dailyReward.lastClaimDayEpoch);
            int daysSince = (TimeProvider.Today - lastClaim.Date).Days;
            if (daysSince > 1 && cycle.missedDayPolicy == MissedDayPolicy.ResetToDayOne)
            {
                DailyRewardState state = profile.dailyReward;
                state.streakDay = 1;
                profile.dailyReward = state;
                SaveManager.Save();
            }
        }

        DailyRewardCycle Cycle() => cycle;
    }
}
