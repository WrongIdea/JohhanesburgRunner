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
        public const int MaxUpgradeLevel = 5;
        public const float SecondsPerUpgradeLevel = 2f;

        [Header("References")]
        [SerializeField] Transform player;
        [SerializeField] GameManager gameManager;
        [SerializeField] TextMeshProUGUI statusText;

        [Header("Effect Tuning")]
        [SerializeField] float magnetRadius = 7f;
        [SerializeField] float magnetPullSpeed = 18f;
        [SerializeField] float sneakersJumpMultiplier = 1.55f;
        [SerializeField] float droneFlightHeight = 3.6f;
        [SerializeField] float droneCoinLiftSpeed = 12f;
        [SerializeField] int ubuntuScoreMultiplier = 2;

        PlayerController playerController;

        static readonly PowerUpType[] AllTypes =
        {
            PowerUpType.TaxiMagnet,
            PowerUpType.JoziSneakers,
            PowerUpType.DroneBoost,
            PowerUpType.UbuntuMultiplier,
            PowerUpType.Hoverboard,
        };

        static readonly float[] BaseDurations = { 8f, 10f, 6f, 10f, 15f };

        readonly float[] remaining = new float[AllTypes.Length];

        public float JumpMultiplier => IsActive(PowerUpType.JoziSneakers) ? sneakersJumpMultiplier : 1f;
        public int ScoreMultiplier => IsActive(PowerUpType.UbuntuMultiplier) ? ubuntuScoreMultiplier : 1;
        public bool DroneActive => IsActive(PowerUpType.DroneBoost);
        public float DroneFlightHeight => droneFlightHeight;
        public bool HasShield => IsActive(PowerUpType.Hoverboard);

        public static string DisplayName(PowerUpType type) => type switch
        {
            PowerUpType.TaxiMagnet => "Taxi Magnet",
            PowerUpType.JoziSneakers => "Jozi Sneakers",
            PowerUpType.DroneBoost => "Drone Boost",
            PowerUpType.UbuntuMultiplier => "Ubuntu Multiplier",
            _ => "Hoverboard",
        };

        public static string Description(PowerUpType type) => type switch
        {
            PowerUpType.TaxiMagnet => "Attracts nearby coins",
            PowerUpType.JoziSneakers => "Higher jumps",
            PowerUpType.DroneBoost => "Temporary flight",
            PowerUpType.UbuntuMultiplier => "Doubles your score",
            _ => "Survives one crash",
        };

        static string UpgradeLevelKey(PowerUpType type) => $"JoburgRunner.PowerLevel.{type}";
        static string PickupCountKey(PowerUpType type) => $"JoburgRunner.Pickups.{type}";

        public static int UpgradeLevel(PowerUpType type) =>
            Mathf.Clamp(PlayerPrefs.GetInt(UpgradeLevelKey(type), 0), 0, MaxUpgradeLevel);

        public static void SetUpgradeLevel(PowerUpType type, int level)
        {
            PlayerPrefs.SetInt(UpgradeLevelKey(type), Mathf.Clamp(level, 0, MaxUpgradeLevel));
            PlayerPrefs.Save();
        }

        public static int PickupCount(PowerUpType type) => PlayerPrefs.GetInt(PickupCountKey(type), 0);

        public static float Duration(PowerUpType type) =>
            BaseDurations[(int)type] + UpgradeLevel(type) * SecondsPerUpgradeLevel;

        public bool IsActive(PowerUpType type) => remaining[(int)type] > 0f;

        public float TimeRemaining(PowerUpType type) => Mathf.Max(0f, remaining[(int)type]);

        public void Activate(PowerUpType type)
        {
            remaining[(int)type] = Duration(type);
            PlayerPrefs.SetInt(PickupCountKey(type), PickupCount(type) + 1);
            UpdateStatusText();
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
            return true;
        }

        void Update()
        {
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
                }
            }

            if (IsActive(PowerUpType.TaxiMagnet))
            {
                PullCoins();
            }

            if (DroneActive)
            {
                LiftCoinsForFlight();
            }

            if (anyActive || (statusText != null && statusText.text.Length > 0))
            {
                UpdateStatusText();
            }
        }

        void PullCoins()
        {
            if (player == null)
            {
                return;
            }

            Vector3 target = player.position + Vector3.up * 1f;
            foreach (Coin coin in Coin.ActiveCoins)
            {
                Vector3 offset = target - coin.transform.position;
                if (offset.sqrMagnitude < magnetRadius * magnetRadius)
                {
                    coin.transform.position += offset.normalized *
                        Mathf.Min(magnetPullSpeed * Time.deltaTime, offset.magnitude);
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
            _ => "<color=#7DFFA8>SHIELD</color>",
        };
    }
}
