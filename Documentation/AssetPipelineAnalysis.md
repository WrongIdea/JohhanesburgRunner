# Existing Jozi Runner asset conventions

Analysis date: 2026-07-28.

- Unity: 6000.5.2f1 (Unity 6.5).
- Rendering: Universal Render Pipeline 17.5.0. Existing authored environment materials use
  `Universal Render Pipeline/Lit`.
- Existing structure: gameplay prefabs are primarily under `Assets/Prefabs`; authored buildings
  are under `Assets/Environment/Buildings`; gameplay definitions are under
  `Assets/GameplayContent`; district configuration is under `Assets/Environment/Zones`.
- Existing content: chunk prefabs reference taxi, barrier, pothole, coin and power-up obstacles;
  environment prefabs cover buildings, pavements, shops, trees, lights, signs and street props.
- Spawn configuration: `RoadSegmentSpawner`, `ChunkManager`, zone profiles/catalogues, chunk
  prefabs and hero-building sets own production spawning. The generated pipeline does not edit
  any of them.
- Naming: imported authored buildings generally use `JHB_...`; final authored building prefabs
  generally use `PF_...`; chunk prefabs use `Chunk_...`; decoration prefabs use `Decor...`.
  Approved generated prefabs use `PF_` while retaining the request name.
- LOD: optimized authored buildings use `LODGroup` with separately authored LOD meshes.
  Approval creates only a non-destructive LOD0 group when requested; it never fabricates or
  decimates lower levels.
- Colliders: static authored buildings may use `MeshCollider`; gameplay obstacles use cheap,
  predictable primitive colliders. Approval defaults to the request's collider choice.
- Static flags: authored buildings use static batching/occlusion-related flags; pooled and moving
  decoration often remains non-static. Generated vehicles remain non-static; stationary assets
  receive batching-static only.
- Camera: production uses a perspective elevated rear-follow camera. Review uses a 60-degree
  elevated rear gameplay camera plus 45-degree orthographic-style inspection viewpoints
  (perspective cameras at fixed positions).
- Performance: the project targets mobile/Android and already uses pooling, LODs, mesh
  simplification tooling and URP. No stricter central numeric budgets were found, so the brief's
  warning defaults are used: 25k hero building, 12k standard building, 15k vehicle, 8k obstacle,
  3k small prop; 1024 textures; two ordinary materials and four vehicle materials.

No existing assets were moved and no production scene, spawn set or prefab was modified.
