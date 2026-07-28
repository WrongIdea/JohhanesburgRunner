using System;
using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Data-driven decoration recipe for one district (e.g. Johannesburg CBD,
    /// Commissioner Street, Park District, Business District). Holds one
    /// <see cref="DecorRule"/> per socket type; the runtime decorator reads these
    /// to decide what to spawn at each socket. Tuning density, spacing, scale and
    /// probability here needs no code changes and no rebuild of the scene.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/District Decor Profile", fileName = "District_Decor")]
    public sealed class DistrictDecorProfile : ScriptableObject
    {
        [Tooltip("Human-readable district name, shown in validation logs.")]
        public string districtName = "District";

        [Tooltip("Rules keyed by socket type. Socket types with no rule here are " +
                 "left empty. Add one entry per decoration category you want to appear.")]
        public DecorRule[] rules = Array.Empty<DecorRule>();

        /// <summary>Finds the rule for a socket type, or null if this district leaves it empty.</summary>
        public DecorRule GetRule(DecorSocketType type)
        {
            if (rules == null)
            {
                return null;
            }

            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i] != null && rules[i].socketType == type)
                {
                    return rules[i];
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Per-socket-type spawning rule. Every field is designer-facing and tunable
    /// in the Inspector. The decorator applies these deterministically from the
    /// segment seed so a dressed tile stays stable until it is recycled.
    /// </summary>
    [Serializable]
    public sealed class DecorRule
    {
        [Tooltip("Which socket category this rule governs.")]
        public DecorSocketType socketType = DecorSocketType.Tree;

        [Header("Chance & count")]
        [Tooltip("Probability (0-1) that any given eligible socket of this type spawns a prop. " +
                 "1 = always, 0.7 = roughly 7 in 10 sockets. Lower values thin props out so " +
                 "the street never reads as a perfect row.")]
        [Range(0f, 1f)] public float spawnProbability = 0.7f;

        [Tooltip("Hard cap on how many props of this type a single 30 m tile may spawn, " +
                 "regardless of how many sockets exist. Keeps density predictable.")]
        [Min(0)] public int maxCountPerSegment = 4;

        [Header("Spacing (metres)")]
        [Tooltip("Minimum metres between two spawned props of this type on the same tile. " +
                 "Sockets closer than this to an already-placed prop are skipped.")]
        [Min(0f)] public float minSpacing = 12f;

        [Tooltip("Target upper spacing. Informational for validation and future jitter; " +
                 "the decorator will not place props denser than minSpacing.")]
        [Min(0f)] public float maxSpacing = 30f;

        [Header("Prefabs")]
        [Tooltip("Candidate prefabs. One is chosen at random per spawn, so several " +
                 "traffic-light or tree variants read as hand-placed rather than cloned.")]
        public GameObject[] allowedPrefabs = Array.Empty<GameObject>();

        [Tooltip("If false, the same prefab index will not be used twice in a row on a tile " +
                 "(when more than one variant exists), further breaking up repetition.")]
        public bool allowConsecutiveRepetition = true;

        [Header("Per-instance variation")]
        [Tooltip("Uniform scale range applied per instance (e.g. 0.9-1.15). Keeps a natural " +
                 "size spread. Set both to 1 to disable.")]
        public Vector2 scaleRange = new Vector2(0.9f, 1.15f);

        [Tooltip("Random yaw range in degrees applied per instance around the socket's own " +
                 "facing. Trees can use the full 0-360; traffic lights should stay near 0.")]
        public Vector2 yawJitterRange = new Vector2(0f, 360f);

        [Tooltip("Random sideways/forward jitter (metres) added to the socket position so " +
                 "props do not line up perfectly. X = across pavement, Y = along street.")]
        public Vector2 positionJitter = new Vector2(0.4f, 1.5f);

        [Tooltip("Extra sideways offset (metres) from the socket, pushed away from the road. " +
                 "Positive nudges the prop further onto the pavement.")]
        public float sidewalkOffset = 0f;

        [Header("Tint")]
        [Tooltip("Slight per-instance colour variation applied via MaterialPropertyBlock " +
                 "(shared material is preserved). Used e.g. to vary jacaranda flower colour. " +
                 "0 = no tint.")]
        [Range(0f, 0.5f)] public float tintVariation = 0f;

        [Header("Placement gates")]
        [Tooltip("If true, this prop only spawns when the tile is flagged an intersection. " +
                 "Traffic lights must set this so they never appear on straight tiles.")]
        public bool requiresIntersection = false;

        [Tooltip("Collision radius (metres) this prop occupies, tested against other props " +
                 "already placed on the tile to prevent overlap (e.g. a lamp beside a trunk).")]
        [Min(0f)] public float collisionRadius = 1.0f;
    }
}
