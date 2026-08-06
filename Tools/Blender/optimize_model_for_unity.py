#!/usr/bin/env python3
"""Optimize an approved generated model and export a new Unity-ready FBX."""

import argparse
import json
import math
import re
import shutil
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

import bpy
from mathutils import Vector


def arguments():
    raw = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--approved", action="store_true")
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--report", required=True)
    parser.add_argument("--target-triangles", type=int, required=True)
    parser.add_argument("--target-height", type=float, required=True)
    parser.add_argument("--max-texture-size", type=int, default=1024)
    return parser.parse_args(raw)


def import_model(path):
    extension = Path(path).suffix.lower()
    if extension == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    elif extension in {".glb", ".gltf"}:
        bpy.ops.import_scene.gltf(filepath=path)
    else:
        raise ValueError(f"Unsupported model format: {extension}")


def mesh_objects():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def triangle_count(objects):
    return sum(sum(max(0, len(poly.vertices) - 2) for poly in obj.data.polygons) for obj in objects)


def world_bounds(objects):
    corners = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    if not corners:
        raise RuntimeError("The imported file contains no visible mesh bounds.")
    minimum = Vector((min(v.x for v in corners), min(v.y for v in corners), min(v.z for v in corners)))
    maximum = Vector((max(v.x for v in corners), max(v.y for v in corners), max(v.z for v in corners)))
    return minimum, maximum


def apply_transforms(objects):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        obj.select_set(False)


def flatten_hierarchy(objects):
    """Remove imported transform parents while preserving each mesh's world transform."""
    for obj in objects:
        world = obj.matrix_world.copy()
        obj.parent = None
        obj.matrix_world = world
    bpy.context.view_layer.update()


def orient_for_unity(objects, source_suffix):
    """Meshy FBX files arrive Z-up; rotate their geometry into Unity's Y-up space."""
    if source_suffix.lower() != ".fbx":
        return
    for obj in objects:
        obj.rotation_euler.rotate_axis("X", math.radians(-90.0))
    bpy.context.view_layer.update()
    apply_transforms(objects)


def resize_and_ground(objects, target_height, up_axis):
    minimum, maximum = world_bounds(objects)
    height = maximum[up_axis] - minimum[up_axis]
    if height <= 0:
        raise RuntimeError("Model height is zero; scaling cannot be calculated.")
    factor = target_height / height
    for obj in bpy.context.scene.objects:
        obj.scale *= factor
    bpy.context.view_layer.update()
    apply_transforms(objects)

    minimum, maximum = world_bounds(objects)
    horizontal_axes = [axis for axis in range(3) if axis != up_axis]
    offset = Vector((0.0, 0.0, 0.0))
    offset[horizontal_axes[0]] = -(minimum[horizontal_axes[0]] + maximum[horizontal_axes[0]]) / 2
    offset[horizontal_axes[1]] = -(minimum[horizontal_axes[1]] + maximum[horizontal_axes[1]]) / 2
    offset[up_axis] = -minimum[up_axis]
    for obj in objects:
        obj.location += offset
    bpy.context.view_layer.update()
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    for obj in objects:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")
        obj.select_set(False)


