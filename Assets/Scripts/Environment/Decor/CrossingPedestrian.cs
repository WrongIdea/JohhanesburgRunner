using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Moves a pooled pedestrian across the zebra once, then leaves him waiting
    /// on the opposite pavement. Active crossings also expose a stop line to taxis.
    /// </summary>
    public sealed class CrossingPedestrian : MonoBehaviour
    {
        static readonly List<CrossingPedestrian> Active = new List<CrossingPedestrian>();

        [SerializeField] Transform actor;
        [SerializeField] float crossingDistance = 8.4f;
        [SerializeField] float walkingSpeed = 1.15f;
        [SerializeField] float taxiStopDistance = 3.8f;
        [SerializeField] float taxiYieldLookAhead = 14f;

        Vector3 origin;
        Quaternion baseRotation;
        float travelled;
        Animator animator;
        bool finished;

        public bool IsCrossing => isActiveAndEnabled && !finished;
        public float CrossingWorldZ => transform.position.z;

        void Awake()
        {
            if (actor == null && transform.childCount > 0)
            {
                actor = transform.GetChild(0);
            }
            animator = actor != null ? actor.GetComponent<Animator>() : null;
            CacheOrigin();
        }

        void OnEnable()
        {
            if (!Active.Contains(this))
            {
                Active.Add(this);
            }
            CacheOrigin();
            travelled = 0f;
            finished = false;
            if (animator != null)
            {
                animator.speed = 1f;
            }
            ApplyPose();
        }

        void OnDisable()
        {
            Active.Remove(this);
        }

        void Update()
        {
            if (actor == null || finished)
            {
                return;
            }

            travelled += walkingSpeed * Time.deltaTime;
            if (travelled >= crossingDistance)
            {
                travelled = crossingDistance;
                finished = true;
            }
            ApplyPose();

            if (finished && animator != null)
            {
                // Hold the final walking pose instead of looping in place.
                animator.speed = 0f;
            }
        }

        void CacheOrigin()
        {
            if (actor == null)
            {
                return;
            }
            origin = actor.localPosition;
            baseRotation = actor.localRotation;
        }

        void ApplyPose()
        {
            if (actor == null)
            {
                return;
            }
            actor.localPosition = origin + Vector3.forward * travelled;
            actor.localRotation = baseRotation;
        }

        /// <summary>
        /// Returns a stop position when a vehicle travelling along world Z is
        /// approaching an occupied zebra crossing. Direction is +1 or -1.
        /// </summary>
        public static bool TryGetTaxiStopZ(float vehicleZ, float direction, out float stopZ)
        {
            for (int i = 0; i < Active.Count; i++)
            {
                CrossingPedestrian crossing = Active[i];
                if (crossing == null || !crossing.IsCrossing)
                {
                    continue;
                }

                float signedAhead = (crossing.CrossingWorldZ - vehicleZ) * direction;
                if (signedAhead < 0f || signedAhead > crossing.taxiYieldLookAhead)
                {
                    continue;
                }

                stopZ = crossing.CrossingWorldZ - direction * crossing.taxiStopDistance;
                return true;
            }

            stopZ = 0f;
            return false;
        }
    }
}
