using UnityEngine;

namespace JoburgRunner
{
    public class RoadSegmentSpawner : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] GameObject roadSegmentPrefab;
        [SerializeField] int visibleSegments = 7;
        [SerializeField] float segmentLength = 30f;
        [SerializeField] float recycleBehindDistance = 35f;
        int nextDistrictIndex = 1;

        void Update()
        {
            if (player == null || roadSegmentPrefab == null)
            {
                return;
            }

            EnsureSegmentsAhead();
            RecycleSegmentsBehind();
        }

        void EnsureSegmentsAhead()
        {
            while (transform.childCount < visibleSegments)
            {
                // Start one segment behind the origin: the menu and run cameras
                // sit behind the player, and with no road there the bottom of
                // tall screens shows the road's cross-section and skybox ground.
                float zPosition = (transform.childCount - 1) * segmentLength;
                GameObject segment = Instantiate(roadSegmentPrefab, new Vector3(0f, 0f, zPosition), Quaternion.identity, transform);
                ApplyNextDistrict(segment);
            }
        }

        void RecycleSegmentsBehind()
        {
            float furthestZ = float.MinValue;
            for (int i = 0; i < transform.childCount; i++)
            {
                furthestZ = Mathf.Max(furthestZ, transform.GetChild(i).position.z);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform segment = transform.GetChild(i);
                if (segment.position.z + segmentLength < player.position.z - recycleBehindDistance)
                {
                    furthestZ += segmentLength;
                    segment.position = new Vector3(0f, 0f, furthestZ);
                    ApplyNextDistrict(segment.gameObject);
                }
            }
        }

        void ApplyNextDistrict(GameObject segment)
        {
            RoadSegmentVisuals visuals = segment.GetComponent<RoadSegmentVisuals>();
            if (visuals != null)
            {
                visuals.SetDistrict(nextDistrictIndex);
                nextDistrictIndex++;
            }
        }
    }
}
