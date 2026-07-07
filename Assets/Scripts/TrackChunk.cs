using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner
{
    public enum ChunkDifficulty
    {
        Easy,
        Medium,
        Hard,
        Special,
    }

    /// <summary>
    /// A hand-designed section of track: obstacles, coins and pickups laid
    /// out over 10–15 metres. Chunks are pooled by the ChunkManager, so this
    /// component captures every child's rest state on first spawn and
    /// restores it each time the chunk is recycled (collected coins come
    /// back, moving taxis return to their posts).
    ///
    /// Fairness contract: <see cref="entrySafeLanes"/> and
    /// <see cref="exitSafeLanes"/> are bitmasks (1=left, 2=centre, 4=right)
    /// naming the lanes a player can occupy at the chunk's start and end
    /// without being forced into a collision. The ChunkManager only chains
    /// chunks whose masks are reachable from each other, so a valid path
    /// always exists.
    /// </summary>
    public class TrackChunk : MonoBehaviour
    {
        [Header("Selection")]
        [SerializeField] ChunkDifficulty difficulty = ChunkDifficulty.Easy;
        [Tooltip("Relative pick weight within its difficulty tier.")]
        [SerializeField] float weight = 10f;

        [Header("Layout")]
        [SerializeField] float length = 15f;
        [Tooltip("Lanes safe at the chunk entry. Bitmask: 1=left, 2=centre, 4=right.")]
        [SerializeField] int entrySafeLanes = 0b111;
        [Tooltip("Lanes safe at the chunk exit. Bitmask: 1=left, 2=centre, 4=right.")]
        [SerializeField] int exitSafeLanes = 0b111;

        struct ChildState
        {
            public Transform Transform;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public bool Active;
        }

        List<ChildState> restStates;
        Transform randomPickGroup;

        public ChunkDifficulty Difficulty => difficulty;
        public float Weight => weight;
        public float Length => length;
        public int EntrySafeLanes => entrySafeLanes;
        public int ExitSafeLanes => exitSafeLanes;

        /// <summary>True when a lane in <paramref name="exitMask"/> is at most one lane away from a lane in <paramref name="entryMask"/>.</summary>
        public static bool Reachable(int exitMask, int entryMask)
        {
            for (int exitLane = 0; exitLane < 3; exitLane++)
            {
                if ((exitMask & (1 << exitLane)) == 0)
                {
                    continue;
                }

                for (int entryLane = 0; entryLane < 3; entryLane++)
                {
                    if ((entryMask & (1 << entryLane)) != 0 && Mathf.Abs(exitLane - entryLane) <= 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Called by the ChunkManager every time this instance is placed on the track.</summary>
        public void OnSpawned()
        {
            if (restStates == null)
            {
                CaptureRestState();
            }
            else
            {
                RestoreRestState();
            }

            foreach (MovingObstacle mover in GetComponentsInChildren<MovingObstacle>(true))
            {
                mover.ResetMotion();
            }

            ActivateRandomPick();
        }

        void CaptureRestState()
        {
            restStates = new List<ChildState>();
            Capture(transform);

            Transform group = transform.Find("RandomPickOne");
            randomPickGroup = group;
        }

        void Capture(Transform parent)
        {
            foreach (Transform child in parent)
            {
                restStates.Add(new ChildState
                {
                    Transform = child,
                    LocalPosition = child.localPosition,
                    LocalRotation = child.localRotation,
                    Active = child.gameObject.activeSelf,
                });
                Capture(child);
            }
        }

        void RestoreRestState()
        {
            foreach (ChildState state in restStates)
            {
                if (state.Transform == null)
                {
                    continue;
                }

                state.Transform.localPosition = state.LocalPosition;
                state.Transform.localRotation = state.LocalRotation;
                state.Transform.gameObject.SetActive(state.Active);
            }
        }

        // A child named "RandomPickOne" holds alternative pickups; exactly
        // one of them is enabled per spawn so the same pooled chunk offers
        // variety without any runtime instantiation.
        void ActivateRandomPick()
        {
            if (randomPickGroup == null || randomPickGroup.childCount == 0)
            {
                return;
            }

            int chosen = Random.Range(0, randomPickGroup.childCount);
            for (int i = 0; i < randomPickGroup.childCount; i++)
            {
                randomPickGroup.GetChild(i).gameObject.SetActive(i == chosen);
            }
        }

        /// <summary>Disables every obstacle in this chunk (used by paid continues).</summary>
        public void DisarmObstacles()
        {
            foreach (RunnerObstacle obstacle in GetComponentsInChildren<RunnerObstacle>(true))
            {
                obstacle.gameObject.SetActive(false);
            }
        }
    }
}
