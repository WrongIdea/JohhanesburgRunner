using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Cosmetic lane-change lean. Rolls only the model pivot (a child of the
    /// Player root) a few degrees into the lateral movement and eases back to
    /// zero when it stops. The Player root, camera and environment are never
    /// rotated. The lean is an absolute value derived from current lateral
    /// velocity, so repeated lane changes can never accumulate rotation.
    /// </summary>
    public class RunnerLeanVisual : MonoBehaviour
    {
        [SerializeField] Transform leanPivot;
        [SerializeField] float maxLeanDegrees = 7f;
        [SerializeField] float degreesPerLateralMeterPerSecond = 1f;
        [Tooltip("Lerp rate while leaning INTO a dash (~0.06 s response).")]
        [SerializeField] float leanInSmoothing = 16f;
        [Tooltip("Lerp rate while easing back upright (~0.12 s response).")]
        [SerializeField] float leanReturnSmoothing = 8f;
        [SerializeField] bool keepSkinnedMeshesVisible = true;
        [SerializeField] Vector3 expandedSkinnedBounds = new Vector3(4f, 5f, 4f);

        float previousX;
        float currentLean;
        Quaternion pivotRestRotation = Quaternion.identity;

        void Awake()
        {
            if (leanPivot != null)
            {
                leanPivot.localPosition = Vector3.zero;
                leanPivot.localScale = Vector3.one;
                pivotRestRotation = leanPivot.localRotation;
            }

            if (keepSkinnedMeshesVisible)
            {
                StabilizeSkinnedMeshBounds();
            }
        }

        void Start()
        {
            previousX = transform.position.x;
        }

        void LateUpdate()
        {
            if (leanPivot == null)
            {
                return;
            }

            float lateralSpeed = Time.deltaTime > 0f
                ? (transform.position.x - previousX) / Time.deltaTime
                : 0f;
            previousX = transform.position.x;

            // Moving right (+X) rolls the model to the right (negative Z).
            float targetLean = Mathf.Clamp(
                -lateralSpeed * degreesPerLateralMeterPerSecond, -maxLeanDegrees, maxLeanDegrees);
            // Snap into the lean, ease back out — the dash should feel
            // instant, the recovery relaxed.
            float smoothing = Mathf.Abs(targetLean) > Mathf.Abs(currentLean)
                ? leanInSmoothing
                : leanReturnSmoothing;
            currentLean = Mathf.Lerp(currentLean, targetLean, smoothing * Time.deltaTime);
            leanPivot.localRotation = pivotRestRotation * Quaternion.Euler(0f, 0f, currentLean);
        }

        void OnDisable()
        {
            if (leanPivot != null)
            {
                leanPivot.localRotation = pivotRestRotation;
            }
        }

        void StabilizeSkinnedMeshBounds()
        {
            foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.updateWhenOffscreen = true;
                renderer.localBounds = new Bounds(Vector3.zero, expandedSkinnedBounds);
            }
        }
    }
}
