# Pigeon Flock System — Implementation Report

Production pigeon flock system for Jozi Runner (Unity 6 URP, Android). Built as an
**additive extension** of the on-device-verified MVP: the pigeon model, rig, clips,
Animator controller, `PigeonController`, `PigeonPool`, `PigeonFlock` and
`PigeonSpawner` that already shipped were preserved; new capability layers on top
with safe fallbacks, so nothing that runs on-device was renamed or removed.

Verified by a headless batch build (`PigeonBuilder.BuildAll` and
`PigeonFlockTestSceneBuilder.Build`): clean compile, no exceptions, prefabs +
settings asset + test scene assembled.

## Files

New:
- `IPigeonThreat.cs` — threat interface + `PigeonThreatBus` (allocation-free scatter hub)
- `PigeonFlockSettings.cs` — ScriptableObject tuning (Part 3)
- `PigeonLandingPoint.cs` — perches, self-registering, reserve/release (Part 9)
- `PigeonSpawnPoint.cs` — placed spawn hints, self-registering (Part 10)
- `PigeonInterestArea.cs` — jacaranda/food/vendor attractors + petal hook (Part 14)
- `PigeonCinematicEvent.cs` — rare set-pieces on a dedicated slot (Part 16)
- `PigeonFlockTestRig.cs` — keyboard/GUI driver for the test scene (Part 20)
- `Editor/PigeonSpawnerEditor.cs` — stats + test buttons (Part 19)
- `Editor/PigeonFlockTestSceneBuilder.cs` — builds `PigeonFlockTest.unity`

Extended: `PigeonController.cs`, `PigeonFlock.cs`, `PigeonPool.cs`,
`PigeonSpawner.cs`, `Editor/PigeonBuilder.cs`, `Editor/EndlessRunnerSceneBuilder.cs`.

Generated: `Assets/Prefabs/Pigeons/{Pigeon,PigeonFlock}.prefab`,
`Assets/Environment/Pigeons/PigeonFlockSettings.asset`,
`Assets/Scenes/PigeonFlockTest.unity`.

## Architecture

One Update in the whole system: `PigeonSpawner.Update` ticks every live flock;
each flock ticks its pigeons. No pigeon or flock has its own `Update`. The spawner
owns the shared `PigeonPool` and a fixed set of pooled `PigeonFlock` objects (one
per ordinary slot + one cinematic slot). Tuning is read once at `Start` from an
optional `PigeonFlockSettings` asset; with none assigned the serialized defaults
apply, so the component is drop-in.

## Pooling

`PigeonPool` prewarms 20 (max 30), tracks free/active via a `Stack` + `HashSet`
(no runtime allocations on the hot path). `GetPigeon` returns null when the fixed
pool is exhausted in a player build (`allowExpansion` defaults to editor-only), so
callers degrade the event rather than allocate. `ReturnPigeon` is idempotent,
silences audio, releases any held perch, deactivates and re-parents under a hidden
root. Original `Get`/`Return` kept as aliases. Pigeons parent to their flock, which
parents to the spawner — never to a road segment — so segment recycling can't drag
them.

## Spawn rules

Default 30% chance, 80–120 m spacing, max 2 ordinary flocks, suppressed for the
first `warmupDistance` (tutorial guard), none behind the camera. Chance is scaled
by weather × district × interest-area multipliers × manual overrides. A registered
`PigeonSpawnPoint` ahead is preferred (with its size/chance/landing overrides);
otherwise procedural pavement placement at x = ±6.2. Placement is jittered polar
(uniform disc) — never a line or grid.

## Player & vehicle reactions

Player: one squared-distance check per **flock** (never per pigeon); inside
`triggerDistance` (~15 m) the flock scatters once (`scared` latch). Each bird gets a
unique 0–0.25 s takeoff delay. Vehicle: `PigeonThreatBus` lets taxis/cars/buses/
police (`IPigeonThreat`) or a horn notify only the ≤3 registered flocks — no scene
search. A passive fallback also walks the maintained `RunnerObstacle.ActiveObstacles`
registry for vehicles that never call in. Horn events use `hornReactionRadius`.

