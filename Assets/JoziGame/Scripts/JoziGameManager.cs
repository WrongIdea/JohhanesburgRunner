using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace JoziGame
{
    public class JoziGameManager : MonoBehaviour
    {
        [SerializeField] int taxiTokensToCollect = 5;
        [SerializeField] TextMeshProUGUI hudText;
        [SerializeField] TextMeshProUGUI messageText;
        [SerializeField] Transform player;
        [SerializeField] Transform finishPoint;

        int collected;
        bool finished;

        void Start()
        {
            ShowMessage("Collect 5 taxi tokens, then reach Maboneng.");
            UpdateHud();
        }

        void Update()
        {
            if (finished && WantsRestart())
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            if (!finished && player != null && finishPoint != null)
            {
                float distance = Vector3.Distance(player.position, finishPoint.position);
                if (distance < 4f && collected >= taxiTokensToCollect)
                {
                    finished = true;
                    ShowMessage("You made it through Jozi! Tap to play again.");
                }
                else if (distance < 4f)
                {
                    ShowMessage("Maboneng is ahead. Collect every taxi token first.");
                }
            }
        }

        public void CollectToken(GameObject token)
        {
            if (finished)
            {
                return;
            }

            collected++;
            Destroy(token);
            UpdateHud();

            if (collected >= taxiTokensToCollect)
            {
                ShowMessage("All tokens collected. Head to Maboneng!");
            }
            else
            {
                ShowMessage("Taxi token collected.");
            }
        }

        void UpdateHud()
        {
            if (hudText != null)
            {
                hudText.text = $"Taxi tokens: {collected}/{taxiTokensToCollect}";
            }
        }

        void ShowMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        static bool WantsRestart()
        {
            bool keyboard = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            bool touch = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            return keyboard || touch;
        }
    }
}
