import bpy
import os
import sys

args = sys.argv[sys.argv.index("--") + 1:]
source = os.path.abspath(args[0])
output = os.path.abspath(args[1])

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source)

for obj in list(bpy.context.scene.objects):
    if obj.type not in {"MESH"}:
        bpy.data.objects.remove(obj, do_unlink=True)

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
for obj in meshes:
    obj.select_set(True)

bpy.ops.export_scene.fbx(
    filepath=output,
    use_selection=True,
    object_types={"MESH"},
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_UNITS",
    axis_forward="-Z",
    axis_up="Y",
    bake_space_transform=True,
    path_mode="COPY",
    embed_textures=False,
    add_leaf_bones=False,
)
