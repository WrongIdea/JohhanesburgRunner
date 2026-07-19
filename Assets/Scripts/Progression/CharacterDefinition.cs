using UnityEngine;

namespace JoburgRunner.Progression
{
    public enum UnlockType
    {
        OwnedByDefault,
        CoinPurchase,
        MissionRequirement,
        AchievementRequirement,
    }

    /// <summary>
    /// Data description of a playable character. The existing in-scene
    /// character system (CharacterSelector + per-character visuals built by the
    /// scene builder) stays authoritative for the actual models; this SO layers
    /// unlock/ownership metadata on top so characters become data-driven and
    /// storable without moving the working visuals.
    ///
    /// Stat modifiers default to neutral — no pay-to-win by default.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Character Definition", fileName = "Character")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Tooltip("Index into the in-scene CharacterSelector visuals this definition maps to.")]
        public int visualIndex;

        [Header("Unlock")]
        public UnlockType unlockType = UnlockType.OwnedByDefault;
        [Min(0)] public int coinPrice;
        public MissionDefinition missionRequirement;
        public AchievementDefinition achievementRequirement;

        [Header("Presentation")]
        public Sprite icon;
        public GameObject previewModel;
        public RuntimeAnimatorController animatorController;

        [Header("Stat Modifiers (keep neutral — no pay-to-win)")]
        [Range(0.9f, 1.1f)] public float forwardSpeedModifier = 1f;
        [Range(0.9f, 1.5f)] public float coinMagnetModifier = 1f;
    }
}
