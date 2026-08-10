# Pigeon v2 — Unity Integration Report

**Project:** Jozi Runner (Unity 6000.5.2f1, URP) · **Date:** 2026-08-06
**Location:** `Assets/Characters/Pigeon/` · **Built headless** by `Assets/Editor/PigeonV2Builder.cs` (menu *Joburg Runner ▸ Pigeon v2 ▸ Build All*), validated by `PigeonV2Validate.cs`.

> All static validation passed with **no compile errors and no console warnings** from the new code. Play-mode was not run headless — open `PigeonAnimationTest` to see it live.

## Deliverables
| File | Path |
|---|---|
| FBX (LODs + 12 clips) | `Assets/Characters/Pigeon/Models/Pigeon_Unity.fbx` |
| Material | `…/Materials/Pigeon.mat` |
| Animator controller | `…/Controllers/PigeonAnimator.controller` |
| Prefab | `…/Prefabs/Pigeon.prefab` |
| Runtime controller | `…/PigeonController.cs` (namespace `JoburgRunner.Characters.Pigeon`) |
| Test driver | `…/PigeonTestDriver.cs` |
| Test scene | `…/Scenes/PigeonAnimationTest.unity` |

*Note:* a new FBX (`Pigeon_Unity.fbx`) was generated with **3 decimated LOD meshes** (1100 / 520 / 240 tris) carrying the same armature + all 12 baked clips — the original 174 k mesh is far too heavy for a runner. The name collision with the existing `JoburgRunner.Environment.Pigeons.PigeonController` is avoided by a distinct namespace; the orphaned old scratch `Pigeon.fbx` at the folder root was left untouched.

## Part 1–3 · Import & rig (verified)
- **Animation Type = Generic**, Avatar = *Create From This Model*, root bone `Root`.
- Model tab: `useFileScale` on, BlendShapes/Cameras/Lights **off**, Read/Write **off**, **Mesh Compression = Medium**, Optimize Mesh on, Generate Colliders off, Keep Quads off, Index Format Auto, `materialImportMode = None` (we assign one material — no duplicate textures).
- **Scale verified true:** world extent **0.76 (wingspan) × 0.30 (height) × 0.34 (length)**, **feet at Y = 0**, prefab root rotation **identity**, head at +Z of tail → **faces Unity +Z (forward)**. No unexpected rotation/scale offset.
- Bones recognised, hierarchy valid, root motion disabled — no import warnings.

## Part 4 · Animation clips (verified)
12 clips, cleaned names, correct lengths and loop flags:

| Loop = ON | Loop = OFF |
|---|---|
| Idle 3.0s · Walk 2.0s · Peck 2.0s · Alert 2.0s · Fly 1.0s · Glide 2.0s · BankLeft 1.0s · BankRight 1.0s | Takeoff 0.7s · Landing 1.0s · ShortHop 0.5s · FrightenedFlutter 0.8s |

Every clip: **Root Transform Rotation / Position Y / Position XZ = Bake Into Pose**, **Based Upon = Original**, imported root motion disabled → plays in place, no drift. Loop Time (+Loop Pose on the cyclic clips) set per the table. First/last frames authored to match.

## Part 5–7 · Animator controller (verified)
- **12 states**, default **Idle**, every state has a motion (0 empty).
- **9 parameters:** `Speed`(float), `IsAlert`(bool), `TakeOff/Land/Peck/ShortHop/Frightened`(triggers), `FlightMode`(int), `GroundState`(int).
- Transitions (all short, 0.08–0.12 s fixed):
  - Ground: Idle↔Walk↔Peck by `GroundState` (0/1/2); Idle/Walk/Peck→Alert on `IsAlert`, Alert→Idle on `!IsAlert`.
  - Any State→Takeoff (`TakeOff`); Takeoff→Fly (exit 0.82).
  - Fly↔Glide and Fly/Glide→BankLeft/BankRight by `FlightMode` (0/1/2/3); banks return to Fly, and can answer each other (no dead ends).
  - Any State→FrightenedFlutter (`Frightened`)→Fly (exit 0.88); Any State→ShortHop→Idle (exit 0.85).
  - Fly/Glide/BankLeft/BankRight→Landing (`Land`); Landing→Idle (exit 0.88).
