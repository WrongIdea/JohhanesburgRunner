using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace JoburgRunner
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] ScoreManager scoreManager;
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] TextMeshProUGUI finalScoreText;
        [SerializeField] GameObject continueButton;
        [SerializeField] TextMeshProUGUI continueLabel;
        [SerializeField] float gameOverPresentationDelay = 0.85f;

        int continuesUsed;

        /// <summary>Continues get pricier each time within one run: 1, 2, 4… R5.</summary>
        public int ContinueCost => 1 << continuesUsed;

        /// <summary>Set before a scene reload to jump straight back into a run.</summary>
        public static bool SkipMenuOnce;

        public bool IsGameOver { get; private set; }

        /// <summary>False while the main menu is up and after a crash.</summary>
        public bool IsRunning { get; private set; }

        bool canRestart;

        void Start()
        {
            // Run at 60fps on mobile instead of the platform default.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            Time.timeScale = 1f;
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        public void StartRun()
        {
            if (!IsGameOver)
            {
                IsRunning = true;
            }
        }

        void Update()
        {
            if (IsGameOver && canRestart && WantsRestart())
            {
                RestartGame();
            }
        }

        public void GameOver()
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            IsRunning = false;
            canRestart = false;

            if (player != null)
            {
                player.enabled = false;
            }

            if (scoreManager != null)
            {
                bool newBest = scoreManager.CommitRun();
                if (continueButton != null)
                {
                    continueButton.SetActive(scoreManager.TotalRareCoins >= ContinueCost);
                    if (continueLabel != null)
                    {
                        continueLabel.text = $"CONTINUE · {ContinueCost} R5";
                    }
                }
                if (finalScoreText != null)
                {
                    string bestLine = newBest
                        ? "<color=#FFC845>New best score!</color>"
                        : $"Best  {scoreManager.HighScore}";
                    finalScoreText.text =
                        $"<size=44><color=#AEB4C2>FINAL SCORE</color></size>\n" +
                        $"<size=130><b>{Mathf.FloorToInt(scoreManager.Score)}</b></size>\n" +
                        $"<size=46>{bestLine}</size>\n" +
                        $"<size=38><color=#AEB4C2>Distance {scoreManager.Distance / 1000f:0.00} km      " +
                        $"Coins {scoreManager.Coins}  (total {scoreManager.TotalCoins})</color></size>";
                }
            }

            StartCoroutine(ShowGameOverAfterFall());
        }

        IEnumerator ShowGameOverAfterFall()
        {
            float elapsed = 0f;
            while (elapsed < gameOverPresentationDelay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = 0f;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            canRestart = true;
        }

        /// <summary>
        /// Spends R5 coins to resume the current run in place: the speed and
        /// difficulty ramps restart from scratch, but score and coins carry on.
        /// </summary>
        public void ContinueRun()
        {
            if (!IsGameOver || scoreManager == null || scoreManager.TotalRareCoins < ContinueCost)
            {
                return;
            }

            ScoreManager.SpendRareCoins(ContinueCost);
            continuesUsed++;

            Time.timeScale = 1f;
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            ChunkManager chunkManager = FindAnyObjectByType<ChunkManager>();
            if (chunkManager != null)
            {
                chunkManager.ResetForContinue();
            }

            if (player != null)
            {
                player.ResetForContinue();
                player.enabled = true;
            }

            IsGameOver = false;
            canRestart = false;
            IsRunning = true;
        }

        public void RestartGame()
        {
            SkipMenuOnce = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void BackToMenu()
        {
            SkipMenuOnce = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Keyboard only: on touch the game-over card has explicit Restart
        // and Menu buttons, and tap-anywhere would swallow their presses.
        static bool WantsRestart()
        {
            return Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        }
    }
}
