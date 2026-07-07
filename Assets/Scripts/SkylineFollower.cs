using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Keeps the distant city skyline (Hillbrow Tower and high-rises) a fixed
    /// distance ahead of the player so it always sits on the horizon, matching
    /// the reference footage where the city never gets closer.
    /// </summary>
    public class SkylineFollower : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] float forwardOffset = 140f;

        void LateUpdate()
        {
            if (player == null)
            {
                return;
            }

            transform.position = new Vector3(0f, 0f, player.position.z + forwardOffset);
        }
    }
}
