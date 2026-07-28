import bpy
from mathutils import Vector


bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath="/Users/thapelopilanyane/My project/meshy_jump_roll/output/Roll_Forward.fbx")

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
rigs = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
actions = list(bpy.data.actions)
if len(meshes) != 1 or len(rigs) != 1 or len(actions) != 1:
    raise RuntimeError(f"Unexpected FBX content: meshes={len(meshes)} rigs={len(rigs)} actions={len(actions)}")

print("FBX_VERIFY", {
    "mesh": meshes[0].name,
    "armature": rigs[0].name,
    "action": actions[0].name,
    "range": tuple(actions[0].frame_range),
    "bones": len(rigs[0].data.bones),
})
