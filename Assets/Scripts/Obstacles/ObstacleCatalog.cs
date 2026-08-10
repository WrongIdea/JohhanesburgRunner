using System.Collections.Generic;
using JoburgRunner.Environment;
using JoburgRunner.VFX;
using UnityEngine;

namespace JoburgRunner.Obstacles
{
    /// <summary>
    /// The pool of obstacle definitions and authored patterns a run can draw
    /// from, plus fair, progression-gated selection. Mirrors the guarantees the
    /// existing ChunkManager already enforces (a survivable path always exists,
    /// gradual introduction of jump/roll/moving obstacles, reduced repeats) but
    /// as reusable data + helpers so both authored and procedural generation
    /// share one fairness definition.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Obstacle Catalog", fileName = "ObstacleCatalog")]
    public sealed class ObstacleCatalog : ScriptableObject
    {
        public ObstacleDefinition[] obstacles = new ObstacleDefinition[0];
        public ObstaclePattern[] patterns = new ObstaclePattern[0];

        [Tooltip("Avoid re-selecting this many of the most-recent obstacles.")]
        [Min(0)] public int avoidRecentObstacles = 2;

        readonly List<ObstacleDefinition> scratch = new List<ObstacleDefinition>(32);

        /// <summary>
        /// Weighted pick of an eligible obstacle for the current run state.
        /// Filters by progression, speed and zone, then honours no-repeat via
        /// the supplied picker. Returns null when nothing qualifies (caller
        /// should fall back to an empty/safe stretch).
        /// </summary>
        public ObstacleDefinition Pick(float progression, float speed, EnvironmentZoneId zone, NoRepeatPicker picker)
        {
            scratch.Clear();
            float totalWeight = 0f;
            foreach (ObstacleDefinition def in obstacles)
            {
                if (def != null && def.prefab != null && def.EligibleAt(progression, speed) && def.AllowedInZone(zone))
                {
                    scratch.Add(def);
                    totalWeight += Mathf.Max(0f, def.spawnWeight);
                }
            }

            if (scratch.Count == 0 || totalWeight <= 0f)
            {
                return null;
            }

            // No-repeat first; fall back to plain weighted if the picker keeps
            // landing on filtered-out indices.
            for (int attempt = 0; attempt < 6; attempt++)
            {
                int index = picker.Next(scratch.Count);
                if (index >= 0)
                {
                    return scratch[index];
                }
            }

            float pick = Random.value * totalWeight;
            foreach (ObstacleDefinition def in scratch)
            {
                pick -= Mathf.Max(0f, def.spawnWeight);
                if (pick <= 0f)
                {
                    return def;
                }
            }

            return scratch[scratch.Count - 1];
        }

        /// <summary>Picks a survivable, eligible authored pattern, or null.</summary>
        public ObstaclePattern PickPattern(float progression, float speed, EnvironmentZoneId zone)
        {
            float totalWeight = 0f;
            foreach (ObstaclePattern pattern in patterns)
            {
                if (IsPatternEligible(pattern, progression, speed, zone))
                {
                    totalWeight += pattern.spawnProbability;
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float pick = Random.value * totalWeight;
            foreach (ObstaclePattern pattern in patterns)
            {
                if (!IsPatternEligible(pattern, progression, speed, zone))
                {
                    continue;
                }

                pick -= pattern.spawnProbability;
                if (pick <= 0f)
                {
                    return pattern;
                }
            }

            return null;
        }

        static bool IsPatternEligible(ObstaclePattern pattern, float progression, float speed, EnvironmentZoneId zone) =>
            pattern != null &&
            progression >= pattern.minPlayerProgression &&
            speed >= pattern.minSpawnSpeed &&
            pattern.AllowedInZone(zone) &&
            pattern.IsSurvivable();

        /// <summary>
        /// Reusable fairness gate for procedural placement: given the lanes the
        /// previous obstacle row left open and the lanes a candidate row would
        /// block, returns true only if a reachable open lane remains. Lets the
        /// spawner reject impossible combinations before committing them.
        /// </summary>
        public static bool LeavesEscapePath(int previousOpenLanes, int candidateBlockedLanes)
        {
            int open = (~candidateBlockedLanes) & 0b111;
            if (open == 0)
            {
                return false;
            }

            for (int from = 0; from < 3; from++)
            {
                if ((previousOpenLanes & (1 << from)) == 0)
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
