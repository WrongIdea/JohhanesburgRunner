using TMPro;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Tracks active power-up effects and applies the ambient ones:
    /// the Taxi Magnet pulls nearby coins to the player, the Ubuntu
    /// Multiplier doubles score gain, Jozi Sneakers raise jumps, the
    /// Drone Boost flies the player over traffic, and the Hoverboard
    /// absorbs one crash. Store upgrades extend each effect's duration.
    /// </summary>
    public class PowerUpManager : MonoBehaviour
    {
        public const int MaxUpgradeLevel = BoosterUpgradeConfig.MaxLevel;

        [Header("References")]
        [SerializeField] Transform player;
        [SerializeField] GameManager gameManager;
        [SerializeField] TextMeshProUGUI statusText;

        [Header("Effect Tuning")]
        [SerializeField] float magnetRadius = 7f;
        [SerializeField] float magnetPullSpeed = 18f;
        [Tooltip("Ubuntu Pulse's own coin-attraction radius, separate from Taxi Magnet's.")]
        [SerializeField] float ubuntuPulseMagnetRadius = 8f;
        [SerializeField] float ubuntuPulseCoinFlySpeed = 16f;
        [SerializeField] AudioClip coinAttractionClip;
        [SerializeField] float coinAttractionVolume = 0.5f;
        [SerializeField] float sneakersJumpMultiplier = 1.55f;
        [Tooltip("How far below the airborne player's feet the sneakers still scoop coins.")]
        [SerializeField] float sneakerScoopDepth = 3.2f;
        [SerializeField] float sneakerScoopHalfWidth = 1.1f;
        [SerializeField] float sneakerScoopPullSpeed = 22f;
        // High enough to clear taxi roofs (~2.3m) with visible daylight —
        // at 3.6 the flying runner visually skimmed and clipped van roofs.
        [SerializeField] float droneFlightHeight = 4.2f;
        [SerializeField] float droneCoinLiftSpeed = 12f;
        [SerializeField] int ubuntuScoreMultiplier = 2;
        [Tooltip("Seconds of remaining time at which a power-up warning event fires.")]
        [SerializeField] float powerUpWarningSeconds = 2f;

        PlayerController playerController;

        static readonly PowerUpType[] AllTypes =
        {
            PowerUpType.TaxiMagnet,
            PowerUpType.JoziSneakers,
            PowerUpType.DroneBoost,
            PowerUpType.UbuntuMultiplier,
            PowerUpType.Hoverboard,
            PowerUpType.DoubleCoins,
            PowerUpType.UbuntuPulse,
        };

        readonly float[] remaining = new float[AllTypes.Length];
        readonly bool[] warned = new bool[AllTypes.Length];

        public float JumpMultiplier => IsActive(PowerUpType.JoziSneakers) ? sneakersJumpMultiplier : 1f;
        public int ScoreMultiplier => IsActive(PowerUpType.UbuntuMultiplier) ? ubuntuScoreMultiplier : 1;
        public int CoinMultiplier => IsActive(PowerUpType.DoubleCoins) ? 2 : 1;
        public bool DroneActive => IsActive(PowerUpType.DroneBoost);
        public float DroneFlightHeight => droneFlightHeight;
        public bool HasShield => IsActive(PowerUpType.Hoverboard);
        public bool UbuntuPulseActive => IsActive(PowerUpType.UbuntuPulse);

        public static string DisplayName(PowerUpType type) => type switch
        {
            PowerUpType.TaxiMagnet => "Taxi Magnet",
            PowerUpType.JoziSneakers => "Jozi Sneakers",
            PowerUpType.DroneBoost => "Drone Boost",
            PowerUpType.UbuntuMultiplier => "Ubuntu Multiplier",
            PowerUpType.DoubleCoins => "Double Coins",
            PowerUpType.UbuntuPulse => "Ubuntu Pulse",
            _ => "Hoverboard",
        };

        public static string Description(PowerUpType type) => type switch
        {
            PowerUpType.TaxiMagnet => "Attracts nearby coins",
            PowerUpType.JoziSneakers => "Higher jumps that scoop coins",
            PowerUpType.DroneBoost => "Temporary flight",
            PowerUpType.UbuntuMultiplier => "Doubles your score",
            PowerUpType.DoubleCoins => "Every coin counts twice",
            PowerUpType.UbuntuPulse => "Coin magnet + one-hit shield",
            _ => "Survives one crash",
        };

        static string UpgradeLevelKey(PowerUpType type) => $"JoburgRunner.PowerLevel.{type}";
        static string PickupCountKey(PowerUpType type) => $"JoburgRunner.Pickups.{type}";

        public static int UpgradeLevel(PowerUpType type) =>
            Mathf.Clamp(
                PlayerPrefs.GetInt(UpgradeLevelKey(type), BoosterUpgradeConfig.MinLevel),
                BoosterUpgradeConfig.MinLevel,
                MaxUpgradeLevel);

        public static void SetUpgradeLevel(PowerUpType type, int level)
        {
            PlayerPrefs.SetInt(
                UpgradeLevelKey(type),
                Mathf.Clamp(level, BoosterUpgradeConfig.MinLevel, MaxUpgradeLevel));
            PlayerPrefs.Save();
        }

        public static int PickupCount(PowerUpType type) => PlayerPrefs.GetInt(PickupCountKey(type), 0);

        public static float Duration(PowerUpType type) =>
            BoosterUpgradeConfig.Duration(type, UpgradeLevel(type));

        public static float Duration(PowerUpType type, int level) =>
            BoosterUpgradeConfig.Duration(type, level);

        public static int UpgradeCost(PowerUpType type) =>
            BoosterUpgradeConfig.UpgradeCost(type, UpgradeLevel(type));

        public static bool IsMaxLevel(PowerUpType type) =>
            BoosterUpgradeConfig.IsMaxLevel(UpgradeLevel(type));

        public bool IsActive(PowerUpType type) => remaining[(int)type] > 0f;

        public float TimeRemaining(PowerUpType type) => Mathf.Max(0f, remaining[(int)type]);

        public void Activate(PowerUpType type)
        {
            remaining[(int)type] = Duration(type);
            warned[(int)type] = false;
            PlayerPrefs.SetInt(PickupCountKey(type), PickupCount(type) + 1);
            UpdateStatusText();
            Coin.SetDoubleStackVisible(CoinMultiplier > 1);
            GameEvents.RaisePowerUpStarted(type);

            if (type == PowerUpType.UbuntuPulse && coinAttractionClip != null && player != null)
            {
                AudioSource.PlayClipAtPoint(coinAttractionClip, player.position, coinAttractionVolume);
            }
        }

        /// <summary>Spends the hoverboard shield. Returns true if a crash was absorbed.</summary>
        public bool TryConsumeShield()
        {
            if (!HasShield)
            {
                return false;
            }

            remaining[(int)PowerUpType.Hoverboard] = 0f;
            UpdateStatusText();
            GameEvents.RaisePowerUpEnded(PowerUpType.Hoverboard);
            return true;
        }

        /// <summary>
        /// Spends the Ubuntu Pulse shield to absorb one obstacle hit —
        /// taxis included, same as the Hoverboard shield.
        /// </summary>
        public bool TryConsumeUbuntuShield()
        {
            if (!UbuntuPulseActive)
            {
                return false;
            }

            remaining[(int)PowerUpType.UbuntuPulse] = 0f;
            UpdateStatusText();
            GameEvents.RaisePowerUpEnded(PowerUpType.UbuntuPulse);
            return true;
        }

        void Update()
        {
            // Keep the stacked-coin visual in sync even while paused or on the
            // menu, so it can't linger after a restart clears the timers.
            Coin.SetDoubleStackVisible(CoinMultiplier > 1);

            if (gameManager != null && !gameManager.IsRunning)
            {
                return;
            }

            bool anyActive = false;
            for (int i = 0; i < remaining.Length; i++)
            {
                if (remaining[i] > 0f)
                {
                    remaining[i] -= Time.deltaTime;
                    anyActive = true;

                    if (!warned[i] && remaining[i] <= powerUpWarningSeconds && remaining[i] > 0f)
                    {
                        warned[i] = true;
                        GameEvents.RaisePowerUpWarning((PowerUpType)i);
                    }

                    if (remaining[i] <= 0f)
                    {
                        GameEvents.RaisePowerUpEnded((PowerUpType)i);
                    }
                }
            }

            if (IsActive(PowerUpType.TaxiMagnet))
            {
                PullCoinsToward(magnetRadius, magnetPullSpeed);
            }

            if (UbuntuPulseActive)
            {
                PullCoinsToward(ubuntuPulseMagnetRadius, ubuntuPulseCoinFlySpeed);
            }

            if (DroneActive)
            {
                LiftCoinsForFlight();
            }

            if (IsActive(PowerUpType.JoziSneakers))
            {
                ScoopJumpedCoins();
            }

            if (anyActive || (statusText != null && statusText.text.Length > 0))
            {
                UpdateStatusText();
            }
        }

        // Shared by Taxi Magnet and Ubuntu Pulse: coins inside radius fly
        // smoothly toward the player rather than teleporting, so normal coin
        // collection (and its score/sound) still fires when they arrive.
        void PullCoinsToward(float radius, float pullSpeed)
        {
            if (player == null)
            {
                return;
            }

            Vector3 target = player.position + Vector3.up * 1f;
            foreach (Coin coin in Coin.ActiveCoins)
            {
                Vector3 offset = target - coin.transform.position;
                if (offset.sqrMagnitude < radius * radius)
                {
                    coin.transform.position += offset.normalized *
                        Mathf.Min(pullSpeed * Time.deltaTime, offset.magnitude);
                }
            }
        }

        // While the drone flies, raise the coins the player can still reach
        // before the boost runs out up to cruising height so they stay
        // collectable; coins beyond that range keep their ground trail.
        void LiftCoinsForFlight()
        {
            if (player == null)
            {
                return;
            }

            if (playerController == null)
            {
                playerController = player.GetComponent<PlayerController>();
            }

            float forwardSpeed = playerController != null ? playerController.CurrentForwardSpeed : 12f;
            float reachZ = player.position.z + forwardSpeed * TimeRemaining(PowerUpType.DroneBoost);

            foreach (Coin coin in Coin.ActiveCoins)
            {
                Vector3 position = coin.transform.position;
                if (position.z > player.position.z - 1f && position.z < reachZ)
                {
                    position.y = Mathf.MoveTowards(position.y, droneFlightHeight, droneCoinLiftSpeed * Time.deltaTime);
                    coin.transform.position = position;
                }
            }
        }

        // Jozi Sneakers: the boosted jump sails clean over whole coin lines,
        // so the sneakers scoop up coins passing under the player's feet
        // instead of letting the reward drift by beneath them.
        void ScoopJumpedCoins()
        {
            if (player == null)
            {
                return;
            }

            foreach (Coin coin in Coin.ActiveCoins)
            {
                Vector3 offset = player.position - coin.transform.position;
                if (offset.y > 0f && offset.y < sneakerScoopDepth &&
                    Mathf.Abs(offset.x) < sneakerScoopHalfWidth &&
                    Mathf.Abs(offset.z) < sneakerScoopHalfWidth)
                {
                    coin.transform.position += offset.normalized *
                        Mathf.Min(sneakerScoopPullSpeed * Time.deltaTime, offset.magnitude);
                }
            }
        }

        void UpdateStatusText()
        {
            if (statusText == null)
            {
                return;
            }

            string text = "";
            foreach (PowerUpType type in AllTypes)
            {
                if (!IsActive(type))
                {
                    continue;
                }

                if (text.Length > 0)
                {
                    text += "   ";
                }

                text += $"{StatusLabel(type)} <color=#FFFFFF>{Mathf.CeilToInt(TimeRemaining(type))}</color>";
            }

            statusText.text = text;
        }

        static string StatusLabel(PowerUpType type) => type switch
        {
            PowerUpType.TaxiMagnet => "<color=#FF6B5E>MAGNET</color>",
            PowerUpType.JoziSneakers => "<color=#6BD4FF>SNEAKERS</color>",
            PowerUpType.DroneBoost => "<color=#B48CFF>DRONE</color>",
            PowerUpType.UbuntuMultiplier => "<color=#FFC845>UBUNTU x2</color>",
            PowerUpType.DoubleCoins => "<color=#FFD700>COINS x2</color>",
            PowerUpType.UbuntuPulse => "<color=#4FC3FF>UBUNTU PULSE</color>",
            _ => "<color=#7DFFA8>SHIELD</color>",
        };
    }
}
