#!/usr/bin/env python3
"""Non-destructive Blender cleanup for an explicitly approved local asset.

Run with Blender, never plain Python:
  blender --background --python Tools/Blender/clean_generated_asset.py -- \
    --input model.glb --output cleaned.fbx --report processing-report.json
"""
import argparse
import json
import os
import sys
import time
import uuid
from datetime import datetime, timezone

import bpy


def arguments():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--report", required=True)
    parser.add_argument("--approved", action="store_true",
                        help="Required acknowledgement that this explicit cleanup was approved.")
    return parser.parse_args(args)


def import_model(path):
    ext = os.path.splitext(path)[1].lower()
    if ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    elif ext in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=path)
    else:
        raise ValueError(f"Unsupported input format: {ext}")


def main():
    opts = arguments()
    if not opts.approved:
        raise RuntimeError("Cleanup requires --approved. No processing was performed.")

    started = datetime.now(timezone.utc)
    start_clock = time.monotonic()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    import_model(os.path.abspath(opts.input))

    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    for obj in meshes:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.mesh.normals_make_consistent(inside=False)
        bpy.ops.object.mode_set(mode="OBJECT")
        obj.select_set(False)

    # Ground the complete model and put all object origins at the ground-centre pivot.
    world_corners = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    if world_corners:
        min_x, max_x = min(v.x for v in world_corners), max(v.x for v in world_corners)
        min_y, max_y = min(v.y for v in world_corners), max(v.y for v in world_corners)
        min_z = min(v.z for v in world_corners)
        pivot = ((min_x + max_x) / 2, (min_y + max_y) / 2, min_z)
        for obj in bpy.context.scene.objects:
            obj.location.z -= min_z
        bpy.context.scene.cursor.location = (pivot[0], pivot[1], 0)
        for obj in meshes:
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
            obj.select_set(False)

    for obj in list(bpy.context.scene.objects):
        if obj.type == "EMPTY" and not obj.children:
            bpy.data.objects.remove(obj, do_unlink=True)

    polygons_before = sum(len(o.data.polygons) for o in meshes)
    materials_before = {slot.material.name for o in meshes for slot in o.material_slots if slot.material}
    bpy.ops.outliner.orphans_purge(do_recursive=True)
    meshes_after = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    polygons_after = sum(len(o.data.polygons) for o in meshes_after)
    materials_after = {slot.material.name for o in meshes_after for slot in o.material_slots if slot.material}

    output = os.path.abspath(opts.output)
    os.makedirs(os.path.dirname(output), exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(filepath=output, use_selection=True, apply_unit_scale=True,
                             axis_forward="-Z", axis_up="Y", add_leaf_bones=False)
    report = {
        "input": os.path.abspath(opts.input),
        "output": output,
        "processedUtc": datetime.now(timezone.utc).isoformat(),
        "triangleCountBefore": polygons_before,
        "triangleCountAfter": polygons_after,
        "materialCountBefore": len(materials_before),
        "materialCountAfter": len(materials_after),
        "materialsBefore": sorted(materials_before),
        "materialsAfter": sorted(materials_after),
        "processingDurationMilliseconds": round((time.monotonic() - start_clock) * 1000),
        "scriptVersion": "1.1.0",
        "exitCode": 0,
        "warnings": [],
        "errors": [],
        "decimationPerformed": False
    }
    report_path = os.path.abspath(opts.report)
    os.makedirs(os.path.dirname(report_path), exist_ok=True)
    with open(report_path, "w", encoding="utf-8") as handle:
        json.dump(report, handle, indent=2)
    append_audit(opts, report, started, report_path)


def append_audit(opts, report, started, report_path):
    project_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    audit_tools = os.path.join(project_root, "Tools", "Audit")
    if audit_tools not in sys.path:
        sys.path.insert(0, audit_tools)
    from auditlib import append_event, sha256_text
    output = os.path.abspath(opts.output)
    action_id = str(uuid.uuid4())
    append_event(Path(project_root) / "Audit/Ledger/actions.jsonl", {
        "actionId": action_id,
        "correlationId": action_id,
        "actor": os.environ.get("USER", "local-user"),
        "provider": "blender",
        "operation": "clean-generated-asset",
        "authenticationType": "none",
        "status": "completed",
        "approvalRequired": True,
        "approvalStatus": "approved",
        "startTimestampUtc": started.isoformat(),
        "finishTimestampUtc": datetime.now(timezone.utc).isoformat(),
        "durationMilliseconds": report["processingDurationMilliseconds"],
        "costType": "free-local-operation",
        "estimatedCost": 0,
        "actualCost": 0,
        "currency": "USD",
        "filesRead": [os.path.abspath(opts.input)],
        "filesCreated": [output, report_path],
        "commandsExecuted": ["blender --background --python clean_generated_asset.py -- --approved [paths recorded separately]"],
        "resultHash": sha256_text(Path(output).read_bytes().hex()),
        "rawEventFilePath": os.path.relpath(report_path, project_root),
    })


if __name__ == "__main__":
    from mathutils import Vector
    from pathlib import Path
    main()
