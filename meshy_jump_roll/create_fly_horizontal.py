import bpy
import math
from pathlib import Path
from mathutils import Matrix, Quaternion, Vector


ROOT = Path("/Users/thapelopilanyane/My project/meshy_jump_roll")
OUTPUT = ROOT / "output"
BLEND_OUT = OUTPUT / "beaded_warrior_fly_horizontal.blend"
FBX_OUT = OUTPUT / "Fly_Horizontal_Loop.fbx"

rig = bpy.data.objects.get("Beaded Warrior Rig")
mesh = bpy.data.objects.get("Beaded Warrior")
camera = bpy.data.objects.get("Action Camera")
if not rig or not mesh:
    raise RuntimeError("The existing Beaded Warrior mesh and rig were not found")

scene = bpy.context.scene
scene.render.fps = 30
scene.frame_start = 1
scene.frame_end = 61

# Preserve every existing animation before switching the active action.
existing_action_names = [action.name for action in bpy.data.actions]
for old_action in bpy.data.actions:
    old_action.use_fake_user = True

if bpy.data.actions.get("Fly_Horizontal_Loop"):
    raise RuntimeError("Fly_Horizontal_Loop already exists; refusing to overwrite it")

action = bpy.data.actions.new("Fly_Horizontal_Loop")
action.use_fake_user = True
rig.animation_data_create()
rig.animation_data.action = action

# Clear inherited animation pose without changing the rest rig or mesh data.
for pb in rig.pose.bones:
    pb.rotation_mode = "QUATERNION"
    pb.location = (0.0, 0.0, 0.0)
    pb.rotation_quaternion = Quaternion()
    pb.scale = (1.0, 1.0, 1.0)
scene.frame_set(1)
bpy.context.view_layer.update()


def aim_bone(name, direction):
    """Aim a pose bone in armature space while preserving its head and bone roll."""
    pb = rig.pose.bones[name]
    bpy.context.view_layer.update()
    head = pb.head.copy()
    current = (pb.tail - pb.head).normalized()
    target = Vector(direction).normalized()
    delta = current.rotation_difference(target)
    pb.matrix = Matrix.Translation(head) @ delta.to_matrix().to_4x4() @ Matrix.Translation(-head) @ pb.matrix
    bpy.context.view_layer.update()


# Base aerodynamic pose. An 86-degree pitch keeps the pelvis and ribcage nearly
# horizontal without making the waist look mechanically locked.
hips = rig.pose.bones["Hips"]
hips.rotation_quaternion = Quaternion((1.0, 0.0, 0.0), math.radians(86.0))
bpy.context.view_layer.update()

# One continuous pelvis-to-chest line with a small, natural thoracic lift.
aim_bone("Spine02", (0.0, -1.0, 0.075))
aim_bone("Spine01", (0.0, -1.0, 0.095))
aim_bone("Spine", (0.0, -1.0, 0.115))
aim_bone("neck", (0.0, -0.985, 0.17))
aim_bone("Head", (0.0, -0.975, 0.22))
# Pitch the face into the airflow while keeping a small protective chin tuck.
rig.pose.bones["neck"].rotation_quaternion = rig.pose.bones["neck"].rotation_quaternion @ Quaternion((1, 0, 0), math.radians(-4.0))
rig.pose.bones["Head"].rotation_quaternion = rig.pose.bones["Head"].rotation_quaternion @ Quaternion((1, 0, 0), math.radians(-9.0))
bpy.context.view_layer.update()

# Clavicles open naturally; arms reach forward with soft elbow bends.
aim_bone("LeftShoulder", (1.0, 0.12, 0.03))
aim_bone("RightShoulder", (-1.0, 0.09, 0.015))
aim_bone("LeftArm", (0.24, -1.0, -0.035))
aim_bone("RightArm", (-0.29, -1.0, 0.005))
aim_bone("LeftForeArm", (0.10, -1.0, 0.055))
aim_bone("RightForeArm", (-0.13, -1.0, 0.025))
aim_bone("LeftHand", (0.05, -1.0, 0.015))
aim_bone("RightHand", (-0.07, -1.0, -0.005))

# Legs trail as a continuation of the pelvis. The knees remain only softly bent
# and the feet follow the shin line instead of curling upward.
aim_bone("LeftUpLeg", (0.065, 1.0, -0.015))
aim_bone("RightUpLeg", (-0.072, 1.0, -0.005))
aim_bone("LeftLeg", (0.030, 1.0, -0.030))
aim_bone("RightLeg", (-0.034, 1.0, -0.020))
aim_bone("LeftFoot", (0.018, 1.0, -0.025))
aim_bone("RightFoot", (-0.020, 1.0, -0.018))
aim_bone("LeftToeBase", (0.008, 1.0, -0.010))
aim_bone("RightToeBase", (-0.008, 1.0, -0.008))

animated_bones = [
    "Hips", "Spine02", "Spine01", "Spine", "neck", "Head",
    "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
    "RightShoulder", "RightArm", "RightForeArm", "RightHand",
    "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
    "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase",
]
base_rotations = {name: rig.pose.bones[name].rotation_quaternion.copy() for name in animated_bones}


