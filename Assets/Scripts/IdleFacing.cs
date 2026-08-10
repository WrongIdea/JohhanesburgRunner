using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Turns the runner around to face the camera during the pre-run idle
    /// showcase, then restores the forward (down-the-road) facing the moment
    /// the run starts. The flip is applied at runtime only — the model's rest
    /// rotation stays forward so the roll and death visuals restore correctly.
    /// </summary>
    public class IdleFacing : MonoBehaviour
    {
        [SerializeField] Transform visual;
        [SerializeField] GameManager gameManager;
        [SerializeField] float idleYaw = 180f;

        Quaternion restRotation;
        bool restored;

        void Start()
        {
            if (visual == null)
            {
                visual = transform;
            }

            restRotation = visual.localRotation;
        }

        void LateUpdate()
        {
            if (visual == null || gameManager == null)
            {
                return;
            }

            if (!gameManager.HasStarted)
            {
                // Face the camera while dancing on the menu.
                visual.localRotation = restRotation * Quaternion.Euler(0f, idleYaw, 0f);
                restored = false;
            }
            else if (!restored)
            {
                // Run started: turn back to face down the road, once.
                visual.localRotation = restRotation;
                restored = true;
            }
        }
    }
}