## Flight movement

Deterministic, no Rigidbody. Each pigeon follows a per-instance quadratic Bézier arc
(start → raised/lateral control point → destination) sampled over takeoff+flight
duration, so no two paths match. Headings fan out (−35°…+35° bias + jitter) across
forward/left/right/diagonal. The body **banks** by rolling around its forward axis
proportional to per-frame yaw change (smoothed, clamped to `maxBankAngle`) — banking
needs no extra clips. Animator states used: Idle/Walk/Peck/TakeOff/Fly(=Glide)/
Land(=Landing) via `CrossFadeInFixedTime` (no parameters).

## Landing behaviour

`PigeonLandingPoint` (pavement, bench, bus-stop roof, traffic-light arm, planter,
ledge, low wall, tree branch) self-registers; `ReserveNearest` respects range,
district, approach cone and max occupants and reserves atomically so two birds can't
grab one narrow perch. On scatter a share of the flock (by `landingChance`) reserves
a perch and descends onto it (Bézier → Land → align → ground behaviours); the rest
fly off and despawn. Perches release on leave / pool-return. No safe point ⇒ keep
flying and despawn.

## District & weather integration

Uses the existing systems, no competing manager. District weighting maps the game's
runtime district index (via `RoadSegmentVisuals.DistrictAt`) onto spec multipliers;
`WeatherState` thins (Rain 0.4×) or clears (HeavyRain) ground flocks. Districts the
game doesn't model (taxi rank, bus stop, Sandton, Jacaranda Avenue) are reached
through `PigeonInterestArea` and landing points instead.

## Road-segment lifecycle

Spawn points, landing points and interest areas self-register on `OnEnable` and
withdraw on `OnDisable`, so a pooled road segment contributes its points only while
active and releases them (including reserved perches) when recycled — no manager
bookkeeping. `PigeonFlock.DespawnFlock()` is available to fly a flock off on segment
teardown; pigeons are never parented to segments.

## Performance

Object pooling; shared material/mesh/Animator controller; `AnimatorCullingMode.
CullCompletely`; 3-LOD `LODGroup`; single manager Update; squared-distance checks;
cached Animator hashes; non-allocating registries walked by index; preallocated
lists. Avoids Instantiate/Destroy in play, LINQ in loops, `FindObjectsOfType` in
loops, per-pigeon Update, Rigidbody flight, NavMesh and per-pigeon pathfinding.
Observed: headless build compiles clean; the MVP baseline runs at 60 FPS on a
Samsung SM-G996B. Live counts (active pigeons, pooled, flocks, cinematic) are shown
by the custom inspector and the test-scene overlay for profiler correlation.

## Test scene

`Assets/Scenes/PigeonFlockTest.unity` (menu *Joburg Runner ▸ Build Pigeon Flock Test
Scene*): mock runner (tagged Player, auto-advances), spawner, pavement spawn point,
six landing points, taxi-rank + jacaranda interest areas with cinematic events, and
a mock vehicle. Keys: Space pause · T spawn · P player · V vehicle · H horn · L land
· R return-all · 1-4 weather. Covers all 10 scenarios (approach, fast vehicle, horn,
landing, no-perch, near-exhausted pool, segment recycle via `DespawnFlock`, rain
disables ground, cinematic, reset+respawn).

## Known limitations

- Banking is physical roll, not a Fly/Glide/BankLeft/BankRight animator blend — the
  authored controller has no bank clips/params; roll reads correctly and needs no
  re-authoring. Add blend states later if desired.
- Ground obstacle avoidance is a roam-radius turn-back, not raycast collision — the
  spec's "lightweight only when needed" guidance; pigeons stay in their local disc.
- Cinematic events are limited to one at a time (single spawner slot); large events
  auto-shrink to the available pool rather than borrowing from ordinary flocks.
- Vehicle `IPigeonThreat` is defined but not yet implemented on `MovingObstacle`;
  the passive obstacle-registry fallback covers taxis until it is.
