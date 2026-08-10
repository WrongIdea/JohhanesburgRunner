using System.Collections.Generic;
using UnityEngine;
using JoburgRunner.Environment.Decor;

namespace JoburgRunner
{
    /// <summary>
    /// Keeps the street alive like the reference footage: minibus taxis drive
    /// ahead of the player on the left shoulder showing their rear, while more
    /// taxis stream past on the oncoming side. Vehicles are decoration only
    /// (no colliders) and are destroyed once they fall behind the player.
    /// </summary>
    public class SceneryTraffic : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] GameObject sameDirectionPrefab;
        [SerializeField] GameObject[] oncomingPrefabs;
        [SerializeField] float sameDirectionSpeed = 6.5f;
        [SerializeField] float oncomingSpeed = 6f;
        [SerializeField] float sameDirectionInterval = 7f;
        [SerializeField] float oncomingInterval = 5f;
        [SerializeField] float shoulderX = 8.4f;
        [SerializeField] float oncomingSpawnDistance = 155f;
        [SerializeField] float oncomingCullAheadDistance = 22f;
        [SerializeField, Min(1)] int prewarmPerPrefab = 6;

        float nextSameDirectionTime;
        float nextOncomingTime;
        readonly Dictionary<GameObject, Stack<GameObject>> pools = new Dictionary<GameObject, Stack<GameObject>>();
        readonly Dictionary<GameObject, GameObject> prefabOf = new Dictionary<GameObject, GameObject>();
        readonly List<SceneryVehicle> active = new List<SceneryVehicle>(12);

        void Start()
        {
            if (player == null || oncomingPrefabs == null || oncomingPrefabs.Length == 0)
            {
                return;
            }

            Prewarm();

            // Seed the street ahead, but keep decorative traffic out of the
            // near foreground so it never reads like an unavoidable obstacle.
            for (float distance = 75f; distance <= oncomingSpawnDistance; distance += 35f)
            {
                GameObject prefab = oncomingPrefabs[Random.Range(0, oncomingPrefabs.Length)];
                Spawn(prefab, new Vector3(shoulderX, 0f, player.position.z + distance), 180f, -oncomingSpeed);
            }

            nextOncomingTime = Time.time + oncomingInterval;
        }

        void Update()
        {
            if (player == null)
            {
                return;
            }

            if (sameDirectionPrefab != null && Time.time >= nextSameDirectionTime)
            {
                nextSameDirectionTime = Time.time + sameDirectionInterval;
                Spawn(sameDirectionPrefab, new Vector3(-shoulderX, 0f, player.position.z + 45f), 0f, sameDirectionSpeed);
            }

            if (oncomingPrefabs != null && oncomingPrefabs.Length > 0 && Time.time >= nextOncomingTime)
            {
                nextOncomingTime = Time.time + oncomingInterval;
                GameObject prefab = oncomingPrefabs[Random.Range(0, oncomingPrefabs.Length)];
                Spawn(prefab, new Vector3(shoulderX, 0f, player.position.z + oncomingSpawnDistance), 180f, -oncomingSpeed);
            }

            MoveAndCleanup();
        }

        void Spawn(GameObject prefab, Vector3 position, float yRotation, float speed)
        {
            if (prefab == null) return;
            GameObject vehicle = Take(prefab);
            vehicle.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yRotation, 0f));
            vehicle.SetActive(true);
            SceneryVehicle marker = vehicle.GetComponent<SceneryVehicle>();
            if (marker == null)
            {
                marker = vehicle.AddComponent<SceneryVehicle>();
            }

            marker.Speed = speed;
            active.Add(marker);
        }

        void MoveAndCleanup()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                SceneryVehicle marker = active[i];
                if (marker == null) { active.RemoveAt(i); continue; }
                Transform vehicle = marker.transform;
                if (marker != null)
                {
                    float direction = Mathf.Sign(marker.Speed);
                    float nextZ = vehicle.position.z + marker.Speed * Time.deltaTime;
                    if (CrossingPedestrian.TryGetTaxiStopZ(vehicle.position.z, direction, out float stopZ))
                    {
                        nextZ = direction > 0f
                            ? Mathf.Min(nextZ, stopZ)
                            : Mathf.Max(nextZ, stopZ);
                    }
                    Vector3 position = vehicle.position;
                    position.z = nextZ;
                    vehicle.position = position;
                }

                float cullZ = marker != null && marker.Speed < 0f
                    ? player.position.z + oncomingCullAheadDistance
                    : player.position.z - 30f;
                if (vehicle.position.z < cullZ)
                {
                    active.RemoveAt(i);
                    Return(vehicle.gameObject);
                }
            }
        }

        void Prewarm()
        {
            var unique = new HashSet<GameObject>();
            if (sameDirectionPrefab != null) unique.Add(sameDirectionPrefab);
            if (oncomingPrefabs != null) foreach (GameObject prefab in oncomingPrefabs) if (prefab != null) unique.Add(prefab);
            foreach (GameObject prefab in unique)
            {
                var stack = new Stack<GameObject>(prewarmPerPrefab); pools[prefab] = stack;
                for (int i=0;i<prewarmPerPrefab;i++) stack.Push(CreatePooled(prefab));
            }
        }

        GameObject CreatePooled(GameObject prefab)
        {
            GameObject instance=Instantiate(prefab,transform);instance.SetActive(false);prefabOf[instance]=prefab;return instance;
        }

        GameObject Take(GameObject prefab)
        {
            if (!pools.TryGetValue(prefab,out Stack<GameObject> stack)) { stack=new Stack<GameObject>();pools[prefab]=stack; }
            return stack.Count>0?stack.Pop():CreatePooled(prefab);
        }

        void Return(GameObject instance)
        {
            instance.SetActive(false);
            if(prefabOf.TryGetValue(instance,out GameObject prefab))pools[prefab].Push(instance);
        }
    }
}
