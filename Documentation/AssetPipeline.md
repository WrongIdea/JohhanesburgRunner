# Controlled generated-asset pipeline

## 1. Create a request

Copy `AssetRequests/Pending/JR-VEH-TAXI-001.json`, give it a unique ID, and complete every field.
Validate it against `AssetRequests/asset-request.schema.json`. Categories, states and collider
names remain readable strings; the Editor review window converts them safely before Unity
deserialisation.

The limits are warnings, never destructive edits: 25,000 triangles for hero buildings, 12,000
for standard buildings, 15,000 for vehicles, 8,000 for obstacles, 3,000 for small props, 1024px
textures, two ordinary materials and four vehicle materials.

## 2. Generate manually in Meshy

Paste the approved prompt into Meshy manually. This repository makes no paid call and contains
no live provider. Each paid generation requires a fresh human approval; never create a
regeneration loop. Keep keys, raw downloads and task responses outside source control.

## 3. Import and validate

Put the downloaded `.fbx`, `.glb`, or `.gltf` and its textures under
`Assets/Art/Generated/Incoming`. Unity records the source and writes
`<model>.validation.json`. The report covers mesh names, vertices/triangles, materials, texture
dimensions/memory, scale, grounding, rotation, roots, negative scaling and probable material
duplicates. Import never means approval.

GLB support comes from the project's glTFast package; if a GLB does not appear as a selectable
GameObject in the installed package version, export FBX from Blender and review that copy.

## 4. Review

Open **Jozi Runner > Asset Pipeline > Asset Review**, select the incoming model and optional
request, then click **Open In Review Scene**. The isolated scene contains neutral ground and
lighting, gameplay/front/side/rear cameras, 1m and 1.8m references, and statistics in the review
window. Visual checks are mandatory even with zero warnings.

## 5. Optional Blender cleanup

Cleanup is explicit and non-destructive:

```sh
blender --background --python Tools/Blender/clean_generated_asset.py -- \
  --approved --input /absolute/model.glb --output /absolute/cleaned.fbx \
  --report /absolute/processing-report.json
```

It applies transforms, makes normals consistent, grounds the asset, sets a ground-centre pivot,
removes empty objects/orphans, exports FBX and writes a report. It does not decimate faces, hero
buildings, characters or moving parts—or anything else.

## 6. Approve or reject

**Approve** preserves a source copy in `Approved`, copies the processed model into Buildings,
Vehicles, Obstacles or Props, creates a `PF_` prefab, applies URP/Lit where legacy Standard is
found, adds the requested collider, optional LOD0 group, static flags and
`GeneratedAssetMetadata`. It does not place or spawn the prefab.

**Reject** retains the source in Incoming, writes a rejection report under `Rejected`, includes
warnings and regeneration guidance, and updates a compatible selected request.

## 7. Add to production spawning (manual)

Only after playtesting the approved prefab, deliberately add it to the relevant chunk prefab,
zone profile/catalogue, hero-building set or gameplay content definition. Confirm lane bounds,
pooling behaviour, district restrictions and spawn weights. This separation prevents approval
from silently altering road generation or live scenes.

## Troubleshooting

- Scale: work in metres; compare the 1m cube/1.8m capsule; use Blender cleanup or correct the
  model import scale.
- Materials/textures: keep URP/Lit, pack maps where practical, keep textures at 1024 unless the
  request documents an exception, and remove duplicate material slots manually.
- Normals: use explicit Blender cleanup, then inspect all four cameras.
- Ground/pivot: the lowest visible point should be at Y=0 and the pivot at ground centre.
- Rotation: Unity expects +Y up and the asset's forward direction at +Z.
- Colliders: use Box/Capsule/Sphere for moving gameplay obstacles; reserve non-convex Mesh
  colliders for static scenery. Inspect collider bounds before production use.
- LODs: approval never auto-decimates. Import separately authored LOD meshes and configure
  transitions based on gameplay-camera visibility.
