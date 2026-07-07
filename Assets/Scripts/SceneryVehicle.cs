using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Marker carrying the signed forward speed of a decorative traffic vehicle.
    /// Movement is driven by SceneryTraffic; positive speed travels with the
    /// player, negative speed is oncoming traffic.
    /// </summary>
    public class SceneryVehicle : MonoBehaviour
    {
        public float Speed;
    }
}
