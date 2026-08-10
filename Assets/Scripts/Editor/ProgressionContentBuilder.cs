using JoburgRunner.Audio;
using JoburgRunner.Core;
using JoburgRunner.Progression;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Generates example Priority-4 (missions, achievements, daily rewards,
    /// characters, cosmetics) and Priority-5 (audio) ScriptableObjects, plus
    /// three quality presets, from existing assets. Additive and idempotent.
    /// Clearly-named placeholders where final art/audio is missing.
    /// </summary>
    public static class ProgressionContentBuilder
    {
        const string MissionFolder = "Assets/GameplayContent/Missions";
        const string AchievementFolder = "Assets/GameplayContent/Achievements";
        const string CharacterFolder = "Assets/GameplayContent/Characters";
        const string AudioFolder = "Assets/GameplayContent/Audio";
        const string CoreFolder = "Assets/GameplayContent";

        [MenuItem("Joburg Runner/Generate Progression + Audio Content")]
        public static void Generate()
        {
            foreach (string f in new[] { CoreFolder, MissionFolder, AchievementFolder, CharacterFolder, AudioFolder })
            {
                EnsureFolder(f);
            }

            GenerateMissions();
            GenerateAchievements();
            GenerateDailyRewards();
            GenerateCharactersAndCosmetics();
            GenerateAudio();
            GenerateQuality();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated progression + audio content under Assets/GameplayContent.");
        }

        static void GenerateMissions()
        {
            Mission("daily_coins_150", "Coin Commuter", "Collect 150 coins in your runs today.",
                MissionType.CollectCoins, 150, 200, MissionDuration.Daily, "{0}/{1} coins");
            Mission("daily_distance_2000", "Cross Town", "Travel 2000 m today.",
                MissionType.TravelDistance, 2000, 250, MissionDuration.Daily, "{0}/{1} m");
            Mission("daily_dodges_10", "Taxi Weaver", "Pull off 10 perfect dodges today.",
                MissionType.PerfectDodges, 10, 300, MissionDuration.Daily, "{0}/{1} dodges");
            Mission("daily_jumps_25", "Pavement Hopper", "Jump 25 times today.",
                MissionType.Jump, 25, 150, MissionDuration.Daily, "{0}/{1} jumps");
            Mission("daily_powerups_3", "Boosted", "Use 3 power-ups today.",
                MissionType.UsePowerUp, 3, 200, MissionDuration.Daily, "{0}/{1} power-ups");
            Mission("weekly_runs_25", "Regular Rider", "Complete 25 runs this week.",
                MissionType.PlayRuns, 25, 1000, MissionDuration.Weekly, "{0}/{1} runs");
        }

        static void GenerateAchievements()
        {
            Achievement("first_run", "First Run", "Complete your very first run.", AchievementStat.RunsPlayed, 1, 100);
            Achievement("taxi_dodger", "Taxi Dodger", "Pull off 100 perfect dodges.", AchievementStat.LifetimePerfectDodges, 100, 500);
            Achievement("coin_collector", "Coin Collector", "Collect 10,000 coins in your lifetime.", AchievementStat.LifetimeCoins, 10000, 1000);
            Achievement("ubuntu_master", "Ubuntu Master", "Use Ubuntu Pulse 25 times.", AchievementStat.LifetimeUbuntuPulseUses, 25, 750);
            Achievement("jozi_legend", "Jozi Legend", "Reach a best score of 100,000.", AchievementStat.BestScore, 100000, 2000);
            Achievement("perfect_timing", "Perfect Timing", "Land 5 perfect dodges in a single run.", AchievementStat.PerfectDodgesInRun, 5, 500);
            Achievement("marathon_runner", "Marathon Runner", "Run 5 km in a single attempt.", AchievementStat.LongestDistance, 5000, 1500);
            Achievement("commuter_king", "Commuter King", "Play 250 runs.", AchievementStat.RunsPlayed, 250, 2000);
            Achievement("night_runner", "Night Runner", "Run through the Jozi night. (tracking reserved)", AchievementStat.NightRuns, 1, 300);
        }

        static void GenerateDailyRewards()
        {
            DailyRewardCycle cycle = LoadOrCreate<DailyRewardCycle>($"{CoreFolder}/DailyRewardCycle.asset");
            cycle.missedDayPolicy = MissedDayPolicy.ResetToDayOne;
            cycle.days = new[]
            {
                new DailyReward { coins = 100, label = "Day 1" },
                new DailyReward { coins = 200, label = "Day 2" },
                new DailyReward { coins = 350, label = "Day 3" },
                new DailyReward { coins = 500, rareCoins = 1, label = "Day 4" },
                new DailyReward { coins = 700, label = "Day 5" },
                new DailyReward { coins = 1000, rareCoins = 2, label = "Day 6" },
                new DailyReward { coins = 2000, rareCoins = 5, label = "Day 7" },
            };
            EditorUtility.SetDirty(cycle);
        }

        static void GenerateCharactersAndCosmetics()
        {
            CharacterDefinition mgijimi = LoadOrCreate<CharacterDefinition>($"{CharacterFolder}/Character_Mgijimi.asset");
            mgijimi.id = "mgijimi"; mgijimi.displayName = "Mgijimi"; mgijimi.description = "The original Jozi street runner.";
            mgijimi.visualIndex = 0; mgijimi.unlockType = UnlockType.OwnedByDefault;
            EditorUtility.SetDirty(mgijimi);

            CharacterDefinition jabu = LoadOrCreate<CharacterDefinition>($"{CharacterFolder}/Character_Jabu.asset");
            jabu.id = "jabu"; jabu.displayName = "Jabu"; jabu.description = "Braamfontein tech speedster.";
            jabu.visualIndex = 1; jabu.unlockType = UnlockType.CoinPurchase; jabu.coinPrice = 5000;
            EditorUtility.SetDirty(jabu);

            Cosmetic("hat_beanie", "Kasi Beanie", CosmeticSlot.Hat, "Head", 800);
            Cosmetic("trail_gold", "Gold Trail", CosmeticSlot.Trail, null, 1200, new Color(1f, 0.8f, 0.1f));
            Cosmetic("pulse_amber", "Amber Ubuntu Pulse", CosmeticSlot.UbuntuPulseColour, null, 1500, new Color(1f, 0.6f, 0.1f));
            Cosmetic("board_neon", "Neon Ion Skin", CosmeticSlot.BoardSkin, null, 2000, new Color(0.2f, 0.9f, 1f));
        }

        static void GenerateAudio()
        {
            AudioClip coin = Load<AudioClip>("Assets/Audio/CoinCollect.wav");
            AudioClip lane = Load<AudioClip>("Assets/Audio/LaneSwitch.mp3");
            AudioClip shift = Load<AudioClip>("Assets/Audio/LaneShift.mp3");
            AudioClip music = Load<AudioClip>("Assets/Audio/Remix.mp3");

            Audio("sfx_coin", AudioCategory.Player, AudioTrigger.Coin, new[] { coin }, 0.9f, 0.06f, 0.03f);
            Audio("sfx_laneswitch", AudioCategory.Player, AudioTrigger.LaneSwitch, new[] { lane }, 0.7f, 0.06f, 0.05f);
            Audio("sfx_perfectdodge", AudioCategory.Effects, AudioTrigger.PerfectDodge, new[] { shift }, 0.8f, 0.05f, 0.1f);
            Audio("sfx_powerup_start", AudioCategory.Effects, AudioTrigger.PowerUpStart, new[] { shift }, 0.8f, 0.04f, 0f);
            Audio("music_menu_PLACEHOLDER", AudioCategory.Music, AudioTrigger.None, new[] { music }, 0.5f, 0f, 0f, loop: true);
        }

        static void GenerateQuality()
        {
            Quality("Quality_Low", Core.QualityLevel.Low, 0.4f, 0.5f, false, false, 0.4f, 0.3f, 0.5f, 20f, 8);
            Quality("Quality_Medium", Core.QualityLevel.Medium, 0.7f, 0.8f, true, false, 0.7f, 0.6f, 0.8f, 35f, 12);
            Quality("Quality_High", Core.QualityLevel.High, 1f, 1f, true, true, 1f, 1f, 1f, 50f, 16);
        }

        // ---- helpers ----
        static void Mission(string id, string title, string desc, MissionType type, int target, int coins, MissionDuration dur, string fmt)
        {
            MissionDefinition m = LoadOrCreate<MissionDefinition>($"{MissionFolder}/Mission_{id}.asset");
            m.id = id; m.title = title; m.description = desc; m.type = type; m.targetAmount = target;
            m.coinReward = coins; m.duration = dur; m.progressFormat = fmt;
            EditorUtility.SetDirty(m);
        }

        static void Achievement(string id, string title, string desc, AchievementStat stat, int threshold, int coins)
        {
            AchievementDefinition a = LoadOrCreate<AchievementDefinition>($"{AchievementFolder}/Achievement_{id}.asset");
            a.id = id; a.title = title; a.description = desc; a.stat = stat; a.threshold = threshold; a.coinReward = coins;
            EditorUtility.SetDirty(a);
        }

        static void Cosmetic(string id, string name, CosmeticSlot slot, string anchor, int price, Color tint = default)
        {
            CosmeticDefinition c = LoadOrCreate<CosmeticDefinition>($"{CharacterFolder}/Cosmetic_{id}.asset");
            c.id = id; c.displayName = name; c.slot = slot; c.anchorName = anchor;
            c.unlockType = UnlockType.CoinPurchase; c.coinPrice = price;
            c.tint = tint == default ? Color.white : tint;
            EditorUtility.SetDirty(c);
        }

        static void Audio(string id, AudioCategory cat, AudioTrigger trig, AudioClip[] clips, float vol, float pitchJitter, float minInterval, bool loop = false)
        {
            AudioDefinition a = LoadOrCreate<AudioDefinition>($"{AudioFolder}/Audio_{id}.asset");
            a.id = id; a.category = cat; a.trigger = trig; a.clips = clips; a.volume = vol;
            a.pitchRange = new Vector2(1f - pitchJitter, 1f + pitchJitter); a.minInterval = minInterval; a.loop = loop;
            EditorUtility.SetDirty(a);
        }

        static void Quality(string name, Core.QualityLevel level, float particles, float trails, bool camFeedback, bool post,
            float prop, float ped, float veh, float shadow, int voices)
        {
            GameQualitySettings q = LoadOrCreate<GameQualitySettings>($"{CoreFolder}/{name}.asset");
            q.level = level; q.particleCountScale = particles; q.trailDurationScale = trails;
            q.cameraFeedback = camFeedback; q.postProcessing = post;
            q.propDensity = prop; q.pedestrianDensity = ped; q.vehicleDensity = veh;
            q.shadowDistance = shadow; q.audioVoiceLimit = voices;
            EditorUtility.SetDirty(q);
        }

        static T Load<T>(string path) where T : Object
        {
            T a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) Debug.LogWarning($"Progression content: asset not found at {path}.");
            return a;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
                AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
            }
        }
    }
}
