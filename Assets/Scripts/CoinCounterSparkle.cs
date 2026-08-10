using UnityEngine;
using UnityEngine.UI;

namespace JoburgRunner
{
    /// <summary>
    /// A quick gold sparkle burst on the HUD coin counter, played each time a
    /// collected coin's flight reaches the counter (CoinCollectPop triggers it
    /// through CoinHudAnchor on arrival). The HUD is a Screen Space - Overlay
    /// canvas that world-space particles can't draw over, so the burst is built
    /// from UGUI Images at runtime: a soft glow flash plus a ring of four-point
    /// star sprites that pop outward and fade, with a scale punch on the coin
    /// icon itself. Re-triggering restarts it, so a stream of coins keeps the
    /// counter twinkling.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CoinCounterSparkle : MonoBehaviour
    {
        [SerializeField] float duration = 0.44f;
        [Tooltip("Pixels each star travels out from the coin icon centre.")]
        [SerializeField] float starTravel = 46f;
        [SerializeField] float starSize = 34f;
        [SerializeField] float glowSize = 122f;
        [SerializeField] int starCount = 6;
        [Tooltip("Extra scale added to the coin icon at the peak of the pop.")]
        [SerializeField] float iconPunch = 0.4f;

        static readonly Color GlowColor = new Color(1f, 0.82f, 0.4f, 1f);
        static readonly Color StarColor = new Color(1f, 0.92f, 0.62f, 1f);

        RectTransform iconRect;
        Vector3 iconBaseScale;

        Image glowImage;
        RectTransform glowRect;
        Image[] starImages;
        RectTransform[] starRects;
        Vector2[] starDirs;

        float elapsed;
        bool playing;
        bool built;

        static Sprite starSprite;
        static Sprite glowSprite;

        void Awake()
        {
            iconRect = (RectTransform)transform;
            iconBaseScale = iconRect.localScale;
            Build();
            HideAll();
        }

        /// <summary>Fire the burst (called on every coin arrival; restarts if already running).</summary>
        public void Play()
        {
            if (!built)
            {
                return;
            }

            elapsed = 0f;
            playing = true;
        }

        void LateUpdate()
        {
            if (!playing)
            {
                return;
            }

            // Unscaled so the flourish still snaps at full speed while the dev
            // time-scale cheat slows gameplay; in a shipped run time scale is 1.
            elapsed += Time.unscaledDeltaTime;
            float n = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float easeOut = 1f - (1f - n) * (1f - n);

            // Coin icon scale punch: swells then settles.
            iconRect.localScale = iconBaseScale * (1f + iconPunch * Mathf.Sin(n * Mathf.PI));

            // Glow flash: brightest at the start, fading as it expands.
            glowRect.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.15f, easeOut);
            SetAlpha(glowImage, (1f - n) * 0.6f);

            // Stars shoot outward and fade, growing in quickly at the start.
            float dist = starTravel * easeOut;
            float grow = Mathf.Clamp01(n * 3f);
            for (int i = 0; i < starRects.Length; i++)
            {
                starRects[i].anchoredPosition = starDirs[i] * dist;
                starRects[i].localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, grow);
                SetAlpha(starImages[i], 1f - n);
            }

            if (n >= 1f)
            {
                playing = false;
                iconRect.localScale = iconBaseScale;
                HideAll();
            }
        }

        void Build()
        {
            glowRect = MakeChild("SparkleGlow", GlowSprite(), glowSize, GlowColor, out glowImage);

            starImages = new Image[starCount];
            starRects = new RectTransform[starCount];
            starDirs = new Vector2[starCount];
            for (int i = 0; i < starCount; i++)
            {
                // Offset half a step so the ring is not axis-aligned.
                float angle = (i + 0.5f) / starCount * Mathf.PI * 2f;
                starDirs[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                starRects[i] = MakeChild($"SparkleStar{i}", StarSprite(), starSize, StarColor, out starImages[i]);
            }

            built = true;
        }

        RectTransform MakeChild(string childName, Sprite sprite, float size, Color color, out Image image)
        {
            GameObject go = new GameObject(childName);
            image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;

            RectTransform rt = image.rectTransform;
            rt.SetParent(transform, false);
            // Centre on the coin icon regardless of the icon's own pivot.
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            return rt;
        }

        void HideAll()
        {
            SetAlpha(glowImage, 0f);
            if (starImages != null)
            {
                for (int i = 0; i < starImages.Length; i++)
                {
                    SetAlpha(starImages[i], 0f);
                }
            }
        }

        static void SetAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        static Sprite GlowSprite()
        {
            if (glowSprite == null)
            {
                Texture2D tex = UbuntuPulseVisual.SoftGlowTexture();
                glowSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), tex.width);
            }

            return glowSprite;
        }

        static Sprite StarSprite()
        {
            if (starSprite == null)
            {
                Texture2D tex = BuildStarTexture(64);
                starSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), tex.width);
            }

            return starSprite;
        }

        // Four-point twinkle: a bright core with thin spikes along each axis.
        static Texture2D BuildStarTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
            };
            Color32[] pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float ax = Mathf.Abs(dx);
                    float ay = Mathf.Abs(dy);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    float horizontal = Mathf.Clamp01(1f - ax) * Mathf.Clamp01(1f - ay * 7f);
                    float vertical = Mathf.Clamp01(1f - ay) * Mathf.Clamp01(1f - ax * 7f);
                    float spike = Mathf.Max(horizontal, vertical);
                    float core = Mathf.Pow(Mathf.Clamp01(1f - r * 1.5f), 2f);
                    float a = Mathf.Clamp01(spike * 0.85f + core);

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
