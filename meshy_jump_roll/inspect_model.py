import bpy
from mathutils import Vector

path = "/Users/thapelopilanyane/My project/meshy_jump_roll/source/Meshy_AI_Beaded_Warrior_biped/Meshy_AI_Beaded_Warrior_biped_Animation_Run_Jump_and_Roll_withSkin.glb"
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=path)

print("OBJECTS")
for obj in bpy.context.scene.objects:
    print(obj.name, obj.type, tuple(round(v, 3) for v in obj.dimensions), tuple(round(v, 3) for v in obj.location), "parent", obj.parent.name if obj.parent else None, "materials", [m.name for m in getattr(obj.data, "materials", [])])
print("ACTIONS")
for action in bpy.data.actions:
    print(action.name, tuple(action.frame_range), action.frame_start, action.frame_end)
print("SCENE", bpy.context.scene.frame_start, bpy.context.scene.frame_end, bpy.context.scene.render.fps)
mesh = bpy.data.objects.get("char1")
depsgraph = bpy.context.evaluated_depsgraph_get()
for frame in range(1, 84, 5):
    bpy.context.scene.frame_set(frame)
    evaluated = mesh.evaluated_get(depsgraph)
    corners = [evaluated.matrix_world @ Vector(corner) for corner in evaluated.bound_box]
    xs, ys, zs = zip(*corners)
    print("BOUNDS", frame, round(min(xs), 3), round(max(xs), 3), round(min(ys), 3), round(max(ys), 3), round(min(zs), 3), round(max(zs), 3))
