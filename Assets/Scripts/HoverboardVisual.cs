using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Rides the selected hoverboard while the Hoverboard shield is active:
    /// the runner stops animating and stands on the board, and the whole
    /// runner-plus-board rises off the road with a gentle hover bob. The lift
    /// is applied through RunnerVisualGrounder (which otherwise pins the feet
    /// to street level every frame) so the character and the board move as one
    /// piece and the board stays directly under the feet. Which board shows
    /// comes from BoardInventory.SelectedIndex, chosen on the Boards page.
    /// </summary>
    public class HoverboardVisual : MonoBehaviour
    {
        [SerializeField] PowerUpManager powerUpManager;
        [Tooltip("Container (under the lean pivot) that holds the board visuals; its Y is driven so the deck sits under the feet.")]
        [SerializeField] Transform boardMount;
        [SerializeField] GameObject[] boardVisuals;
        [Tooltip("Must match RunnerVisualGrounder.groundSink so the board's top lines up with the planted feet.")]
        [SerializeField] float groundSink = 0.35f;
        [Tooltip("How far the runner-plus-board floats above the road while riding.")]
        [SerializeField] float rideHeight = 0.5f;
        [SerializeField] float blendSeconds = 0.25f;
        [SerializeField] float bobAmplitude = 0.05f;
        [SerializeField] float bobFrequency = 1.4f;
        [Tooltip("Run-clip frame frozen as the standing-on-the-board pose.")]
        [SerializeField] float standPoseFrame = 0.02f;

        float blend;
        bool wasRiding;
        GameObject activeBoard;

        RunnerVisualGrounder grounder;
        Animator animator;

        void LateUpdate()
        {
            bool riding = powerUpManager != null && powerUpManager.HasShield;

            if (riding && !wasRiding)
            {
                BeginRide();
            }
            else if (!riding && wasRiding)
            {
                EndRide();
            }

            wasRiding = riding;

            blend = Mathf.MoveTowards(blend, riding ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, blendSeconds));

            // Once fully settled back down, drop the board and clear the lift.
            if (!riding && blend <= 0f)
            {
                if (activeBoard != null)
                {
                    activeBoard.SetActive(false);
                    activeBoard = null;
                }

                if (grounder != null)
                {
                    grounder.RideLift = 0f;
                    grounder = null;
                }

                if (boardMount != null)
                {
                    boardMount.localPosition = new Vector3(0f, 0f, boardMount.localPosition.z);
                }

                return;
            }

            float bob = Mathf.Sin(Time.time * bobFrequency * 2f * Mathf.PI) * bobAmplitude;
            float lift = (rideHeight + bob) * blend;

            // Lift the character through the grounder (which owns the feet
            // height) and drop the board mount so the deck top lands exactly on
            // the lifted feet: mount.y = feetLift - groundSink puts the board's
            // baked top (one groundSink above the mount at rest) at the feet.
            if (grounder != null)
            {
                grounder.RideLift = lift;
            }

            if (boardMount != null)
            {
                boardMount.localPosition = new Vector3(0f, lift - groundSink, boardMount.localPosition.z);
            }
        }

        void BeginRide()
        {
            // Active-only lookups: with selectable characters only the chosen
            // visual is enabled, so grab its grounder and animator now.
            grounder = GetComponentInChildren<RunnerVisualGrounder>();
            animator = GetComponentInChildren<Animator>();

            if (boardVisuals != null && boardVisuals.Length > 0)
            {
                int index = Mathf.Clamp(BoardInventory.SelectedIndex, 0, boardVisuals.Length - 1);
                activeBoard = boardVisuals[index];
                if (activeBoard != null)
                {
                    activeBoard.SetActive(true);
                }
            }

            // Freeze the runner on a near-upright run frame so it stands still
            // on the board instead of pumping its legs (same freeze trick the
            // drone uses for its flying pose).
            if (animator != null)
            {
                animator.Play("Run", 0, standPoseFrame);
                animator.Update(0f);
                animator.speed = 0f;
            }
        }

        void EndRide()
        {
            if (animator != null)
            {
                animator.speed = 1f;
            }
        }

        void OnDisable()
        {
            if (animator != null)
            {
                animator.speed = 1f;
            }

            if (grounder != null)
            {
                grounder.RideLift = 0f;
            }

            if (boardMount != null)
            {
                boardMount.localPosition = new Vector3(0f, 0f, boardMount.localPosition.z);
            }

            if (activeBoard != null)
            {
                activeBoard.SetActive(false);
                activeBoard = null;
            }

            blend = 0f;
            wasRiding = false;
            grounder = null;
            animator = null;
        }
    }
}
