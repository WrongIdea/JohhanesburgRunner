using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Walks a pooled pedestrian steadily along the pavement in its socket's
    /// forward direction. The <see cref="WalkingPedestrian"/> sockets are yawed to
    /// face world -Z, so the pedestrian travels opposite to the player (who runs
    /// +Z) and passes by them. The visual is animated in place (root motion off);
    /// this component supplies the actual translation.
    /// </summary>
    /// <remarks>
    /// Like <see cref="CrossingPedestrian"/>, only the <c>actor</c> child moves, in
    /// root-local space, so the walk is independent of where the decorator places
    /// the socketed root. The actor's clean home position is cached in
    /// <see cref="Awake"/> (before any Update can shift it) and restored on every
    /// enable, so repeated pooling never drifts the start point.
    ///
    /// A tiny static census enforces "one of each character visible at a time":
    /// active walkers register here, and <see cref="AnyInView"/> reports whether a
    /// given character is still in front of the camera. The decorator consults it
    /// before spawning, so a new man/woman only appears once the previous one of
    /// the same kind has passed out of view.
    /// </remarks>
    public sealed class PavementWalker : MonoBehaviour
    {
        [SerializeField] Transform actor;

        [Tooltip("0 = woman, 1 = man. Distinguishes characters for the one-in-view rule.")]
        [SerializeField] int characterId;

        [Tooltip("Metres per second along the pavement (opposite the player).")]
        [SerializeField] float walkingSpeed = 1.3f;

        [Tooltip("How far it walks before wrapping back to the start. Kept large "
            + "enough that the wrap always happens off-screen behind the player.")]
        [SerializeField] float wrapDistance = 140f;

        // A walker counts as "in view" until it is this far behind the camera.
        const float BehindViewMargin = 3f;

        static readonly List<PavementWalker> Active = new List<PavementWalker>();

        Vector3 actorHome;
        float travelled;
        Animator animator;

        public int CharacterId => characterId;

        void Awake()
        {
            if (actor == null && transform.childCount > 0)
            {
                actor = transform.GetChild(0);
            }
            actorHome = actor != null ? actor.localPosition : Vector3.zero;
            animator = actor != null ? actor.GetComponent<Animator>() : null;
        }

        void OnEnable()
        {
            travelled = 0f;
            if (actor != null)
            {
                actor.localPosition = actorHome;
            }
            if (animator != null)
            {
                animator.speed = 1f;
            }
            if (!Active.Contains(this))
            {
                Active.Add(this);
            }
        }

        void OnDisable()
        {
            Active.Remove(this);
        }

        void Update()
        {
            if (actor == null)
            {
                return;
            }

            travelled += walkingSpeed * Time.deltaTime;
            if (travelled >= wrapDistance)
            {
                travelled -= wrapDistance;
            }

            // Root-local +Z; the socket's 180 yaw maps that to world -Z.
            actor.localPosition = actorHome + Vector3.forward * travelled;
        }

        /// <summary>
        /// True if any active walker of <paramref name="id"/> is still in front of
        /// the camera (not yet passed out of view). The camera looks down +Z, so a
        /// walker is out of view once it is behind the camera plane.
        /// </summary>
        public static bool AnyInView(int id)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return false;
            }
            float viewZ = cam.transform.position.z;
            for (int i = 0; i < Active.Count; i++)
            {
                PavementWalker w = Active[i];
                if (w == null || w.characterId != id)
                {
                    continue;
                }
                float z = w.actor != null ? w.actor.position.z : w.transform.position.z;
                if (z > viewZ - BehindViewMargin)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
