import bpy


bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath="/Users/thapelopilanyane/My project/jozi_runner/output/Jozi_Runner_CBD_Environment.fbx")

objects = list(bpy.context.scene.objects)
meshes = [obj for obj in objects if obj.type == "MESH"]
empties = [obj for obj in objects if obj.type == "EMPTY"]
road_modules = [obj for obj in objects if obj.name.startswith("Road_Module_")]
spawn_points = [obj for obj in objects if obj.name.startswith("UNITY_")]

print("ENV_VERIFY", {
    "objects": len(objects),
    "meshes": len(meshes),
    "empties": len(empties),
    "road_modules": len(road_modules),
    "unity_markers": len(spawn_points),
})

if len(road_modules) != 6 or not spawn_points:
    raise RuntimeError("Environment FBX verification failed")
