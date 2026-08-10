# Imported Hero Buildings Report

## Sources

- `/Users/thapelopilanyane/Downloads/Building3.glb`
- `/Users/thapelopilanyane/Downloads/building4.glb`
- `/Users/thapelopilanyane/Downloads/building5.glb`
- `/Users/thapelopilanyane/Downloads/Building6.glb`
- `/Users/thapelopilanyane/Downloads/building7.glb`

The Downloads files were not modified. `Building3.glb` and `building4.glb` have the
same SHA-256 hash and are exact source duplicates.

## Blender preparation

- Blender 5.2 LTS working files:
  - `blender_buildings/production/JHB_HeroBuilding03/JHB_HeroBuilding03.blend`
  - `blender_buildings/production/JHB_HeroBuilding04/JHB_HeroBuilding04.blend`
  - `blender_buildings/production/JHB_HeroBuilding05/JHB_HeroBuilding05.blend`
- Original dimensions: approximately 1.15 × 1.12 × 1.90 m.
- Normalized dimensions: approximately 10.92 × 10.66 × 18.00 m.
- Pivot: ground centre; Unity base Y=0; Blender -Y façade exports to Unity +Z.
- Original geometry: 410,780 triangles per source.
- LOD0: 40,000 triangles.
- LOD1: 12,000 triangles.
- LOD2: 3,000 triangles.
- Collision: one 12-triangle box.
- Embedded 2048 textures were extracted without altering their content.

## Unity assets

- Root: `Assets/Environment/Buildings/ImportedHeroes`
- Prefabs:
  - `JHB_HeroBuilding03/Prefabs/JHB_HeroBuilding03.prefab`
  - `JHB_HeroBuilding04/Prefabs/JHB_HeroBuilding04.prefab`
  - `JHB_HeroBuilding05/Prefabs/JHB_HeroBuilding05.prefab`
- Each prefab uses URP/Lit, GPU instancing, three exclusive LOD levels, no LOD
  cross-fade, LOD2 shadows disabled, and one non-convex collision MeshCollider.
- Textures use 2048 maximum size, mipmaps, streaming mipmaps, and mobile compression.
- glTF metallic/roughness was converted to Unity metallic/smoothness:
  R = source B and A = 1 - source G.

## Integration and validation

- All three prefabs are enabled in `HeroBuildingSet.asset` with equal weight.
- They use both road sides, uniform scale 1, 40-degree presentation yaw, and the
  existing pooled weighted no-repeat system.
- Automated Unity validation passed for all three:
  - URP materials present.
  - LOD transitions 0.55 / 0.25 / 0.08.
  - Bounds approximately 10.98 × 18.07 × 10.69 m.
  - Visual base Y=0.000.
  - One collision MeshCollider and no visual-mesh colliders.
- Source duplication means HeroBuilding03 and HeroBuilding04 currently look identical.
- HeroBuilding06 is a distinct ornate tower normalized from 855,842 source triangles
  to the same 40K / 12K / 3K mobile LOD budgets and an 8.57 × 8.62 × 18.00 m footprint.
- HeroBuilding07 is a distinct colorful rooftop-services building normalized from
  1,008,616 source triangles to 40K / 12K / 3K LODs and approximately
  13.62 × 10.32 × 18.00 m.
