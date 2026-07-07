using UnityEngine;
using UnityEngine.InputSystem;

namespace JoburgRunner
{
    /// <summary>
    /// Three-lane endless runner controller.
    /// The player always moves forward on Z, can switch lanes on X, jump on Y,
    /// and can roll under low obstacles through the RollController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Lane Settings")]
        [Tooltip("Fixed X positions for Left, Center, Right lanes.")]
        [SerializeField] float[] laneXPositions = { -2.7f, 0f, 2.7f };
        [Tooltip("Seconds to settle into the target lane; ease-in/ease-out, no first-frame jerk.")]
        [SerializeField] float laneChangeSmoothTime = 0.12f;

        [Header("Forward Running")]
        [SerializeField] float forwardSpeed = 9f;
        [SerializeField] float maxForwardSpeed = 16f;
        [SerializeField] float speedIncreasePer100Meters = 0.8f;

        [Header("Jumping")]
        [SerializeField] float jumpHeight = 2.2f;
        [SerializeField] float gravity = -28f;

        [Header("Swipe Input")]
        [SerializeField] float minimumSwipeDistance = 80f;

        [Header("References")]
        [SerializeField] GameManager gameManager;
        [SerializeField] PowerUpManager powerUpManager;
        [SerializeField] RollController rollController;
        [SerializeField] PlayerAnimator playerAnimator;
        [SerializeField] PlayerDeathVisual deathVisual;
        [SerializeField] float droneClimbSpeed = 6f;

        CharacterController controller;
        int currentLane = 1;
        float lateralVelocity;
        float verticalVelocity;
        Vector2 touchStartPosition;
        bool isTouching;
        float startZ;
        float lastGroundedTime;

        public float CurrentForwardSpeed { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (rollController == null)
            {
                rollController = GetComponent<RollController>();
            }

            if (playerAnimator == null)
            {
                playerAnimator = GetComponent<PlayerAnimator>();
            }

            if (deathVisual == null)
            {
                deathVisual = GetComponentInChildren<PlayerDeathVisual>();
            }

            startZ = transform.position.z;
            CurrentForwardSpeed = forwardSpeed;

            // Start in the center lane even if the object was placed slightly off-grid.
            currentLane = Mathf.Clamp(currentLane, 0, laneXPositions.Length - 1);
            transform.position = new Vector3(laneXPositions[currentLane], transform.position.y, transform.position.z);
        }

        void Update()
        {
            if (gameManager != null && !gameManager.IsRunning)
            {
                return;
            }

            ReadKeyboardInput();
            ReadSwipeInput();
            MovePlayer();
        }

        public void MoveLeft()
        {
            currentLane = Mathf.Max(0, currentLane - 1);
        }

        public void MoveRight()
        {
            currentLane = Mathf.Min(laneXPositions.Length - 1, currentLane + 1);
        }

        public void Jump()
        {
            bool groundedStable = controller.isGrounded || Time.time - lastGroundedTime < 0.15f;
            if (groundedStable && (rollController == null || !rollController.IsRolling))
            {
                float height = jumpHeight * (powerUpManager != null ? powerUpManager.JumpMultiplier : 1f);
                verticalVelocity = Mathf.Sqrt(height * -2f * gravity);
                playerAnimator?.SetJumping(true);
            }
        }

        public void Slide()
        {
            rollController?.TryStartRoll();
        }

