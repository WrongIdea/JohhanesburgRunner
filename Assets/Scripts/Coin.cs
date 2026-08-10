using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Rotating collectible coin. The common R1 is worth one coin; the rare
    /// R5 variant is worth five and is tracked as a collectable.
    /// Set the collider to Is Trigger and make sure the Player has PlayerController.
    /// </summary>
    public class Coin : MonoBehaviour
    {
        public static readonly List<Coin> ActiveCoins = new List<Coin>();

        [SerializeField] float rotationSpeed = 120f;
        [SerializeField] int coinValue = 1;
        [SerializeField] bool isRare;
        [SerializeField] GameObject collectParticlePrefab;
        [SerializeField] AudioClip collectClip;
        [SerializeField] float collectVolume = 1f;
        [SerializeField] ScoreManager scoreManager;
        [SerializeField] float cameraHideDistance = 1.6f;
        [Tooltip("Stacked-pair coin art swapped in while Double Coins is active.")]
        [SerializeField] Sprite doubleStackSprite;
        [SerializeField] SpriteRenderer artRenderer;
        [Tooltip("Art scale-up while doubled so each coin in the pair art stays full size.")]
        [SerializeField] float doubleStackScale = 1.4f;

        Renderer[] renderers;
        bool renderersVisible = true;
        static Camera mainCamera;

        static bool doubleStackVisible;
        Sprite singleSprite;
        Vector3 artRestScale;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            if (artRenderer != null)
            {
                singleSprite = artRenderer.sprite;
                artRestScale = artRenderer.transform.localScale;
            }
        }

        void OnEnable()
        {
            ActiveCoins.Add(this);
            SetRenderersVisible(true);
            ApplyDoubleStack();
        }

        void OnDisable()
        {
            ActiveCoins.Remove(this);
        }

        void Update()
        {
            // Billboard coins (Higgsfield art) set rotationSpeed to 0 and face the
            // camera via the Billboard component, so skip the per-frame transform
            // write for every active coin on the track.
            if (rotationSpeed != 0f)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }

            // Magnet-pulled coins can swing right past the chase camera and
            // fill the screen as a giant blob for a moment; hide them once
            // they get that close (trigger collection is unaffected).
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                float sqrDistance = (transform.position - mainCamera.transform.position).sqrMagnitude;
                SetRenderersVisible(sqrDistance > cameraHideDistance * cameraHideDistance);
            }
        }

        /// <summary>
        /// While Double Coins is active, every coin swaps its art for the
        /// stacked-pair sprite so the doubled value is readable at a glance.
        /// Coins without the pair art (e.g. the rare R5) are left unchanged.
        /// </summary>
        public static void SetDoubleStackVisible(bool visible)
        {
            if (doubleStackVisible == visible)
            {
                return;
            }

            doubleStackVisible = visible;
            foreach (Coin coin in ActiveCoins)
            {
                coin.ApplyDoubleStack();
            }
        }

        void ApplyDoubleStack()
        {
            if (artRenderer == null || doubleStackSprite == null || singleSprite == null)
            {
                return;
            }

            bool showDouble = doubleStackVisible;
            if (artRenderer.sprite == (showDouble ? doubleStackSprite : singleSprite))
            {
                return;
            }

            artRenderer.sprite = showDouble ? doubleStackSprite : singleSprite;

            // Normalize for the two sprites' differing pixel sizes, then boost
            // slightly so each coin in the pair art stays near full size.
            float sizeRatio = doubleStackSprite.bounds.size.y > 0.01f
                ? singleSprite.bounds.size.y / doubleStackSprite.bounds.size.y
                : 1f;
            artRenderer.transform.localScale = showDouble
                ? artRestScale * (sizeRatio * doubleStackScale)
                : artRestScale;
        }

        void SetRenderersVisible(bool visible)
        {
            if (renderersVisible == visible || renderers == null)
            {
                return;
            }

            renderersVisible = visible;
            foreach (Renderer coinRenderer in renderers)
            {
                if (coinRenderer != null)
                {
                    coinRenderer.enabled = visible;
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            if (collectParticlePrefab != null)
            {
                Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);
            }

            if (collectClip != null)
            {
                // Self-managed one-shot: spawns its own AudioSource and destroys
                // itself when done, so the clip survives this coin deactivating
                // on the very next line. Global mute is AudioListener.volume
                // (see SoundSettings), which this still mixes through.
                AudioSource.PlayClipAtPoint(collectClip, transform.position, collectVolume);
            }

            if (scoreManager == null)
            {
                scoreManager = FindAnyObjectByType<ScoreManager>();
            }

            if (scoreManager != null)
            {
                scoreManager.AddCoins(coinValue, isRare);
            }

            GameEvents.RaiseCoinCollected(coinValue, isRare, transform.position);

            // Deactivate instead of destroy: coins live inside pooled track
            // chunks and reappear when their chunk is recycled.
            gameObject.SetActive(false);
        }
    }
}
