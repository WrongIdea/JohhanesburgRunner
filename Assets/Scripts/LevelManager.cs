using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Spawns endless road segments and obstacles ahead of the player.
    /// Old road segments are destroyed after they pass behind the main camera.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Player and Camera")]
        [SerializeField] Transform player;
        [SerializeField] Transform mainCamera;

        [Header("Road Segments")]
        [SerializeField] GameObject[] roadSegmentPrefabs;
        [SerializeField] int startingSegments = 6;
        [SerializeField] float segmentLength = 30f;
        [SerializeField] float spawnAheadDistance = 90f;
        [SerializeField] float destroyBehindCameraDistance = 35f;

        [Header("Lanes")]
        [SerializeField] float[] laneXPositions = { -2.7f, 0f, 2.7f };

        [Header("Obstacles")]
        [SerializeField] GameObject[] obstaclePrefabs;
        [SerializeField] int minObstaclesPerSegment = 1;
        [SerializeField] int maxObstaclesPerSegment = 3;
        [SerializeField] float firstObstacleZOffset = 8f;
        [SerializeField] float obstacleSpacing = 8f;

        [Header("Coins")]
        [SerializeField] GameObject coinPrefab;
        [SerializeField] int coinTrailsPerSegment = 1;
        [SerializeField] int coinsPerTrail = 6;
        [SerializeField] float firstCoinZOffset = 5f;
        [SerializeField] float coinSpacing = 2f;
        [SerializeField] float coinHeight = 1.25f;

        readonly List<GameObject> activeSegments = new List<GameObject>();
        float nextSegmentZ;

        void Start()
        {
            if (mainCamera == null && Camera.main != null)
            {
                mainCamera = Camera.main.transform;
            }

            for (int i = 0; i < startingSegments; i++)
            {
                SpawnRoadSegment();
            }
        }

        void Update()
        {
            if (player == null)
            {
                return;
            }

            while (nextSegmentZ < player.position.z + spawnAheadDistance)
            {
                SpawnRoadSegment();
            }

            DestroyOldSegments();
        }

        void SpawnRoadSegment()
        {
            if (roadSegmentPrefabs == null || roadSegmentPrefabs.Length == 0)
            {
                return;
            }

            GameObject prefab = roadSegmentPrefabs[Random.Range(0, roadSegmentPrefabs.Length)];
            GameObject segment = Instantiate(prefab, new Vector3(0f, 0f, nextSegmentZ), Quaternion.identity, transform);
            activeSegments.Add(segment);

            SpawnObstaclesOnSegment(segment.transform, nextSegmentZ);
            SpawnCoinsOnSegment(segment.transform, nextSegmentZ);
            nextSegmentZ += segmentLength;
        }

        void SpawnObstaclesOnSegment(Transform segment, float segmentStartZ)
        {
            if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
            {
                return;
            }

            int obstacleCount = Random.Range(minObstaclesPerSegment, maxObstaclesPerSegment + 1);
            int previousLane = -1;

            for (int i = 0; i < obstacleCount; i++)
            {
                int laneIndex = Random.Range(0, laneXPositions.Length);

                // Avoid creating a fully unfair wall by trying not to repeat lanes too often.
                if (laneIndex == previousLane && laneXPositions.Length > 1)
                {
                    laneIndex = (laneIndex + 1) % laneXPositions.Length;
                }

                previousLane = laneIndex;

                float x = laneXPositions[laneIndex];
                float z = segmentStartZ + firstObstacleZOffset + i * obstacleSpacing;
                GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                Instantiate(obstaclePrefab, new Vector3(x, 0f, z), Quaternion.identity, segment);
            }
        }

        void SpawnCoinsOnSegment(Transform segment, float segmentStartZ)
        {
            if (coinPrefab == null)
            {
                return;
            }

            for (int trail = 0; trail < coinTrailsPerSegment; trail++)
            {
                int laneIndex = Random.Range(0, laneXPositions.Length);
                float x = laneXPositions[laneIndex];
                float startZ = segmentStartZ + firstCoinZOffset + trail * 10f;

                for (int coin = 0; coin < coinsPerTrail; coin++)
                {
                    Vector3 position = new Vector3(x, coinHeight, startZ + coin * coinSpacing);
                    Instantiate(coinPrefab, position, Quaternion.identity, segment);
                }
            }
        }

        void DestroyOldSegments()
        {
            if (mainCamera == null)
            {
                return;
            }

            for (int i = activeSegments.Count - 1; i >= 0; i--)
            {
                GameObject segment = activeSegments[i];
                if (segment == null)
                {
                    activeSegments.RemoveAt(i);
                    continue;
                }

                float segmentEndZ = segment.transform.position.z + segmentLength;
                if (segmentEndZ < mainCamera.position.z - destroyBehindCameraDistance)
                {
                    activeSegments.RemoveAt(i);
                    Destroy(segment);
                }
            }
        }
    }
}
