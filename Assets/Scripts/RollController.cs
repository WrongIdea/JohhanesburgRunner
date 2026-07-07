using System.Collections;
using UnityEngine;

namespace JoburgRunner
{
    [RequireComponent(typeof(CharacterController))]
    public class RollController : MonoBehaviour
    {
        [Header("Roll")]
        [SerializeField] float rollDuration = 0.7f;
        [Range(0.35f, 0.75f)]
        [SerializeField] float rollColliderHeight = 0.5f;
        [SerializeField] float rollAnimationSpeed = 1f;
        [SerializeField] float rollCooldown;

        [Header("References")]
        [SerializeField] PlayerAnimator playerAnimator;
        [SerializeField] PlayerRollVisual rollVisual;
        [SerializeField] GameManager gameManager;

        CharacterController controller;
        float originalHeight;
        Vector3 originalCenter;
        float nextRollTime;

        public bool IsRolling { get; private set; }
        public bool IsStunned { get; set; }

        public float RollDuration => rollDuration;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            originalHeight = controller.height;
            originalCenter = controller.center;

            if (playerAnimator == null)
            {
                playerAnimator = GetComponent<PlayerAnimator>();
            }

            if (rollVisual == null)
            {
                rollVisual = GetComponentInChildren<PlayerRollVisual>();
            }
        }

        public bool TryStartRoll()
        {
            if (!CanRoll())
            {
                return false;
            }

            StartCoroutine(RollRoutine());
            return true;
        }

        bool CanRoll()
        {
            if (IsRolling || IsStunned || Time.timeScale <= 0f || Time.time < nextRollTime)
            {
                return false;
            }

            if (gameManager != null && gameManager.IsGameOver)
            {
                return false;
            }

            return controller != null && controller.isGrounded;
        }

        IEnumerator RollRoutine()
        {
            IsRolling = true;
            nextRollTime = Time.time + rollDuration + rollCooldown;

            ShrinkCollider();
            playerAnimator?.PlayRoll();
            rollVisual?.PlayRoll(rollDuration, rollAnimationSpeed);

            yield return new WaitForSeconds(rollDuration);

            RestoreCollider();
            IsRolling = false;
        }

        void ShrinkCollider()
        {
            float targetHeight = originalHeight * rollColliderHeight;
            float heightDelta = originalHeight - targetHeight;
            controller.height = targetHeight;
            controller.center = originalCenter + Vector3.down * (heightDelta * 0.5f);
        }

        void RestoreCollider()
        {
            controller.height = originalHeight;
            controller.center = originalCenter;
        }

        void OnDisable()
        {
            if (controller != null)
            {
                RestoreCollider();
            }

            IsRolling = false;
        }
    }
}
