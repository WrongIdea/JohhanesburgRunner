using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Coin-collect animation. The Higgsfield coin sprite pops in and rises at
    /// the pickup point while the prefab's gold sparkle burst plays, then flies
    /// up into the HUD coin counter (see CoinHudAnchor), shrinking and fading as
    /// it arrives, so the pickup reads as feeding the score. Without a HUD
    /// anchor in the scene it falls back to rising and fading in place. The
    /// object destroys itself once both the flight and the sparkles finish, and
    /// the Billboard component keeps the sprite facing the camera throughout.
    /// </summary>
    public class CoinCollectPop : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;

        [Header("Pop")]
        [SerializeField] float duration = 0.28f;
        [SerializeField] float rise = 0.8f;
        [SerializeField] float startScale = 0.4f;
        [SerializeField] float endScale = 0.9f;

        [Header("Flight to the coin counter")]
        [SerializeField] float flyDuration = 0.4f;
        [Tooltip("Arc height as a fraction of the distance to the HUD. The flight ends " +
                 "a few metres from the camera, where a whole screen height spans only " +
                 "~2.5 world units, so anything past ~0.1 lobs the coin off the top edge.")]
        [SerializeField, Range(0f, 0.3f)] float flyArcFraction = 0.07f;
        [Tooltip("Scale on arrival, tuned so the coin lands at roughly the size of the HUD coin icon.")]
        [SerializeField] float flyEndScale = 0.15f;
        [Tooltip("Fraction of the flight spent fading out at the end.")]
        [SerializeField, Range(0.05f, 1f)] float flyFadeTail = 0.18f;

        [SerializeField] float sparkleLinger = 0.5f;

        float elapsed;
        Vector3 basePosition;
        Vector3 baseScale;
        Color baseColor = Color.white;

        Camera gameplayCamera;
        CoinHudAnchor anchor;
        Vector3 flyStart;
        bool flying;
        bool sparkleFired;

        // One shared additive material for every pop: the prefab ships with a
        // plain unlit gold placeholder because the soft glow sprite texture
        // only exists at runtime.
        static Material sparkleGlowMaterial;

        void OnEnable()
        {
            elapsed = 0f;
            flying = false;
            sparkleFired = false;
            basePosition = transform.position;
            baseScale = transform.localScale;

            gameplayCamera = Camera.main;
            anchor = CoinHudAnchor.Instance;

            if (sprite == null)
            {
                sprite = GetComponentInChildren<SpriteRenderer>();
            }

            if (sprite != null)
            {
                baseColor = sprite.color;
                baseColor.a = 1f;
            }

            foreach (ParticleSystemRenderer sparkleRenderer in GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (sparkleGlowMaterial == null)
                {
                    sparkleGlowMaterial = UbuntuPulseVisual.MakeAdditiveGlowMaterial(
                        sparkleRenderer.sharedMaterial, UbuntuPulseVisual.SoftGlowTexture());
                }

                if (sparkleGlowMaterial != null)
                {
                    sparkleRenderer.sharedMaterial = sparkleGlowMaterial;
                }
            }
        }

        void LateUpdate()
        {
            elapsed += Time.deltaTime;

            Vector3 hudPoint = Vector3.zero;
            bool hasTarget = anchor != null && anchor.TryGetWorldPoint(gameplayCamera, out hudPoint);

            if (elapsed < duration || !hasTarget)
            {
                AnimatePop(hasTarget);
            }
            else
            {
                AnimateFlight(hudPoint);
            }

            // Sparkles simulate in world space and stay behind at the pickup
            // point, so hold the object alive until they have finished too.
            float popLife = duration + sparkleLinger;
            float flightLife = hasTarget ? duration + flyDuration : popLife;
            if (elapsed >= Mathf.Max(popLife, flightLife))
            {
                Destroy(gameObject);
            }
        }

        void AnimatePop(bool hasTarget)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);

            transform.position = basePosition + Vector3.up * (rise * eased);
            transform.localScale = baseScale * Mathf.Lerp(startScale, endScale, eased);

            // Only fade in place when there is no counter to fly to; otherwise
            // the coin has to stay solid for the trip.
            if (sprite != null && !hasTarget)
            {
                Color color = baseColor;
                color.a = 1f - t * t;
                sprite.color = color;
            }

            flyStart = transform.position;
        }

        void AnimateFlight(Vector3 hudPoint)
        {
            if (!flying)
            {
                flying = true;
                flyStart = transform.position;
            }

            float t = flyDuration > 0f ? Mathf.Clamp01((elapsed - duration) / flyDuration) : 1f;

            // Quadratic bezier through a slightly raised control point: the coin
            // hangs for a beat, then whips into the counter.
            float arc = Vector3.Distance(flyStart, hudPoint) * flyArcFraction;
            Vector3 control = Vector3.Lerp(flyStart, hudPoint, 0.5f) + Vector3.up * arc;
            float inverse = 1f - t;
            transform.position =
                inverse * inverse * flyStart +
                2f * inverse * t * control +
                t * t * hudPoint;

            // Shrink early (ease-out) rather than late: the coin is closing on the
            // camera the whole way, and perspective magnifies it as it goes. A
            // late shrink cancels out to a near-constant screen size that fills a
            // third of the width, then snaps small; shrinking early reads as the
            // coin tightening into the counter.
            float shrink = 1f - (1f - t) * (1f - t);
            transform.localScale = baseScale * Mathf.Lerp(endScale, flyEndScale, shrink);

            if (sprite != null)
            {
                Color color = baseColor;
                color.a = Mathf.Clamp01((1f - t) / flyFadeTail);
                sprite.color = color;
            }

            // Land on the counter: fire its sparkle once as the coin arrives.
            if (!sparkleFired && t >= 1f)
            {
                sparkleFired = true;
                anchor.PlayArrivalSparkle();
            }
        }
    }
}
