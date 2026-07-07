using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Position-only chase camera. The framing rotation is computed once at
    /// startup and then never changes: the camera must not LookAt or rotate
    /// per frame, otherwise lane changes make the whole world appear to
    /// swing and tilt opposite to the swipe while the camera re-aims.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0f, 2.05f, -4.35f);
        [SerializeField] float followSpeed = 8f;
        [Header("Pre-run idle showcase")]
        [SerializeField] GameManager gameManager;
        [Tooltip("Camera offset before the first run, framing the idle/dance runner in the middle of the screen.")]
        [SerializeField] Vector3 idleOffset = new Vector3(0f, 1.2f, -5.2f);
        [SerializeField] float idleLookHeight = 0.95f;
        [Tooltip("How much of the player's X the camera adopts. High enough to keep side lanes visible on portrait screens while still showing the lane switch.")]
        [Range(0f, 1f)]
        [SerializeField] float lateralFollowFraction = 1f;
        [SerializeField] float lateralFollowSpeed = 18f;
        [SerializeField] float lookHeight = 1.05f;
        [SerializeField] float minTargetViewportX = 0.32f;
        [SerializeField] float maxTargetViewportX = 0.68f;

        Camera followCamera;
        bool runFramingLocked;

        void Awake()
        {
            followCamera = GetComponent<Camera>();
        }

        void Start()
        {
            if (target == null)
            {
                return;
            }

            if (IsPreRun())
            {
                ApplyIdleFraming();
            }
            else
            {
                LockRunFraming();
            }
        }

        bool IsPreRun() => gameManager != null && !gameManager.HasStarted;

        // Before the run: centre the dancing runner in frame. The player is
        // stationary here, so re-aiming each frame is safe (the no-LookAt rule
        // exists only to stop lane changes swinging the world during a run).
        void ApplyIdleFraming()
        {
            Vector3 anchoredPosition = target.position + idleOffset;
            transform.position = anchoredPosition;
            transform.rotation = Quaternion.LookRotation(
                target.position + Vector3.up * idleLookHeight - anchoredPosition);
            runFramingLocked = false;
        }

        // Fixed framing, locked for the whole run: pitch only, zero yaw, zero
        // roll. Follow is position-only from here on.
        void LockRunFraming()
        {
            Vector3 anchoredPosition = target.position + offset;
            transform.position = anchoredPosition;
            transform.rotation = Quaternion.LookRotation(
                target.position + Vector3.up * lookHeight - anchoredPosition);
            runFramingLocked = true;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (IsPreRun())
            {
                ApplyIdleFraming();
                return;
            }

            // Snap to the locked run framing on the first frame after PLAY.
            if (!runFramingLocked)
            {
                LockRunFraming();
            }

            Vector3 desired = target.position + offset;
            desired.x = target.position.x * lateralFollowFraction + offset.x;
            Vector3 position = transform.position;
            position.x = Mathf.Lerp(position.x, desired.x, lateralFollowSpeed * Time.deltaTime);
            position.y = Mathf.Lerp(position.y, desired.y, followSpeed * Time.deltaTime);
            position.z = Mathf.Lerp(position.z, desired.z, followSpeed * Time.deltaTime);
            transform.position = position;

            KeepTargetInsidePortraitFrame();
        }

        void KeepTargetInsidePortraitFrame()
        {
            if (followCamera == null)
            {
                return;
            }

            Vector3 focusPoint = target.position + Vector3.up * lookHeight;
            Vector3 viewportPoint = followCamera.WorldToViewportPoint(focusPoint);
            if (viewportPoint.z <= 0f)
            {
                return;
            }

            float viewportError = 0f;
            if (viewportPoint.x < minTargetViewportX)
            {
                viewportError = viewportPoint.x - minTargetViewportX;
            }
            else if (viewportPoint.x > maxTargetViewportX)
            {
                viewportError = viewportPoint.x - maxTargetViewportX;
            }

            if (Mathf.Approximately(viewportError, 0f))
            {
                return;
            }

            float halfHorizontalWidth = Mathf.Tan(followCamera.fieldOfView * Mathf.Deg2Rad * 0.5f)
                * followCamera.aspect
                * viewportPoint.z;
            Vector3 correctedPosition = transform.position;
            correctedPosition.x += viewportError * halfHorizontalWidth * 2f;
            transform.position = correctedPosition;
        }
    }
}
