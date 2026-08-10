using UnityEngine;

namespace JoburgRunner
{
    public enum JunctionDecorSocketType
    {
        TrafficLight,
        PedestrianTrafficLight,
        StreetLight,
        RoadSign,
        UtilityBox,
        Pedestrian,
        Vehicle,
        DeliveryVan,
        Vendor,
        BusStop,
        Bench,
        Bin,
        Bollard,
        Planter,
        JacarandaTree,
        Barrier
    }

    /// <summary>Visual-only future decoration marker; never creates gameplay content.</summary>
    public sealed class JunctionDecorSocket : MonoBehaviour
    {
        [SerializeField] JunctionDecorSocketType socketType;
        public JunctionDecorSocketType SocketType => socketType;

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = socketType switch
            {
                JunctionDecorSocketType.TrafficLight => Color.red,
                JunctionDecorSocketType.PedestrianTrafficLight => new Color(1f, 0.35f, 0.2f),
                JunctionDecorSocketType.StreetLight => Color.yellow,
                JunctionDecorSocketType.RoadSign => Color.blue,
                JunctionDecorSocketType.UtilityBox => Color.gray,
                JunctionDecorSocketType.Pedestrian => Color.cyan,
                JunctionDecorSocketType.Vehicle => Color.yellow,
                JunctionDecorSocketType.DeliveryVan => new Color(1f, 0.55f, 0.1f),
                JunctionDecorSocketType.Vendor => Color.magenta,
                JunctionDecorSocketType.JacarandaTree => new Color(0.65f, 0.25f, 1f),
                _ => Color.white
            };
            Gizmos.DrawWireSphere(transform.position, 0.35f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up);
        }
#endif
    }
}
