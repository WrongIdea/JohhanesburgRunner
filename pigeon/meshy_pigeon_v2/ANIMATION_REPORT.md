# Pigeon Animation Library — Report

**Rig:** `PigeonV2_rigged.blend` (25-bone Generic, −Y forward, feet-origin, 0.30 m)
**Date:** 2026-08-06 · **FPS:** 30 · **Platform:** Unity 6 URP (Generic rig)
**Tooling:** Blender 5.2 headless — `build_all_anims.py` (author+export), `validate_anims.py` (render checks).
**Deliverables:** `PigeonV2_animated.blend`, `PigeonV2_animations.fbx` (all 12 clips, textures embedded), this report.

---

## 1. Clip list

| # | Clip | Duration | Frames (30 fps) | Loop | Behaviour |
|---|------|----------|-----------------|------|-----------|
| 1 | **Idle** | 3.00 s | 1–91 (90 int.) | ✅ loop | breathing, small head moves, weight sway, tail settle |
| 2 | **Walk** | 2.00 s | 1–61 (60) | ✅ loop | 4 footfalls, head-bob synced, body bounce, tail stabilise |
| 3 | **Peck** | 2.00 s | 1–61 (60) | ✅ loop | look-down → jab (neck compression) → raise → look around |
| 4 | **Alert** | 2.00 s | 1–61 (60) | ✅ loop | snap look L/R, slight crouch, tail flicks |
| 5 | **Takeoff** | 0.70 s | 1–22 (21) | ⛔ once | crouch → leg push → wings open, body pitches up → blends to Fly |
| 6 | **Fly** | 1.00 s | 1–31 (30) | ✅ loop | 2 strong wingbeats, shoulder/elbow/wrist follow-through, body osc., stable head |
| 7 | **Glide** | 2.00 s | 1–61 (60) | ✅ loop | wings extended, slow corrections, tail balance |
| 8 | **BankLeft** | 1.00 s | 1–31 (30) | ✅ loop | roll left, left wing folds / right extends, tail assists |
| 9 | **BankRight** | 1.00 s | 1–31 (30) | ✅ loop | mirror of BankLeft |
| 10 | **Landing** | 1.00 s | 1–31 (30) | ⛔ once | wings cup wide, legs extend, feet plant, knees absorb, wings fold → blends to Idle |
| 11 | **ShortHop** | 0.50 s | 1–16 (15) | ⛔ once | small jump, quick flap, land |
| 12 | **FrightenedFlutter** | 0.80 s | 1–25 (24) | ⛔ once | 3 frantic beats, body jitter → blends to Fly |

All durations exact at 30 fps. Loop clips have identical first/last-frame poses; non-loop clips start/end where the spec's blend targets need them (Takeoff/Flutter end wings-up → Fly; Landing ends folded → Idle).

## 2. Loop status & Unity import config

- **Loop = ON:** Idle, Walk, Peck, Alert, Fly, Glide, BankLeft, BankRight.
- **Loop = OFF:** Takeoff, Landing, ShortHop, FrightenedFlutter.
- FBX take names are stored as `PigeonArmature|<Clip>` — they **contain** the clean names, so the project's existing `PigeonModelPostprocessor` (`clip.name.Contains(...)`) canonicalises them to `Idle/Walk/...` and sets loop flags automatically, exactly like the shipping bird.
- Recommended importer settings: **Rig = Generic**, **Anim Compression = Optimal**, **Resample Curves = on**, per-clip **Loop Time** per the table (+ Loop Pose for the cyclic ground/flight clips).

## 3. Root motion

**Disabled / in-place.** Verified: the Root bone has **zero horizontal (X/Y) translation** in every clip. Only small *vertical* (Z) bob exists on Walk/Fly/Takeoff/Hop/Landing/Flutter for weight — it animates the body in place and is not forward travel. Unity keeps `applyRootMotion = false` (as the existing `PigeonController` already does); Unity scripts drive world movement.

## 4. Optimisations performed

- **Hand-authored sparse keyframes** (only pose extremes + timing beats), Bezier interpolation between — minimal authored data, smooth curves.
- **In-place** authoring (no root translation curves to strip later).
- **Influences capped at 4 bones/vertex** (from the rig) — mobile GPU skinning.
- **Constraints muted before bake** (Limit-Rotation don't export to Unity and would clamp poses) — clean baked transforms.
- Export baked per-frame with `simplify_factor 0` to protect loop endpoints; **Unity's Optimal keyframe reduction** does the final curve compression on import.
- 25 bones only, no helper bones → low animation memory per clip.

## 5. Animation quality checks

Validated by rendered key frames (`anim_val/`):
- **Foot sliding:** legs plant/lift with knee+ankle bend; no visible slide at contact.
- **Neck stretching / body distortion:** none — neck/head bend cleanly (peck head-to-ground verified), body pitch/roll clean.
- **Wing clipping (flight):** Fly up/down-stroke, Glide, Bank, Takeoff, Landing all deform cleanly and symmetrically.
- **Smooth interpolation:** matching loop endpoints; no pops.

## 6. Issues encountered / limitations

1. **Folded-wing bunching on ground clips (Idle/Walk/Peck/Alert).** The mesh was authored **wings fully spread with individually-modelled feathers and no per-feather bones**, so a tight fold forces the rigid primaries to overlap/crumple. A moderate "resting" fold is used (best compromise); it reads acceptably at the small on-screen size of a runner background bird, but is the one imperfect look. Flight clips (wings open — the mesh's natural pose) are unaffected and look best. *Fix if ever needed: a folded-wing mesh variant or added feather bones — out of scope here (brief said don't modify the mesh).*
2. **Blink** (Idle spec) not authored — the rig has **no eye/eyelid bones or blendshapes**; blinking isn't riggable on this mesh. All other Idle behaviours are present.
3. **Tail "spread"** — the single `Tail` bone can raise/lower/tilt but cannot *fan*; takeoff/landing tail "spread" is approximated by raise/tilt.

## 7. Unity validation

- ✅ **All 12 clips present** in the FBX (verified by re-import: 12 baked takes, exact frame ranges).
- ✅ **Generic-rig compatible** — same 25-bone skinned armature as the validated rig FBX (Root at origin, applied transforms).
- ✅ **Loop settings** defined per §2 (auto-applied by the Contains-based postprocessor).
- ✅ **Root transforms consistent** — no horizontal root translation in any clip.
- ✅ **No animation warnings expected** — clean single-material skinned mesh, ≤4 influences, valid take names.

## 8. Conclusion

A complete, 30 fps, in-place 12-clip library covering idle/walk/peck/alert and the full flight set (takeoff → fly → glide → bank L/R → landing, plus hop and frightened flutter), authored to loop cleanly and blend between states. Flight animation is the strong suit and is production-ready; ground clips carry the documented folded-wing limitation. Ready to drop into Jozi Runner alongside the existing pigeon controller pattern.
