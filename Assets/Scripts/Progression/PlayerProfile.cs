using System;
using System.Collections.Generic;

namespace JoburgRunner.Progression
{
    /// <summary>Per-mission saved state.</summary>
    [Serializable]
    public struct MissionState
    {
        public string id;
        public int progress;
        public bool completed;
        public bool claimed;
        public long assignedDayEpoch; // day this mission was rolled onto the slate
    }

    /// <summary>Seven-day reward cycle state.</summary>
    [Serializable]
    public struct DailyRewardState
    {
        public int streakDay;        // 1..7, next reward to claim
        public long lastClaimDayEpoch;
        public string lastClaimIso;  // for auditing / future server reconciliation
    }

    /// <summary>One equipped cosmetic per slot.</summary>
    [Serializable]
    public struct EquippedCosmetic
    {
        public string slot;   // CosmeticSlot name
        public string id;     // CosmeticDefinition id
    }

    /// <summary>
    /// The structured, versioned player save. Serialised to JSON on disk (not
    /// PlayerPrefs — this is large structured data). Small scalar settings and
    /// the working coin/high-score economy stay in their existing PlayerPrefs
    /// keys; this profile owns everything new (lifetime stats, missions,
    /// achievements, daily rewards, unlocks, cosmetics).
    ///
    /// Forward-compatible: JsonUtility leaves unknown fields untouched and
    /// fills missing fields with defaults, and <see cref="SaveManager"/> runs
    /// <c>version</c>-based migrations, so older saves load safely.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;

        // ---- Lifetime stats ----
        public int totalRuns;
        public long lifetimeCoins;
        public float longestDistance;
        public int totalJumps;
        public int totalRolls;
        public int totalPerfectDodges;
        public int obstaclesAvoided;

        // ---- Missions & achievements ----
        public List<MissionState> missions = new List<MissionState>();
        public List<string> unlockedAchievements = new List<string>();

        // ---- Daily reward ----
        public DailyRewardState dailyReward = new DailyRewardState { streakDay = 1 };

        // ---- Unlocks ----
        public List<string> unlockedZones = new List<string>();
        public List<string> ownedCharacters = new List<string>();
        public string selectedCharacter = "";
        public List<string> ownedCosmetics = new List<string>();
        public List<EquippedCosmetic> equippedCosmetics = new List<EquippedCosmetic>();

        // ---- Settings mirror (authoritative store stays SoundSettings/PlayerPrefs) ----
        public bool musicEnabled = true;
        public bool sfxEnabled = true;

        public bool HasAchievement(string id) => unlockedAchievements.Contains(id);
        public bool OwnsCharacter(string id) => ownedCharacters.Contains(id);
        public bool OwnsCosmetic(string id) => ownedCosmetics.Contains(id);
        public bool ZoneUnlocked(string id) => unlockedZones.Contains(id);

        public int FindMission(string id) => missions.FindIndex(m => m.id == id);
    }
}