- Graph reviewed for stuck/looping states — every state has a return path; AnyState transitions use `canTransitionToSelf = false`.

## Part 8–9 · PigeonController.cs
- **Cached parameter hashes** (static readonly) — no runtime `StringToHash`, no per-frame allocations (coroutines use `yield return null`; no `WaitForSeconds` inside the controller).
- API: `Initialize`, `ResetPigeon`, `SetGroundState`, `SetAlert`, `TriggerTakeoff`, `SetFlightMode`, `TriggerLanding`, `TriggerFrightened`, `TriggerShortHop`, `ReturnToPool`.
- **Autonomous ground behaviour** (coroutine): weighted random Idle/Walk/Peck/Alert with configurable duration ranges (Idle 2–5, Walk 1–3, Peck 1.5–3, Alert 0.5–1.5 s). **Random clip start offset** on spawn so a flock never syncs.
- **Local wander** within `wanderRadius` around the spawn home, clamped so it **never crosses the kerb into the road** (side auto-detected from spawn X).

## Part 10–13 · Prefab, LOD, material, audio (verified)
- Hierarchy: `Pigeon` (Animator, PigeonController, LODGroup, CapsuleCollider, AudioSource) → LOD renderers (Model), `PigeonArmature` (Rig), `GroundCheck`.
- **LODGroup, 3 LODs:** LOD0 1100 / LOD1 520 / LOD2 240 tris, screen-relative switches ≈13 m / 26 m, **cull ≈45 m** (tune later). All LODs share the one material.
- **Collider:** trigger CapsuleCollider around the torso only; **disabled during flight** by the controller.
- **Material:** single **URP/Lit**, single **512×512** albedo atlas, **GPU instancing on**, Opaque, Metallic 0 / Smoothness 0.15 — subtle green/purple neck preserved (baked in albedo). Validated **1 unique material** across all renderers/instances.
- **Audio:** one AudioSource, Play-On-Awake off, 3D (spatialBlend 1), Doppler 0.05, max distance 22 m. Fields exposed: `Coo/WingFlap/Takeoff/Landing/Alert`; coo throttled to a random 4–11 s interval.

## Part 14 · Pooling
- **Activate:** `Initialize`/`ResetPigeon` → reset transform, `Animator.Rebind()`, clear all triggers, reset bools/ints/float, restore collider, re-enable behaviour, randomise clip phase.
- **Return:** `ReturnToPool` → stop audio, stop coroutines, reset flight/ground state, `SetActive(false)` — never Destroy. No Instantiate/Destroy at runtime.

## Part 15 · Test scene
`PigeonAnimationTest` spawns a ground bird, a repeating flyer, a glider, a left/right banker, a lander, and a 4-bird flock (all pool-`Initialize`d with random offsets). `PigeonTestDriver` self-runs those roles and maps keys: **1–5** select a bird; **A/S** alert on/off, **T** takeoff, **F** fly, **G** glide, **L/R** bank, **N** land, **X** frightened, **H** short hop. On-screen legend via OnGUI.

## Part 16 · Validation summary
✅ 12 clips import with correct lengths & loop flags · ✅ Generic rig, bones valid, root motion off · ✅ scale 0.30 m, feet at ground, +Z forward, identity root · ✅ material single/instanced/512 · ✅ LODGroup 3 levels · ✅ prefab fully wired (Animator+controller, PigeonController refs, collider, audio) · ✅ controller graph has no dead-ends · ✅ random offsets prevent flock sync · ✅ pool reset implemented · ✅ no compile errors/warnings from new code · ✅ no per-frame GC in PigeonController.

**Known limitation (carried from rigging):** ground-clip folded wings bunch slightly (spread-authored feathers, no per-feather bones) — flight clips look best. Not an import issue.

## Not done (per scope)
Flock spawner / district spawning intentionally excluded — this delivers one reusable, pool-ready animated prefab. Wiring it into the live flock (à la `Environment.Pigeons`) is the next step.
