import bpy

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath="/Users/thapelopilanyane/Downloads/jumpandroll.fbx")

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
actions = list(bpy.data.actions)

print(f"VERIFY meshes={len(meshes)} armatures={len(armatures)} actions={len(actions)}")
for action in actions:
    print(f"ACTION {action.name} range={tuple(action.frame_range)}")

if not meshes or not armatures or not actions:
    raise RuntimeError("FBX verification failed: mesh, armature, or animation is missing")
