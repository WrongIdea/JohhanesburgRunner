using JoburgRunner.Environment.Pigeons;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="PigeonSpawner"/> (Part 19): live pool /
    /// flock stats plus play-mode test buttons so the whole system can be exercised
    /// without waiting for the runner to reach a spawn point.
    /// </summary>
    [CustomEditor(typeof(PigeonSpawner))]
    public sealed class PigeonSpawnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var spawner = (PigeonSpawner)target;
            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Live stats", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Active pigeons:  {spawner.ActivePigeonCount}");
                EditorGUILayout.LabelField($"Pooled (free):   {spawner.AvailablePigeonCount}");
                EditorGUILayout.LabelField($"Active flocks:    {spawner.ActiveFlockCount}");
                EditorGUILayout.LabelField($"Cinematic live:   {spawner.CinematicActive}");
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Test actions", EditorStyles.boldLabel);
                if (GUILayout.Button("Spawn Test Flock"))
                {
                    if (!spawner.SpawnTestFlockAtPlayer())
                    {
                        Debug.LogWarning("No free flock slot / player to spawn a test flock.");
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Player Reaction"))
                    {
                        spawner.TriggerPlayerReactionAll();
                    }
                    if (GUILayout.Button("Vehicle Reaction"))
                    {
                        spawner.TriggerVehicleReactionAll();
                    }
                    if (GUILayout.Button("Horn"))
                    {
                        spawner.TriggerHornAll();
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Force Landing"))
                    {
                        spawner.ForceLandingAll();
                    }
                    if (GUILayout.Button("Return All Pigeons"))
                    {
                        spawner.ReturnAllPigeons();
                    }
                }
                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Enter Play mode to see live pool stats and use the test buttons " +
                    "(Spawn Test Flock, Player/Vehicle/Horn reaction, Force Landing, Return All).",
                    MessageType.Info);
            }
        }
    }
}
