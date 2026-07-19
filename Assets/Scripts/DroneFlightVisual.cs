using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Cosmetic flying pose for the Drone Boost. While the drone is active the
    /// animator is frozen on a mid-jump frame and the character is pitched prone
    /// so he reads as flying. A horizontal facing offset can flip the character
    /// back-to-front without rotating the Player root.
    /// </summary>
    public class DroneFlightVisual : MonoBehaviour
    {
        [SerializeField] PowerUpManager powerUpManager;
        [SerializeField] Transform flightPivot;

        [Tooltip("Forward pitch while flying; 90 would be perfectly flat.")]
        [SerializeField] float proneDegrees = 78f;

        [Tooltip("Horizontal facing correction while flying. Use 0 to face oncoming taxis; use 180 if the character faces away.")]
        [SerializeField] float horizontalFacingOffset = 0f;

        [SerializeField] float blendSeconds = 0.35f;

        [Tooltip("Normalized time into the jump clip whose frame is held as the flying pose.")]
        [SerializeField] float flightPoseFrame = 0.4f;

        [Tooltip("Yaw per unit of lateral speed; face swings away from the swipe direction.")]
        [SerializeField] float yawDegreesPerLateralMeterPerSecond = 3.2f;

        [SerializeField] float maxYawDegrees = 40f;
        [SerializeField] float yawSmoothing = 8f;

        float blend;
        bool wasFlying;
        Animator animator;
        float previousX;
        float currentYaw;

        void Awake()
        {
            // Active-only lookup: with selectable characters the player carries
            // one visual per character and only the selected one is active —
            // an include-inactive search could freeze the wrong animator.
            animator = GetComponentInChildren<Animator>();
            previousX = transform.position.x;
        }

        void LateUpdate()
        {
            if (flightPivot == null)
            {
                return;
            }

            bool flying = powerUpManager != null && powerUpManager.DroneActive;

            if (flying != wasFlying)
            {
                wasFlying = flying;

                if (animator != null)
                {
                    if (flying)
                    {
                        animator.Play("Jump", 0, flightPoseFrame);
                        animator.Update(0f);
                        animator.speed = 0f;
                    }
                    else
                    {
                        animator.speed = 1f;
                    }
                }
            }

            float lateralSpeed = Time.deltaTime > 0f
                ? (transform.position.x - previousX) / Time.deltaTime
                : 0f;

            previousX = transform.position.x;

            float targetBlend = flying ? 1f : 0f;

            blend = Mathf.MoveTowards(
                blend,
                targetBlend,
                Time.deltaTime / Mathf.Max(0.01f, blendSeconds)
            );

            float targetYaw = flying
                ? Mathf.Clamp(
                    -lateralSpeed * yawDegreesPerLateralMeterPerSecond,
                    -maxYawDegrees,
                    maxYawDegrees
                )
                : 0f;

            float yawBlend = 1f - Mathf.Exp(-yawSmoothing * Time.deltaTime);
            currentYaw = Mathf.Lerp(currentYaw, targetYaw, yawBlend);

            Quaternion facingRotation = Quaternion.AngleAxis(horizontalFacingOffset * blend, Vector3.up);
            Quaternion proneRotation = Quaternion.AngleAxis(proneDegrees * blend, Vector3.right);
            Quaternion bankingYaw = Quaternion.AngleAxis(currentYaw * blend, Vector3.up);

            flightPivot.localRotation = facingRotation * proneRotation * bankingYaw;
        }

        void OnDisable()
        {
            if (animator != null)
            {
                animator.speed = 1f;
            }

            if (flightPivot != null)
            {
                flightPivot.localRotation = Quaternion.identity;
            }

            blend = 0f;
            currentYaw = 0f;
            wasFlying = false;
        }
    }
}
