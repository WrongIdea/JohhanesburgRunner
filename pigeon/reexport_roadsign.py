import bpy
from pathlib import Path

SRC = "/Users/thapelopilanyane/My project/Assets/Art/Generated/Incoming/road_sign_new/Meshy_AI_Create_a_low_poly_mod_0802182911_texture_fbx/Meshy_AI_Create_a_low_poly_mod_0802182911_texture.fbx"
OUT = "/Users/thapelopilanyane/My project/Assets/Art/Generated/Incoming/road_sign_new/road_sign_unity.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)

bpy.ops.object.select_all(action="SELECT")
for o in bpy.data.objects:
    if o.type == 'MESH':
        bpy.context.view_layer.objects.active = o
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

meshes = [o for o in bpy.data.objects if o.type == 'MESH']
for o in meshes:
    d = o.dimensions
    print(f"ROADSIGN-REEXPORT {o.name} dims=({d.x:.3f},{d.y:.3f},{d.z:.3f})")

bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]

bpy.ops.export_scene.fbx(
    filepath=OUT,
    use_selection=True,
    object_types={"MESH"},
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    use_space_transform=True,
    bake_space_transform=True,
    axis_forward="-Z",
    axis_up="Y",
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    path_mode="COPY",
    embed_textures=False,
)
print(f"ROADSIGN-REEXPORT done -> {OUT}")
