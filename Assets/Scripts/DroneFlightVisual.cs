using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Flying visual for the Drone Boost. While the drone is active the animator
    /// plays the looping Falling clip and the body is pitched forward into a
    /// dive so the clip reads as falling/soaring through the air rather than
    /// walking upright; a banking yaw leans him into lateral swipes.
    /// </summary>
    public class DroneFlightVisual : MonoBehaviour
    {
        [SerializeField] PowerUpManager powerUpManager;
        [SerializeField] Transform flightPivot;

        [Tooltip("Forward dive pitch while flying. The Falling clip alone looks like walking when upright (0); ~45 tips it into a convincing forward fall.")]
        [SerializeField] float proneDegrees = 45f;

        [Tooltip("Horizontal facing correction while flying. Use 0 to face oncoming taxis; use 180 if the character faces away.")]
        [SerializeField] float horizontalFacingOffset = 0f;

        [SerializeField] float blendSeconds = 0.35f;

        [Tooltip("Animator state played (looping) while the drone carries the runner.")]
        [SerializeField] string flightStateName = "Fall";

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
                        // Play the looping Falling clip at full speed so the
                        // runner actually animates in the air (the old flight
                        // pose froze a jump frame).
                        animator.speed = 1f;
                        animator.Play(flightStateName, 0, 0f);
                    }
                    else
                    {
                        // Hand control back to locomotion; the Run→Idle
                        // transition settles it if the run has stopped.
                        animator.speed = 1f;
                        animator.Play("Run", 0, 0f);
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
