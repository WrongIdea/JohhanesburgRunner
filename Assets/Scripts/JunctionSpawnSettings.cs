using UnityEngine;

namespace JoburgRunner
{
    [CreateAssetMenu(menuName = "Jozi Runner/Junction Spawn Settings", fileName = "JunctionSpawnSettings")]
    public sealed class JunctionSpawnSettings : ScriptableObject
    {
        [Min(0f)] public float straightWeight = 70f;
        [Min(0f)] public float crossroadWeight = 20f;
        [Min(0f)] public float tJunctionWeight = 10f;
        [Min(1)] public int minimumStraightSegments = 4;
        [Min(0)] public int openingSegmentCount = 9;
        [Tooltip("Guarantee a visible crossroad on the first eligible segment after the opening.")]
        public bool guaranteeFirstJunctionAfterOpening = true;
        public bool allowInJacarandaAvenue;
        public int deterministicSeed = 2407;
    }
}
