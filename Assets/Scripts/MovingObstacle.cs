using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Attach this to moving obstacles, such as yellow minibus taxis.
    /// They move toward the player by travelling backwards along the Z-axis.
    /// </summary>
    public class MovingObstacle : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 7f;
        [Tooltip("Stays parked until the player is this close, so chunk taxis hold their designed lane instead of drifting into earlier chunks.")]
        [SerializeField] float activationDistance = 70f;
        bool isStopped;
        GameManager gameManager;
        Transform player;

        public bool IsStopped => isStopped;

        public void StopMoving()
        {
            isStopped = true;
        }

        /// <summary>Clears the crash-stop when a pooled chunk is recycled.</summary>
        public void ResetMotion()
        {
            isStopped = false;
        }

        void Start()
        {
            gameManager = FindAnyObjectByType<GameManager>();
            PlayerController controller = FindAnyObjectByType<PlayerController>();
            player = controller != null ? controller.transform : null;
        }

        void Update()
        {
            if (isStopped || (gameManager != null && !gameManager.IsRunning))
            {
                return;
            }

            if (player != null && transform.position.z - player.position.z > activationDistance)
            {
                return;
            }

            transform.Translate(Vector3.back * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
