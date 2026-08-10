using UnityEngine;
using UnityEngine.UI;

namespace JoburgRunner
{
    /// <summary>Dismisses the Game Over overlay without restarting the run.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class GameOverCloseButton : MonoBehaviour
    {
        void Awake()
        {
            Button button = GetComponent<Button>();
            button.onClick.RemoveListener(Close);
            button.onClick.AddListener(Close);
        }

        void Close()
        {
            GameManager manager = FindAnyObjectByType<GameManager>();
            manager?.CloseGameOverMenu();
        }
    }
}
