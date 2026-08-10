using UnityEngine;

namespace JoburgRunner.Progression
{
    /// <summary>
    /// Accumulates lifetime player statistics into the saved profile from
    /// central events, and exposes the current run's live counters for
    /// achievements/UI. Saves at run end (not per event) to avoid disk churn.
    /// Disable-safe: unsubscribes cleanly and can be removed with no effect on
    /// gameplay.
    /// </summary>
    public sealed class StatsRecorder : MonoBehaviour
    {
        public static StatsRecorder Instance { get; private set; }

        public int RunPerfectDodges { get; private set; }
        public int RunCoins { get; private set; }
        public float RunDistance { get; private set; }
        public int RunScore { get; private set; }

        void Awake() => Instance = this;

        void OnEnable()
        {
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.RunEnded += OnRunEnded;
            GameEvents.PlayerJumped += OnJump;
            GameEvents.PlayerRolled += OnRoll;
            GameEvents.PerfectDodge += OnPerfectDodge;
            GameEvents.CoinCollected += OnCoin;
            GameEvents.DistanceChanged += OnDistance;
            GameEvents.ScoreChanged += OnScore;
        }

        void OnDisable()
        {
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.RunEnded -= OnRunEnded;
            GameEvents.PlayerJumped -= OnJump;
            GameEvents.PlayerRolled -= OnRoll;
            GameEvents.PerfectDodge -= OnPerfectDodge;
            GameEvents.CoinCollected -= OnCoin;
            GameEvents.DistanceChanged -= OnDistance;
            GameEvents.ScoreChanged -= OnScore;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void OnRunStarted()
        {
            RunPerfectDodges = 0;
            RunCoins = 0;
            RunDistance = 0f;
            RunScore = 0;
            PlayerProfile profile = SaveManager.Profile;
            profile.totalRuns++;
        }

        void OnRunEnded(RunSummary s)
        {
            PlayerProfile profile = SaveManager.Profile;
            profile.lifetimeCoins += s.coins;
            if (s.distance > profile.longestDistance)
            {
                profile.longestDistance = s.distance;
            }

            SaveManager.Save();
        }

        void OnJump(Vector3 p) => SaveManager.Profile.totalJumps++;
        void OnRoll(Transform t) => SaveManager.Profile.totalRolls++;
        void OnCoin(int value, bool rare, Vector3 pos) => RunCoins += value;
        void OnDistance(float m) => RunDistance = m;
        void OnScore(int s) => RunScore = s;

        void OnPerfectDodge(Vector3 p)
        {
            RunPerfectDodges++;
            PlayerProfile profile = SaveManager.Profile;
            profile.totalPerfectDodges++;
            profile.obstaclesAvoided++;
        }
    }
}
