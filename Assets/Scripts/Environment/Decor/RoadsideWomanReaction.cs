using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Keeps a pooled roadside woman in her dozing loop until the runner comes
    /// close, then blends to the friendly wave. No per-frame allocations.
    /// </summary>
    public sealed class RoadsideWomanReaction : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] float waveDistance = 13f;
        [SerializeField] float resetDistanceBehind = 5f;
        [SerializeField] float blendDuration = 0.2f;

        static readonly int DozingState = Animator.StringToHash("Base Layer.Dozing");
        static readonly int WavingState = Animator.StringToHash("Base Layer.BigWave");

        Transform runner;
        bool waving;

        void OnEnable()
        {
            runner = null;
            waving = false;
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
            if (animator != null)
            {
                animator.Play(DozingState, 0, Random.value);
            }
        }

        void Update()
        {
            if (animator == null)
            {
                return;
            }

            if (runner == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                runner = player != null ? player.transform : null;
                if (runner == null)
                {
                    return;
                }
            }

            float longitudinalDistance = transform.position.z - runner.position.z;
            bool shouldWave = longitudinalDistance <= waveDistance &&
                              longitudinalDistance >= -resetDistanceBehind;
            if (shouldWave == waving)
            {
                return;
            }

            waving = shouldWave;
            animator.CrossFadeInFixedTime(
                waving ? WavingState : DozingState,
                blendDuration,
                0,
                0f);
        }
    }
}
