using System.Collections.Generic;
using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Dresses a single road tile from a <see cref="DistrictDecorProfile"/>. Lives
    /// on the RoadSegment prefab alongside <see cref="RoadSegmentVisuals"/>. All
    /// sockets are cached once in <see cref="Awake"/>; decoration is applied only
    /// when the spawner (re)assigns the tile a district, never per frame.
    ///
    /// Placement is deterministic from the segment seed, so a dressed tile stays
    /// stable until it is recycled, and it never touches gameplay objects – props
    /// are clamped clear of the three lanes and tested for mutual overlap.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SegmentDecorator : MonoBehaviour
    {
        [Tooltip("Legacy hand-baked decoration (fixed trees/lights). Shown only when " +
                 "the socket system is toggled OFF, so you can A/B the two approaches.")]
        [SerializeField] GameObject legacyDecorRoot;

        // Half-width of the gameplay area the props must stay clear of. Lanes sit
        // at x = -2.7 / 0 / +2.7; a vehicle spans ~1.2, so 4.6 leaves head-room.
        const float LaneKeepOut = 4.6f;

        DecorSocket[] sockets;
        // Independent hero slots per street side so a tile can show one or both.
        GameObject heroLeft, heroRight;
        HeroPlaceholderGroup affectedLeft, affectedRight;
        readonly List<GameObject> activeInstances = new List<GameObject>(32);
        readonly List<Placed> placed = new List<Placed>(32);
        readonly Dictionary<DecorSocketType, int> typeCounts = new Dictionary<DecorSocketType, int>();
        MaterialPropertyBlock mpb;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        struct Placed
        {
            public Vector3 Position;
            public float Radius;
        }

        // Order sockets are resolved in: bigger, position-defining props first so
        // lamps/bins can yield to trees and bus stops (avoids lamp-in-trunk).
        static int Priority(DecorSocketType type) => type switch
        {
            DecorSocketType.BusStop => 0,
            DecorSocketType.TrafficLight => 1,
            DecorSocketType.Billboard => 2,
            DecorSocketType.Tree => 3,
            DecorSocketType.Bench => 4,
            DecorSocketType.Lamp => 5,
            DecorSocketType.Bin => 6,
            _ => 7
        };

        void Awake()
        {
            EnsureSockets();
        }

        // Cache sockets once, sorted by resolve priority. Called from Awake at
        // runtime and lazily from Decorate so an editor capture (where Awake does
        // not run) can still drive the decorator. Spawned props are parented to
        // their socket, which lives under the "DecorSockets" group that
        // RoadSegmentVisuals excludes from static batching.
        void EnsureSockets()
        {
            if (sockets != null)
            {
                return;
            }

            sockets = GetComponentsInChildren<DecorSocket>(true);
            System.Array.Sort(sockets, (a, b) =>
            {
                int pa = Priority(a.SocketType), pb = Priority(b.SocketType);
                return pa != pb ? pa.CompareTo(pb) : a.name.CompareTo(b.name);
            });
        }

        /// <summary>Legacy mode: keep the baked decoration, spawn nothing from sockets.</summary>
        public void ApplyLegacyMode(DecorPool pool)
        {
            EnsureSockets();
            ReturnAll(pool);
            if (legacyDecorRoot != null)
            {
                legacyDecorRoot.SetActive(true);
            }
        }

        /// <summary>
        /// Socket mode: hide legacy decoration and re-dress the tile from the given
        /// profile. Deterministic in <paramref name="seed"/>; <paramref name="isIntersection"/>
        /// gates intersection-only props (traffic lights).
        /// </summary>
        public void Decorate(
            DistrictDecorProfile profile, int seed, bool isIntersection, DecorPool pool,
            DecorSocketType? onlySocketType = null)
        {
            if (legacyDecorRoot != null)
            {
                legacyDecorRoot.SetActive(false);
            }

            EnsureSockets();
            ReturnAll(pool);

            if (profile == null || sockets == null || pool == null)
            {
                return;
            }

            mpb ??= new MaterialPropertyBlock();
            var rng = new System.Random(seed);

            for (int i = 0; i < sockets.Length; i++)
            {
                DecorSocket socket = sockets[i];
                if (onlySocketType.HasValue && socket.SocketType != onlySocketType.Value)
                {
                    continue;
                }
                DecorRule rule = profile.GetRule(socket.SocketType);
                if (rule == null || rule.allowedPrefabs == null || rule.allowedPrefabs.Length == 0)
                {
                    continue;
                }

                if (rule.requiresIntersection && !isIntersection)
                {
                    continue;
                }

                if (CountOf(rule.socketType) >= rule.maxCountPerSegment)
                {
                    continue;
                }

                // Deterministic probability roll.
                if (NextFloat(rng) > rule.spawnProbability)
                {
                    continue;
                }

                Vector3 pos = ResolvePosition(socket, rule, rng);
                if (Overlaps(pos, rule.collisionRadius))
                {
                    continue;
                }

                GameObject prefab = PickPrefab(rule, rng);
                GameObject instance = pool.Get(prefab, socket.transform);
                if (instance == null)
                {
                    continue;
                }

                // Position relative to the socket (identity-transform anchor).
                instance.transform.localPosition = socket.transform.InverseTransformPoint(pos);
                instance.transform.localRotation = Quaternion.Euler(0f, NextRange(rng, rule.yawJitterRange), 0f);
                float scale = NextRange(rng, rule.scaleRange);
                instance.transform.localScale = new Vector3(scale, scale, scale);

                if (rule.tintVariation > 0f)
                {
                    ApplyTint(instance, rng, rule.tintVariation);
                }

                activeInstances.Add(instance);
                placed.Add(new Placed { Position = pos, Radius = rule.collisionRadius });
                Increment(rule.socketType);
            }
        }

        /// <summary>
        /// Sets (or clears) the hero building on one street side. <paramref name="side"/>
        /// must be Left or Right (use <see cref="ClearHeroes"/> to clear the whole tile);
        /// a null <paramref name="entry"/> clears just that side. Left and right are
        /// independent slots, so a tile can show one or both. Heroes come from a dedicated
        /// low-count pool, keep their own LODGroup, and never join the district cull LODGroup.
        /// </summary>
        public void SetHero(HeroEntry entry, HeroSide side, DecorPool heroPool)
        {
            if (side != HeroSide.Left && side != HeroSide.Right)
            {
                return;
            }

            bool left = side == HeroSide.Left;
            ReleaseSlot(left, heroPool);

            if (entry == null || entry.prefab == null || heroPool == null)
            {
                return;
            }

            EnsureSockets();
            DecorSocket socket = FindHeroSocket(side);
            if (socket == null)
            {
                return;
            }

            GameObject instance = heroPool.Get(entry.prefab, socket.transform);
            if (instance == null)
            {
                return;
            }

            // Angle the façade toward the oncoming player, signed per side (the socket
            // already faces the road; +yaw on the left / -yaw on the right both turn the
            // façade toward the camera).
            float faceYaw = (left ? 1f : -1f) * entry.facePlayerDegrees;
            instance.transform.localPosition = entry.localPositionOffset;
            instance.transform.localRotation = Quaternion.Euler(
                entry.eulerOffset.x, entry.eulerOffset.y + faceYaw, entry.eulerOffset.z);
            instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, entry.uniformScale);

            // Non-interactive roadside visual: strip physics interaction at runtime.
            SetCollidersEnabled(instance, false);

            HeroBuildingSocket heroSocket = socket.GetComponent<HeroBuildingSocket>();
            HeroPlaceholderGroup grp = null;
            if (heroSocket != null && heroSocket.PlaceholderGroup != null)
            {
                heroSocket.PlaceholderGroup.Hide(socket.transform.position, entry.footprintRadius);
                grp = heroSocket.PlaceholderGroup;
            }

            HideBlockingProps(socket, entry.footprintRadius);

            if (left) { heroLeft = instance; affectedLeft = grp; }
            else { heroRight = instance; affectedRight = grp; }
        }

        // Removes only the façade-blocking décor (trees, billboards) on the hero's side
        // that fall inside its footprint — the jacaranda canopy, traffic lights, benches
        // and other street furniture are all kept. Hidden props are re-dressed fresh on
        // the next Decorate (ReturnAll returns them to the pool), so no restore is needed.
        void HideBlockingProps(DecorSocket heroSocket, float radius)
        {
            if (sockets == null)
            {
                return;
            }

            Vector3 c = heroSocket.transform.position;
            float r2 = radius * radius;
            bool heroIsLeft = heroSocket.Side == DecorSocket.StreetSide.Left;

            for (int i = 0; i < sockets.Length; i++)
            {
                DecorSocket s = sockets[i];
                if (s.SocketType != DecorSocketType.Tree && s.SocketType != DecorSocketType.Billboard)
                {
                    continue; // keep traffic lights, benches, lamps, bins, bus stops
                }

                if ((s.Side == DecorSocket.StreetSide.Left) != heroIsLeft)
                {
                    continue; // only the hero's own side
                }

                Vector3 d = s.transform.position - c;
                d.y = 0f;
                if (d.sqrMagnitude > r2)
                {
                    continue;
                }

                foreach (Transform child in s.transform)
                {
                    if (child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        /// <summary>Releases both hero slots and restores all placeholders. Called on every
        /// dress so a recycled tile returns to full default state before new content.</summary>
        public void ClearHeroes(DecorPool heroPool)
        {
            ReleaseSlot(true, heroPool);
            ReleaseSlot(false, heroPool);
        }

        void ReleaseSlot(bool left, DecorPool heroPool)
        {
            HeroPlaceholderGroup grp = left ? affectedLeft : affectedRight;
            if (grp != null)
            {
                grp.RestoreAll();
                if (left) affectedLeft = null; else affectedRight = null;
            }

            GameObject hero = left ? heroLeft : heroRight;
            if (hero != null)
            {
                ResetHeroTransform(hero);
                if (heroPool != null) heroPool.Release(hero);
                else hero.SetActive(false);
                if (left) heroLeft = null; else heroRight = null;
            }
        }

        DecorSocket FindHeroSocket(HeroSide side)
        {
            if (sockets == null)
            {
                return null;
            }

            for (int i = 0; i < sockets.Length; i++)
            {
                DecorSocket s = sockets[i];
                if (s.SocketType != DecorSocketType.HeroBuilding)
                {
                    continue;
                }

                bool isLeft = s.Side == DecorSocket.StreetSide.Left;
                if ((side == HeroSide.Left && isLeft) || (side == HeroSide.Right && !isLeft))
                {
                    return s;
                }
            }

            return null;
        }

        static void SetCollidersEnabled(GameObject go, bool enabled)
        {
            var colliders = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = enabled;
            }
        }

        static void ResetHeroTransform(GameObject go)
        {
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        Vector3 ResolvePosition(DecorSocket socket, DecorRule rule, System.Random rng)
        {
            Vector3 world = socket.transform.position;
            float sideSign = socket.Side == DecorSocket.StreetSide.Left ? -1f : 1f;

            float jitterX = (NextFloat(rng) * 2f - 1f) * rule.positionJitter.x;
            float jitterZ = (NextFloat(rng) * 2f - 1f) * rule.positionJitter.y;

            world += transform.right * (jitterX + sideSign * rule.sidewalkOffset);
            world += transform.forward * jitterZ;

            // Hard safety: never let a prop drift into the gameplay lanes. Measured
            // in the segment's local X so it holds regardless of tile rotation.
            Vector3 local = transform.InverseTransformPoint(world);
            if (Mathf.Abs(local.x) < LaneKeepOut)
            {
                local.x = Mathf.Sign(local.x == 0f ? sideSign : local.x) * LaneKeepOut;
                world = transform.TransformPoint(local);
            }

            return world;
        }

        bool Overlaps(Vector3 pos, float radius)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                float minDist = placed[i].Radius + radius;
                if ((placed[i].Position - pos).sqrMagnitude < minDist * minDist)
                {
                    return true;
                }
            }

            return false;
        }

        static GameObject PickPrefab(DecorRule rule, System.Random rng)
        {
            if (rule.allowedPrefabs.Length == 1)
            {
                return rule.allowedPrefabs[0];
            }

            int index = rng.Next(rule.allowedPrefabs.Length);
            return rule.allowedPrefabs[index];
        }

        void ApplyTint(GameObject instance, System.Random rng, float variation)
        {
            float t = (NextFloat(rng) * 2f - 1f) * variation;
            // A gentle hue-ish nudge: lift/drop the blue and red channels slightly.
            Color mul = new Color(1f + t, 1f - t * 0.4f, 1f + t * 0.6f, 1f);

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                Material shared = r.sharedMaterial;
                Color baseColor = shared != null && shared.HasProperty(BaseColorId)
                    ? shared.GetColor(BaseColorId)
                    : (shared != null && shared.HasProperty(ColorId) ? shared.GetColor(ColorId) : Color.white);

                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, baseColor * mul);
                mpb.SetColor(ColorId, baseColor * mul);
                r.SetPropertyBlock(mpb);
            }
        }

        void ReturnAll(DecorPool pool)
        {
            if (pool != null)
            {
                for (int i = 0; i < activeInstances.Count; i++)
                {
                    pool.Release(activeInstances[i]);
                }
            }

            activeInstances.Clear();
            placed.Clear();
            typeCounts.Clear();
        }

        int CountOf(DecorSocketType type) => typeCounts.TryGetValue(type, out int c) ? c : 0;
        void Increment(DecorSocketType type) => typeCounts[type] = CountOf(type) + 1;

        static float NextFloat(System.Random rng) => (float)rng.NextDouble();
        static float NextRange(System.Random rng, Vector2 range) =>
            Mathf.Lerp(range.x, range.y, NextFloat(rng));

#if UNITY_EDITOR
        /// <summary>Editor-only read of cached sockets for validation tooling.</summary>
        public DecorSocket[] EditorSockets => GetComponentsInChildren<DecorSocket>(true);

        /// <summary>Editor-only: a pooled hero on this tile (left preferred), or null.</summary>
        public GameObject EditorActiveHero => heroLeft != null ? heroLeft : heroRight;

        /// <summary>Editor-only: the left/right hero instances (either may be null).</summary>
        public GameObject EditorHeroLeft => heroLeft;
        public GameObject EditorHeroRight => heroRight;

        /// <summary>Editor-only snapshot for capture/diagnostic logging.</summary>
        public string DebugState()
        {
            string legacy = legacyDecorRoot == null ? "NULL-ref"
                : legacyDecorRoot.activeSelf ? "ACTIVE(!)" : "hidden";
            return $"legacyDecorRoot={legacy}, sockets={(sockets?.Length ?? -1)}, spawned={activeInstances.Count}";
        }
#endif
    }
}