def local_delta(name, xyz_degrees):
    pb = rig.pose.bones[name]
    delta = Quaternion()
    for axis, degrees in zip(((1, 0, 0), (0, 1, 0), (0, 0, 1)), xyz_degrees):
        delta = delta @ Quaternion(axis, math.radians(degrees))
    pb.rotation_quaternion = base_rotations[name] @ delta


def key_pose(frame, phase):
    # Periodic secondary motion; phase 0 and 2*pi are exactly identical.
    breath = math.sin(phase)
    secondary = math.sin(phase + math.pi / 2.0)
    bank = math.sin(phase) * 2.5

    for name in animated_bones:
        rig.pose.bones[name].rotation_quaternion = base_rotations[name].copy()

    # Root rotates only for a subtle bank. Translation remains exactly zero.
    local_delta("Hips", (0.0, bank, 0.0))
    rig.pose.bones["Hips"].location = (0.0, 0.0, 0.0)

    # Breathing and aerodynamic stabilisation.
    local_delta("Spine02", (0.30 * breath, 0.28 * bank, 0.0))
    local_delta("Spine01", (-0.35 * breath, 0.14 * bank, 0.0))
    local_delta("Spine", (0.55 * breath, -0.12 * bank, 0.0))
    local_delta("neck", (-0.45 * breath, -0.25 * bank, 0.15 * secondary))
    local_delta("Head", (0.3 * breath, -0.3 * bank, -0.12 * secondary))

    # Airflow drift with deliberate left/right asymmetry.
    local_delta("LeftShoulder", (0.15 * breath, 0.35 * secondary, 0.4 * bank))
    local_delta("RightShoulder", (-0.12 * breath, -0.28 * secondary, 0.32 * bank))
    local_delta("LeftArm", (0.65 * secondary, 0.18 * breath, 0.30 * bank))
    local_delta("RightArm", (-0.48 * secondary, -0.15 * breath, 0.24 * bank))
    local_delta("LeftForeArm", (-0.55 * secondary, 0.0, -0.12 * bank))
    local_delta("RightForeArm", (0.42 * secondary, 0.0, -0.10 * bank))
    local_delta("LeftHand", (0.20 * breath, 0.25 * secondary, 0.0))
    local_delta("RightHand", (-0.16 * breath, -0.20 * secondary, 0.0))

    # Small trailing-leg reaction; no crossing and no extreme knees.
    local_delta("LeftUpLeg", (0.20 * secondary, 0.06 * breath, 0.12 * bank))
    local_delta("RightUpLeg", (-0.16 * secondary, -0.05 * breath, 0.10 * bank))
    local_delta("LeftLeg", (-0.24 * secondary, 0.0, -0.06 * bank))
    local_delta("RightLeg", (0.20 * secondary, 0.0, -0.05 * bank))
    local_delta("LeftFoot", (0.16 * breath, 0.07 * secondary, 0.0))
    local_delta("RightFoot", (-0.13 * breath, -0.06 * secondary, 0.0))

    # Key only pose rotations plus the constant root location. Never key bone scale.
    for name in animated_bones:
        rig.pose.bones[name].keyframe_insert("rotation_quaternion", frame=frame, group=name)
    rig.pose.bones["Hips"].keyframe_insert("location", frame=frame, group="Hips")


for frame, phase in [(1, 0.0), (16, math.pi / 2), (31, math.pi), (46, 3 * math.pi / 2), (61, 2 * math.pi)]:
    key_pose(frame, phase)

# Set exact action metadata where supported.
try:
    action.frame_start = 1.0
    action.frame_end = 61.0
except Exception:
    pass
if hasattr(action, "asset_mark"):
    action.asset_mark()

# Smooth automatic Bezier handles for legacy and layered Actions.
for slot in getattr(action, "slots", []):
    channelbag = action.layers[0].strips[0].channelbag(slot)
    if channelbag:
        for fcurve in channelbag.fcurves:
            for key in fcurve.keyframe_points:
                key.interpolation = "BEZIER"
                key.handle_left_type = "AUTO_CLAMPED"
                key.handle_right_type = "AUTO_CLAMPED"

# Timeline labels make the loop easy to inspect.
for marker in list(scene.timeline_markers):
    scene.timeline_markers.remove(marker)
scene.timeline_markers.new("FLY LOOP START", frame=1)
scene.timeline_markers.new("SUBTLE BANK", frame=16)
scene.timeline_markers.new("NEUTRAL MIDPOINT", frame=31)
scene.timeline_markers.new("COUNTER MOTION", frame=46)
scene.timeline_markers.new("FLY LOOP END", frame=61)

# Keep the armature root object untouched and select only the export pair.
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig

scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_OUT))

# Unity-ready FBX: same mesh/armature, current action only, no leaf bones.
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
print("ANIMATED_BONES", animated_bones)
print("BLEND", BLEND_OUT)
print("FBX", FBX_OUT)
