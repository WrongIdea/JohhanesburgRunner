using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Marks the HUD coin counter as the destination for collect effects.
    /// CoinCollectPop asks this for a world-space point that lines up with the
    /// icon on screen but sits just in front of the gameplay camera, so a
    /// collected coin can fly from the pickup point into the counter without
    /// the effect having to live on the canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CoinHudAnchor : MonoBehaviour
    {
        public static CoinHudAnchor Instance { get; private set; }

        [Tooltip("How far in front of the camera a coin's flight ends.")]
        [SerializeField] float cameraDistance = 3f;

        RectTransform rect;
        Canvas canvas;
        CoinCounterSparkle sparkle;

        void Awake()
        {
            rect = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            sparkle = GetComponent<CoinCounterSparkle>();
        }

        /// <summary>Play the counter sparkle, called when a flying coin lands on the icon.</summary>
        public void PlayArrivalSparkle()
        {
            if (sparkle != null)
            {
                sparkle.Play();
            }
        }

        void OnEnable()
        {
            Instance = this;
        }

        void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// World point the coin should land on, recomputed per frame because the
        /// chase camera keeps moving. False when there is no camera to project
        /// through, in which case the caller falls back to its own animation.
        /// </summary>
        public bool TryGetWorldPoint(Camera camera, out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;
            if (camera == null || rect == null)
            {
                return false;
            }

            // A Screen Space - Overlay canvas has no camera of its own and wants
            // a null one here; every other render mode projects through its own.
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, rect.position);
            worldPoint = camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, cameraDistance));
            return true;
        }
    }
}
