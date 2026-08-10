using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// Drives the <c>PigeonFlockTest</c> scene (Part 20): moves a mock runner
    /// forward and exposes every reaction/weather control from the keyboard plus an
    /// on-screen stats overlay, so all 10 test scenarios can be reproduced without
    /// the full game. Harmless if left in a build (it only reads input / draws GUI).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PigeonFlockTestRig : MonoBehaviour
    {
        [SerializeField] PigeonSpawner spawner;
        [SerializeField] Transform runner;
        [SerializeField] Transform mockVehicle;
        [SerializeField] float runSpeed = 10f;
        [SerializeField] float vehicleSpeed = 18f;

        bool running = true;
        Vector3 vehicleStart;

        void Start()
        {
            if (spawner == null)
            {
                spawner = FindFirstObjectByType<PigeonSpawner>();
            }
            if (mockVehicle != null)
            {
                vehicleStart = mockVehicle.position;
            }
        }

        void Update()
        {
            if (running && runner != null)
            {
                runner.position += Vector3.forward * (runSpeed * Time.deltaTime);
            }

            // Mock vehicle sweeps forward and loops, notifying the threat bus.
            if (mockVehicle != null)
            {
                mockVehicle.position += Vector3.forward * (vehicleSpeed * Time.deltaTime);
                if (runner != null && mockVehicle.position.z > runner.position.z + 40f)
                {
                    mockVehicle.position = new Vector3(vehicleStart.x, vehicleStart.y,
                        runner.position.z - 20f);
                }
                PigeonThreatBus.NotifyVehicleApproach(mockVehicle.position, vehicleSpeed, 8f);
            }

            if (spawner == null)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.Space)) running = !running;
            if (Input.GetKeyDown(KeyCode.H)) spawner.TriggerHornAll();
            if (Input.GetKeyDown(KeyCode.V)) spawner.TriggerVehicleReactionAll();
            if (Input.GetKeyDown(KeyCode.P)) spawner.TriggerPlayerReactionAll();
            if (Input.GetKeyDown(KeyCode.L)) spawner.ForceLandingAll();
            if (Input.GetKeyDown(KeyCode.R)) spawner.ReturnAllPigeons();
            if (Input.GetKeyDown(KeyCode.T)) spawner.SpawnTestFlockAtPlayer();
            if (Input.GetKeyDown(KeyCode.Alpha1)) WeatherState.Current = Weather.Sunny;
            if (Input.GetKeyDown(KeyCode.Alpha2)) WeatherState.Current = Weather.Overcast;
            if (Input.GetKeyDown(KeyCode.Alpha3)) WeatherState.Current = Weather.Rain;
            if (Input.GetKeyDown(KeyCode.Alpha4)) WeatherState.Current = Weather.HeavyRain;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (spawner == null)
            {
                return;
            }
            GUI.Box(new Rect(8, 8, 260, 168), "Pigeon Flock Test");
            GUILayout.BeginArea(new Rect(16, 30, 244, 140));
            GUILayout.Label($"Weather: {WeatherState.Current}");
            GUILayout.Label($"Active pigeons: {spawner.ActivePigeonCount}");
            GUILayout.Label($"Pooled (free):  {spawner.AvailablePigeonCount}");
            GUILayout.Label($"Active flocks:  {spawner.ActiveFlockCount}");
            GUILayout.Label($"Cinematic:      {spawner.CinematicActive}");
            GUILayout.Label($"Runner: {(running ? "moving" : "paused")}  [Space]");
            GUILayout.Label("T spawn · P player · V veh · H horn");
            GUILayout.Label("L land · R return · 1-4 weather");
            GUILayout.EndArea();
        }
#endif
    }
}
