import bpy
import math
import os
import sys
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
source = os.path.abspath(args[0])
output = os.path.abspath(args[1])
target_height = float(args[2])

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source)
for obj in list(bpy.context.scene.objects):
    if obj.type != "MESH":
        bpy.data.objects.remove(obj, do_unlink=True)

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
for obj in meshes:
    obj.select_set(True)
    # The foliage projects from the planter's +X side; turn that side upward.
    obj.rotation_euler.rotate_axis("Z", math.radians(-90.0))
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)

def bounds():
    points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return lo, hi

minimum, maximum = bounds()
scale = target_height / (maximum.y - minimum.y)
for obj in meshes:
    obj.scale *= scale
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

minimum, maximum = bounds()
centre = (minimum + maximum) * 0.5
offset = Vector((-centre.x, -minimum.y, -centre.z))
for obj in meshes:
    obj.location += offset

bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
for obj in meshes:
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")

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
