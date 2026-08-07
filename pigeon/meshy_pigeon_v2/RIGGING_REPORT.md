# Pigeon Rig — Rigging Report

**Model:** `PigeonV2_clean.blend` (cleaned Meshy pigeon, 0.30 m tall, feet-origin, −Y forward)
**Date:** 2026-08-06
**Tooling:** Blender 5.2 headless — `build_rig_v2.py` (rig), `pose_tests.py` (validation, not saved).
**Deliverables:** `PigeonV2_rigged.blend`, `PigeonV2_rigged.fbx` (textures embedded, Unity-scale), this report.

---

## 1. Total bone count

**25 deform bones.** No helper/control bones (kept minimal for mobile). Every bone deforms the mesh.

## 2. Bone hierarchy

```
Root                                   (master, at world origin 0,0,0)
├── Body                               (torso/chest)
├── Neck
│   └── Head
├── Tail
├── LeftWing ─ LeftShoulder ─ LeftUpperWing ─ LeftLowerWing ─ LeftWrist ─ LeftWingTip
├── RightWing ─ RightShoulder ─ RightUpperWing ─ RightLowerWing ─ RightWrist ─ RightWingTip
├── LeftLeg ─ LeftShin ─ LeftAnkle ─ LeftFoot
└── RightLeg ─ RightShin ─ RightAnkle ─ RightFoot
```

**Interpretation note:** the spec diagram drew the five wing bones (and the leg bones) as a flat list under `LeftWing`/`LeftLeg`. They are implemented as **chains** (each bone parented to the previous one), because flapping, folding and walking require serial joints — a flat sibling layout cannot fold a wing or bend a knee. All bone **names match the spec exactly**. `Root` is the central hub; `Body`, `Neck`, `Tail`, both `*Wing` chains and both `*Leg` chains parent directly to it, per the diagram.

- Bone counts: Root 1, Body 1, Neck+Head 2, Tail 1, each Wing chain 6 (×2 = 12), each Leg chain 4 (×2 = 8) → **25**.
- **Symmetry:** left bones authored, right bones mirrored across X; validator reports **0 mismatches** (perfect X-symmetry).

## 3. Weight painting & adjustments

- **Method:** Blender's Automatic (bone-heat) weights **failed** on this mesh ("failed to find solution") — expected for dense 174 k-triangle Meshy triangle-soup. Replaced with a robust **distance-to-bone-segment skinning** (numpy): each vertex weighted to its 4 nearest bone segments with an inverse-distance falloff (power 3.2), then normalized.
- **Cross-side bleed guard:** `Left*` bones are forbidden from weighting vertices on the −X side (and vice-versa), so wings/legs never pull the opposite side.
- **Refinement:** 3× weight-smoothing (factor 0.5) to soften joint seams; influences capped at **4 per vertex** (mobile GPU skinning); all weights normalized.
- **Coverage:** **0 unweighted vertices** (full coverage — no collapse-to-origin).
- Smooth deformation confirmed (see §5) around neck, shoulders, wing roots, elbows, wrists, tail base, hips, knees, ankles.

## 4. Constraints used

Blender-side animation aids (Limit Rotation), applied in Pose mode:
- **Wing joints** (both sides): `Shoulder`, `UpperWing` — bounded flap/sweep range; `LowerWing`, `Wrist` — one-way fold limits (elbow/wrist fold inward only).
- **Legs** (both sides): `Shin` (knee) and `Ankle` limited to hinge motion.

> Note: Unity's **Generic Rig imports baked bone transforms only** — Blender constraints do not transfer. These limits guide hand-animation in Blender; the animation is baked to keyframes before the Unity export in the next stage. **IK for the legs is recommended to add in the animation file** (kept out of the rig here to avoid extra exported bones while no animation exists yet).

## 5. Pose-test results (temporary, not saved)

| Test | Result |
|---|---|
| Wings flapped up / fully extended | ✅ clean shoulder bend, no tearing — excellent |
| Wings half-open | ✅ smooth sweep + fold, no tearing |
| Wings fully folded (tight tuck) | ⚠️ deforms without tearing but feathers **bunch/self-overlap** (see limits) |
| Walking (leg swing + knee/ankle bend) | ✅ legs articulate, foot plants, no collapse |
| Tail raise | ✅ clean at tail base |
| Neck rotation | ✅ smooth bend at neck/body junction |
| Head rotation | ✅ follows neck, no shearing |

No mesh **tearing**, no **collapsing** geometry, no extreme **stretching**, negligible **clipping** in all flight-relevant poses.

## 6. Topology limitations

- The mesh is the original **174,638-triangle Meshy "triangle soup"** with **no anatomical edge loops** — deformation relies on the dense skin + smoothed weights rather than clean loops. It holds up well for flap/glide/bank/walk.
- **No per-feather bones** (per spec) — wing tips deform with the single `WingTip` bone. Consequence: a **tight full-fold bunches** the primaries (the model was authored wings-fully-spread, so folding forces feather overlap the rig can't separate). Half-fold and all flight poses look correct; a tight resting tuck is the one imperfect pose and is the least relevant for a flapping/gliding runner bird.
- The 174 k density is heavy for mobile; **decimating to ~8–15 k before shipping** is still recommended (weights transfer through a decimate/data-transfer) — does not affect this rig's validity.

## 7. Unity compatibility

- ✅ Single **Root** bone at **world origin (0,0,0)**.
- ✅ Forward axis **−Y** (Meshy/project standard); exported `-Z`/`Y` FBX axes.
- ✅ **Applied transforms** — armature and mesh at loc 0 / rot 0 / scale 1.
- ✅ **Generic-Rig compatible** — verified by re-importing the FBX: 25 bones, mesh skinned (Armature modifier + 25 vertex groups), 0 unweighted, height 0.30 m, feet at Z 0.
- ✅ Clear, consistent bone names (`Left*`/`Right*`, PascalCase).

## 8. Ready for animation?

**Yes.** Correct hierarchy, perfect symmetry, full-coverage smooth weights, clean deformation in all flight/walk/head/tail poses, and a verified Unity-Generic-ready FBX. The rig is production-ready for the next stage (walk, takeoff, flap, glide, bank, landing). No animation clips were created.
