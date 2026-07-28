import bpy
import json
import math
from pathlib import Path
from mathutils import Vector


OUTPUT = Path("/Users/thapelopilanyane/My project/meshy_jump_roll/output")
rig = bpy.data.objects["Beaded Warrior Rig"]
mesh = bpy.data.objects["Beaded Warrior"]
camera = bpy.data.objects["Action Camera"]
scene = bpy.context.scene
action = bpy.data.actions["Fly_Horizontal_Loop"]
rig.animation_data.action = action


def snapshot(frame):
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    return {
        name: [list(row) for row in rig.pose.bones[name].matrix]
        for name in rig.pose.bones.keys()
    }


first = snapshot(1)
last = snapshot(61)
max_loop_error = max(
    abs(first[name][r][c] - last[name][r][c])
    for name in first for r in range(4) for c in range(4)
)

root_locations = []
finite_mesh = True
heights = []
for frame in range(1, 62):
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    root_locations.append(tuple(rig.pose.bones["Hips"].location))
    evaluated = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    points = [evaluated.matrix_world @ vertex.co for vertex in evaluated.data.vertices]
    finite_mesh = finite_mesh and all(math.isfinite(v) for point in points for v in point)
    heights.append((min(p.z for p in points), max(p.z for p in points)))

max_root_drift = max(Vector(loc).length for loc in root_locations)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


scene.render.resolution_x = 720
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.frame_set(16)
target = (0.0, -0.05, 0.93)
views = {
    "fly_side.png": (4.2, -0.05, 1.0),
    "fly_front.png": (0.0, -4.2, 1.0),
    "fly_rear.png": (0.0, 4.2, 1.0),
    "fly_perspective.png": (3.2, -4.0, 2.35),
}
camera.data.lens = 58
for filename, position in views.items():
    camera.location = position
    look_at(camera, target)
    scene.render.filepath = str(OUTPUT / filename)
    bpy.ops.render.render(write_still=True)

# Leave the project in the useful perspective inspection view.
camera.location = views["fly_perspective.png"]
look_at(camera, target)
scene.frame_set(1)
scene.render.filepath = str(OUTPUT / "fly_preview_")
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT / "beaded_warrior_fly_horizontal.blend"))

report = {
    "action": action.name,
    "frame_range": list(action.frame_range),
    "fps": scene.render.fps,
    "actions_present": [a.name for a in bpy.data.actions],
    "loop_max_matrix_error": max_loop_error,
    "max_root_location_drift": max_root_drift,
    "armature_object_location": list(rig.location),
    "armature_object_rotation": list(rig.rotation_euler),
    "armature_object_scale": list(rig.scale),
    "finite_mesh_vertices_all_frames": finite_mesh,
    "mesh_z_range": [min(v[0] for v in heights), max(v[1] for v in heights)],
    "constraints": {
        "object": len(rig.constraints),
        "pose_bones": sum(len(pb.constraints) for pb in rig.pose.bones),
    },
}
(OUTPUT / "fly_validation.json").write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))

if max_loop_error > 1e-5 or max_root_drift > 1e-7 or not finite_mesh:
    raise RuntimeError("Flight loop validation failed")
