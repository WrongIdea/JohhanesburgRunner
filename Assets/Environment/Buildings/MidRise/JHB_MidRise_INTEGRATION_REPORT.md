# Jozi Runner — JHB Mid-Rise Integration Report

## Project

- Unity project: `/Users/thapelopilanyane/My project`
- Unity: 6000.5.2f1
- Universal Render Pipeline: 17.5.0
- Addressables: not installed; the project uses direct prefab references and object pools.

## Imported runtime assets

The five source reports were reviewed before import. Only FBX and PNG runtime assets were
copied. Blender files and source reports were not copied into `Assets` or modified.

- Root: `Assets/Environment/Buildings/MidRise`
- Per-building folders: `JHB_MidRise01` through `JHB_MidRise05`
- Models: `<building>/Models`
- Source and converted textures: `<building>/Textures`
- URP materials: `<building>/Materials`
- Production prefab: `<building>/Prefabs/<building>.prefab`

## Modified project assets

- `Assets/Scripts/Editor/BuildingImporter.cs`
  - Added the idempotent `Joburg Runner/Import JHB Mid-Rise Buildings` command.
  - Configures model/texture import, converts packed masks, builds materials and prefabs,
    validates the results, and updates the hero catalogue.
- `Assets/Environment/Decor/HeroBuildingSet.asset`
  - Added five enabled variants without removing the two existing hero buildings.
- No gameplay scene, runtime script, existing prefab, or hero-building asset was modified.
- Safety copy: `_Backups/MidRiseIntegration_20260724/HeroBuildingSet.asset.bak`

## Model and texture import

- Visual meshes: scale factor 1, file-unit conversion enabled, animation/cameras/lights/
  visibility/blendshapes/colliders disabled, imported normals, calculated MikkTSpace
  tangents, Low mesh compression, optimized vertices/polygons, Auto index format,
  Read/Write disabled.
- Collision meshes: same settings, with Read/Write enabled to match the existing project
  convention for reliable runtime MeshCollider cooking.
- Base colour and JHB_MidRise04 emissive: sRGB.
- Normal: linear Normal Map.
- Metallic/smoothness: generated without changing the source; output R = source B
  (metallic), output A = 1 - source G (smoothness), sRGB disabled.
- Textures: mipmaps and streaming mipmaps enabled, repeat wrap, bilinear filtering,
  maximum size 2048, alpha transparency disabled.

## Materials and performance

- URP/Lit, one persistent material per authored FBX material slot, shared by all three
  LODs of that building.
- GPU instancing enabled.
- No transparent surfaces, real-time lights, reflection probes, per-child scripts, text,
  logos, or readable signage were added.
- JHB_MidRise04 alone uses its emissive atlas.
- LOD2 shadow casting is disabled; LOD0/LOD1 cast and all visual LODs receive shadows.

## Prefabs, LODs, and collision

- Hierarchy: root (LODGroup) > `Visual` > `LOD0`, `LOD1`, `LOD2`; root > `Collision`.
- Root transforms and child placement are identity with uniform scale 1.
- LOD fade mode: None (mobile-oriented; no cross-fade).
- Project-standard transition heights: LOD0 0.55, LOD1 0.25, LOD2/cull 0.08.
- Each prefab has one non-convex MeshCollider sourced only from the supplied low-poly
  collision FBX. Visual LOD meshes have no colliders.
- Existing environment layer/tag convention is Default/Untagged and was retained.
- Existing hero-prefab static flags were retained. Runtime hero placement disables
  colliders and uses the established pool.

## Procedural placement

- Catalogue: `Assets/Environment/Decor/HeroBuildingSet.asset`
- Each new building has weight 4, both roadside orientations allowed, identity scale,
  40-degree signed road-facing yaw, and footprint radius 8.5 m.
- Hero placement is enabled across all districts at the scene's 1–2 segment cadence,
  making the new set approximately 91% of eligible weighted selections.
- The existing `EnvironmentDecorDirector` continues to provide weighted selection,
  recent-entry avoidance, sparse CBD cadence, independent roadside sockets, pooling,
  placeholder fallback, and overlap/clearance handling.
- No spawn logic was replaced or removed.

## Automated validation results

| Prefab | Bounds (m) | Ground | LOD renderers | Collider |
| --- | --- | --- | --- | --- |
| JHB_MidRise01 | 10.25 × 21.45 × 8.25 | Y=0.000 | 5 / 5 / 4 | 1 MeshCollider |
| JHB_MidRise02 | 9.25 × 14.45 × 9.70 | Y=0.000 | 6 / 6 / 5 | 1 MeshCollider |
| JHB_MidRise03 | 11.75 × 12.45 × 8.18 | Y=0.000 | 6 / 6 / 5 | 1 MeshCollider |
| JHB_MidRise04 | 8.25 × 18.45 × 8.32 | Y=0.000 | 5 / 5 / 5 | 1 MeshCollider |
| JHB_MidRise05 | 10.75 × 21.95 × 9.25 | Y=0.000 | 5 / 5 / 5 | 1 MeshCollider |

Unity batch import and validation completed with no reported problems. It verified URP
shaders, non-null materials, unique renderer membership per LOD, upright metre bounds,
ground alignment, non-negative transforms, and collider type/count.

## Manual review

- Current spawn override: all five mid-rise catalogue entries are disabled. The active
  roadside hero pool contains only `JHB_Building02` and `JHB_SkylinePop_02`.
- A device playtest is still recommended for visual LOD popping, metallic/smoothness
  response under the final lighting, JHB_MidRise04 emission at night, camera composition,
  and sustained mobile frame time.
- The established project transition convention culls after LOD2 at 0.08. If a longer
  silhouette range is desired after device testing, lower the final LOD threshold toward
  0.01 and re-check overdraw/draw-call cost.
- Collision is disabled by the current hero-roadside runtime path because these buildings
  sit outside the playable lanes. Enable it only for a placement mode that intentionally
  allows runner contact.

## Generated logs

- `Builds/midrise_import_report.txt`
- `Builds/midrise_unity.log`
