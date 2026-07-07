using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Procedural sprint cycle for the runner: swings the arm and leg pivots in
    /// opposite phase, folds the knees when each leg swings back, and bobs the
    /// visual root so the character reads as running like the reference footage.
    /// </summary>
    public class RunnerLimbSwing : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] Transform leftArmPivot;
        [SerializeField] Transform rightArmPivot;
        [SerializeField] Transform leftLegPivot;
        [SerializeField] Transform rightLegPivot;
        [SerializeField] Transform leftKneePivot;
        [SerializeField] Transform rightKneePivot;
        [SerializeField] float strideFrequency = 9f;
        [SerializeField] float armSwingDegrees = 50f;
        [SerializeField] float legSwingDegrees = 58f;
        [SerializeField] float kneeBendDegrees = 80f;
        [SerializeField] float bobHeight = 0f;

        float cycleTime;
        Vector3 visualRootRestPosition;

        void Awake()
        {
            if (visualRoot != null)
            {
                visualRootRestPosition = visualRoot.localPosition;
            }
        }

        void Update()
        {
            cycleTime += Time.deltaTime * strideFrequency;
            float swing = Mathf.Sin(cycleTime);

            // Negative X rotation swings a hanging limb forward, positive swings it back.
            if (leftLegPivot != null)
            {
                leftLegPivot.localRotation = Quaternion.Euler(-swing * legSwingDegrees, 0f, 0f);
            }

            if (rightLegPivot != null)
            {
                rightLegPivot.localRotation = Quaternion.Euler(swing * legSwingDegrees, 0f, 0f);
            }

            // Knees stay slightly bent and fold hard while the leg swings back.
            if (leftKneePivot != null)
            {
                float bend = kneeBendDegrees * (0.15f + 0.85f * Mathf.Max(0f, -swing));
                leftKneePivot.localRotation = Quaternion.Euler(bend, 0f, 0f);
            }

            if (rightKneePivot != null)
            {
                float bend = kneeBendDegrees * (0.15f + 0.85f * Mathf.Max(0f, swing));
                rightKneePivot.localRotation = Quaternion.Euler(bend, 0f, 0f);
            }

            // Arms counter-swing against the legs.
            if (leftArmPivot != null)
            {
                leftArmPivot.localRotation = Quaternion.Euler(swing * armSwingDegrees, 0f, 0f);
            }

            if (rightArmPivot != null)
            {
                rightArmPivot.localRotation = Quaternion.Euler(-swing * armSwingDegrees, 0f, 0f);
            }

            if (visualRoot != null)
            {
                float bob = Mathf.Abs(Mathf.Cos(cycleTime)) * bobHeight;
                visualRoot.localPosition = visualRootRestPosition + Vector3.up * bob;
            }
        }
    }
}
