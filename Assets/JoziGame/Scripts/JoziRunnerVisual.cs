using UnityEngine;

namespace JoziGame
{
    public class JoziRunnerVisual : MonoBehaviour
    {
        [SerializeField] CharacterController controller;
        [SerializeField] Transform leftArm;
        [SerializeField] Transform rightArm;
        [SerializeField] Transform leftLeg;
        [SerializeField] Transform rightLeg;
        [SerializeField] Transform body;

        Vector3 bodyStart;

        void Awake()
        {
            if (controller == null)
            {
                controller = GetComponentInParent<CharacterController>();
            }

            if (body != null)
            {
                bodyStart = body.localPosition;
            }
        }

        void Update()
        {
            float speed = controller != null ? new Vector2(controller.velocity.x, controller.velocity.z).magnitude : 0f;
            float runAmount = Mathf.Clamp01(speed / 3f);
            float swing = Mathf.Sin(Time.time * 10f) * 34f * runAmount;

            SetLimb(leftArm, swing);
            SetLimb(rightArm, -swing);
            SetLimb(leftLeg, -swing);
            SetLimb(rightLeg, swing);

            if (body != null)
            {
                body.localPosition = bodyStart + Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.08f * runAmount;
            }
        }

        static void SetLimb(Transform limb, float angle)
        {
            if (limb != null)
            {
                limb.localRotation = Quaternion.Euler(angle, 0f, 0f);
            }
        }
    }
}
