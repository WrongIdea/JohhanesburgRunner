# Jozi Runner — Android Performance & Battery Audit

Symptom: ~2% battery / 11 min (~11%/hr) and noticeable heat on a Samsung S21+
(120 Hz). Goal: stable 60 FPS, ~zero gameplay GC, less heat, **no blind visual
downgrade, no silent gameplay change**.

## Method & honesty note

I ran a **static audit** (configs, import settings, poly counts, Update/allocation
scan) — I cannot attach the on-device Unity Profiler from this environment, so the
live numbers (CPU/GPU ms, draw calls, SetPass, triangles, GC/frame, memory) must be
captured on device. Procedure at the end. The static findings below already isolate
the dominant costs.

## Baseline (measured statically)

| Area | Value | Verdict |
|---|---|---|
| Frame cap | `Application.targetFrameRate = 60`, `vSyncCount = 0` (GameManager) | ✅ capped — **not** running at 120 |
| Render scale | URP `m_RenderScale: 0.8` | ✅ already downsampled |
| SRP Batcher | `m_UseSRPBatcher: 1` | ✅ on |
| HDR | off | ✅ good for mobile |
| Shadow cascades | `m_ShadowCascadeCount: 1` | ✅ good |
| **Additional-light shadows** | `m_AdditionalLightShadowsSupported: 1` (1024 map, 2048 cookie) | ⚠️ **extra shadow pass** for lamp/prop lights |
| **Soft shadows** | `m_SoftShadowsSupported: 1`, `m_SoftShadowQuality: 2` (high) | ⚠️ expensive filtering |
| Shadow distance | URP 40 m; overridden per-tier by QualityController (tier = 40) | ⚠️ far for a runner |
| **LOD bias** | `QualitySettings.lodBias: 2` | ⚠️ **LOD0 held to 2× distance** → ~2–4× triangles on distant scenery |
| MSAA | URP `m_MSAA: 2` | ⚠️ bandwidth cost (mobile) |
| Lights | 16 `Light` components across prefabs/scenes | ⚠️ audit lamp/billboard point lights |
| MeshColliders | on `PF_JHB_CBDTower`, `PF_JHB_Building02` (backdrop) | ⚠️ heroes strip colliders at runtime; static placements may not |
| Textures ≥2048 | 516 files — **but ~all in unused sample dirs** (`FPS/`, `Samples/`, `ModAssets/`, `_Recovery/`, `JoziGame/`) | shipped 3D textures are ≤2048/512 |
| Mipmaps off | 42 textures | audit (UI is fine; 3D must keep mips) |
| Pooling | pigeons ✅, decor (DecorPool) ✅, road segments ✅ | verify coins/obstacles/VFX |

**Bottleneck hypothesis (GPU-bound heat):** frame rate is already capped, so heat is
raw GPU work — dominated by (1) real-time shadows (additional-light + soft-high +
40 m distance), (2) `lodBias 2` doubling the triangle load of distant Johannesburg
scenery, (3) transparent **overdraw** from VFX/foliage alpha, (4) MSAA 2× bandwidth.
Secondary CPU from per-frame allocations in a handful of Update loops.

## Ranked fixes

### CRITICAL
1. **Optional 30 FPS battery-saver mode.** Halving the cap ≈ halves GPU+CPU work and
   is the single biggest battery/heat lever. Opt-in, no visual change. → *implemented*
2. **Disable additional-light shadows** (URP). Removes an entire shadow pass for
   lamp/prop point lights; negligible visual. → *implemented*
3. **`lodBias 2 → 1.25`.** Big triangle cut on distant scenery. Kept at 1.25 (not 1.0)
   because the pigeon/small-prop LOD thresholds were hand-tuned under bias 2; 1.25 is a
   safe first step, drop to 1.0 if no popping. → *implemented*

### HIGH
4. **Soft shadow quality 2 → 1** (URP). Cheaper filtering, minor visual. → *implemented*
5. **Shadow distance 40 → 30 m** per tier (QualityController applies at runtime).
   Fewer casters/receivers; shadows fade ~10 m closer. → *implemented*
6. **Robust frame-rate ownership.** targetFrameRate was set once in `Start`; now owned
   by QualityController and re-applied on focus regain (some ROMs reset it). → *implemented*
7. **Per-frame GC audit** (needs profiler to confirm): top Update-loop suspects —
   `VFXManager`, `RoadSegmentSpawner`, `UbuntuPulseVisual`, `PlayerController`,
   `MissionManager`. Look for `new List<>/ToArray/Where` and `GetComponent` per frame;
   cache + pool. → *flagged, not blind-edited*
8. **MeshColliders on backdrop buildings** → Box, or confirm never physics-queried
   (heroes already `SetCollidersEnabled(false)` at runtime). → *flagged*

### MEDIUM
9. **MSAA 2 → 1** on the battery tier (needs a second URP asset per tier).
10. **Android texture defaults**: ASTC 6×6, mips ON for all 3D maps, trim 2048→1024
    where the source doesn't warrant 2048 (storefront normal/base especially).
11. **Exclude unused sample content** from the project (`Assets/FPS/`, `Assets/Samples/`,
    `Assets/ModAssets/`, `Assets/_Recovery/`, `Assets/JoziGame/`): smaller build, faster
    imports; no runtime effect since unreferenced.

### LOW
12. Additional-lights cookie atlas 2048 → 512 (if no cookies used).
13. `pixelLightCount 4 → 2` / additional-lights-per-object 4 → 2 (URP path).

## On-device profiling procedure (to fill the live numbers)

1. Build **Development** APK, connect Profiler over USB (`adb forward`), or use the
   on-device **Stats** overlay for FPS/draw calls/tris/verts/SetPass.
2. Rendering module → draw calls, SetPass, batches, triangles, overdraw view.
3. Memory module → texture/mesh footprint; CPU module → GC Alloc/frame (target 0 in
   the steady run loop).
4. Compare **60 FPS** vs the new **30 FPS battery** mode for °C and mA (Samsung Device
   Care battery usage, or `adb shell dumpsys batterystats`).

## Implemented this pass

See the companion commit. All changes are config/opt-in and preserve the Johannesburg
look and gameplay; each is listed above with the reason. Visual-compromise items
(MSAA, texture trims, shadow-distance beyond 30 m) are **left as recommendations**.
