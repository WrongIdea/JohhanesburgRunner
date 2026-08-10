using JoburgRunner.Environment;
using UnityEngine;

namespace JoburgRunner.Obstacles
{
    /// <summary>
    /// An authored obstacle arrangement: a sequence of elements placed at
    /// increasing distances down the track, each occupying some lanes and
    /// demanding an action. Complements procedural generation — the spawner can
    /// drop a whole pattern for a hand-tuned moment, then return to random.
    ///
    /// Patterns are validated for fairness by <see cref="IsSurvivable"/>: every
    /// distance slice must leave at least one lane open, and consecutive slices
    /// must be laterally reachable (you can only move one lane at a time).
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Obstacle Pattern", fileName = "Pattern")]
    public sealed class ObstaclePattern : ScriptableObject
    {
        [System.Serializable]
        public struct Element
        {
            [Tooltip("Metres from the pattern start where this element sits.")]
            [Min(0f)] public float relativeDistance;
            [Tooltip("Lanes occupied/blocked here. Bit 0=left,1=centre,2=right.")]
            [Range(1, 7)] public int laneMask;
            public RequiredAction requiredAction;
            [Tooltip("Optional specific obstacle; leave null to let the spawner pick by category/weight.")]
            public ObstacleDefinition obstacle;
            public ObstacleCategory categoryHint;
        }

        public Element[] elements = new Element[0];

        [Header("Gating")]
        [Range(0f, 1f)] public float difficultyRating = 0.5f;
        [Min(0f)] public float minSpawnSpeed = 0f;
        [Min(0f)] public float minPlayerProgression = 0f;
        [Min(0f)] public float spawnProbability = 1f;
        [Tooltip("Zones this pattern is allowed in (empty = all).")]
        public EnvironmentZoneId[] allowedZones = new EnvironmentZoneId[0];

        [Tooltip("Total length of the pattern in metres (for spacing the next spawn).")]
        [Min(1f)] public float length = 24f;

        public bool AllowedInZone(EnvironmentZoneId zone)
        {
            if (allowedZones == null || allowedZones.Length == 0)
            {
                return true;
            }

            foreach (EnvironmentZoneId z in allowedZones)
            {
                if (z == zone)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True when the pattern can be cleared: each slice leaves an open lane
        /// and the open lanes of consecutive slices are within one lane-step of
        /// each other. Impossible patterns are rejected before ever spawning.
        /// </summary>
        public bool IsSurvivable()
        {
            if (elements == null || elements.Length == 0)
            {
                return true;
            }

            int previousOpen = 0b111;
            // Elements are assumed authored in distance order; sort defensively.
            System.Array.Sort(elements, (a, b) => a.relativeDistance.CompareTo(b.relativeDistance));

            float sliceStart = elements[0].relativeDistance;
            int sliceBlocked = 0;
            foreach (Element element in elements)
            {
                bool newSlice = element.relativeDistance - sliceStart > 0.5f;
                if (newSlice)
                {
                    int open = (~sliceBlocked) & 0b111;
                    if (!SliceReachable(previousOpen, open))
                    {
                        return false;
                    }

                    previousOpen = open;
                    sliceStart = element.relativeDistance;
                    sliceBlocked = 0;
                }

                sliceBlocked |= element.laneMask;
            }

            int lastOpen = (~sliceBlocked) & 0b111;
            return SliceReachable(previousOpen, lastOpen);
        }

        // A slice is survivable if at least one lane is open and reachable from
        // any lane that was open in the previous slice (one lane-step apart).
        static bool SliceReachable(int previousOpen, int open)
        {
            if (open == 0)
            {
                return false; // fully blocked — impossible
            }

            for (int from = 0; from < 3; from++)
            {
                if ((previousOpen & (1 << from)) == 0)
                {
                    continue;
                }

                for (int to = 0; to < 3; to++)
                {
                    if ((open & (1 << to)) != 0 && Mathf.Abs(from - to) <= 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
