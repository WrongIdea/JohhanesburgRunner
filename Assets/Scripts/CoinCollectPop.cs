using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Coin-collect animation played at the pickup point: the Higgsfield coin
    /// sprite pops in, rises, and fades, then the object destroys itself. The
    /// Billboard component keeps it facing the camera.
    /// </summary>
    public class CoinCollectPop : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] float duration = 0.28f;
        [SerializeField] float rise = 0.8f;
        [SerializeField] float startScale = 0.4f;
        [SerializeField] float endScale = 0.9f;

        float elapsed;
        Vector3 basePosition;
        Vector3 baseScale;
        Color baseColor = Color.white;

        void OnEnable()
        {
            elapsed = 0f;
            basePosition = transform.position;
            baseScale = transform.localScale;

            if (sprite == null)
            {
                sprite = GetComponentInChildren<SpriteRenderer>();
            }

            if (sprite != null)
            {
                baseColor = sprite.color;
                baseColor.a = 1f;
            }
        }

        void LateUpdate()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);

            transform.position = basePosition + Vector3.up * (rise * eased);
            transform.localScale = baseScale * Mathf.Lerp(startScale, endScale, eased);

            if (sprite != null)
            {
                Color color = baseColor;
                color.a = 1f - t * t;
                sprite.color = color;
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
