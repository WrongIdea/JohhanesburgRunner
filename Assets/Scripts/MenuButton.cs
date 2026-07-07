using UnityEngine;
using UnityEngine.UI;

namespace JoburgRunner
{
    /// <summary>Returns to the main menu from the game-over screen.</summary>
    [RequireComponent(typeof(Button))]
    public class MenuButton : MonoBehaviour
    {
        [SerializeField] GameManager gameManager;

        void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }

            GetComponent<Button>().onClick.AddListener(GoToMenu);
        }

        void GoToMenu()
        {
            if (gameManager != null)
            {
                gameManager.BackToMenu();
            }
        }
    }
}
