using UnityEngine;

namespace JoburgRunner.VFX
{
    /// <summary>
    /// The gameplay moment a <see cref="VFXDefinition"/> binds to. The central
    /// <see cref="JoburgRunner.GameEvents"/> hub is the single source of these
    /// moments; <see cref="VFXManager"/> translates its events into these
    /// presentation triggers. Kept as a flat enum so definitions reference a
    /// trigger in the Inspector with no code dependency on gameplay scripts.
    /// </summary>
    public enum GameplayVFXTrigger
    {
        CoinCollected,
        RareCoinCollected,
        Jump,
        Land,
        Roll,
        LaneSwitch,
        PerfectDodge,
        PowerUpStart,
        PowerUpActive,
        PowerUpWarning,
        PowerUpEnd,
        Crash,
    }

    /// <summary>
    /// Context for a presentation trigger. Only the fields relevant to a
    /// trigger are set; the rest stay at their defaults.
    /// </summary>
    public struct GameplayVFXContext
    {
        public Vector3 position;
        /// <summary>Optional transform the effect should follow (feet, player root…).</summary>
        public Transform follow;
        /// <summary>-1 / +1 for directional effects (lane switch), else 0.</summary>
        public float direction;
        /// <summary>Coin/score amount for floating-value effects.</summary>
        public int amount;
        /// <summary>Power-up this trigger relates to, when applicable.</summary>
        public PowerUpType powerUp;
    }
}
