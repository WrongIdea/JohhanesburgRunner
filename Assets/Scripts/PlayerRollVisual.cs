using System.Collections;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Visual-only roll for the endless runner. Gameplay collision lives in
    /// RollController; this only tucks and spins the character mesh so a down
    /// swipe reads as a roll.
    /// </summary>
    public class PlayerRollVisual : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] RunnerLimbSwing limbSwing;
        [SerializeField] float spinDegrees = 0f;
        [SerializeField] float tuckHeight = 0f;
        [SerializeField] float forwardOffset = 0f;
        [SerializeField] Vector3 tuckedScale = new Vector3(1.02f, 0.82f, 1.02f);
        [SerializeField] float minimumDuration = 0.05f;

        Coroutine rollRoutine;
        Vector3 restPosition;
        Quaternion restRotation;
        Vector3 restScale;
        LimbPose limbPose;

        void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (limbSwing == null)
            {
                limbSwing = GetComponentInParent<RunnerLimbSwing>();
            }

            restPosition = visualRoot.localPosition;
            restRotation = visualRoot.localRotation;
            restScale = visualRoot.localScale;
            limbPose = LimbPose.Capture(visualRoot);
        }

        public void PlayRoll(float duration)
        {
            PlayRoll(duration, 1f);
        }

        public void PlayRoll(float duration, float animationSpeed)
        {
            if (!isActiveAndEnabled || visualRoot == null)
            {
                return;
            }

            if (rollRoutine != null)
            {
                StopCoroutine(rollRoutine);
                RestoreRestPose();
            }

            float adjustedDuration = Mathf.Max(minimumDuration, duration / Mathf.Max(0.05f, animationSpeed));
            rollRoutine = StartCoroutine(RollRoutine(adjustedDuration));
        }

        IEnumerator RollRoutine(float duration)
        {
            if (limbSwing != null)
            {
                limbSwing.enabled = false;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float tuck = Mathf.Sin(t * Mathf.PI);
                float easedSpin = Mathf.SmoothStep(0f, 1f, t) * spinDegrees;

                visualRoot.localPosition = restPosition + new Vector3(0f, tuckHeight * tuck, forwardOffset * tuck);
                visualRoot.localRotation = restRotation * Quaternion.Euler(easedSpin, 0f, 0f);
                visualRoot.localScale = Vector3.Lerp(restScale, Vector3.Scale(restScale, tuckedScale), tuck);
                limbPose.Apply(tuck);
                yield return null;
            }

            RestoreRestPose();
            rollRoutine = null;
        }

        public void StopRollNow()
        {
            if (rollRoutine != null)
            {
                StopCoroutine(rollRoutine);
                rollRoutine = null;
            }

            RestoreRestPose();
        }

        void RestoreRestPose()
        {
            visualRoot.localPosition = restPosition;
            visualRoot.localRotation = restRotation;
            visualRoot.localScale = restScale;
            limbPose.Restore();

            if (limbSwing != null)
            {
                limbSwing.enabled = true;
            }
        }

        readonly struct LimbPose
        {
            readonly Bone spine;
            readonly Bone head;
            readonly Bone leftUpperLeg;
            readonly Bone rightUpperLeg;
            readonly Bone leftLowerLeg;
            readonly Bone rightLowerLeg;
            readonly Bone leftUpperArm;
            readonly Bone rightUpperArm;
            readonly Bone leftForeArm;
            readonly Bone rightForeArm;

            LimbPose(
                Bone spine,
                Bone head,
                Bone leftUpperLeg,
                Bone rightUpperLeg,
                Bone leftLowerLeg,
                Bone rightLowerLeg,
                Bone leftUpperArm,
                Bone rightUpperArm,
                Bone leftForeArm,
                Bone rightForeArm)
            {
                this.spine = spine;
                this.head = head;
                this.leftUpperLeg = leftUpperLeg;
                this.rightUpperLeg = rightUpperLeg;
                this.leftLowerLeg = leftLowerLeg;
                this.rightLowerLeg = rightLowerLeg;
                this.leftUpperArm = leftUpperArm;
                this.rightUpperArm = rightUpperArm;
                this.leftForeArm = leftForeArm;
                this.rightForeArm = rightForeArm;
            }

            public static LimbPose Capture(Transform root)
            {
                return new LimbPose(
                    Bone.Find(root, "Spine", "Spine02", "Chest"),
                    Bone.Find(root, "Head"),
                    Bone.Find(root, "LeftUpLeg", "LeftUpperLeg", "LeftLegPivot"),
                    Bone.Find(root, "RightUpLeg", "RightUpperLeg", "RightLegPivot"),
                    Bone.Find(root, "LeftLeg", "LeftLowerLeg", "LeftKneePivot"),
                    Bone.Find(root, "RightLeg", "RightLowerLeg", "RightKneePivot"),
                    Bone.Find(root, "LeftArm", "LeftUpperArm", "LeftArmPivot"),
                    Bone.Find(root, "RightArm", "RightUpperArm", "RightArmPivot"),
                    Bone.Find(root, "LeftForeArm", "LeftLowerArm", "LeftElbowPivot"),
                    Bone.Find(root, "RightForeArm", "RightLowerArm", "RightElbowPivot"));
            }

            public void Apply(float amount)
            {
                spine.Apply(Quaternion.Euler(-42f, 0f, 0f), amount);
                head.Apply(Quaternion.Euler(26f, 0f, 0f), amount);
                leftUpperLeg.Apply(Quaternion.Euler(-118f, 6f, -8f), amount);
                rightUpperLeg.Apply(Quaternion.Euler(-118f, -6f, 8f), amount);
                leftLowerLeg.Apply(Quaternion.Euler(128f, 0f, 0f), amount);
                rightLowerLeg.Apply(Quaternion.Euler(128f, 0f, 0f), amount);
                leftUpperArm.Apply(Quaternion.Euler(-95f, -22f, -18f), amount);
                rightUpperArm.Apply(Quaternion.Euler(-95f, 22f, 18f), amount);
                leftForeArm.Apply(Quaternion.Euler(-110f, 0f, 0f), amount);
                rightForeArm.Apply(Quaternion.Euler(-110f, 0f, 0f), amount);
            }

            public void Restore()
            {
                spine.Restore();
                head.Restore();
                leftUpperLeg.Restore();
                rightUpperLeg.Restore();
                leftLowerLeg.Restore();
                rightLowerLeg.Restore();
                leftUpperArm.Restore();
                rightUpperArm.Restore();
                leftForeArm.Restore();
                rightForeArm.Restore();
            }
        }

        readonly struct Bone
        {
            readonly Transform transform;
            readonly Quaternion restRotation;

            Bone(Transform transform)
            {
                this.transform = transform;
                restRotation = transform != null ? transform.localRotation : Quaternion.identity;
            }

            public static Bone Find(Transform root, params string[] names)
            {
                return new Bone(FindChild(root, names));
            }

            public void Apply(Quaternion tuckRotation, float amount)
            {
                if (transform != null)
                {
                    transform.localRotation = Quaternion.Slerp(restRotation, restRotation * tuckRotation, amount);
                }
            }

            public void Restore()
            {
                if (transform != null)
                {
                    transform.localRotation = restRotation;
                }
            }

            static Transform FindChild(Transform root, string[] names)
            {
                if (root == null)
                {
                    return null;
                }

                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    foreach (string name in names)
                    {
                        if (child.name == name)
                        {
                            return child;
                        }
                    }
                }

                return null;
            }
        }
    }
}
