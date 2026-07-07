using System.Collections;
using UnityEngine;

namespace JoburgRunner
{
    public class PlayerDeathVisual : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] RunnerLimbSwing limbSwing;
        [SerializeField] PlayerRollVisual rollVisual;
        [SerializeField] float fallDuration = 0.55f;
        [SerializeField] float forwardFallDegrees = 88f;
        [SerializeField] float backwardFallDegrees = -92f;
        [SerializeField] float sideFallDegrees = 28f;
        [SerializeField] Vector3 fallOffset = new Vector3(0f, -0.18f, 0.55f);
        [SerializeField] Vector3 backwardFallOffset = new Vector3(0f, -0.18f, -3.2f);

        Coroutine deathRoutine;
        Vector3 restPosition;
        Quaternion restRotation;

        public bool IsDead { get; private set; }

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

            if (rollVisual == null)
            {
                rollVisual = GetComponent<PlayerRollVisual>();
            }

            restPosition = visualRoot.localPosition;
            restRotation = visualRoot.localRotation;
        }

        public void PlayDeath(Vector3 impactNormal)
        {
            PlayDeath(impactNormal, false);
        }

        public void PlayFrontTaxiDeath(Vector3 impactNormal)
        {
            PlayDeath(impactNormal, true);
        }

        void PlayDeath(Vector3 impactNormal, bool fallBackward)
        {
            if (IsDead || visualRoot == null)
            {
                return;
            }

            IsDead = true;
            rollVisual?.StopRollNow();

            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
            }

            deathRoutine = StartCoroutine(DeathRoutine(impactNormal, fallBackward));
        }

        /// <summary>Restores the standing pose after a paid continue.</summary>
        public void Revive()
        {
            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
                deathRoutine = null;
            }

            if (visualRoot != null)
            {
                visualRoot.localPosition = restPosition;
                visualRoot.localRotation = restRotation;
            }

            if (limbSwing != null)
            {
                limbSwing.enabled = true;
            }

            IsDead = false;
        }

        IEnumerator DeathRoutine(Vector3 impactNormal, bool fallBackward)
        {
            if (limbSwing != null)
            {
                limbSwing.enabled = false;
            }

            Vector3 startPosition = visualRoot.localPosition;
            Quaternion startRotation = visualRoot.localRotation;
            float side = Mathf.Abs(impactNormal.x) > 0.05f ? -Mathf.Sign(impactNormal.x) : 1f;
            float fallDegrees = fallBackward ? backwardFallDegrees : forwardFallDegrees;
            Vector3 offset = fallBackward ? backwardFallOffset : fallOffset;
            Quaternion targetRotation = restRotation * Quaternion.Euler(fallDegrees, 0f, sideFallDegrees * side);
            Vector3 targetPosition = restPosition + offset;

            float elapsed = 0f;
            while (elapsed < fallDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fallDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);

                visualRoot.localPosition = Vector3.Lerp(startPosition, targetPosition, eased);
                visualRoot.localRotation = Quaternion.Slerp(startRotation, targetRotation, eased);
                yield return null;
            }

            visualRoot.localPosition = targetPosition;
            visualRoot.localRotation = targetRotation;
            deathRoutine = null;
        }
    }
}
