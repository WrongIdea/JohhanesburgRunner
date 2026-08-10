using UnityEngine;
using JoburgRunner.Environment;
using JoburgRunner.Environment.Decor;

namespace JoburgRunner
{
    // Runs after the environment director's default-order Start so its pools exist,
    // while still completing initial dressing before the first rendered frame.
    [DefaultExecutionOrder(100)]
    public class RoadSegmentSpawner : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] GameObject roadSegmentPrefab;
        [SerializeField] int visibleSegments = 7;
        [SerializeField] float segmentLength = 30f;
        [SerializeField] float recycleBehindDistance = 35f;
        [Header("Jacaranda Avenue")]
        [SerializeField, Min(0)] int jacarandaFirstSegment = 17;
        [SerializeField, Min(1)] int jacarandaLengthSegments = 5;
        [SerializeField, Min(6)] int jacarandaRepeatEverySegments = 60;
        [Header("Decorative Junctions")]
        [SerializeField] JunctionSpawnSettings junctionSettings;
        int nextDistrictIndex = 1;
        int dressedSegmentCount;
        int straightSegmentsSinceJunction;
        int junctionOrdinal;
        JunctionType lastJunctionType;
        bool firstJunctionSpawned;
        bool previousDressedSegmentWasJunction;

        void Start()
        {
            if (player == null || roadSegmentPrefab == null)
            {
                return;
            }

            // Dress the baked ring before the first frame is rendered so pooled
            // buildings never visibly activate around the player.
            for (int i = 0; i < transform.childCount; i++)
            {
                ApplyNextDistrict(transform.GetChild(i).gameObject);
            }

            EnsureSegmentsAhead();
        }

        void Update()
        {
            if (player == null || roadSegmentPrefab == null)
            {
                return;
            }

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
                    // Rent/activate and position pooled buildings while this complete
                    // segment is still behind the camera. Only then teleport the fully
                    // dressed tile ahead, preventing visible in-frustum spawn pops.
                    ApplyNextDistrict(segment.gameObject);
                    segment.position = new Vector3(0f, 0f, furthestZ);
                }
            }
        }

        void ApplyNextDistrict(GameObject segment)
        {
            RoadSegmentVisuals visuals = segment.GetComponent<RoadSegmentVisuals>();
            JunctionVisuals junctionVisuals = segment.GetComponent<JunctionVisuals>();
            if (visuals != null)
            {
                int segmentIndex = dressedSegmentCount++;
                bool jacaranda = IsJacarandaSegment(segmentIndex);
                if (jacaranda)
                {
                    junctionVisuals?.SetType(JunctionType.None);
                    previousDressedSegmentWasJunction = false;
                    // Keep the park visual family beneath the dedicated canopy recipe.
                    visuals.SetDistrict(2);
                    EnvironmentDecorDirector.Instance?.DecorateJacarandaSegment(
                        segment.GetComponent<SegmentDecorator>());
                    return;
                }

                int district = EnvironmentDirector.Instance != null &&
                    EnvironmentDirector.Instance.ActiveZone != null &&
                    EnvironmentDirector.Instance.ActiveZone.zoneId == EnvironmentZoneId.MandelaBridge
                        ? 4
                        : nextDistrictIndex;
                JunctionType junction = PickJunction(segmentIndex, district, false);
                bool faceBuildingsTowardRunner =
                    previousDressedSegmentWasJunction && district != 4;
                junctionVisuals?.SetType(junction);
                visuals.SetDistrict(district);

                // Decoration is data-driven and district-keyed; the director dresses
                // this tile from its socket layout. Purely visual – it never touches
                // obstacles, coins, power-ups, lanes or the road geometry.
                EnvironmentDecorDirector.Instance?.DecorateSegment(
                    segment.GetComponent<SegmentDecorator>(), district, junction,
                    faceBuildingsTowardRunner);

                nextDistrictIndex++;
                previousDressedSegmentWasJunction = junction != JunctionType.None;
            }
        }

        JunctionType PickJunction(int segmentIndex, int district, bool jacaranda)
        {
            if (junctionSettings == null || district == 4 ||
                segmentIndex < junctionSettings.openingSegmentCount ||
                (jacaranda && !junctionSettings.allowInJacarandaAvenue) ||
                straightSegmentsSinceJunction < junctionSettings.minimumStraightSegments)
            {
                IncrementStraightCount();
                return JunctionType.None;
            }

            if (!firstJunctionSpawned && junctionSettings.guaranteeFirstJunctionAfterOpening)
            {
                firstJunctionSpawned = true;
                lastJunctionType = JunctionType.Crossroad;
                straightSegmentsSinceJunction = 0;
                return JunctionType.Crossroad;
            }

            float total = junctionSettings.straightWeight +
                          junctionSettings.crossroadWeight +
                          junctionSettings.tJunctionWeight;
            if (total <= 0f)
            {
                IncrementStraightCount();
                return JunctionType.None;
            }

            var rng = new System.Random(unchecked(
                junctionSettings.deterministicSeed + segmentIndex * 73856093));
            float roll = (float)rng.NextDouble() * total;
            if (roll < junctionSettings.straightWeight)
            {
                IncrementStraightCount();
                return JunctionType.None;
            }

            JunctionType type = roll < junctionSettings.straightWeight +
                                      junctionSettings.crossroadWeight
                ? JunctionType.Crossroad
                : ((junctionOrdinal++ & 1) == 0 ? JunctionType.TLeft : JunctionType.TRight);
            if (type == lastJunctionType && type == JunctionType.Crossroad)
            {
                type = (junctionOrdinal++ & 1) == 0 ? JunctionType.TLeft : JunctionType.TRight;
            }

            lastJunctionType = type;
            firstJunctionSpawned = true;
            straightSegmentsSinceJunction = 0;
            return type;
        }

        void IncrementStraightCount()
        {
            if (straightSegmentsSinceJunction < int.MaxValue)
            {
                straightSegmentsSinceJunction++;
            }
        }

        bool IsJacarandaSegment(int segmentIndex)
        {
            if (segmentIndex < jacarandaFirstSegment)
            {
                return false;
            }

            int cycle = Mathf.Max(jacarandaLengthSegments + 1, jacarandaRepeatEverySegments);
            return (segmentIndex - jacarandaFirstSegment) % cycle < jacarandaLengthSegments;
        }
    }
}
