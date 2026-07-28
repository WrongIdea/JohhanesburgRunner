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
action = bpy.data.actions["Roll_Forward"]
rig.animation_data.action = action


def pose_snapshot(frame):
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    return {name: [list(row) for row in pb.matrix] for name, pb in rig.pose.bones.items()}


first = pose_snapshot(1)
last = pose_snapshot(30)
start_end_error = max(
    abs(first[name][r][c] - last[name][r][c])
    for name in first for r in range(4) for c in range(4)
)

root_xy = []
root_z = []
character_centers_xy = []
heights = {}
min_zs = {}
frame_bounds = {}
finite_mesh = True
for frame in range(1, 31):
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    loc = rig.pose.bones["Hips"].location.copy()
    root_xy.append(Vector((loc.x, loc.y)))
    root_z.append(loc.z)
    evaluated = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    points = [evaluated.matrix_world @ vertex.co for vertex in evaluated.data.vertices]
    finite_mesh = finite_mesh and all(math.isfinite(v) for point in points for v in point)
    zs = [p.z for p in points]
    xs = [p.x for p in points]
    ys = [p.y for p in points]
    character_centers_xy.append(Vector(((min(xs) + max(xs)) * 0.5, (min(ys) + max(ys)) * 0.5)))
    heights[frame] = max(zs) - min(zs)
    min_zs[frame] = min(zs)
    if frame in (1, 5, 8, 10, 14, 18, 22, 25, 27, 30):
        frame_bounds[frame] = [min(xs), max(xs), min(ys), max(ys), min(zs), max(zs)]

low_frames = [frame for frame, height in heights.items() if height <= 1.05]
low_groups = []
for frame in low_frames:
    if not low_groups or frame != low_groups[-1][-1] + 1:
        low_groups.append([frame])
    else:
        low_groups[-1].append(frame)
main_low_group = max(low_groups, key=len) if low_groups else []
low_interval = [main_low_group[0], main_low_group[-1]] if main_low_group else []


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


scene.render.resolution_x = 720
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.frame_set(14)
target = (0.0, 0.0, 0.62)
views = {
    "roll_side.png": (4.2, 0.0, 1.25),
    "roll_front.png": (0.0, -4.2, 1.25),
    "roll_rear.png": (0.0, 4.2, 1.65),
    "roll_perspective.png": (3.3, -3.6, 2.25),
}
camera.data.lens = 58
for filename, position in views.items():
    camera.location = position
    look_at(camera, target)
    scene.render.filepath = str(OUTPUT / filename)
    bpy.ops.render.render(write_still=True)

# Also inspect recovery silhouette.
scene.frame_set(25)
camera.location = views["roll_perspective.png"]
look_at(camera, (0.0, 0.0, 0.85))
scene.render.filepath = str(OUTPUT / "roll_recovery.png")
bpy.ops.render.render(write_still=True)

scene.frame_set(1)
scene.render.filepath = str(OUTPUT / "roll_preview_")
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT / "beaded_warrior_roll_forward.blend"))

report = {
    "action": action.name,
    "frame_range": list(action.frame_range),
    "fps": scene.render.fps,
    "duration_seconds": 30 / scene.render.fps,
    "actions_present": [a.name for a in bpy.data.actions],
    "start_end_pose_error": start_end_error,
    "max_visual_horizontal_drift": max(v.length for v in character_centers_xy),
    "hips_local_xy_channel_range": [
        [min(v.x for v in root_xy), max(v.x for v in root_xy)],
        [min(v.y for v in root_xy), max(v.y for v in root_xy)],
    ],
    "root_vertical_range": [min(root_z), max(root_z)],
    "lowest_collider_interval": low_interval,
    "minimum_character_height": min(heights.values()),
    "lowest_mesh_point": min(min_zs.values()),
    "finite_mesh_vertices_all_frames": finite_mesh,
    "key_pose_bounds": frame_bounds,
    "armature_object_transform": {
        "location": list(rig.location),
        "rotation": list(rig.rotation_euler),
        "scale": list(rig.scale),
    },
    "constraints": {
        "object": len(rig.constraints),
        "pose_bones": sum(len(pb.constraints) for pb in rig.pose.bones),
    },
}
(OUTPUT / "roll_validation.json").write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))

if report["max_visual_horizontal_drift"] > 1e-5 or report["lowest_mesh_point"] < -1e-4 or not finite_mesh:
    raise RuntimeError("Roll validation failed")
