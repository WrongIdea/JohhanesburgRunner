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
        [SerializeField] float oncomingSpawnDistance = 145f;
        [SerializeField] float oncomingCullAheadDistance = 22f;

        float nextSameDirectionTime;
        float nextOncomingTime;

        void Start()
        {
            if (player == null || oncomingPrefabs == null || oncomingPrefabs.Length == 0)
            {
                return;
            }

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
            GameObject vehicle = Instantiate(prefab, position, Quaternion.Euler(0f, yRotation, 0f), transform);
            SceneryVehicle marker = vehicle.GetComponent<SceneryVehicle>();
            if (marker == null)
            {
                marker = vehicle.AddComponent<SceneryVehicle>();
            }

            marker.Speed = speed;
        }

        void MoveAndCleanup()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform vehicle = transform.GetChild(i);
                SceneryVehicle marker = vehicle.GetComponent<SceneryVehicle>();
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
                    Destroy(vehicle.gameObject);
                }
            }
        }
    }
}
