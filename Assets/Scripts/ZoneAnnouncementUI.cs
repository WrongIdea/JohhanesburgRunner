using JoburgRunner.Environment;
using TMPro;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>Briefly announces each new route leg so progression feels like distinct levels.</summary>
    public sealed class ZoneAnnouncementUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] CanvasGroup group;
        [SerializeField] float holdSeconds = 2.2f;
        [SerializeField] float fadeSeconds = 0.45f;

        EnvironmentDirector director;
        float visibleUntil;

        void Start()
        {
            director = EnvironmentDirector.Instance;
            if (director == null)
            {
                group.alpha = 0f;
                return;
            }

            director.ZoneChanged += Show;
            if (director.ActiveZone != null)
            {
                Show(director.ActiveZone);
            }
        }

        void OnDestroy()
        {
            if (director != null)
            {
                director.ZoneChanged -= Show;
            }
        }

        void Update()
        {
            float remaining = visibleUntil - Time.unscaledTime;
            group.alpha = remaining > 0f
                ? Mathf.Clamp01(remaining / Mathf.Max(0.01f, fadeSeconds))
                : 0f;
        }

        void Show(EnvironmentZoneProfile zone)
        {
            int level = Mathf.Max(1, zone.routeLeg + 1);
            label.text = $"LEVEL {level}\n<size=44>{zone.displayName.ToUpperInvariant()}</size>";
            group.alpha = 1f;
            visibleUntil = Time.unscaledTime + holdSeconds + fadeSeconds;
        }
    }
}
