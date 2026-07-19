using JoburgRunner.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JoburgRunner
{
    /// <summary>
    /// Main-menu flow: the run only starts when the player presses PLAY.
    /// Also drives the Store (coin-funded power-up duration upgrades), the
    /// Collectables screen (coin bank, rare R5s, power-up pickups) and the
    /// ME screen (choose the playable character).
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        /// <summary>
        /// Reopens the ME page after the scene reload that applies a new
        /// character selection, so the player sees their choice take effect.
        /// </summary>
        public static bool OpenMeOnce;

        static readonly PowerUpType[] StoreOrder =
        {
            PowerUpType.TaxiMagnet,
            PowerUpType.JoziSneakers,
            PowerUpType.DroneBoost,
            PowerUpType.UbuntuMultiplier,
            PowerUpType.Hoverboard,
            PowerUpType.DoubleCoins,
            PowerUpType.UbuntuPulse,
        };

        // Special items are consumables paid for with rare R5 coins; one of
        // each owned item is applied automatically at the start of a run.
        static readonly string[] SpecialKeys =
        {
            "JoburgRunner.Item.HeadStart",
            "JoburgRunner.Item.ShieldStart",
            "JoburgRunner.Item.PulseStart",
        };

        static readonly string[] SpecialNames = { "Head Start", "Shield Start", "Pulse Start" };

        static readonly string[] SpecialDescriptions =
        {
            "Begin your next run flying the Drone",
            "Begin your next run with a Hoverboard shield",
            "Begin your next run with Ubuntu Pulse active",
        };

        static readonly int[] SpecialCosts = { 1, 2, 3 };

        static readonly PowerUpType[] SpecialGrants = { PowerUpType.DroneBoost, PowerUpType.Hoverboard, PowerUpType.UbuntuPulse };

        /// <summary>Coin price of one hoverboard booster on the Boards page.</summary>
        public const int BoardBoosterCost = 250;

        [Header("References")]
        [SerializeField] GameManager gameManager;
        [SerializeField] ScoreManager scoreManager;
        [SerializeField] PowerUpManager powerUpManager;
        [SerializeField] GameObject hudRoot;

        [Header("Panels")]
        [SerializeField] GameObject menuPanel;
        [SerializeField] GameObject storePanel;
        [SerializeField] GameObject collectablesPanel;
        [SerializeField] GameObject mePanel;
        [SerializeField] GameObject boardsPanel;
        [SerializeField] GameObject missionsPanel;

        [Header("Menu Buttons")]
        [SerializeField] Button playButton;
        [SerializeField] Button storeButton;
        [SerializeField] Button collectablesButton;
        [SerializeField] Button meButton;
        [SerializeField] Button boardsButton;
        [SerializeField] Button missionsButton;
        [SerializeField] Button soundButton;
        [SerializeField] Button storeBackButton;
        [SerializeField] Button collectablesBackButton;
        [SerializeField] Button meBackButton;
        [SerializeField] Button boardsBackButton;
        [SerializeField] Button missionsBackButton;

        [Header("Missions & Rewards")]
        [SerializeField] TextMeshProUGUI missionsText;
        [SerializeField] Button claimRewardButton;
        [SerializeField] TextMeshProUGUI claimRewardLabel;

        [Header("Character Select")]
        [SerializeField] Button[] characterSelectButtons;
        [SerializeField] TextMeshProUGUI[] characterSelectLabels;

        [Header("Boards")]
        [SerializeField] TextMeshProUGUI boardsOwnedText;
        [SerializeField] Button boardBuyButton;
        [SerializeField] Button[] boardSelectButtons;
        [SerializeField] TextMeshProUGUI[] boardSelectLabels;

        [Header("Store")]
        [SerializeField] TextMeshProUGUI storeBalanceText;
        [SerializeField] TextMeshProUGUI[] storeItemLabels;
        [SerializeField] Button[] storeUpgradeButtons;
        [SerializeField] TextMeshProUGUI[] storeUpgradeLabels;
        [SerializeField] TextMeshProUGUI[] specialItemLabels;
        [SerializeField] Button[] specialBuyButtons;
        [SerializeField] TextMeshProUGUI[] specialBuyLabels;

        [Header("Collectables")]
        [SerializeField] TextMeshProUGUI collectablesText;

        void Awake()
        {
            Bind(playButton, Play);
            Bind(storeButton, OpenStore);
            Bind(collectablesButton, OpenCollectables);
            Bind(meButton, OpenMe);
            Bind(boardsButton, OpenBoards);
            Bind(missionsButton, OpenMissions);
            Bind(soundButton, ToggleSound);
            Bind(storeBackButton, BackToMenu);
            Bind(collectablesBackButton, BackToMenu);
            Bind(meBackButton, BackToMenu);
            Bind(boardsBackButton, BackToMenu);
            Bind(missionsBackButton, BackToMenu);
            Bind(claimRewardButton, ClaimDailyReward);
            Bind(boardBuyButton, BuyBoardBooster);

            RefreshSoundButton();

            if (characterSelectButtons != null)
            {
                for (int i = 0; i < characterSelectButtons.Length; i++)
                {
                    int index = i;
                    Bind(characterSelectButtons[i], () => SelectCharacter(index));
                }
            }

            if (boardSelectButtons != null)
            {
                for (int i = 0; i < boardSelectButtons.Length; i++)
                {
                    int index = i;
                    Bind(boardSelectButtons[i], () => SelectBoard(index));
                }
            }

            if (storeUpgradeButtons != null)
            {
                for (int i = 0; i < storeUpgradeButtons.Length; i++)
                {
                    int index = i;
                    Bind(storeUpgradeButtons[i], () => Upgrade(index));
                }
            }

            if (specialBuyButtons != null)
            {
                for (int i = 0; i < specialBuyButtons.Length; i++)
                {
                    int index = i;
                    Bind(specialBuyButtons[i], () => BuySpecial(index));
                }
            }
        }

        void Start()
        {
            if (GameManager.SkipMenuOnce)
            {
                GameManager.SkipMenuOnce = false;
                Play();
                return;
            }

            if (OpenMeOnce)
            {
                OpenMeOnce = false;
                OpenMe();
                SetActive(hudRoot, false);
                return;
            }

            ShowPanel(menuPanel);
            SetActive(hudRoot, false);
        }

        public void Play()
        {
            ShowPanel(null);
            SetActive(hudRoot, true);
            if (gameManager != null)
            {
                gameManager.StartRun();
            }

            for (int i = 0; i < SpecialKeys.Length; i++)
            {
                ConsumeSpecial(i);
            }
        }

        void ConsumeSpecial(int index)
        {
            int owned = PlayerPrefs.GetInt(SpecialKeys[index], 0);
            if (owned <= 0 || powerUpManager == null)
            {
                return;
            }

            PlayerPrefs.SetInt(SpecialKeys[index], owned - 1);
            PlayerPrefs.Save();
            powerUpManager.Activate(SpecialGrants[index]);
        }

        void BuySpecial(int index)
        {
            int balance = scoreManager != null ? scoreManager.TotalRareCoins : 0;
            if (balance < SpecialCosts[index])
            {
                return;
            }

            ScoreManager.SpendRareCoins(SpecialCosts[index]);
            PlayerPrefs.SetInt(SpecialKeys[index], PlayerPrefs.GetInt(SpecialKeys[index], 0) + 1);
            PlayerPrefs.Save();
            RefreshStore();
        }

        public void OpenStore()
        {
            RefreshStore();
            ShowPanel(storePanel);
        }

        public void OpenCollectables()
        {
            RefreshCollectables();
            ShowPanel(collectablesPanel);
        }

        public void OpenMe()
        {
            RefreshMe();
            ShowPanel(mePanel);
        }

        public void OpenBoards()
        {
            RefreshBoards();
            ShowPanel(boardsPanel);
        }

        public void OpenMissions()
        {
            RefreshMissions();
            ShowPanel(missionsPanel);
        }

        void ClaimDailyReward()
        {
            if (DailyRewardManager.Instance != null && DailyRewardManager.Instance.Claim())
            {
                RefreshMissions();
            }
        }

        void RefreshMissions()
        {
            // ---- Daily reward claim button ----
            if (claimRewardButton != null)
            {
                DailyRewardManager rewards = DailyRewardManager.Instance;
                bool canClaim = rewards != null && rewards.CanClaimToday();
                claimRewardButton.interactable = canClaim;
                if (claimRewardLabel != null)
                {
                    if (rewards == null)
                    {
                        claimRewardLabel.text = "DAILY REWARD";
                    }
                    else if (canClaim)
                    {
                        DailyReward reward = rewards.CurrentReward;
                        string rare = reward.rareCoins > 0 ? $" + {reward.rareCoins} R5" : "";
                        claimRewardLabel.text = $"CLAIM DAY {rewards.CurrentDay}: {reward.coins}{rare}";
                    }
                    else
                    {
                        claimRewardLabel.text = "COME BACK TOMORROW";
                    }
                }
            }

            // ---- Missions + achievements text ----
            if (missionsText == null)
            {
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("<size=40><color=#AEB4C2>TODAY'S MISSIONS</color></size>\n");

            MissionManager missions = MissionManager.Instance;
            if (missions != null && missions.ActiveMissions.Count > 0)
            {
                PlayerProfile profile = SaveManager.Profile;
                foreach (MissionDefinition def in missions.ActiveMissions)
                {
                    int idx = profile.FindMission(def.id);
                    int progress = idx >= 0 ? profile.missions[idx].progress : 0;
                    bool done = idx >= 0 && profile.missions[idx].completed;
                    string tick = done ? "<color=#7DFFA8>✔</color> " : "";
                    string reward = def.rareCoinReward > 0 ? $"{def.coinReward}c +{def.rareCoinReward}R5" : $"{def.coinReward}c";
                    sb.Append($"\n<size=38>{tick}<b>{def.title}</b></size>\n");
                    sb.Append($"<size=30><color=#AEB4C2>{def.FormatProgress(progress)}  ·  {reward}</color></size>\n");
                }
            }
            else
            {
                sb.Append("\n<size=32><color=#AEB4C2>No active missions.</color></size>\n");
            }

            AchievementManager achievements = AchievementManager.Instance;
            if (achievements != null)
            {
                sb.Append($"\n\n<size=40><color=#AEB4C2>ACHIEVEMENTS</color></size>\n");
                sb.Append($"<size=38><color=#FFC845>{achievements.UnlockedCount}</color> / {achievements.TotalCount} unlocked</size>");
            }

            missionsText.text = sb.ToString();
        }

        void BuyBoardBooster()
        {
            int balance = scoreManager != null ? scoreManager.TotalCoins : 0;
            if (balance < BoardBoosterCost)
            {
                return;
            }

            ScoreManager.SpendCoins(BoardBoosterCost);
            BoardInventory.Add(1);
            RefreshBoards();
        }

        // Board skins swap at hoverboard-activation time (HoverboardVisual
        // reads the selection), so unlike characters no scene reload is needed.
        void SelectBoard(int index)
        {
            BoardInventory.Select(index);
            RefreshBoards();
        }

        void RefreshBoards()
        {
            if (boardsOwnedText != null)
            {
                boardsOwnedText.text =
                    $"<b>Hoverboard Boosters</b>  <color=#FFC845>Owned x{BoardInventory.OwnedCount}</color>\n" +
                    "<size=30><color=#AEB4C2>Double-tap during a run to ride · absorbs one crash</color></size>";
            }

            if (boardBuyButton != null)
            {
                boardBuyButton.interactable =
                    scoreManager == null || scoreManager.TotalCoins >= BoardBoosterCost;
            }

            if (boardSelectButtons == null)
            {
                return;
            }

            int selected = Mathf.Clamp(BoardInventory.SelectedIndex, 0, boardSelectButtons.Length - 1);
            for (int i = 0; i < boardSelectButtons.Length; i++)
            {
                if (boardSelectButtons[i] != null)
                {
                    boardSelectButtons[i].interactable = i != selected;
                }

                if (boardSelectLabels != null && i < boardSelectLabels.Length && boardSelectLabels[i] != null)
                {
                    boardSelectLabels[i].text = i == selected ? "IN USE" : "SELECT";
                }
            }
        }

        void ToggleSound()
        {
            SoundSettings.Enabled = !SoundSettings.Enabled;
            RefreshSoundButton();
        }

        void RefreshSoundButton()
        {
            if (soundButton == null)
            {
                return;
            }

            TextMeshProUGUI label = soundButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = SoundSettings.Enabled ? "SOUND: ON" : "SOUND: OFF";
            }
        }

        void SelectCharacter(int index)
        {
            if (index == CharacterSelector.SelectedIndex)
            {
                return;
            }

            CharacterSelector.Select(index);

            // Reload the scene so every script rebinds to the newly selected
            // visual (the same reset pattern the menu/restart buttons use),
            // then land back on the ME page to show the choice took effect.
            OpenMeOnce = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void RefreshMe()
        {
            int selected = CharacterSelector.SelectedIndex;
            if (characterSelectButtons == null)
            {
                return;
            }

            for (int i = 0; i < characterSelectButtons.Length; i++)
            {
                if (characterSelectButtons[i] != null)
                {
                    characterSelectButtons[i].interactable = i != selected;
                }

                if (characterSelectLabels != null && i < characterSelectLabels.Length && characterSelectLabels[i] != null)
                {
                    characterSelectLabels[i].text = i == selected ? "IN USE" : "SELECT";
                }
            }
        }

        public void BackToMenu()
        {
            ShowPanel(menuPanel);
        }

        void Upgrade(int index)
        {
            PowerUpType type = StoreOrder[index];
            int level = PowerUpManager.UpgradeLevel(type);
            int cost = PowerUpManager.UpgradeCost(type);
            int balance = scoreManager != null ? scoreManager.TotalCoins : 0;
            if (PowerUpManager.IsMaxLevel(type) || balance < cost)
            {
                return;
            }

            ScoreManager.SpendCoins(cost);
            PowerUpManager.SetUpgradeLevel(type, level + 1);
            RefreshStore();
        }

        void RefreshStore()
        {
            if (storeBalanceText != null && scoreManager != null)
            {
                storeBalanceText.text =
                    $"Coins: <color=#FFC845>{scoreManager.TotalCoins}</color>    ·    " +
                    $"R5: <color=#FFC845>{scoreManager.TotalRareCoins}</color>";
            }

            for (int i = 0; i < SpecialKeys.Length; i++)
            {
                if (specialItemLabels != null && i < specialItemLabels.Length && specialItemLabels[i] != null)
                {
                    specialItemLabels[i].text =
                        $"<b>{SpecialNames[i]}</b>  <color=#FFC845>Owned x{PlayerPrefs.GetInt(SpecialKeys[i], 0)}</color>\n" +
                        $"<size=30><color=#AEB4C2>{SpecialDescriptions[i]}</color></size>";
                }

                if (specialBuyLabels != null && i < specialBuyLabels.Length && specialBuyLabels[i] != null)
                {
                    specialBuyLabels[i].text = $"{SpecialCosts[i]} R5";
                }
            }

            for (int i = 0; i < StoreOrder.Length; i++)
            {
                PowerUpType type = StoreOrder[i];
                int level = PowerUpManager.UpgradeLevel(type);
                bool isMaxLevel = PowerUpManager.IsMaxLevel(type);
                float currentDuration = PowerUpManager.Duration(type, level);

                if (storeItemLabels != null && i < storeItemLabels.Length && storeItemLabels[i] != null)
                {
                    if (isMaxLevel)
                    {
                        storeItemLabels[i].text =
                            $"<b>{PowerUpManager.DisplayName(type)}</b>  <color=#FFC845>Lv {level}/{PowerUpManager.MaxUpgradeLevel}</color>\n" +
                            $"<size=30><color=#AEB4C2>{PowerUpManager.Description(type)} · Active {currentDuration:0}s · MAX LEVEL</color></size>";
                    }
                    else
                    {
                        float nextDuration = PowerUpManager.Duration(type, level + 1);
                        int cost = PowerUpManager.UpgradeCost(type);
                        storeItemLabels[i].text =
                            $"<b>{PowerUpManager.DisplayName(type)}</b>  <color=#FFC845>Lv {level}/{PowerUpManager.MaxUpgradeLevel}</color>\n" +
                            $"<size=30><color=#AEB4C2>Active {currentDuration:0}s -> {nextDuration:0}s · Upgrade {cost} coins</color></size>";
                    }
                }

                if (storeUpgradeLabels != null && i < storeUpgradeLabels.Length && storeUpgradeLabels[i] != null)
                {
                    storeUpgradeLabels[i].text = isMaxLevel
                        ? "MAX"
                        : $"{PowerUpManager.UpgradeCost(type)}";
                }

                if (storeUpgradeButtons != null && i < storeUpgradeButtons.Length && storeUpgradeButtons[i] != null)
                {
                    storeUpgradeButtons[i].interactable =
                        !isMaxLevel && (scoreManager == null || scoreManager.TotalCoins >= PowerUpManager.UpgradeCost(type));
                }
            }
        }

        void RefreshCollectables()
        {
            if (collectablesText == null || scoreManager == null)
            {
                return;
            }

            string pickups = "";
            foreach (PowerUpType type in StoreOrder)
            {
                pickups += $"\n{PowerUpManager.DisplayName(type)}   <color=#FFC845>x{PowerUpManager.PickupCount(type)}</color>";
            }

            // Lifetime profile stats (from the versioned JSON save).
            PlayerProfile profile = SaveManager.Profile;
            string lifetime =
                $"\nRuns   <color=#FFC845>{profile.totalRuns}</color>" +
                $"\nLongest run   <color=#FFC845>{profile.longestDistance / 1000f:0.00} km</color>" +
                $"\nLifetime coins   <color=#FFC845>{profile.lifetimeCoins}</color>" +
                $"\nJumps   <color=#FFC845>{profile.totalJumps}</color>" +
                $"\nRolls   <color=#FFC845>{profile.totalRolls}</color>" +
                $"\nPerfect dodges   <color=#FFC845>{profile.totalPerfectDodges}</color>";

            collectablesText.text =
                $"<size=44>R1 Coins banked   <color=#FFC845>{scoreManager.TotalCoins}</color></size>\n" +
                $"<size=44>R5 Rare Coins   <color=#FFC845>{scoreManager.TotalRareCoins}</color></size>\n" +
                $"<size=44>Best Score   <color=#FFC845>{scoreManager.HighScore}</color></size>\n" +
                $"\n<size=36><color=#AEB4C2>PROFILE</color></size>" +
                $"<size=36>{lifetime}</size>" +
                $"\n\n<size=36><color=#AEB4C2>POWER-UPS COLLECTED</color></size>" +
                $"<size=36>{pickups}</size>";
        }

        void ShowPanel(GameObject panel)
        {
            SetActive(menuPanel, panel == menuPanel);
            SetActive(storePanel, panel == storePanel);
            SetActive(collectablesPanel, panel == collectablesPanel);
            SetActive(mePanel, panel == mePanel);
            SetActive(boardsPanel, panel == boardsPanel);
            SetActive(missionsPanel, panel == missionsPanel);
        }

        static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }
    }
}
