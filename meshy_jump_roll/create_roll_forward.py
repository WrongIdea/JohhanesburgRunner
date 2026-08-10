import bpy
from pathlib import Path
from mathutils import Quaternion, Vector


ROOT = Path("/Users/thapelopilanyane/My project/meshy_jump_roll")
OUTPUT = ROOT / "output"
BLEND_OUT = OUTPUT / "beaded_warrior_roll_forward.blend"
FBX_OUT = OUTPUT / "Roll_Forward.fbx"

rig = bpy.data.objects.get("Beaded Warrior Rig")
mesh = bpy.data.objects.get("Beaded Warrior")
if not rig or not mesh:
    raise RuntimeError("The existing Beaded Warrior mesh and rig were not found")

source = bpy.data.actions.get("Armature|Run_Jump_and_Roll|baselayer")
if not source:
    raise RuntimeError("The preserved source roll Action was not found")
if bpy.data.actions.get("Roll_Forward"):
    raise RuntimeError("Roll_Forward already exists; refusing to overwrite it")

scene = bpy.context.scene
scene.name = "Roll_Forward"
scene.render.fps = 30
scene.frame_start = 1
scene.frame_end = 30

# Preserve all current clips before assigning a new Action.
existing_action_names = [action.name for action in bpy.data.actions]
for old_action in bpy.data.actions:
    old_action.use_fake_user = True

rig.animation_data_create()
rig.animation_data.action = source

animated_bones = [
    "Hips", "Spine02", "Spine01", "Spine", "neck", "Head",
    "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
    "RightShoulder", "RightArm", "RightForeArm", "RightHand",
    "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
    "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase",
]

def capture_source_pose(source_frame):
    whole_frame = int(source_frame)
    scene.frame_set(whole_frame, subframe=source_frame - whole_frame)
    bpy.context.view_layer.update()
    return {
        name: {
            "location": rig.pose.bones[name].location.copy(),
            "rotation": rig.pose.bones[name].rotation_quaternion.copy(),
        }
        for name in animated_bones
    }


def blend_pose(left, right, factor):
    # Cubic smoothstep gives controlled acceleration without traversing unrelated
    # frames from the original jump portion of the source Action.
    factor = factor * factor * factor * (factor * (factor * 6.0 - 15.0) + 10.0)
    result = {}
    for name in animated_bones:
        left_rotation = left[name]["rotation"].copy()
        right_rotation = right[name]["rotation"].copy()
        right_rotation.make_compatible(left_rotation)
        result[name] = {
            "location": left[name]["location"].lerp(right[name]["location"], factor),
            "rotation": left_rotation.slerp(right_rotation, factor),
        }
    return result


run_pose = capture_source_pose(6.0)
entry_pose = capture_source_pose(45.0)
recovery_pose = capture_source_pose(77.0)
samples = {}
for output_frame in range(1, 31):
    if output_frame <= 5:
        samples[output_frame] = blend_pose(run_pose, entry_pose, (output_frame - 1) / 4.0)
    elif output_frame <= 25:
        source_frame = 45.0 + ((output_frame - 5) / 20.0) * 32.0
        samples[output_frame] = capture_source_pose(source_frame)
    else:
        samples[output_frame] = blend_pose(recovery_pose, run_pose, (output_frame - 25) / 5.0)

# Maintain quaternion continuity through the complete forward rotation.
previous = {}
for frame in sorted(samples):
    for name in animated_bones:
        rotation = samples[frame][name]["rotation"]
        if name in previous:
            rotation.make_compatible(previous[name])
        previous[name] = rotation.copy()

action = bpy.data.actions.new("Roll_Forward")
action.use_fake_user = True
rig.animation_data.action = action

for frame in sorted(samples):
    scene.frame_set(frame)
    for name in animated_bones:
        pb = rig.pose.bones[name]
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = samples[frame][name]["rotation"]
    hips = rig.pose.bones["Hips"]
    hips.location = samples[frame]["Hips"]["location"]
    bpy.context.view_layer.update()

    # Ground and recenter the evaluated character while preserving the sampled pose.
    evaluated = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    points = [evaluated.matrix_world @ vertex.co for vertex in evaluated.data.vertices]
    center_x = (min(p.x for p in points) + max(p.x for p in points)) * 0.5
    center_y = (min(p.y for p in points) + max(p.y for p in points)) * 0.5
    min_z = min(p.z for p in points)
    world_delta = Vector((-center_x, -center_y, -min_z))
    armature_delta = rig.matrix_world.inverted().to_3x3() @ world_delta
    hips_matrix = hips.matrix.copy()
    hips_matrix.translation += armature_delta
    hips.matrix = hips_matrix
    bpy.context.view_layer.update()

    for name in animated_bones:
        rig.pose.bones[name].keyframe_insert("rotation_quaternion", frame=frame, group=name)
    hips.keyframe_insert("location", frame=frame, group="Hips")

try:
    action.frame_start = 1.0
    action.frame_end = 30.0
except Exception:
    pass
if hasattr(action, "asset_mark"):
    action.asset_mark()

# Fast but controlled Bezier arcs with clamped handles—no overshoot at contact poses.
for slot in getattr(action, "slots", []):
    channelbag = action.layers[0].strips[0].channelbag(slot)
    if channelbag:
        for fcurve in channelbag.fcurves:
            for key in fcurve.keyframe_points:
                key.interpolation = "BEZIER"
                key.handle_left_type = "AUTO_CLAMPED"
                key.handle_right_type = "AUTO_CLAMPED"
                if fcurve.data_path.endswith('pose.bones["Hips"].location'):
                    key.interpolation = "LINEAR"

for marker in list(scene.timeline_markers):
    scene.timeline_markers.remove(marker)
scene.timeline_markers.new("Roll_Start", frame=1)
scene.timeline_markers.new("Roll_Collider_Low", frame=9)
scene.timeline_markers.new("Roll_Recovery", frame=22)
scene.timeline_markers.new("Roll_End", frame=30)

# Select only the character pair for a clean Unity export.
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_OUT))

bpy.ops.export_scene.fbx(
    filepath=str(FBX_OUT),
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    use_space_transform=True,
    bake_space_transform=True,
    axis_forward="-Z",
    axis_up="Y",
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    use_armature_deform_only=True,
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True,
    bake_anim_step=1.0,
    bake_anim_simplify_factor=0.0,
    path_mode="COPY",
    embed_textures=True,
)

print("CREATED_ACTION", action.name, tuple(action.frame_range))
print("EXISTING_ACTIONS_PRESERVED", existing_action_names)
print("BLEND", BLEND_OUT)
print("FBX", FBX_OUT)
