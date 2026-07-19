using UnityEngine;

namespace JoburgRunner.VFX
{
    /// <summary>
    /// Layers transient, self-restoring impulses on top of the normal camera
    /// follow: a small positional kick, an (optional, deliberately tiny)
    /// rotational kick, and a field-of-view pulse. Runs after CameraFollow via
    /// a high execution order and re-applies its decaying offset each frame, so
    /// it never permanently moves the camera — when the impulse decays the
    /// camera sits exactly where the follow put it.
    ///
    /// Deliberately conservative: lane switches add no tilt here (the project's
    /// camera is intentionally tilt-free to avoid the "world leans" artefact),
    /// so rotational impulse defaults off and callers keep it near zero.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class CameraFeedbackController : MonoBehaviour
    {
        public static CameraFeedbackController Instance { get; private set; }

        [SerializeField] Camera targetCamera;
        [Tooltip("Global scale on every impulse; 0 disables all camera feedback.")]
        [Range(0f, 2f)] [SerializeField] float intensity = 1f;
        [Tooltip("How fast impulses decay back to rest (higher = snappier).")]
        [SerializeField] float recovery = 12f;

        Vector3 positionOffset;
        Vector3 rotationOffset;      // euler degrees
        float fovOffset;
        float baseFov;

        void Awake()
        {
            Instance = this;
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>() ?? Camera.main;
            }

            if (targetCamera != null)
            {
                baseFov = targetCamera.fieldOfView;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Adds a one-shot impulse. Safe to call every frame or in bursts.</summary>
        public void AddImpulse(Vector3 positional, Vector3 rotational, float fovPulse)
        {
            if (intensity <= 0f)
            {
                return;
            }

            positionOffset += positional * intensity;
            rotationOffset += rotational * intensity;
            fovOffset += fovPulse * intensity;
        }

        public void AddImpulse(in VFXDefinition definition)
        {
            if (definition == null || !definition.cameraFeedback)
            {
                return;
            }

            AddImpulse(definition.positionalImpulse, definition.rotationalImpulse, definition.fovPulse);
        }

        void LateUpdate()
        {
            if (targetCamera == null)
            {
                return;
            }

            // Apply on top of whatever CameraFollow set this frame (we run after
            // it), then decay toward zero so the offset is fully transient.
            if (positionOffset.sqrMagnitude > 1e-6f)
            {
                targetCamera.transform.position += targetCamera.transform.TransformVector(positionOffset);
            }

            if (rotationOffset.sqrMagnitude > 1e-6f)
            {
                targetCamera.transform.rotation *= Quaternion.Euler(rotationOffset);
            }

            targetCamera.fieldOfView = baseFov + fovOffset;

            float t = 1f - Mathf.Exp(-recovery * Time.unscaledDeltaTime);
            positionOffset = Vector3.Lerp(positionOffset, Vector3.zero, t);
            rotationOffset = Vector3.Lerp(rotationOffset, Vector3.zero, t);
            fovOffset = Mathf.Lerp(fovOffset, 0f, t);
        }
    }
}
