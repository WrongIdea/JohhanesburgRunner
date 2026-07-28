using System;
using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Which street side(s) a hero building may occupy. Flags so a single entry can
    /// support Left, Right, or Both — and so a future rare "two-sided showcase" mode
    /// can be added without changing the data model.
    /// </summary>
    [Flags]
    public enum HeroSide
    {
        None = 0,
        Left = 1,
        Right = 2,
        Both = Left | Right
    }

    /// <summary>
    /// Data-driven catalogue of roadside hero buildings. Adding a new landmark is a
    /// single array entry — the spawn system (director + decorator) reads this at
    /// runtime, so the design scales to 20+ buildings with no code changes.
    ///
    /// Hero buildings are spawned only on eligible CBD segments, sparsely, one per
    /// segment, via weighted no-repeat selection. See <see cref="EnvironmentDecorDirector"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Hero Building Set", fileName = "HeroBuildingSet")]
    public sealed class HeroBuildingSet : ScriptableObject
    {
        [Tooltip("One entry per hero building. Disabled or null-prefab entries are ignored.")]
        public HeroEntry[] entries = Array.Empty<HeroEntry>();

        public int Count => entries != null ? entries.Length : 0;

        /// <summary>True if any enabled entry with a prefab can occupy the given side.</summary>
        public bool HasEntryForSide(HeroSide side)
        {
            if (entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                HeroEntry e = entries[i];
                if (e != null && e.enabled && e.prefab != null && e.SupportsSide(side))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// One hero building and how it is placed relative to a hero socket. All fields
    /// are designer-facing; offsets/rotation/scale let the same prefab be positioned
    /// per side without editing the source prefab.
    /// </summary>
    [Serializable]
    public sealed class HeroEntry
    {
        [Tooltip("Label for logs / Inspector readability.")]
        public string label = "Hero";

        [Tooltip("The building prefab (keeps its own LODGroup). Non-interactive visual.")]
        public GameObject prefab;

        [Tooltip("Relative selection weight among eligible entries. Higher = more frequent.")]
        [Min(0f)] public float weight = 1f;

        [Tooltip("Which street side(s) this building may occupy. Both = eligible on either side.")]
        public HeroSide allowedSides = HeroSide.Both;

        [Tooltip("Position offset from the hero socket, in the socket's local space " +
                 "(socket already faces the road). Tune so the façade clears the pavement.")]
        public Vector3 localPositionOffset = Vector3.zero;

        [Tooltip("Rotation offset (Euler degrees) added on top of the socket's road-facing " +
                 "orientation. Leave 0 to face straight at the road.")]
        public Vector3 eulerOffset = Vector3.zero;

        [Tooltip("Extra yaw (degrees) that angles the façade toward the oncoming player, " +
                 "applied with the correct sign per side (left/right both turn to face the " +
                 "camera). 0 = face straight at the road; ~40 = a clear 3/4 view.")]
        [Range(0f, 80f)] public float facePlayerDegrees = 0f;

        [Tooltip("Uniform scale applied to the instance. Imported prefabs are authored at 1.")]
        [Min(0.01f)] public float uniformScale = 1f;

        [Tooltip("Ground half-extent (metres) of the building footprint. Used to decide which " +
                 "placeholders it overlaps and to keep nearby props clear of the façade.")]
        [Min(0f)] public float footprintRadius = 9f;

        [Tooltip("Optional: minimum metres of travel before this SAME building may reappear. " +
                 "0 = no distance constraint (the no-repeat history still prevents back-to-back).")]
        [Min(0f)] public float minRepeatDistance = 0f;

        [Tooltip("Master on/off for this entry without deleting it.")]
        public bool enabled = true;

        public bool SupportsSide(HeroSide side) => (allowedSides & side) != 0;
    }
}
