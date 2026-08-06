import bpy
import json
import math
import os
import sys
from mathutils import Vector


def args_after_double_dash():
    if "--" not in sys.argv:
        return []
    return sys.argv[sys.argv.index("--") + 1:]


def parse_args(values):
    result = {}
    index = 0
    while index < len(values):
        key = values[index]
        if key.startswith("--") and index + 1 < len(values):
            result[key[2:]] = values[index + 1]
            index += 2
        else:
            index += 1
    return result


def world_bounds(objects):
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in objects
        for corner in obj.bound_box
    ]
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return minimum, maximum


options = parse_args(args_after_double_dash())
input_path = os.path.abspath(options["input"])
output_path = os.path.abspath(options["output"])
report_path = os.path.abspath(options["report"])

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=input_path)

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if not meshes:
    raise RuntimeError("Taxi2 FBX contains no mesh objects")

# Meshy occasionally exports Blender's untouched default cube with the asset.
# Keep the detailed vehicle and discard only obvious primitive debris.
for obj in list(meshes):
    triangles = sum(len(poly.vertices) - 2 for poly in obj.data.polygons)
    if obj.name.lower().startswith("cube") and triangles <= 12:
        bpy.data.objects.remove(obj, do_unlink=True)

for obj in list(bpy.context.scene.objects):
    if obj.type in {"CAMERA", "LIGHT"}:
        bpy.data.objects.remove(obj, do_unlink=True)

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if not meshes:
    raise RuntimeError("No vehicle mesh remains after cleanup")

# Normalize the source in its current axes, then rotate it so Unity receives
# width on X, height on Y, and vehicle length on Z.
minimum, maximum = world_bounds(meshes)
dimensions = maximum - minimum
target_source_axes = Vector((4.8, 2.1, 1.9))
scale = Vector((
    target_source_axes.x / dimensions.x,
    target_source_axes.y / dimensions.y,
    target_source_axes.z / dimensions.z,
))

for obj in meshes:
    obj.scale = Vector((
        obj.scale.x * scale.x,
        obj.scale.y * scale.y,
        obj.scale.z * scale.z,
    ))

bpy.context.view_layer.objects.active = meshes[0]
for obj in meshes:
    obj.select_set(True)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# Source vehicle length is X. Rotate it to Unity-forward Z and bake transforms.
for obj in meshes:
    obj.rotation_euler.rotate_axis("Y", math.radians(90.0))
bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)

minimum, maximum = world_bounds(meshes)
centre = (minimum + maximum) * 0.5
offset = Vector((-centre.x, -minimum.y, -centre.z))
for obj in meshes:
    obj.location += offset

bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
for obj in meshes:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")
    obj.select_set(False)

for obj in meshes:
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.shade_smooth_by_angle()

minimum, maximum = world_bounds(meshes)
final_dimensions = maximum - minimum
triangles = sum(
    sum(len(poly.vertices) - 2 for poly in obj.data.polygons)
    for obj in meshes
)

os.makedirs(os.path.dirname(output_path), exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=output_path,
    use_selection=True,
    object_types={"MESH"},
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_UNITS",
    axis_forward="-Z",
    axis_up="Y",
    bake_space_transform=False,
    path_mode="COPY",
    embed_textures=False,
    add_leaf_bones=False,
)

report = {
    "input": input_path,
    "output": output_path,
    "removed_default_cube": True,
    "mesh_count": len(meshes),
    "triangles": triangles,
    "dimensions_metres": [round(value, 6) for value in final_dimensions],
    "bounds_min": [round(value, 6) for value in minimum],
    "bounds_max": [round(value, 6) for value in maximum],
    "unity_forward_axis": "Z",
    "pivot": "bottom-centre",
}
with open(report_path, "w", encoding="utf-8") as report_file:
    json.dump(report, report_file, indent=2)

print(json.dumps(report, indent=2))