        void ReadKeyboardInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                MoveLeft();
            }

            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                MoveRight();
            }

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
            {
                Jump();
            }

            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                Slide();
            }
        }

        void ReadSwipeInput()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            bool pressed = touchscreen.primaryTouch.press.isPressed;

            if (pressed && !isTouching)
            {
                isTouching = true;
                touchStartPosition = touchscreen.primaryTouch.position.ReadValue();
                return;
            }

            if (pressed || !isTouching)
            {
                return;
            }

            isTouching = false;
            Vector2 touchEndPosition = touchscreen.primaryTouch.position.ReadValue();
            Vector2 swipeDelta = touchEndPosition - touchStartPosition;

            if (swipeDelta.magnitude < minimumSwipeDistance)
            {
                return;
            }

            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                if (swipeDelta.x > 0f)
                {
                    MoveRight();
                }
                else
                {
                    MoveLeft();
                }
            }
            else if (swipeDelta.y > 0f)
            {
                Jump();
            }
            else
            {
                Slide();
            }
        }

        void MovePlayer()
        {
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                // Strong downward stick keeps ground contact steady while
                // sliding sideways between lanes.
                verticalVelocity = -6f;
                playerAnimator?.SetJumping(false);
            }

            verticalVelocity += gravity * Time.deltaTime;

            float yMovement = verticalVelocity * Time.deltaTime;
            if (powerUpManager != null && powerUpManager.DroneActive)
            {
                // Drone Boost: hold a cruising height above the traffic.
                verticalVelocity = 0f;
                float targetY = powerUpManager.DroneFlightHeight;
                yMovement = Mathf.MoveTowards(transform.position.y, targetY, droneClimbSpeed * Time.deltaTime)
                    - transform.position.y;
            }

            float distance = Mathf.Max(0f, transform.position.z - startZ);
            CurrentForwardSpeed = Mathf.Min(maxForwardSpeed, forwardSpeed + distance / 100f * speedIncreasePer100Meters);
            float targetX = laneXPositions[currentLane];
            // SmoothDamp eases in and out of the lane slide; the old
            // exponential lerp jumped ~20% of the gap on the first frame,
            // which read as the character darting sideways.
            float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref lateralVelocity, laneChangeSmoothTime);
            float xMovement = newX - transform.position.x;

            Vector3 movement = new Vector3(
                xMovement,
                yMovement,
                CurrentForwardSpeed * Time.deltaTime);

            controller.Move(movement);

            // isGrounded flickers for a frame while Move() slides laterally;
            // feed the animator a debounced value so the run pose does not
            // strobe into the jump/idle pose during lane changes.
            if (controller.isGrounded)
            {
                lastGroundedTime = Time.time;
            }

            bool groundedStable = Time.time - lastGroundedTime < 0.15f;
            playerAnimator?.SetGrounded(groundedStable);
            playerAnimator?.SetRunning(groundedStable);
        }

        /// <summary>
        /// After a paid continue: stand the runner back up in the middle of
        /// their lane and restart the forward-speed ramp from scratch.
        /// </summary>
        public void ResetForContinue()
        {
            startZ = transform.position.z;
            CurrentForwardSpeed = forwardSpeed;
            verticalVelocity = 0f;
            lateralVelocity = 0f;

            controller.enabled = false;
            transform.position = new Vector3(laneXPositions[currentLane], 1.1f, transform.position.z);
            controller.enabled = true;

            deathVisual?.Revive();
            playerAnimator?.SetJumping(false);
            playerAnimator?.SetGrounded(true);
            playerAnimator?.SetRunning(true);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (gameManager == null || gameManager.IsGameOver)
            {
                return;
            }

            RunnerObstacle obstacle = hit.collider.GetComponentInParent<RunnerObstacle>();
            if (obstacle == null)
            {
                return;
            }

            // Landing on a roof is survivable: the controller stands on the
            // collider like ground. Only side and front impacts crash.
            if (hit.normal.y > 0.5f)
            {
                return;
            }

            if (powerUpManager != null && powerUpManager.DroneActive)
            {
                return;
            }

            if (powerUpManager != null && powerUpManager.TryConsumeShield())
            {
                // Hoverboard absorbs the crash; clear the obstacle and run on.
                Destroy(obstacle.gameObject);
                return;
            }

            MovingObstacle taxi = obstacle.GetComponentInParent<MovingObstacle>();
            bool frontTaxiImpact = taxi != null && IsFrontTaxiImpact(hit, obstacle.transform);
            if (frontTaxiImpact)
            {
                taxi.StopMoving();
                deathVisual?.PlayFrontTaxiDeath(hit.normal);
            }
            else
            {
                deathVisual?.PlayDeath(hit.normal);
            }

            gameManager.GameOver();
        }

        bool IsFrontTaxiImpact(ControllerColliderHit hit, Transform obstacleTransform)
        {
            float obstacleAhead = obstacleTransform.position.z - transform.position.z;
            float laneDelta = Mathf.Abs(obstacleTransform.position.x - transform.position.x);
            bool taxiIsAhead = obstacleAhead > -0.35f;
            bool sameLane = laneDelta < 1.35f;
            bool frontSurfaceHit = Vector3.Dot(hit.normal.normalized, Vector3.back) > 0.45f;
            return taxiIsAhead && sameLane && frontSurfaceHit;
        }
    }
}
