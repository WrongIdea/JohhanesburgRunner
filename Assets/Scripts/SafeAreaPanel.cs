using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Keeps this RectTransform inside the device safe area so notches,
    /// punch-holes and home indicators never cover the HUD. Attach to a
    /// full-stretch child of the canvas and parent HUD elements under it.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaPanel : MonoBehaviour
    {
        Rect applied;

        void OnEnable()
        {
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != applied)
            {
                Apply();
            }
        }

        void Apply()
        {
            applied = Screen.safeArea;

            Vector2 min = applied.min;
            Vector2 max = applied.max;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
