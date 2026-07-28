using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Sits alongside a <see cref="DecorSocket"/> of type
    /// <see cref="DecorSocketType.HeroBuilding"/> and links it to the placeholder
    /// group on the same street side. The decorator, when it spawns a hero here,
    /// hides that group's overlapping placeholders and restores them on release.
    /// Pure data holder — no per-frame code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroBuildingSocket : MonoBehaviour
    {
        [Tooltip("Placeholder buildings on this socket's side. Overlapping ones are " +
                 "hidden while a hero occupies this socket.")]
        [SerializeField] HeroPlaceholderGroup placeholderGroup;

        public HeroPlaceholderGroup PlaceholderGroup => placeholderGroup;
    }
}
