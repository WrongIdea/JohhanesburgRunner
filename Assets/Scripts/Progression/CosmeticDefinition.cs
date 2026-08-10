using UnityEngine;

namespace JoburgRunner.Progression
{
    /// <summary>Cosmetic attachment slots. Each character exposes matching anchor points.</summary>
    public enum CosmeticSlot
    {
        Hat,
        Outfit,
        Shoes,
        Backpack,
        Trail,
        UbuntuPulseColour,
        BoardSkin,
    }

    /// <summary>
    /// A cosmetic item. Attaches to a named anchor on the character at runtime
    /// (never modifies the source character prefab — the equip system
    /// instantiates <see cref="attachmentPrefab"/> under the slot's anchor and
    /// removes it on unequip). Trail/colour cosmetics carry a colour instead of
    /// a prefab.
    /// </summary>
    [CreateAssetMenu(menuName = "Jozi Runner/Cosmetic Definition", fileName = "Cosmetic")]
    public sealed class CosmeticDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public CosmeticSlot slot;

        [Header("Attachment")]
        [Tooltip("Prefab instantiated under the slot's anchor (hats/outfits/boards). Null for colour-only cosmetics.")]
        public GameObject attachmentPrefab;
        [Tooltip("Anchor transform name on the character rig, e.g. \"Head\", \"Back\".")]
        public string anchorName;
        [Tooltip("Colour for Trail / Ubuntu Pulse / board-glow cosmetics.")]
        public Color tint = Color.white;

        [Header("Unlock")]
        public UnlockType unlockType = UnlockType.CoinPurchase;
        [Min(0)] public int coinPrice = 500;

        [Header("Presentation")]
        public Sprite icon;
    }
}
