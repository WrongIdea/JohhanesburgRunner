using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Keeps the pooled stall visible first, then reveals its vendor shortly
    /// afterwards. Reset on every pool return so the order stays consistent.
    /// </summary>
    public sealed class StallVendorReveal : MonoBehaviour
    {
        [SerializeField] GameObject vendor;
        [SerializeField, Min(0f)] float revealDelay = 1f;

        float remaining;

        void Awake()
        {
            HideAndReset();
        }

        void OnEnable()
        {
            HideAndReset();
        }

        void OnDisable()
        {
            HideAndReset();
        }

        void Update()
        {
            if (vendor == null || vendor.activeSelf)
            {
                return;
            }

            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                vendor.SetActive(true);
            }
        }

        void HideAndReset()
        {
            remaining = revealDelay;
            if (vendor != null)
            {
                vendor.SetActive(false);
            }
        }
    }
}
