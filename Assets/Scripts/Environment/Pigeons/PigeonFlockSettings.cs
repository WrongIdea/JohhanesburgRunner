using UnityEngine;

namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>
    /// Data-driven tuning for the whole pigeon system (Part 3 of the flock spec).
    /// Optional: <see cref="PigeonSpawner"/> works from its own serialized defaults
    /// when no asset is assigned, and copies these values over them when one is.
    /// One shared asset keeps designers out of the prefab and lets districts /
    /// weather be balanced without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "Joburg Runner/Pigeons/Flock Settings", fileName = "PigeonFlockSettings")]
    public sealed class PigeonFlockSettings : ScriptableObject
    {
        [Header("Ground behaviour")]
        public int minFlockSize = 3;
        public int maxFlockSize = 6;
        public float minSpacing = 0.4f;
        public float maxSpacing = 1.8f;
        public float groundRoamRadius = 1.6f;
        public Vector2 idleDuration = new Vector2(1.5f, 4f);
        public Vector2 walkDuration = new Vector2(1.0f, 2.5f);
        public Vector2 peckDuration = new Vector2(0.8f, 2.0f);
        public Vector2 alertDuration = new Vector2(0.2f, 0.6f);

        [Header("Player reaction")]
        public float triggerDistance = 15f;
        public float minTakeoffDelay = 0f;
        public float maxTakeoffDelay = 0.25f;

        [Header("Flight")]
        public float minSpeed = 5f;
        public float maxSpeed = 7f;
        public float minHeightGain = 3f;
        public float maxHeightGain = 5f;
        public float minFlightDuration = 4f;
        public float maxFlightDuration = 6f;
        [Range(0f, 1f)] public float landingChance = 0.3f;
        [Range(0f, 1f)] public float despawnChance = 0.7f;
        public float maxBankAngle = 28f;
        [Range(0f, 3f)] public float flightPathVariation = 1f;

        [Header("Spawning")]
        [Range(0f, 1f)] public float spawnChance = 0.3f;
        public float minEventSpacing = 80f;
        public float maxEventSpacing = 120f;
        public int maxActiveFlocks = 2;
        public float maxFlockDistanceFromPlayer = 90f;
        public float cullDistance = 45f;

        [Header("Vehicle reaction")]
        public float vehicleTriggerDistance = 8f;
        public float minScaryVehicleSpeed = 6f;
        public float hornReactionRadius = 22f;

        [Header("Weather multipliers")]
        [Range(0f, 1f)] public float rainMultiplier = 0.4f;
        [Range(0f, 1f)] public float heavyRainGroundMultiplier = 0f;
        public float overcastMultiplier = 1f;
        public float sunnyMultiplier = 1f;

        [Header("District multipliers (spec names, Part 8 tuning)")]
        public float cbd = 1.20f;
        public float taxiRank = 1.60f;
        public float busStop = 1.35f;
        public float mandelaBridge = 0.75f;
        public float sandton = 0.50f;
        public float residential = 0.65f;
        public float park = 1.40f;
        public float jacarandaAvenue = 1.10f;

        [Header("Cinematic events")]
        [Range(0f, 1f)] public float cinematicChance = 0.06f;
        public int cinematicMinSize = 8;
        public int cinematicMaxSize = 12;
        [Tooltip("Minimum metres between cinematic events (Part 10/21).")]
        public float cinematicMinSpacing = 500f;
        public float cinematicMaxSpacing = 800f;

        /// <summary>
        /// Maps the game's runtime district index (0 CBD, 1 Commissioner,
        /// 2 Park, 3 Business, 4 Mandela Bridge; -1 = none) onto the spec's named
        /// multipliers. Districts the game doesn't model yet (taxi rank / bus stop
        /// / Sandton / residential / Jacaranda) are reachable through
        /// <see cref="PigeonInterestArea"/> and landing points instead.
        /// </summary>
        public float DistrictMultiplier(int gameDistrict) => gameDistrict switch
        {
            0 => cbd,
            1 => busStop,       // Commissioner reads as a busy stop corridor
            2 => park,
            3 => sandton,       // Business ≈ Sandton's quieter pavements
            4 => mandelaBridge,
            _ => 1f,
        };
    }
}