def decimate(objects, target):
    before = triangle_count(objects)
    if before <= target:
        return False
    ratio = max(0.001, min(1.0, target / before))
    for obj in objects:
        modifier = obj.modifiers.new(name="MobileTriangleBudget", type="DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = ratio
        modifier.use_collapse_triangulate = True
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
    return True


def recalculate_normals(objects):
    for obj in objects:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.mesh.normals_make_consistent(inside=False)
        bpy.ops.object.mode_set(mode="OBJECT")
        obj.select_set(False)


def copy_textures(source_dir, output_dir):
    copied = []
    for pattern in ("*.png", "*.jpg", "*.jpeg", "*.tga"):
        for source in source_dir.glob(pattern):
            destination = output_dir / source.name
            if source.resolve() != destination.resolve():
                shutil.copy2(source, destination)
            copied.append(str(destination))
    return sorted(set(copied))


def extract_embedded_textures(output_dir):
    """Write images packed inside GLB/glTF files beside the exported FBX."""
    extracted = []
    output_dir.mkdir(parents=True, exist_ok=True)
    for index, image in enumerate(bpy.data.images):
        if image.type != "IMAGE":
            continue
        width, height = image.size  # Access forces lazy-loaded GLB images into memory.
        if width <= 0 or height <= 0 or not image.has_data:
            continue
        safe_name = re.sub(r"[^A-Za-z0-9._-]+", "_", Path(image.name).stem).strip("._")
        if not safe_name:
            safe_name = f"texture_{index}"
        destination = output_dir / f"{safe_name}.png"
        image.filepath_raw = str(destination)
        image.file_format = "PNG"
        image.save()
        extracted.append(str(destination))
    return sorted(set(extracted))


def relink_material_textures(texture_paths):
    paths = [Path(path) for path in texture_paths]

    def texture_for(role):
        role = role.lower()
        if role == "base color":
            candidates = [
                path for path in paths
                if not any(tag in path.stem.lower() for tag in ("normal", "roughness", "metallic", "emission"))
            ]
        else:
            candidates = [path for path in paths if role.replace(" ", "") in path.stem.lower().replace("_", "")]
        return candidates[0] if candidates else None

    relinked = []
    for material in bpy.data.materials:
        if not material.use_nodes or not material.node_tree:
            continue
        for node in material.node_tree.nodes:
            if node.type != "TEX_IMAGE":
                continue
            destinations = [
                link.to_socket.name
                for output in node.outputs
                for link in output.links
            ]
            role = None
            if "Base Color" in destinations:
                role = "base color"
            elif "Emission Color" in destinations:
                role = "emission"
            elif "Roughness" in destinations:
                role = "roughness"
            elif "Metallic" in destinations:
                role = "metallic"
            elif any(link.to_node.type == "NORMAL_MAP" for output in node.outputs for link in output.links):
                role = "normal"
            selected = texture_for(role) if role else None
            if selected:
                image = bpy.data.images.load(str(selected), check_existing=True)
                if role in {"normal", "roughness", "metallic"}:
                    image.colorspace_settings.name = "Non-Color"
                node.image = image
                relinked.append({"role": role, "path": str(selected)})
    return relinked


def resize_textures(texture_paths, maximum):
    resized = []
    for path_value in texture_paths:
        path = Path(path_value)
        image = bpy.data.images.load(str(path), check_existing=True)
        width, height = image.size
        longest = max(width, height)
        if longest <= maximum:
            continue
        factor = maximum / longest
        image.scale(max(1, round(width * factor)), max(1, round(height * factor)))
        image.filepath_raw = str(path)
        image.save()
        resized.append({"path": str(path), "from": [width, height], "to": list(image.size)})
    return resized


def main():
    opts = arguments()
    if not opts.approved:
        raise RuntimeError("Optimization requires --approved; the original was not changed.")
    if opts.target_triangles < 100:
        raise ValueError("--target-triangles must be at least 100.")
    if not math.isfinite(opts.target_height) or opts.target_height <= 0:
        raise ValueError("--target-height must be a positive number of metres.")
    if opts.max_texture_size < 256:
        raise ValueError("--max-texture-size must be at least 256.")

    source = Path(opts.input).expanduser().resolve()
    output = Path(opts.output).expanduser().resolve()
    report_path = Path(opts.report).expanduser().resolve()
    project_root = Path(__file__).resolve().parents[2]
    incoming = (project_root / "Assets/Art/Generated/Incoming").resolve()
    if incoming not in output.parents:
        raise RuntimeError(f"Output must be below Unity Incoming: {incoming}")
    if source == output:
        raise RuntimeError("Output must differ from input; originals are immutable.")

    started = time.monotonic()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    import_model(str(source))
    objects = mesh_objects()
    if not objects:
        raise RuntimeError("No mesh objects were imported.")
    # Unity and the FBX exporter both preserve Y as vertical for the generated
    # assets used by this project (including Meshy FBX and GLB downloads).
    up_axis = 1
    flatten_hierarchy(objects)
    orient_for_unity(objects, source.suffix)
    output.parent.mkdir(parents=True, exist_ok=True)
    embedded_textures = (
        extract_embedded_textures(output.parent)
        if source.suffix.lower() in {".glb", ".gltf"}
        else []
    )

    before_triangles = triangle_count(objects)
    before_min, before_max = world_bounds(objects)
    apply_transforms(objects)
    resize_and_ground(objects, opts.target_height, up_axis)
    decimation_performed = decimate(objects, opts.target_triangles)
    recalculate_normals(objects)
    # Decimation can move the extreme vertices slightly; enforce final dimensions and pivot.
    resize_and_ground(objects, opts.target_height, up_axis)
    bpy.ops.outliner.orphans_purge(do_recursive=True)
    objects = mesh_objects()
    after_triangles = triangle_count(objects)
    after_min, after_max = world_bounds(objects)

    report_path.parent.mkdir(parents=True, exist_ok=True)
    if source.suffix.lower() in {".glb", ".gltf"}:
        copied_textures = embedded_textures
    else:
        copied_textures = copy_textures(source.parent, output.parent)
    resized_textures = resize_textures(copied_textures, opts.max_texture_size)
    relinked_textures = relink_material_textures(copied_textures)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(output),
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        path_mode="COPY",
        embed_textures=False,
    )
    report = {
        "input": str(source),
        "output": str(output),
        "processedUtc": datetime.now(timezone.utc).isoformat(),
        "triangleCountBefore": before_triangles,
        "triangleCountAfter": after_triangles,
        "targetTriangleCount": opts.target_triangles,
        "dimensionsBefore": list(before_max - before_min),
        "dimensionsAfter": list(after_max - after_min),
        "targetHeightMetres": opts.target_height,
        "decimationPerformed": decimation_performed,
        "texturesCopied": copied_textures,
        "texturesResized": resized_textures,
        "texturesRelinked": relinked_textures,
        "processingDurationMilliseconds": round((time.monotonic() - started) * 1000),
        "warnings": (
            ["Decimation did not reach the exact target; inspect the Unity validation report."]
            if after_triangles > opts.target_triangles * 1.1 else []
        ),
        "errors": [],
    }
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
