# Pigeon System — Production Polish (Pass 2)

Final polish/AI/realism pass over the already-integrated flock system. No
architecture redesign — the existing pooled, single-Update, quality/weather/
district-aware design was kept. Only additive, self-contained behaviour changes
plus a documentation/audit sweep.

## Audit findings (Part 1)

**Live vs. dead assets.** The canonical, scene-referenced set is:
- `Assets/Prefabs/Pigeons/Pigeon.prefab` (guid `0c64fd86…`) → used by
  `JoburgEndlessRunner.unity` and `PigeonFlockTest.unity`.
- Material `Assets/Materials/Pigeon_URP.mat` (URP/Lit, shared, 1 material).
- Controller `Assets/Animations/PigeonAnimator.controller`.
- Script `Assets/Scripts/Environment/Pigeons/PigeonController.cs`.

**Dead duplicate set — REMOVED this pass.** The whole `Assets/Characters/Pigeon/`
folder (legacy prototype prefab, material, animator controller, FBX + atlas +
textures, test scene, `PigeonController.cs`, `PigeonTestDriver.cs`, namespace
`JoburgRunner.Characters.Pigeon`) had **zero** asset references (verified by GUID
sweep across every `.unity/.prefab/.asset/.mat/.controller`) and its test scene was
not in build settings. The shipping bird lives in `Assets/Characters/PigeonHD/`
(the live prefab's mesh guid resolves there, not here). Deleted.

Its authoring counterpart — the editor tools `PigeonV2Builder.cs` and
`PigeonV2Validate.cs` (`Joburg Runner ▸ Pigeon v2 ▸ …` menu) — generated and
validated *only* that deleted folder and referenced the deleted runtime types by
name (`groundCheck`/`bodyCollider` fields the live controller doesn't have). They
would not compile without the folder, so they were removed as part of the same
dead pipeline. The live authoring tool `PigeonBuilder.cs` (targets `PigeonHD`) is
untouched.

**Everything else already present and healthy:** pooling with no runtime
Instantiate/Destroy, one Update for the whole system (spawner ticks flocks tick
pigeons), cached `Animator.StringToHash`, squared-distance checks, per-instance
animator speed/offset, weather/district/vehicle/horn hooks, dedicated cinematic
slot, landing-point reservation with district + approach-cone filtering, editor
debugger window (Part 13), stress test (Part 14), integration validation, and
three design docs.

## Changes made this pass

- **Leader-led, distance-propagated alarm (Parts 2 & 6)** — `PigeonFlock.Scatter`
  now takes a threat origin, elects the nearest bird as **leader** (random for a
  blind takeoff), and staggers every other bird's take-off delay by its distance
  from the leader plus jitter (`AlarmSpreadPerMetre`/`AlarmMaxDelay`/`AlarmJitter`).
  The flock lifts as a spreading wave, never a synchronised block.
- **Alert-before-takeoff (Part 6)** — `PigeonController.Scare` snaps the bird to an
  alert idle (head up, stops pecking/wandering) the instant the alarm reaches it,
  `delay` seconds before lift-off, using only the existing 6 states.
- **Anti-repetition ground behaviour (Part 3)** — `PickGroundBehaviour` re-rolls
  once if the weighted pick repeats the previous behaviour, so no bird plays the
  same clip twice running.
- **Idle micro-motion (Part 4)** — per-instance procedural breathing + weight-shift
  sway on the root while grounded and calm; held frozen while alert. One sine per
  bird, no extra states, no rig dependency.
- **Docs** — updated `PIGEON_ARCHITECTURE.md` (state machines) and corrected a stale
  "IPigeonThreat not implemented" note in `PIGEON_FLOCK_SYSTEM.md` (`MovingObstacle`
  implements it: `IsScary` gates on cruise + `pigeonMinScarySpeed`).

All changes are self-contained, allocation-free, and stay on the single-Update
hot path. Brace-balanced; no signature churn outside the four flock reaction
methods (all internal callers updated).

## Requires the Unity editor (cannot be verified from CLI)

These need the editor/profiler and the existing tools (Jozi Runner ▸ Pigeon
Debugger, Pigeon Stress Test, Integration Validation):
- Runtime 60 FPS / allocation profiling on-device (Part 9).
- LOD transition popping and shadow appearance (Part 10).
- Animator transition inspection for console errors/warnings (Parts 12, 16).
- GPU-instancing checkbox on `Pigeon_URP.mat` — recommend enabling it in the
  Material inspector (do not hand-edit the `.mat` YAML). With URP the SRP Batcher
  already batches the shared material; the flag is a belt-and-braces win.
- Running the 100/500/1000 spawn-despawn stress cycles (Part 14).

## Recommended next action

Open the project in Unity and let it recompile (the dead folder + its two V2 editor
tools are gone — expect a clean compile). Then run the Pigeon Debugger + Stress Test
in-editor to sign off Parts 9/12/14/16, and tick GPU-instancing on `Pigeon_URP.mat`.
