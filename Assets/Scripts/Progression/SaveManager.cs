using System;
using System.IO;
using UnityEngine;

namespace JoburgRunner.Progression
{
    /// <summary>
    /// Owns the JSON player profile: load, versioned migration, and atomic
    /// save. Loads lazily on first access and caches, so systems just read
    /// <see cref="Profile"/> and call <see cref="Save"/> after mutating it.
    /// Writes via a temp file + replace so a crash mid-write can't corrupt the
    /// save. Small settings and the coin economy deliberately stay in
    /// PlayerPrefs; this is only for structured data.
    /// </summary>
    public static class SaveManager
    {
        const string FileName = "player_profile.json";

        static PlayerProfile cached;

        static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static PlayerProfile Profile
        {
            get
            {
                if (cached == null)
                {
                    cached = Load();
                }

                return cached;
            }
        }

        public static PlayerProfile Load()
        {
            try
            {
                if (File.Exists(Path))
                {
                    string json = File.ReadAllText(Path);
                    PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(json);
                    if (profile != null)
                    {
                        Migrate(profile);
                        cached = profile;
                        return profile;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: failed to load profile ({e.Message}); starting fresh.");
            }

            cached = new PlayerProfile();
            return cached;
        }

        public static void Save()
        {
            if (cached == null)
            {
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(cached, prettyPrint: true);
                string temp = Path + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }

                File.Move(temp, Path);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: failed to save profile: {e.Message}");
            }
        }

        /// <summary>Wipes the profile (debug / "reset progression").</summary>
        public static void ResetAll()
        {
            cached = new PlayerProfile();
            Save();
        }

        // Applies field defaults / field migrations for older save versions.
        // JsonUtility already fills missing fields with defaults, so most new
        // fields need nothing here; this is the hook for real data reshaping.
        static void Migrate(PlayerProfile profile)
        {
            if (profile.version < PlayerProfile.CurrentVersion)
            {
                // (no destructive migrations yet — bump handles field additions)
                profile.version = PlayerProfile.CurrentVersion;
            }
        }
    }
}
