using TMPro;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Endless-runner scoring: points scale with distance travelled and a
    /// multiplier that grows the further you run in one attempt. Coins are a
    /// separate currency. Best score and total coins persist between runs
    /// and app restarts via PlayerPrefs.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        const string HighScoreKey = "JoburgRunner.HighScore";
        const string TotalCoinsKey = "JoburgRunner.TotalCoins";
        const string RareCoinsKey = "JoburgRunner.RareCoins";

        [Header("UI")]
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] TextMeshProUGUI coinText;

        [Header("Scoring")]
        [SerializeField] Transform player;
        [SerializeField] float pointsPerMeter = 2f;
        [SerializeField] float multiplierDistanceStep = 250f;
        [SerializeField] int maxMultiplier = 10;

        [Header("References")]
        [SerializeField] GameManager gameManager;
        [SerializeField] PowerUpManager powerUpManager;

        float startZ;
        float lastZ;
        bool initialised;
        int committedCoins;
        int committedRareCoins;

        public float Score { get; private set; }
        public float Distance => initialised && player != null ? Mathf.Max(0f, player.position.z - startZ) : 0f;
        public int Coins { get; private set; }
        public int RareCoins { get; private set; }
        public int Multiplier { get; private set; } = 1;
        public int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);
        public int TotalCoins => PlayerPrefs.GetInt(TotalCoinsKey, 0);
        public int TotalRareCoins => PlayerPrefs.GetInt(RareCoinsKey, 0);

        public static void SpendCoins(int amount)
        {
            int balance = PlayerPrefs.GetInt(TotalCoinsKey, 0);
            PlayerPrefs.SetInt(TotalCoinsKey, Mathf.Max(0, balance - amount));
            PlayerPrefs.Save();
        }

        public static void SpendRareCoins(int amount)
        {
            int balance = PlayerPrefs.GetInt(RareCoinsKey, 0);
            PlayerPrefs.SetInt(RareCoinsKey, Mathf.Max(0, balance - amount));
            PlayerPrefs.Save();
        }

        void Start()
        {
            if (player != null)
            {
                startZ = player.position.z;
                lastZ = startZ;
                initialised = true;
            }

            UpdateHud();
        }

        void Update()
        {
            if (!initialised || (gameManager != null && !gameManager.IsRunning))
            {
                lastZ = player != null ? player.position.z : lastZ;
                return;
            }

            float z = player.position.z;
            float delta = Mathf.Max(0f, z - lastZ);
            lastZ = z;

            Multiplier = Mathf.Min(maxMultiplier, 1 + Mathf.FloorToInt(Distance / multiplierDistanceStep));
            int powerMultiplier = powerUpManager != null ? powerUpManager.ScoreMultiplier : 1;
            Score += delta * pointsPerMeter * Multiplier * powerMultiplier;
            UpdateHud();
        }

        public void AddCoins(int value, bool rare)
        {
            Coins += value;
            if (rare)
            {
                RareCoins++;
            }

            UpdateHud();
        }

        public void AddPoints(float points)
        {
            Score += points;
            UpdateHud();
        }

        /// <summary>Persists this run's results. Returns true on a new best score.</summary>
        /// <summary>
        /// Persists this run's results. Safe to call more than once per run
        /// (a continued run commits at every game over): only the coins
        /// gathered since the previous commit are banked.
        /// </summary>
        public bool CommitRun()
        {
            PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins + Coins - committedCoins);
            PlayerPrefs.SetInt(RareCoinsKey, TotalRareCoins + RareCoins - committedRareCoins);
            committedCoins = Coins;
            committedRareCoins = RareCoins;

            int finalScore = Mathf.FloorToInt(Score);
            bool newBest = finalScore > HighScore;
            if (newBest)
            {
                PlayerPrefs.SetInt(HighScoreKey, finalScore);
            }

            PlayerPrefs.Save();
            return newBest;
        }

        void UpdateHud()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {Mathf.FloorToInt(Score)}  x{Multiplier}";
            }

            if (coinText != null)
            {
                coinText.text = $"Coins: {Coins}";
            }
        }
    }
}
