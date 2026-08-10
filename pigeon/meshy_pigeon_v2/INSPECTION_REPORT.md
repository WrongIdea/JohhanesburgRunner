# Meshy Pigeon Model — Inspection & Cleanup Report

**Source:** `PegionModel.zip` → `Meshy_AI_Create_a_realistic_lo_0806182151_texture.fbx`
**Date:** 2026-08-06
**Tooling:** Blender 5.2 (headless) — see `inspect_v2.py` (read-only) and `cleanup_v2.py` (non-destructive prep).

---

## 1. Import & Transform

| Check | As imported | After cleanup |
|---|---|---|
| Object transform | identity (loc 0, rot 0, scale 1) | identity, applied |
| World size (X×Y×Z) | **1.899 × 0.844 × 0.746 m** (≈2.5× too big) | **0.764 × 0.339 × 0.300 m** |
| Height (Z) | 0.746 m | **0.300 m** ✓ (target ~0.30) |
| Body length (Y, bill→tail) | 0.844 m | **0.339 m** ✓ (target ~0.35) |
| Wingspan (X) | 1.899 m | 0.764 m (wings are spread) |
| Forward axis | **−Y = head/forward** (Meshy standard); +Z up | unchanged |
| Origin / pivot | body centre (feet at Z −0.374) | **feet, bottom-centre** |
| Feet on ground | no (straddled origin) | **yes, lowest Z = 0.00000** ✓ |
| Centred in X/Y | ~yes | **exact (0.0000, 0.0000)** ✓ |

Scaling was a single **uniform factor (÷2.49)** so proportions are untouched.

## 2. Geometry

| Metric | Value |
|---|---|
| **Triangle count** | **174,638** |
| **Vertex count** | **87,289** |
| Face types | 100% triangles (0 quads, **0 n-gons**) |
| Non-manifold edges | **0** |
| Open/boundary edges | **0** (watertight) |
| Loose/wire geometry | **0** |
| Duplicate-position verts | **0** |
| Internal / hidden faces | **none** (single closed manifold shell) |
| Connected islands | **1** |
| Flipped normals | 33 disagreeing edge-pairs → **fixed** (recalculated outward) |
| Symmetry | shape symmetric to **~1 mm median / 2 mm p90** vs 339 mm body |

**Verdict:** geometrically **clean and watertight** — no errors typical of bad scans. The one real concern is **density**: 174 k triangles is very heavy (the previous game pigeon was 2.2 k). Most of it is the individually-modelled wing/tail feathers.

## 3. Topology

Edge flow evaluated at neck, shoulders, wing roots/elbows/tips, tail, legs, feet.

- The mesh is **uniform, isotropic "triangle soup"** (Meshy's typical output). It has **no anatomical edge loops** following the shoulder, wing joints, neck, or hip.
- Deformation prognosis: with a good skeleton + weight painting it **will skin and deform acceptably** (the density hides most artefacts), but it is **not "professional" deformation topology** — there are no loops to control creasing at the wing elbow/shoulder during **flap / fold**, and the feathers are rigid slabs that won't spread/overlap naturally.
- **Pose:** wings **fully spread + tail fanned** (a glide/display pose), not a neutral folded rest pose.

## 4. UVs & Materials

| Check | Result |
|---|---|
| UV layers | 1 (`UVMap`) |
| UVs in 0–1 range | **yes** (no loops outside bounds) |
| Stretching / seams | none flagged; textures render correctly across the body |
| Overlap | none harmful; a single full-body layout |
| Material slots | 1 (`Material.001`) |
| Textures | full PBR @ **2048×2048**: base_color, normal, roughness, metallic, emissive |

**Verdict:** UVs and materials are **good and production-usable**. Textures embedded in the exported FBX for a self-contained handoff.

## 5. Cleanup Actions Performed (non-destructive)

1. ✅ Recalculated normals consistently outward (fixed 33 flipped pairs).
2. ✅ Welded exact-coincident verts (threshold 1e-6 — 0 moved, safety only).
3. ✅ Applied object rotation/scale; uniform scale to 30 cm height (Unity-correct).
4. ✅ Moved origin to **feet / bottom-centre**; placed on world origin.
5. ✅ Preserved silhouette, geometry, UVs, and textures — **appearance unchanged**.

**Not done (deliberately):** no decimation/retopology, no edge-loop rebuilding — per brief to preserve the original Meshy model. See recommendation below.

## 6. Deliverables

- `PigeonV2_clean.blend` — cleaned Blender scene.
- `PigeonV2_clean.fbx` — clean FBX, textures embedded, Unity-scale, feet-origin.
- `raw/` — original FBX + PBR PNGs (untouched).
- `validate_frames/`, `inspect_frames/` — render proofs.

## 7. Ready for rigging?

**Yes — with one caveat.** The model is clean, watertight, correctly scaled, correctly oriented, grounded, symmetric, and UV/texture-ready, so it **can go straight to skeleton + skinning**.

**Strong recommendation before or alongside rigging:** the **174 k-triangle triangle-soup** is impractical for real-time use (the shipping game bird is ~2 k) and non-ideal for clean joint deformation. Two options for the next step:
- **Decimate** to ~8–15 k tris (fast, preserves silhouette, mild softening of feather edges) — good enough for a game/background bird, or
- **Retopologize** the wings/shoulders/neck with proper edge loops (more work, best deformation) if this is a hero close-up asset.

Say which and I'll produce that version next.
