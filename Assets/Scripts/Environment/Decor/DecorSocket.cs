using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// A hand-placed anchor on a road tile where the runtime decorator may spawn
    /// a prop. Sockets carry no gameplay logic and never run per-frame code – they
    /// are pure markers that the <see cref="SegmentDecorator"/> reads once when a
    /// segment is (re)dressed. Editor Gizmos make the socket layout visible in the
    /// scene so placement can be audited without entering play mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DecorSocket : MonoBehaviour
    {
        [Tooltip("Which decoration category this socket anchors. The active district " +
                 "profile decides whether it actually spawns anything here.")]
        [SerializeField] DecorSocketType socketType = DecorSocketType.Tree;

        [Tooltip("Clearance radius (metres) used by the decorator's overlap test and " +
                 "by validation. A prop is skipped if another already sits within this " +
                 "radius, preventing trees/lamps/benches from colliding.")]
        [SerializeField, Min(0f)] float clearanceRadius = 1.2f;

        [Tooltip("Left/right side of the street this socket sits on. Lets the decorator " +
                 "keep a left/right lamp rhythm and offset trees onto the pavement.")]
        [SerializeField] StreetSide side = StreetSide.Auto;

        public DecorSocketType SocketType => socketType;
        public float ClearanceRadius => clearanceRadius;

        /// <summary>Resolved street side; Auto is derived from the socket's local X.</summary>
        public StreetSide Side => side == StreetSide.Auto
            ? (transform.localPosition.x < 0f ? StreetSide.Left : StreetSide.Right)
            : side;

        public enum StreetSide
        {
            /// <summary>Derive from the socket's local X sign.</summary>
            Auto,
            Left,
            Right
        }

#if UNITY_EDITOR
        // Colour key requested for the socket visualiser:
        //   Tree = green, Lamp = yellow, Bench/Bin = blue, TrafficLight = red,
        //   Billboard = magenta, BusStop = cyan.
        static Color ColorFor(DecorSocketType type) => type switch
        {
            DecorSocketType.Tree => new Color(0.2f, 0.85f, 0.25f),
            DecorSocketType.Lamp => new Color(0.95f, 0.85f, 0.15f),
            DecorSocketType.Bench => new Color(0.2f, 0.45f, 0.95f),
            DecorSocketType.Bin => new Color(0.35f, 0.6f, 0.95f),
            DecorSocketType.TrafficLight => new Color(0.95f, 0.2f, 0.2f),
            DecorSocketType.Billboard => new Color(0.9f, 0.25f, 0.85f),
            DecorSocketType.BusStop => new Color(0.2f, 0.85f, 0.85f),
            _ => Color.white
        };

        void OnDrawGizmos()
        {
            Color c = ColorFor(socketType);
            Gizmos.color = c;
            Gizmos.DrawSphere(transform.position, 0.22f);

            // Clearance / collision-exclusion zone.
            Gizmos.color = new Color(c.r, c.g, c.b, 0.35f);
            Gizmos.DrawWireSphere(transform.position, clearanceRadius);

            // A short up-stalk so sockets are easy to spot against the road.
            Gizmos.color = c;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.6f);
        }
#endif
    }
}
