# Pigeon System — Architecture

Living-street pigeon flocks for Jozi Runner (Unity 6 URP, Android). Pooled,
data-driven, one manager Update, no runtime Instantiate/Destroy.

## Class relationships

```
PigeonSpawner ──owns──> PigeonPool ──rents──> PigeonController (per bird)
      │  (only Update)        ▲                     ▲
      ├──owns──> PigeonFlock[] ┘ (rents/returns) ───┘ (ticks)
      │            └─ registers with ─> PigeonThreatBus <── NotifyHorn / NotifyVehicleApproach
      ├──reads──> PigeonFlockSettings (SO)      MovingObstacle : IPigeonThreat ─┘
      ├──reads──> QualityController (caps)   PigeonWeatherService.Provider : IPigeonWeatherProvider
      └──reads──> RoadSegmentVisuals.DistrictAt / PigeonInterestArea.MultiplierAt

World anchors (self-register on enable, on RoadSegment.prefab):
  PigeonSpawnPoint · PigeonLandingPoint · PigeonInterestArea
Optional set-pieces: PigeonCinematicEvent → spawner.SpawnCinematic (dedicated slot)
```

## Update / tick flow (one Update in the system)

```
PigeonSpawner.Update
  ├─ tick each active PigeonFlock
  │     └─ flock detects player (1 sqr check) / scans vehicle registry (IsScary)
  │     └─ ticks each PigeonController (ground / takeoff / fly / land)
  ├─ tick the cinematic flock (if live)
  └─ walk spawn slots ahead → TryDeployAt (chance × weather × district × interest)
```

## Pigeon state machine (PigeonController)

```
Ground(Idle⇄Walk⇄Peck) ──Scare(delay)──> TakeOff ──> Fly(bezier + bank) ──> {Land ──> Ground} | {Done ──> pool}
```

- Ground behaviour is weighted-random with per-bird timing; feeding areas bias to Idle/Peck.
  A re-roll prevents the same behaviour running twice in a row (kills visible repetition).
- While grounded and calm, the body carries a per-instance procedural sway (breathing +
  weight-shift) so a resting flock is never statue-still or synchronised. Held frozen the
  instant a scare arrives — the bird snaps to an alert idle `delay` seconds before lift-off.
- Flight is a per-bird quadratic bezier arc; body banks by rolling into lateral turns.
- Land descends to a reserved perch (or ground), settles, resumes ground behaviour.

## Flock state machine (PigeonFlock)

```
Inactive ─Deploy─> Grounded ─react─> Alerted ─> TakingOff ─> Flying ─> Landing ─> Completed ─> (pool + Inactive)
```

Reactions (`ReactToPlayer` / `ReactToVehicle` / `ReactToHorn` / `TriggerTakeoff`)
all funnel to one guarded `Scatter(origin, hasOrigin)` (the `scared` latch → no double
takeoff). Scatter picks a **leader** — the bird nearest the threat (or a random bird for a
blind takeoff) — which lifts first; every other bird's take-off delay grows with its
distance from that leader plus jitter, so the alarm ripples outward instead of the flock
launching as one synchronised block.

## Spawn flow

```
player advances ─> nextSpawnZ reached ─> TryDeployAt(z):
  tutorial warm-up? weather allows ground? free flock + under flock cap?
  chance = base × weather × districtMultiplier × interestMultiplier × overrides
  cinematic? (dedicated slot, 500–800 m spacing, one-at-a-time, quality-gated)
  else roll chance ─> clamp count to quality active budget ─> flock.Deploy(jittered polar)
```

## Pooling flow

```
PigeonPool(prewarm 20, max 30, expansion=editor-only)
  GetPigeon ─> pop free | (editor) create | null  ─> activate, parent, track active
  ReturnPigeon ─> silence audio, release perch, deactivate, re-parent idle, push free (idempotent)
  ReturnAllPigeons ─> snapshot active, return each
```

## Landing flow

```
Scatter ─> some birds (by landingChance) PigeonLandingPoint.ReserveNearest(range, district, approach)
  reserved ─> SetLandingTarget ─> bezier to perch ─> Land clip ─> settle y ─> ground behaviour
  none free ─> keep flying ─> Done ─> pool (perch auto-released on return)
```

## Integration points

- **Road segments**: anchors live on `RoadSegment.prefab` under `PigeonPoints`;
  self-register on enable / unregister on disable, so pooling is automatic.
- **Districts**: `RoadSegmentVisuals.DistrictAt(z)` → `PigeonFlockSettings.DistrictMultiplier`.
- **Weather**: `PigeonWeatherService.Provider` (default backed by `WeatherState`).
- **Vehicles**: `MovingObstacle : IPigeonThreat`; flock scan gated on `IsScary`; horns via bus.
- **Quality**: `QualityController` statics → `PigeonSpawner.ApplyQuality()`.

## Editor tooling

- **Jozi Runner ▸ Pigeon Debugger** — live stats + actions + edit-mode tools.
- **Jozi Runner ▸ Pigeons ▸ Validate Integration / Integrate Into Road Segment / Stress Test**
- **Joburg Runner ▸ Build Pigeon Flock Test Scene** — `PigeonFlockTest.unity`.
- `PigeonSpawner` custom inspector — play-mode stats + buttons.

## Extending

- New reaction source: implement `IPigeonThreat` and call `PigeonThreatBus`.
- New weather system: implement `IPigeonWeatherProvider`, set `PigeonWeatherService.Provider`.
- New perch/attractor: drop a `PigeonLandingPoint` / `PigeonInterestArea` in a segment (self-registers).
- New set-piece: place a `PigeonCinematicEvent`, or extend the spawner's cinematic picker.
- Tuning: edit `PigeonFlockSettings` (spawn/flight/district/weather/cinematic) and the
  pigeon fields on the three `GameQualitySettings` assets.
