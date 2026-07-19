using UnityEngine;
using UnityEngine.UI;

namespace JoburgRunner
{
    /// <summary>
    /// HUD icon for Ubuntu Pulse: hidden while inactive, shown with a
    /// radial countdown ring (Image.fillAmount) and a softly pulsing glow
    /// backdrop while active.
    /// </summary>
    public class UbuntuPulseUI : MonoBehaviour
    {
        [SerializeField] PowerUpManager powerUpManager;
        [SerializeField] GameObject root;
        [SerializeField] Image fillImage;
        [SerializeField] Image glow;

        void Update()
        {
            bool active = powerUpManager != null && powerUpManager.UbuntuPulseActive;
            if (root != null)
            {
                root.SetActive(active);
            }

            if (!active)
            {
                return;
            }

            float duration = PowerUpManager.Duration(PowerUpType.UbuntuPulse);
            if (fillImage != null && duration > 0f)
            {
                fillImage.fillAmount = powerUpManager.TimeRemaining(PowerUpType.UbuntuPulse) / duration;
            }

            if (glow != null)
            {
                Color color = glow.color;
                color.a = 0.3f + 0.18f * Mathf.Sin(Time.time * 4f);
                glow.color = color;
            }
        }
    }
}
