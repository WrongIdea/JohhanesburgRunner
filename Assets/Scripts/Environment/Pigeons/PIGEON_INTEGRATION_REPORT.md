# Pigeon Integration — Pass 1 Report

Integration of the pigeon flock system into the real Jozi Runner gameplay scene.
Built **additively** on the verified system; no runner controls, lane logic,
obstacle rules, coin spawning or district sequencing were changed. Work isolated on
branch `feat/pigeon-integration`.

## Segments updated

- **`Assets/Prefabs/RoadSegment.prefab`** — the single tile reused for every road
  segment. One idempotent `PigeonPoints` child holds all attached points, so a
  single prefab edit propagates to every pooled tile and rides the existing pooling
  (points self-register on enable / unregister on disable — no lifecycle code).

## Spawn & landing-point counts (per tile)

- **8 landing points**: 4 pavement (x = ±6.2, both sides, spread along the tile),
  2 traffic-light arms (bounds-driven from the baked `TrafficLight_FBX`), 2 jacaranda
  feeding perches (pavement, under the interest areas).
- **2 interest areas**: jacaranda, one per pavement side, radius 3.5, ×1.4 spawn pull.
- Spawn points are **not** embedded per tile (that would make every segment
  eligible); ground flocks stay spawner-driven and gated so pigeons feel special.
- Placement is **deterministic by side** (x = ±6.2) for pavement/jacaranda and
  **bounds-driven** for traffic arms, because the baked tree/sidewalk nodes are
  runtime-positioned templates sitting at the origin in the prefab. Validated:
  **0 FAIL, 0 WARN** — nothing below ground or inside the road.

## District settings (Part 8, in `PigeonFlockSettings`)

CBD 1.20 · Taxi rank 1.60 · Bus stop 1.35 · Park 1.40 · Jacaranda 1.10 ·
Mandela Bridge 0.75 · Sandton 0.50 · Residential 0.65. Mapped onto the game's
runtime district index via `DistrictMultiplier(int)`; the spawner reads the asset
(falls back to its hardcoded weights if unassigned). Base straight-segment
eligibility stays low; interest areas and district weight create the variation.

## Weather behaviour (Part 9)

New `IPigeonWeatherProvider` adapter (default `DefaultPigeonWeatherProvider` backed
by the existing `WeatherState`). Light rain ×0.4 ground spawns + prefer sheltered
landing; heavy rain disables ground flocks (and cinematics) and suppresses coo.
Swap `PigeonWeatherService.Provider` to connect a richer weather system later.

## Vehicle reaction (Part 7)

`MovingObstacle` (taxi) now implements `IPigeonThreat` (position, speed, radius,
`IsScary` = cruising ≥ 6 u/s and not stopped). The flock's existing obstacle-registry
scan (no scene search) consults `IsScary`, so a fast taxi pass flushes a nearby
flock while a parked/slow taxi or a static barrier does not. The `scared` latch
prevents a double takeoff when the runner and a taxi trigger the same flock. Horns
route through `PigeonThreatBus.NotifyHorn`.

## Cinematic events (Part 10)

Spawner-driven on the dedicated cinematic slot (separate from the 2-flock cap),
**one at a time**, min-spacing 500–800 m, disabled in the tutorial warm-up, quality-
gated, and auto-shrunk to the free pool. The standalone `PigeonCinematicEvent`
component remains for hand-placed set-pieces (used in `PigeonFlockTest`).

## Quality levels (Part 14)

`GameQualitySettings` gained pigeon fields (max active, max flocks, cinematics,
cull distance, landing, shadows, audio); `QualityController` exposes them as statics
(same pattern as particle/vehicle scales). `PigeonSpawner.ApplyQuality()` folds them
in at Start: clamps active flocks, sets cull distance, gates audio, and enforces a
hard active-pigeon budget per deploy. Suggested tiers — Low 6/1/no-cinematics/cull
30, Medium 12/2/small-cinematics/cull 40, High 20/2/cinematics/cull 50 — authored in
the three `GameQualitySettings` assets.

## Pool & lifecycle validation (Part 12)

Points self-register/unregister with tile activation, so segment recycling releases
perches and withdraws points automatically. Pigeons parent to their flock (under the
spawner), never to a segment, so recycling can't teleport them. `DespawnFlock()`
flies a flock off on teardown. Editor validator: **Jozi Runner ▸ Pigeons ▸ Validate
Integration** (shared-material, animator clips, flock range, below-ground / in-road
perches, scene wiring). **Jozi Runner ▸ Pigeons ▸ Integrate Into Road Segment**
re-attaches points idempotently.

## Bugs fixed this pass

- Interest/perch placement at road-centre (x=0) from trusting runtime-template
  transforms — now deterministic-by-side, caught by the validator.
- Interest-multiplier runaway when many baked jacarandas overlap — `MultiplierAt`
  now takes the max, not the product.
- Vehicle scan flushing flocks for parked taxis / static barriers — now gated on
  `IPigeonThreat.IsScary`.

## Performance

Still one Update (the spawner). No new per-frame allocations; the vehicle scan does
a `GetComponent<IPigeonThreat>()` only when a vehicle is already within a flock's
trigger radius (rare).

**Pool stress test (headless, real run):** 100 / 500 / 1000 rent-return cycles all
`active=0 free=20 total=20 peakTotal=20` — no leak, no growth, idempotent return.
Exhaustion rents exactly 20/20 then returns null safely; `ReturnAllPigeons` restores
20/0. **RESULT: PASS.** (Menu: Jozi Runner ▸ Pigeons ▸ Stress Test.)

**Device build/profiling status:** the dev APK built successfully
(`Builds/JoburgEndlessRunner.apk`, scene rebuilt, junction validation passed). The
test device dropped off USB before install (macOS sees no device — physical/cable),
so on-device install + FPS/GC/draw-call profiling **remains outstanding** and will
run once the phone is reconnected. The spec permits deferring device profiling.

## Known limitations / future (Pass 2)

- No true per-segment-type eligibility (taxi-rank/bus-stop/intersection percentages
  from Part 2) — approximated by district + interest areas. Needs `RoadSegmentVisuals`
  to expose segment type / intersection.
- Jacaranda petals left unwired (the baked `Falling_Blossom_Petals` template sits at
  road-centre); the interest area's takeoff `UnityEvent` is ready for per-scene wiring.
- Prop-perches limited to traffic-light arms; bench/bus-stop-roof perches deferred
  (those props are pooled onto sockets, so presence/alignment needs on-device checks).
- Flight-path clipping tuning (Part 11) and readability tuning (Part 13) need eyes
  on device.
- No formal play-mode unit tests (no test assembly in the project); validation is via
  the editor tool + runtime checks.
