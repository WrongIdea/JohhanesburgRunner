using UnityEngine;

namespace JoburgRunner
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] Animator animator;

        static readonly int IsRunningHash = Animator.StringToHash("isRunning");
        static readonly int IsJumpingHash = Animator.StringToHash("isJumping");
        static readonly int RollHash = Animator.StringToHash("Roll");
        static readonly int AirRollHash = Animator.StringToHash("AirRoll");
        static readonly int GroundedHash = Animator.StringToHash("Grounded");

        void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        public void SetRunning(bool value)
        {
            if (animator != null)
            {
                animator.SetBool(IsRunningHash, value);
            }
        }

        public void SetJumping(bool value)
        {
            if (animator != null)
            {
                animator.SetBool(IsJumpingHash, value);
            }
        }

        public void SetGrounded(bool value)
        {
            if (animator != null)
            {
                animator.SetBool(GroundedHash, value);
            }
        }

        public void PlayRoll()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool(IsJumpingHash, false);
            animator.ResetTrigger(RollHash);
            animator.SetTrigger(RollHash);
        }

        /// <summary>Dive-roll played when the player swipes down while airborne.</summary>
        public void PlayAirRoll()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool(IsJumpingHash, false);
            animator.ResetTrigger(AirRollHash);
            animator.SetTrigger(AirRollHash);
        }
    }
}
