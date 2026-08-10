using UnityEngine;
using UnityEngine.UI;

namespace JoburgRunner
{
    [RequireComponent(typeof(Button))]
    public class ResumeButton : MonoBehaviour
    {
        [SerializeField] GameManager gameManager;

        void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }

            GetComponent<Button>().onClick.AddListener(Resume);
        }

        void Resume()
        {
            if (gameManager != null)
            {
                gameManager.ResumeGame();
            }
        }
    }
}
