using UnityEngine;
using UnityEngine.UI;

namespace JoburgRunner
{
    [RequireComponent(typeof(Button))]
    public class PauseButton : MonoBehaviour
    {
        [SerializeField] GameManager gameManager;

        void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }

            GetComponent<Button>().onClick.AddListener(Pause);
        }

        void Pause()
        {
            if (gameManager != null)
            {
                gameManager.PauseGame();
            }
        }
    }
}
