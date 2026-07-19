using TMPro;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Detects near misses and plays the reward package: pooled white speed
    /// streaks, a brief FOV kick (this project has no post-processing volume,
    /// so "motion blur" is faked with a 2-3 degree FOV pulse), a subtle
    /// camera shake (same offset-and-restore pattern as UbuntuPulseVisual),
    /// a whoosh one-shot, a HUD label pop, and score/coin bonuses.
    ///
    /// Detection walks RunnerObstacle.ActiveObstacles each frame (the same
    /// idiom PowerUpManager uses for Coin.ActiveCoins): while an obstacle's
    /// bounds overlap the player's z it records the closest gap, and the
    /// moment the obstacle is fully behind, a gap inside the threshold pays
    /// out exactly once — contact anywhere marks the obstacle spent, so
    /// crashes, side scrapes and shield absorbs never count as dodges.
    /// </summary>
    public class PerfectDodge : MonoBehaviour
    {
        [Header("Detection")]
        // Lanes sit 2.5m apart: a centred adjacent-lane pass gaps ~1.2m, so
        // 0.9 rewards passes while drifting toward the vehicle (mid lane
        // change) and barrier-edge shaves, without firing on every pass.
        [SerializeField] float perfectDodgeDistance = 0.9f;
        [SerializeField] float perfectDodgeCooldown = 0.35f;
        [SerializeField] float trackAheadMeters = 3f;
        [SerializeField] float passedBehindMeters = 0.6f;

        [Header("Rewards")]
        [SerializeField] int rewardScore = 10;
        [SerializeField] int rewardCoins = 0;

        [Header("Feedback Toggles")]
        [SerializeField] bool enableCameraShake = true;
        [SerializeField] bool enableMotionBlur = true;
        [SerializeField] bool enableWhiteStreaks = true;
        [SerializeField] bool enableAudio = true;

        [Header("References")]
        [SerializeField] GameObject vfxPrefab;
        [SerializeField] ScoreManager scoreManager;
        [SerializeField] GameManager gameManager;
        [SerializeField] AudioClip whooshClip;
        [SerializeField] float whooshVolume = 0.65f;
        [SerializeField] TextMeshProUGUI dodgeLabel;

        [Header("Camera Feel")]
        [SerializeField] float shakeSeconds = 0.1f;
        [SerializeField] float shakeAmplitude = 0.04f;
        [SerializeField] float fovKickDegrees = 2.5f;
        [SerializeField] float fovKickSeconds = 0.15f;

        [Header("Label")]
        [SerializeField] float labelSeconds = 0.5f;

        CharacterController controller;
        Camera followCamera;
        float baseFov;
        float playerRadius = 0.35f;
        float nextDodgeTime;
        float shakeTimer;
        float fovTimer;
        float labelTimer;
        Vector3 shakeOffset;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                playerRadius = controller.radius;
            }

            if (scoreManager == null)
            {
                scoreManager = FindAnyObjectByType<ScoreManager>();
            }

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }
        }

        void Start()
        {
            // Pre-warm one pooled instance so no Instantiate happens mid-run.
            if (vfxPrefab != null)
            {
                PerfectDodgeVFX.Spawn(vfxPrefab, new Vector3(0f, -80f, 0f), Quaternion.identity).ReturnToPool();
            }
        }

        void Update()
        {
            AnimateFeedback();

            if (gameManager == null || !gameManager.IsRunning)
            {
                return;
            }

            float playerZ = transform.position.z;
            Vector3 probeCentre = transform.position + Vector3.up * 0.9f;

            for (int i = RunnerObstacle.ActiveObstacles.Count - 1; i >= 0; i--)
            {
                RunnerObstacle obstacle = RunnerObstacle.ActiveObstacles[i];
                if (obstacle == null || obstacle.DodgeSpent || obstacle.BodyCollider == null)
                {
                    continue;
                }

                // Coarse z reject before touching collider bounds.
                float roughZ = obstacle.transform.position.z;
                if (roughZ - playerZ > trackAheadMeters + 12f || playerZ - roughZ > 16f)
                {
                    continue;
                }

                Bounds bounds = obstacle.BodyCollider.bounds;
                if (bounds.min.z > playerZ + trackAheadMeters)
                {
                    continue;
                }

                if (bounds.max.z >= playerZ - passedBehindMeters)
                {
                    // Being passed right now: track the closest approach.
                    Vector3 closest = obstacle.BodyCollider.ClosestPoint(probeCentre);
                    float gap = Vector3.Distance(closest, probeCentre) - playerRadius;
                    if (gap < obstacle.ClosestApproach)
                    {
                        obstacle.ClosestApproach = gap;
                    }
                }
                else
                {
                    // Fully behind the player: settle this pass exactly once.
                    obstacle.DodgeSpent = true;
                    if (obstacle.ClosestApproach <= perfectDodgeDistance && Time.time >= nextDodgeTime)
                    {
                        nextDodgeTime = Time.time + perfectDodgeCooldown;
                        TriggerPerfectDodge();
                    }
                }
            }
        }

        void TriggerPerfectDodge()
        {
            GameEvents.RaisePerfectDodge(transform.position);

            if (enableWhiteStreaks && vfxPrefab != null)
            {
                PerfectDodgeVFX.Spawn(vfxPrefab, transform.position + Vector3.up * 0.9f, transform.rotation);
            }

            if (enableAudio && whooshClip != null)
            {
                AudioSource.PlayClipAtPoint(whooshClip, transform.position, whooshVolume);
            }

            if (enableCameraShake)
            {
                shakeTimer = shakeSeconds;
            }

            if (enableMotionBlur)
            {
                fovTimer = fovKickSeconds;
            }

            if (scoreManager != null)
            {
                if (rewardScore > 0)
                {
                    scoreManager.AddPoints(rewardScore);
                }

                if (rewardCoins > 0)
                {
                    scoreManager.AddCoins(rewardCoins, false);
                }
            }

            if (dodgeLabel != null)
            {
                labelTimer = labelSeconds;
                dodgeLabel.gameObject.SetActive(true);
            }
        }

        void AnimateFeedback()
        {
            if (followCamera == null)
            {
                followCamera = Camera.main;
                if (followCamera != null)
                {
                    baseFov = followCamera.fieldOfView;
                }
            }

            if (fovTimer > 0f && followCamera != null)
            {
                fovTimer -= Time.deltaTime;
                // Fast attack, smooth release back to the exact base FOV.
                float progress = 1f - Mathf.Clamp01(fovTimer / Mathf.Max(0.01f, fovKickSeconds));
                float envelope = progress < 0.3f ? progress / 0.3f : 1f - (progress - 0.3f) / 0.7f;
                followCamera.fieldOfView = baseFov + fovKickDegrees * envelope;
                if (fovTimer <= 0f)
                {
                    followCamera.fieldOfView = baseFov;
                }
            }

            if (labelTimer > 0f && dodgeLabel != null)
            {
                labelTimer -= Time.deltaTime;
                float progress = 1f - Mathf.Clamp01(labelTimer / Mathf.Max(0.01f, labelSeconds));
                float scale = progress < 0.25f
                    ? Mathf.Lerp(0.6f, 1.15f, progress / 0.25f)
                    : Mathf.Lerp(1.15f, 1f, (progress - 0.25f) / 0.75f);
                dodgeLabel.rectTransform.localScale = new Vector3(scale, scale, 1f);
                Color color = dodgeLabel.color;
                color.a = 1f - progress * progress;
                dodgeLabel.color = color;
                if (labelTimer <= 0f)
                {
                    dodgeLabel.gameObject.SetActive(false);
                }
            }
        }

        void LateUpdate()
        {
            if (shakeOffset != Vector3.zero && followCamera != null)
            {
                followCamera.transform.localPosition -= shakeOffset;
                shakeOffset = Vector3.zero;
            }

            if (shakeTimer <= 0f || followCamera == null)
            {
                return;
            }

            shakeTimer -= Time.deltaTime;
            float strength = shakeAmplitude * Mathf.Clamp01(shakeTimer / Mathf.Max(0.01f, shakeSeconds));
            shakeOffset = Random.insideUnitSphere * strength;
            shakeOffset.z = 0f;
            followCamera.transform.localPosition += shakeOffset;
        }
    }
}
