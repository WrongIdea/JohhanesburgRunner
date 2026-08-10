import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector


def args():
    raw = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output-right", required=True)
    parser.add_argument("--output-left", required=True)
    parser.add_argument("--report", required=True)
    parser.add_argument("--height", type=float, default=9.0)
    parser.add_argument("--source-z-up", action="store_true")
    return parser.parse_args(raw)


def meshes():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def bounds(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    return (
        Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points))),
        Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points))),
    )


def apply_transforms(objects):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)


def orient_scale_ground(objects, height, mirrored, source_z_up):
    # The Jozi Meshy optimiser exports its Z-up result as Unity depth. Bake Z
    # into Y here so Unity receives a genuinely Y-up, grounded tree.
    if not source_z_up:
        for obj in objects:
            obj.rotation_euler.x = -math.pi / 2
        apply_transforms(objects)

    low, high = bounds(objects)
    current_height = (high.z - low.z) if source_z_up else (high.y - low.y)
    scale = height / current_height
    for obj in objects:
        obj.scale = (scale * (-1 if mirrored else 1), scale, scale)
    apply_transforms(objects)

    low, high = bounds(objects)
    centre_x = (low.x + high.x) * 0.5
    if source_z_up:
        centre_other = (low.y + high.y) * 0.5
        offset = Vector((-centre_x, -centre_other, -low.z))
    else:
        centre_other = (low.z + high.z) * 0.5
        offset = Vector((-centre_x, -low.y, -centre_other))
    for obj in objects:
        obj.location += offset
    bpy.context.view_layer.update()
    apply_transforms(objects)

    for obj in objects:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(obj.data)
        bm.free()
        obj.data.update()

    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")
        obj.select_set(False)


def export(path):
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        path_mode="COPY",
        embed_textures=False,
        bake_space_transform=True,
    )


def import_source(path):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=path)


def process(source, output, height, mirrored, source_z_up):
    import_source(source)
    objects = meshes()
    orient_scale_ground(objects, height, mirrored, source_z_up)
    low, high = bounds(objects)
    export(output)
    return {
        "output": output,
        "mirrored": mirrored,
        "boundsBeforeExport": {
            "width": high.x - low.x,
            "height": high.y - low.y,
            "depth": high.z - low.z,
            "groundMinimum": low.z if source_z_up else low.y,
        },
    }


def main():
    options = args()
    right = process(options.input, options.output_right, options.height, False, options.source_z_up)
    left = process(options.input, options.output_left, options.height, True, options.source_z_up)
    Path(options.report).write_text(
        json.dumps({"source": options.input, "right": right, "left": left}, indent=2)
    )
    print(json.dumps({"right": right, "left": left}, indent=2))


main()
