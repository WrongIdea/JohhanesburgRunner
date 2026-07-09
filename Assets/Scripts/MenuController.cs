using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoburgRunner
{
    /// <summary>
    /// Main-menu flow: the run only starts when the player presses PLAY.
    /// Also drives the Store (coin-funded power-up duration upgrades) and
    /// the Collectables screen (coin bank, rare R5s, power-up pickups).
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        static readonly PowerUpType[] StoreOrder =
        {
            PowerUpType.TaxiMagnet,
            PowerUpType.JoziSneakers,
            PowerUpType.DroneBoost,
            PowerUpType.UbuntuMultiplier,
            PowerUpType.Hoverboard,
        };

        // Special items are consumables paid for with rare R5 coins; one of
        // each owned item is applied automatically at the start of a run.
        static readonly string[] SpecialKeys =
        {
            "JoburgRunner.Item.HeadStart",
            "JoburgRunner.Item.ShieldStart",
        };

        static readonly string[] SpecialNames = { "Head Start", "Shield Start" };

        static readonly string[] SpecialDescriptions =
        {
            "Begin your next run flying the Drone",
            "Begin your next run with a Hoverboard shield",
        };

        static readonly int[] SpecialCosts = { 1, 2 };

        static readonly PowerUpType[] SpecialGrants = { PowerUpType.DroneBoost, PowerUpType.Hoverboard };

        [Header("References")]
        [SerializeField] GameManager gameManager;
        [SerializeField] ScoreManager scoreManager;
        [SerializeField] PowerUpManager powerUpManager;
        [SerializeField] GameObject hudRoot;

        [Header("Panels")]
        [SerializeField] GameObject menuPanel;
        [SerializeField] GameObject storePanel;
        [SerializeField] GameObject collectablesPanel;

        [Header("Menu Buttons")]
        [SerializeField] Button playButton;
        [SerializeField] Button storeButton;
        [SerializeField] Button collectablesButton;
        [SerializeField] Button storeBackButton;
        [SerializeField] Button collectablesBackButton;

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
            Bind(storeBackButton, BackToMenu);
            Bind(collectablesBackButton, BackToMenu);

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

            collectablesText.text =
                $"<size=44>R1 Coins banked   <color=#FFC845>{scoreManager.TotalCoins}</color></size>\n" +
                $"<size=44>R5 Rare Coins   <color=#FFC845>{scoreManager.TotalRareCoins}</color></size>\n" +
                $"<size=44>Best Score   <color=#FFC845>{scoreManager.HighScore}</color></size>\n" +
                $"\n<size=36><color=#AEB4C2>POWER-UPS COLLECTED</color></size>" +
                $"<size=38>{pickups}</size>";
        }

        void ShowPanel(GameObject panel)
        {
            SetActive(menuPanel, panel == menuPanel);
            SetActive(storePanel, panel == storePanel);
            SetActive(collectablesPanel, panel == collectablesPanel);
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
