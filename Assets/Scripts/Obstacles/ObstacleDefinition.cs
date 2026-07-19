using JoburgRunner.Environment;
using JoburgRunner.VFX;
using UnityEngine;

namespace JoburgRunner.Obstacles
{
    /// <summary>Johannesburg obstacle families the runner can meet.</summary>
    public enum ObstacleCategory
    {
        StationaryTaxi,
        MovingTaxi,
        ParkedCar,
        MovingCar,
        Bus,
        DeliveryVehicle,
        ConstructionBarricade,
        ConcreteBarrier,
        TrafficConeGroup,
        Pothole,
        RoadworksTrench,
        VendorStall,
        RoadSign,
        BrokenDownVehicle,
        TaxiRankQueue,
        LowJumpObstacle,
        HighRollObstacle,
        WideDoubleLane,
        LaneChangingVehicle,
    }

    /// <summary>What the player must do to survive this obstacle.</summary>
    public enum RequiredAction
    {
        Avoid,   // switch out of its lane(s)
        Jump,
        Roll,
        None,    // decorative / non-blocking
    }

    /// <summary>
    /// Movement personalities, mirroring what <c>MovingObstacle</c> already
    /// supports so definitions map onto real behaviour instead of new code.
    /// </summary>
    public enum MovementBehaviour
    {
        Static,
        DriveForward,
        DriveTowardPlayer,
        ChangeLaneOnce,
        ChangeLanesPeriodically,
        BrakeAndStop,
        Accelerate,
        StopAndStart,
    }

    /// <summary>
    /// Inspector-authored description of one obstacle: its prefab, how it must
    /// be avoided, where and how fast it may spawn, how far apart it needs to
    /// be from its neighbours, and how it moves. Pure data consumed by the
    /// obstacle spawner / chunk system; the fairness rules in
    /// <see cref="ObstacleCatalog"/> use the lane + action fields to guarantee a
    /// survivable path.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Obstacle Definition", fileName = "Obstacle")]
    public sealed class ObstacleDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName;
        public ObstacleCategory category;
        public GameObject prefab;

        [Header("Placement")]
        [Tooltip("Lanes this obstacle may occupy. Bit 0=left, 1=centre, 2=right.")]
        [Range(1, 7)] public int allowedLanes = 0b111;
        [Tooltip("Lanes the obstacle BLOCKS when placed (usually its own lane; wide obstacles block more).")]
        [Range(1, 7)] public int blockedLanes = 0b001;
        public RequiredAction requiredAction = RequiredAction.Avoid;

        [Header("Spawn Gating")]
        [Tooltip("Metres the player must have run before this obstacle can appear (teaches jump/roll/moving gradually).")]
        [Min(0f)] public float minPlayerProgression = 0f;
        [Min(0f)] public float minSpawnSpeed = 0f;
        [Min(0f)] public float maxSpawnSpeed = 999f;
        [Min(0f)] public float spawnWeight = 1f;
        [Tooltip("Zones this obstacle is allowed in (empty = all zones).")]
        public EnvironmentZoneId[] zoneRestrictions = new EnvironmentZoneId[0];

        [Header("Footprint")]
        [Tooltip("Clear metres required ahead of the player before the next obstacle — the reaction window.")]
        [Min(0f)] public float safeGap = 12f;
        [Min(0.1f)] public float width = 2.4f;
        [Min(0.1f)] public float height = 2.2f;

        [Header("Movement")]
        public MovementBehaviour movement = MovementBehaviour.Static;
        public bool indicatorBlink;
        public bool steeringAnimation;

        [Header("Reward")]
        public int scoreReward = 0;
        public bool perfectDodgeEligible = true;

        [Header("Presentation (optional)")]
        public AudioClip approachAudio;
        public VFXDefinition spawnVfx;
        public VFXDefinition destroyVfx;

        public bool AllowedInZone(EnvironmentZoneId zone)
        {
            if (zoneRestrictions == null || zoneRestrictions.Length == 0)
            {
                return true;
            }

            foreach (EnvironmentZoneId allowed in zoneRestrictions)
            {
                if (allowed == zone)
                {
                    return true;
                }
            }

            return false;
        }

        public bool EligibleAt(float progression, float speed) =>
            progression >= minPlayerProgression && speed >= minSpawnSpeed && speed <= maxSpawnSpeed;
    }
}
