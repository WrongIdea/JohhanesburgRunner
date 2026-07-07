using UnityEngine;
using UnityEngine.UI;

namespace JoburgRunner
{
    /// <summary>Spends R5 coins to continue the current run after a crash.</summary>
    [RequireComponent(typeof(Button))]
    public class ContinueButton : MonoBehaviour
    {
        [SerializeField] GameManager gameManager;

        void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }

            GetComponent<Button>().onClick.AddListener(Continue);
        }

        void Continue()
        {
            if (gameManager != null)
            {
                gameManager.ContinueRun();
            }
        }
    }
}
