using System;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>Immutable snapshot of a finished run, passed with RunEnded.</summary>
    public struct RunSummary
    {
        public int score;
        public float distance;
        public int coins;
        public int rareCoins;
    }

    /// <summary>
    /// The single central gameplay event hub. Systems raise and observe moments
    /// here instead of referencing each other directly, so gameplay, VFX,
    /// audio, missions, achievements and analytics stay fully decoupled.
    ///
    /// Contract:
    ///  - subscribe in OnEnable, unsubscribe in OnDisable (never leak a
    ///    subscription past an object's lifetime);
    ///  - all delegates are cleared on subsystem registration so a domain
    ///    reload in the editor can't carry stale subscribers into play mode.
    ///
    /// Raisers are null-safe no-ops when nothing is listening, so gameplay can
    /// raise events unconditionally with zero cost when a system is disabled.
    /// </summary>
    public static class GameEvents
    {
        // ---- Run lifecycle ----
        public static event Action RunStarted;
        public static event Action<RunSummary> RunEnded;
        public static event Action PlayerCrashed;

        // ---- Movement ----
        public static event Action<Vector3> PlayerJumped;
        public static event Action<Vector3> PlayerLanded;
        public static event Action<Transform> PlayerRolled;
        public static event Action<int> LaneChanged;        // direction -1 / +1

        // ---- Pickups & scoring ----
        public static event Action<int, bool, Vector3> CoinCollected; // value, rare, position
        public static event Action<float> DistanceChanged;  // metres this run
        public static event Action<int> ScoreChanged;

        // ---- Obstacles ----
        public static event Action ObstaclePassed;
        public static event Action<Vector3> PerfectDodge;

        // ---- Power-ups ----
        public static event Action<PowerUpType> PowerUpStarted;
        public static event Action<PowerUpType> PowerUpWarning;
        public static event Action<PowerUpType> PowerUpEnded;

        // ---- Progression ----
        public static event Action<string, int, int> MissionProgressed; // id, current, target
        public static event Action<string> MissionCompleted;
        public static event Action<string> AchievementUnlocked;
        public static event Action<int> RewardClaimed;      // cycle day 1..7
        public static event Action<int> ZoneChanged;        // EnvironmentZoneId as int

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            RunStarted = null; RunEnded = null; PlayerCrashed = null;
            PlayerJumped = null; PlayerLanded = null; PlayerRolled = null; LaneChanged = null;
            CoinCollected = null; DistanceChanged = null; ScoreChanged = null;
            ObstaclePassed = null; PerfectDodge = null;
            PowerUpStarted = null; PowerUpWarning = null; PowerUpEnded = null;
            MissionProgressed = null; MissionCompleted = null; AchievementUnlocked = null;
            RewardClaimed = null; ZoneChanged = null;
        }

        public static void RaiseRunStarted() => RunStarted?.Invoke();
        public static void RaiseRunEnded(RunSummary s) => RunEnded?.Invoke(s);
        public static void RaisePlayerCrashed() => PlayerCrashed?.Invoke();
        public static void RaisePlayerJumped(Vector3 p) => PlayerJumped?.Invoke(p);
        public static void RaisePlayerLanded(Vector3 p) => PlayerLanded?.Invoke(p);
        public static void RaisePlayerRolled(Transform t) => PlayerRolled?.Invoke(t);
        public static void RaiseLaneChanged(int dir) => LaneChanged?.Invoke(dir);
        public static void RaiseCoinCollected(int value, bool rare, Vector3 p) => CoinCollected?.Invoke(value, rare, p);
        public static void RaiseDistanceChanged(float m) => DistanceChanged?.Invoke(m);
        public static void RaiseScoreChanged(int s) => ScoreChanged?.Invoke(s);
        public static void RaiseObstaclePassed() => ObstaclePassed?.Invoke();
        public static void RaisePerfectDodge(Vector3 p) => PerfectDodge?.Invoke(p);
        public static void RaisePowerUpStarted(PowerUpType t) => PowerUpStarted?.Invoke(t);
        public static void RaisePowerUpWarning(PowerUpType t) => PowerUpWarning?.Invoke(t);
        public static void RaisePowerUpEnded(PowerUpType t) => PowerUpEnded?.Invoke(t);
        public static void RaiseMissionProgressed(string id, int cur, int target) => MissionProgressed?.Invoke(id, cur, target);
        public static void RaiseMissionCompleted(string id) => MissionCompleted?.Invoke(id);
        public static void RaiseAchievementUnlocked(string id) => AchievementUnlocked?.Invoke(id);
        public static void RaiseRewardClaimed(int day) => RewardClaimed?.Invoke(day);
        public static void RaiseZoneChanged(int zoneId) => ZoneChanged?.Invoke(zoneId);
    }
}
