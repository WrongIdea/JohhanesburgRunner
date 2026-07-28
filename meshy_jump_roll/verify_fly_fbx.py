import bpy
from mathutils import Vector


bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath="/Users/thapelopilanyane/My project/meshy_jump_roll/output/Fly_Horizontal_Loop.fbx")

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
rigs = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
actions = list(bpy.data.actions)
if len(meshes) != 1 or len(rigs) != 1 or len(actions) != 1:
    raise RuntimeError(f"Unexpected FBX content: meshes={len(meshes)} rigs={len(rigs)} actions={len(actions)}")

rig = rigs[0]
scene = bpy.context.scene
locations = []
for frame in (1, 16, 31, 46, 61):
    scene.frame_set(frame)
    locations.append(Vector(rig.pose.bones["Hips"].location))

print("FBX_VERIFY", {
    "mesh": meshes[0].name,
    "armature": rig.name,
    "action": actions[0].name,
    "range": tuple(actions[0].frame_range),
    "bones": len(rig.data.bones),
    "max_sampled_root_drift": max(v.length for v in locations),
})
