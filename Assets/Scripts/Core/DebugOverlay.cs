using UnityEngine;
using JoburgRunner.Progression;

namespace JoburgRunner.Core
{
    /// <summary>
    /// On-screen developer tools: FPS readout plus cheat buttons (give coins,
    /// reset progression, activate power-ups, change game speed). The entire
    /// interactive body is compiled only in the editor and development builds,
    /// so it is excluded from release. Toggle with the on-screen button or by
    /// leaving <see cref="startVisible"/> off.
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        [SerializeField] bool startVisible;

        float fps;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        bool visible;
        float accum;
        int frames;
        float timeLeft = 0.5f;

        void Awake() => visible = startVisible;

        void Update()
        {
            timeLeft -= Time.unscaledDeltaTime;
            accum += 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            frames++;
            if (timeLeft <= 0f)
            {
                fps = accum / frames;
                accum = 0f; frames = 0; timeLeft = 0.5f;
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 260, 400), GUI.skin.box);
            GUILayout.Label($"FPS: {fps:0}");
            if (GUILayout.Button(visible ? "Hide tools" : "Show tools"))
            {
                visible = !visible;
            }

            if (visible)
            {
                if (GUILayout.Button("Give 1000 coins")) ScoreManager.GrantCoins(1000);
                if (GUILayout.Button("Give 10 R5")) ScoreManager.GrantRareCoins(10);
                if (GUILayout.Button("Reset progression")) SaveManager.ResetAll();

                PowerUpManager pum = FindAnyObjectByType<PowerUpManager>();
                if (pum != null)
                {
                    if (GUILayout.Button("Activate Hoverboard")) pum.Activate(PowerUpType.Hoverboard);
                    if (GUILayout.Button("Activate Ubuntu Pulse")) pum.Activate(PowerUpType.UbuntuPulse);
                    if (GUILayout.Button("Activate Drone Boost")) pum.Activate(PowerUpType.DroneBoost);
                }

                GUILayout.Label($"Time scale: {Time.timeScale:0.0}");
                float newScale = GUILayout.HorizontalSlider(Time.timeScale, 0.2f, 2f);
                if (!Mathf.Approximately(newScale, Time.timeScale))
                {
                    Time.timeScale = newScale;
                }
            }

            GUILayout.EndArea();
        }
#endif
    }
}
