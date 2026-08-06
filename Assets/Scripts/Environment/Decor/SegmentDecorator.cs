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

        [Tooltip("Visual-only zebra crossing embedded in the road tile. The decor director " +
                 "enables it for selected traffic-light intersections.")]
        [SerializeField] GameObject pedestrianCrossingRoot;

        // Shared keep-out follows the narrowed production lane centres.
        const float LaneKeepOut = RoadMetrics.LaneKeepOut;

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
            public DecorSocketType Type;
            public DecorSocket.StreetSide Side;
        }

        // Order sockets are resolved in: bigger, position-defining props first so
        // lamps/bins can yield to trees and bus stops (avoids lamp-in-trunk).
        static int Priority(DecorSocketType type) => type switch
        {
            DecorSocketType.BusStop => 0,
            DecorSocketType.TrafficLight => 1,
            DecorSocketType.Pedestrian => 2,
            DecorSocketType.WavingPedestrian => 2,
            DecorSocketType.Billboard => 3,
            DecorSocketType.UtilityBox => 4,
            DecorSocketType.Planter => 4,
            DecorSocketType.Tree => 4,
            DecorSocketType.Bench => 5,
            DecorSocketType.Lamp => 6,
            DecorSocketType.Bin => 7,
            _ => 8
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
            DecorSocketType? onlySocketType = null, bool showPedestrianCrossing = false,
            int decorationSequence = int.MaxValue,
            JunctionType junctionType = JunctionType.None)
        {
            if (pedestrianCrossingRoot != null)
            {
                pedestrianCrossingRoot.SetActive(showPedestrianCrossing);
            }

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
            int paletteIndex = rng.Next(5);

            for (int i = 0; i < sockets.Length; i++)
            {
                DecorSocket socket = sockets[i];
                if (SocketBlockedByJunction(socket, junctionType))
                {
                    continue;
                }
                if (onlySocketType.HasValue && socket.SocketType != onlySocketType.Value)
                {
                    continue;
                }
                DecorRule rule = profile.GetRule(socket.SocketType);
                if (rule == null || rule.allowedPrefabs == null || rule.allowedPrefabs.Length == 0)
                {
                    continue;
                }

                if (decorationSequence < rule.minimumDecorationSequence)
                {
                    continue;
                }

                if (rule.requiresIntersection && !isIntersection)
                {
                    continue;
                }

                if (rule.requiresPedestrianCrossing && !showPedestrianCrossing)
                {
                    continue;
                }

                if (rule.requiresNoPedestrianCrossing && showPedestrianCrossing)
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
                if (socket.SocketType != DecorSocketType.BusStop && Overlaps(pos, rule, socket))
                {
                    continue;
                }

                GameObject prefab;
                if (socket.SocketType == DecorSocketType.WalkingPedestrian)
                {
                    // Only one of each character may be on screen at a time; a new
                    // one spawns only once the previous of that kind has left view.
                    prefab = PickAvailableWalker(rule, rng);
                    if (prefab == null)
                    {
                        continue;
                    }
                }
                else
                {
                    prefab = PickPrefab(rule, socket, rng);
                }
                if (prefab != null && prefab.name.Contains("DecorBusStopCluster"))
                {
                    pos = ResolveBusStandBuildingGap(profile, pos);
                }
                if (socket.SocketType == DecorSocketType.BusStop && Overlaps(pos, rule, socket))
                {
                    continue;
                }
                GameObject instance = pool.Get(prefab, socket.transform);
                if (instance == null)
                {
                    continue;
                }

                // Socket decoration is visual street furniture outside the gameplay lanes.
                // Approved generated prefabs may carry review colliders, so disable them
                // when pooled as roadside décor to avoid invisible physics interactions.
                SetCollidersEnabled(instance, socket.SocketType == DecorSocketType.Pedestrian);

                // Position relative to the socket (identity-transform anchor).
                instance.transform.localPosition = socket.transform.InverseTransformPoint(pos);
                float yaw = NextRange(rng, rule.yawJitterRange);
                RoadsidePropOrientation propOrientation =
                    instance.GetComponent<RoadsidePropOrientation>();
                if (socket.SocketType == DecorSocketType.BusStop &&
                    instance.name.Contains("DecorBusStopCluster"))
                {
                    // Set an explicit segment-space facing instead of composing
                    // with each socket's unrelated vendor-stall yaw: left-side
                    // shelters face right (+X), right-side shelters face left (-X).
                    Vector3 towardRoad = transform.TransformDirection(
                        socket.Side == DecorSocket.StreetSide.Left
                            ? Vector3.right
                            : Vector3.left);
                    // This Meshy shelter's open side is local -Z, opposite the
                    // imported model-forward axis, so point +Z away from the road.
                    Quaternion roadFacing = Quaternion.LookRotation(-towardRoad, transform.up);
                    instance.transform.rotation = propOrientation != null
                        ? propOrientation.ApplyAfter(roadFacing)
                        : roadFacing;
                }
                else
                {
                    Quaternion roadFacing = Quaternion.Euler(0f, yaw, 0f);
                    instance.transform.localRotation = propOrientation != null
                        ? propOrientation.ApplyAfter(roadFacing)
                        : roadFacing;
                }
                float scale = NextRange(rng, rule.scaleRange);
                instance.transform.localScale = new Vector3(scale, scale, scale);

                // Pitch/roll offsets rotate around the prefab root and can swing
                // an imported mesh below the pavement even when its original
                // prefab was correctly grounded. Re-ground only tilted prefabs
                // after their final rotation and scale; yaw-only and legacy props
                // retain their exact existing Y placement. X/Z never changes.
                if (propOrientation != null && propOrientation.RequiresPostRotationGrounding)
                {
                    GroundTiltedProp(instance, pos.y);
                }

                if (rule.tintVariation > 0f || rule.useJacarandaPalette)
                {
                    ApplyTint(instance, rng, rule, paletteIndex++);
                }

                activeInstances.Add(instance);
                placed.Add(new Placed
                {
                    Position = pos,
                    Radius = rule.collisionRadius,
                    Type = rule.socketType,
                    Side = socket.Side
                });
                Increment(rule.socketType);
            }
        }

        /// <summary>
        /// Bus shelters snap to the left pavement's bus-stand slot in the spread
        /// street-furniture layout instead of using generic longitudinal jitter,
        /// so the shelter stays 10 m clear of the electric box and planter on that
        /// pavement on every procedural re-dress. X/Y are preserved, so pavement
        /// distance and grounding are unchanged. Fruit stalls share the BusStop
        /// socket type but intentionally do not use this correction.
        /// </summary>
        Vector3 ResolveBusStandBuildingGap(DistrictDecorProfile profile, Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            _ = profile; // District no longer changes the shelter's z slot.
            // The shelter snaps to the left pavement's bus-stand slot in the spread
            // street-furniture layout (z = 9 box / 19 bus / 29 planter) rather than
            // a per-district building gap, keeping it 10 m clear of its neighbours
            // on every procedural re-dress. X/Y are preserved.
            local.z = 19f;
            return transform.TransformPoint(local);
        }

        static void GroundTiltedProp(GameObject instance, float pavementWorldY)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 position = instance.transform.position;
            position.y += pavementWorldY - bounds.min.y;
            instance.transform.position = position;
        }

        static bool SocketBlockedByJunction(DecorSocket socket, JunctionType junctionType)
        {
            if (junctionType == JunctionType.None ||
                socket.SocketType == DecorSocketType.TrafficLight ||
                socket.SocketType == DecorSocketType.Pedestrian ||
                socket.SocketType == DecorSocketType.WavingPedestrian)
            {
                return false;
            }

            float z = socket.transform.localPosition.z;
            if (z < 1f || z > 13f)
            {
                return false;
            }

            bool left = socket.Side == DecorSocket.StreetSide.Left;
            return junctionType == JunctionType.Crossroad ||
                   (junctionType == JunctionType.TLeft && left) ||
                   (junctionType == JunctionType.TRight && !left);
        }

        /// <summary>
        /// Sets (or clears) the hero building on one street side. <paramref name="side"/>
        /// must be Left or Right (use <see cref="ClearHeroes"/> to clear the whole tile);
        /// a null <paramref name="entry"/> clears just that side. Left and right are
        /// independent slots, so a tile can show one or both. Heroes come from a dedicated
        /// low-count pool, keep their own LODGroup, and never join the district cull LODGroup.
        /// </summary>
        public void SetHero(
            HeroEntry entry, HeroSide side, DecorPool heroPool,
            bool forceFaceRunner = false)
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
            float faceYaw = (left ? 1f : -1f) *
                            (forceFaceRunner ? 90f : entry.facePlayerDegrees);
            instance.transform.localPosition = entry.localPositionOffset;
            instance.transform.localRotation = Quaternion.Euler(
                entry.eulerOffset.x, entry.eulerOffset.y + faceYaw, entry.eulerOffset.z);
            instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, entry.uniformScale);
            ApplyHeroTint(instance, entry.colorTint);
            if (forceFaceRunner)
            {
                PushHeroOutsidePavement(instance, left);
            }

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

        // A 90-degree façade turn swaps a building's width and depth. Imported
        // heroes vary greatly in footprint, so a fixed offset cannot keep every
        // model clear. Measure the final renderer bounds and push the whole
        // instance outward until its nearest geometry clears the pavement edge.
        static void PushHeroOutsidePavement(GameObject instance, bool left)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            const float buildingInnerClearance = 7.6f;
            Vector3 position = instance.transform.position;
            if (left)
            {
                float overlap = bounds.max.x + buildingInnerClearance;
                if (overlap > 0f)
                {
                    position.x -= overlap;
                }
            }
            else
            {
                float overlap = buildingInnerClearance - bounds.min.x;
                if (overlap > 0f)
                {
                    position.x += overlap;
                }
            }
            instance.transform.position = position;
        }

        // Removes only the façade-blocking décor (trees, billboards and market stalls) on the hero's side
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
                if (s.SocketType != DecorSocketType.Tree &&
                    s.SocketType != DecorSocketType.Billboard &&
                    s.SocketType != DecorSocketType.BusStop)
                {
                    continue; // keep traffic lights, benches, lamps and bins
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

        void ApplyHeroTint(GameObject instance, Color tint)
        {
            if (tint == Color.white)
            {
                return;
            }

            mpb ??= new MaterialPropertyBlock();
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material shared = renderer.sharedMaterial;
                Color baseColor = shared != null && shared.HasProperty(BaseColorId)
                    ? shared.GetColor(BaseColorId)
                    : (shared != null && shared.HasProperty(ColorId)
                        ? shared.GetColor(ColorId)
                        : Color.white);
                renderer.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, baseColor * tint);
                mpb.SetColor(ColorId, baseColor * tint);
                renderer.SetPropertyBlock(mpb);
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
            if (socket.SocketType != DecorSocketType.Pedestrian &&
                Mathf.Abs(local.x) < LaneKeepOut)
            {
                local.x = Mathf.Sign(local.x == 0f ? sideSign : local.x) * LaneKeepOut;
                world = transform.TransformPoint(local);
            }

            return world;
        }

        bool Overlaps(Vector3 pos, DecorRule rule, DecorSocket socket)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                // These pedestrians are deliberately paired with traffic lights:
                // the crossing gentleman starts beside the kerb-side pole and
                // the waving gentleman stands at the non-zebra robot. Their
                // authored sockets are safe, so a traffic-light decoration must
                // not suppress the NPC through the generic prop-radius test.
                bool pedestrianAtRobot =
                    (rule.socketType == DecorSocketType.Pedestrian ||
                     rule.socketType == DecorSocketType.WavingPedestrian) &&
                    placed[i].Type == DecorSocketType.TrafficLight;
                if (pedestrianAtRobot)
                {
                    continue;
                }

                float minDist = placed[i].Radius + rule.collisionRadius;
                if ((placed[i].Position - pos).sqrMagnitude < minDist * minDist)
                {
                    return true;
                }

                if (placed[i].Type == rule.socketType && placed[i].Side == socket.Side &&
                    (placed[i].Position - pos).sqrMagnitude < rule.minSpacing * rule.minSpacing)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Picks a walking-pedestrian prefab whose character is not currently in
        /// view (reservoir-sampled among the available ones), or null if every
        /// character is already on screen — in which case the socket stays empty
        /// until the visible walker of that kind passes by.
        /// </summary>
        static GameObject PickAvailableWalker(DecorRule rule, System.Random rng)
        {
            if (rule.allowedPrefabs == null)
            {
                return null;
            }
            GameObject chosen = null;
            int available = 0;
            for (int i = 0; i < rule.allowedPrefabs.Length; i++)
            {
                GameObject prefab = rule.allowedPrefabs[i];
                if (prefab == null)
                {
                    continue;
                }
                PavementWalker walker = prefab.GetComponent<PavementWalker>();
                int id = walker != null ? walker.CharacterId : 0;
                if (PavementWalker.AnyInView(id))
                {
                    continue;
                }
                available++;
                if (rng.Next(available) == 0)
                {
                    chosen = prefab;
                }
            }
            return chosen;
        }

        static GameObject PickPrefab(DecorRule rule, DecorSocket socket, System.Random rng)
        {
            if (rule.socketType == DecorSocketType.BusStop && rule.allowedPrefabs.Length >= 2)
            {
                // Keep the large shelter on the left pavement only. The right
                // market socket continues to use the fruit stall, preserving
                // roadside variety without moving a shelter across playable lanes.
                GameObject shelter = null;
                GameObject marketStall = null;
                for (int i = 0; i < rule.allowedPrefabs.Length; i++)
                {
                    GameObject candidate = rule.allowedPrefabs[i];
                    if (candidate == null)
                    {
                        continue;
                    }

                    if (candidate.name.Contains("DecorBusStopCluster"))
                    {
                        shelter = candidate;
                    }
                    else
                    {
                        marketStall ??= candidate;
                    }
                }

                if (socket.Side == DecorSocket.StreetSide.Left && shelter != null)
                {
                    return shelter;
                }
                if (socket.Side == DecorSocket.StreetSide.Right && marketStall != null)
                {
                    return marketStall;
                }
            }

            if (rule.sideMatchedPrefabPair && rule.allowedPrefabs.Length >= 2)
            {
                return socket.Side == DecorSocket.StreetSide.Left
                    ? rule.allowedPrefabs[0]
                    : rule.allowedPrefabs[1];
            }

            if (rule.allowedPrefabs.Length == 1)
            {
                return rule.allowedPrefabs[0];
            }

            int index = rng.Next(rule.allowedPrefabs.Length);
            return rule.allowedPrefabs[index];
        }

        void ApplyTint(GameObject instance, System.Random rng, DecorRule rule, int paletteIndex)
        {
            Color mul;
            if (rule.useJacarandaPalette)
            {
                // Cycle rather than independently roll, guaranteeing visibly
                // different neighbours while retaining a cohesive blossom palette.
                mul = (Mathf.Abs(paletteIndex) % 5) switch
                {
                    0 => new Color(0.94f, 0.78f, 1.25f, 1f), // deep violet
                    1 => new Color(1.22f, 1.05f, 1.30f, 1f), // lavender
                    2 => new Color(1.38f, 0.82f, 1.12f, 1f), // magenta-purple
                    3 => new Color(1.48f, 0.94f, 0.98f, 1f), // soft pink
                    _ => new Color(1.28f, 1.20f, 1.18f, 1f), // pale blossom
                };
            }
            else
            {
                float t = (NextFloat(rng) * 2f - 1f) * rule.tintVariation;
                mul = new Color(1f + t, 1f - t * 0.4f, 1f + t * 0.6f, 1f);
            }

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
