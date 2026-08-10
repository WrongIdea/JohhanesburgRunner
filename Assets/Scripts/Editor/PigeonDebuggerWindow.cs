using JoburgRunner.Environment;
using JoburgRunner.Environment.Pigeons;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Pigeon Debugger window (Part 13): Jozi Runner ▸ Pigeon Debugger. Live pool /
    /// flock / pigeon counts plus one-click actions (spawn flock, takeoff, horn,
    /// force landing, toggle weather) against the running <see cref="PigeonSpawner"/>,
    /// and edit-mode utilities (validate, stress test, re-integrate). Play-mode
    /// actions are disabled until a spawner is live.
    /// </summary>
    public sealed class PigeonDebuggerWindow : EditorWindow
    {
        [MenuItem("Jozi Runner/Pigeon Debugger")]
        public static void Open()
        {
            var w = GetWindow<PigeonDebuggerWindow>("Pigeon Debugger");
            w.minSize = new Vector2(300, 380);
        }

        PigeonSpawner spawner;

        void OnInspectorUpdate() => Repaint();

        void OnGUI()
        {
            if (spawner == null && Application.isPlaying)
            {
                spawner = FindAnyObjectByType<PigeonSpawner>();
            }

            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode for live stats and reaction actions.", MessageType.Info);
            }
            else if (spawner == null)
            {
                EditorGUILayout.HelpBox("No PigeonSpawner in the scene.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"Active pigeons: {spawner.ActivePigeonCount}");
                EditorGUILayout.LabelField($"Pooled (free):  {spawner.AvailablePigeonCount}");
                EditorGUILayout.LabelField($"Active flocks:   {spawner.ActiveFlockCount}");
                EditorGUILayout.LabelField($"Cinematic live:  {spawner.CinematicActive}");
                EditorGUILayout.LabelField($"Weather:         {WeatherState.Current}");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
                if (GUILayout.Button("Spawn Test Flock")) spawner.SpawnTestFlockAtPlayer();
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Player")) spawner.TriggerPlayerReactionAll();
                    if (GUILayout.Button("Vehicle")) spawner.TriggerVehicleReactionAll();
                    if (GUILayout.Button("Horn")) spawner.TriggerHornAll();
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Force Landing")) spawner.ForceLandingAll();
                    if (GUILayout.Button("Return All")) spawner.ReturnAllPigeons();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Weather", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Sunny")) SetWeather(Weather.Sunny);
                    if (GUILayout.Button("Overcast")) SetWeather(Weather.Overcast);
                    if (GUILayout.Button("Rain")) SetWeather(Weather.Rain);
                    if (GUILayout.Button("Heavy")) SetWeather(Weather.HeavyRain);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Edit-mode tools", EditorStyles.boldLabel);
            if (GUILayout.Button("Run Validation")) PigeonIntegrationValidation.Validate();
            if (GUILayout.Button("Run Pool Stress Test")) PigeonStressTest.Run();
            if (GUILayout.Button("Re-integrate Road Segment")) PigeonSegmentIntegration.Integrate();
        }

        static void SetWeather(Weather w)
        {
            WeatherState.Current = w;
            // Re-apply pigeon audio suppression etc. through the spawner if present.
            var s = FindAnyObjectByType<PigeonSpawner>();
            if (s != null)
            {
                s.ApplyQuality();
            }
        }
    }
}
