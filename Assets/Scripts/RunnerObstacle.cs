using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Marker for anything that ends (or shield-absorbs) a run on contact.
    /// Also keeps a registry of live obstacles plus the per-pass state that
    /// PerfectDodge needs to reward a near miss exactly once per obstacle.
    /// </summary>
    public class RunnerObstacle : MonoBehaviour
    {
        public static readonly List<RunnerObstacle> ActiveObstacles = new List<RunnerObstacle>();

        /// <summary>
        /// True once this pass can no longer earn a dodge reward: the player
        /// touched the obstacle, or the reward was already granted. Reset on
        /// every activation for the pooled chunk flow.
        /// </summary>
        [System.NonSerialized] public bool DodgeSpent;

        /// <summary>Closest gap recorded while the player passes this obstacle.</summary>
        [System.NonSerialized] public float ClosestApproach = float.PositiveInfinity;

        public Collider BodyCollider { get; private set; }

        void Awake()
        {
            BodyCollider = GetComponentInChildren<Collider>();
        }

        void OnEnable()
        {
            DodgeSpent = false;
            ClosestApproach = float.PositiveInfinity;
            ActiveObstacles.Add(this);
        }

        void OnDisable()
        {
            ActiveObstacles.Remove(this);
        }
    }
}
