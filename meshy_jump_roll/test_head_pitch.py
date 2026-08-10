import bpy
import math
from mathutils import Quaternion, Vector
from pathlib import Path

out = Path("/Users/thapelopilanyane/My project/meshy_jump_roll/output")
scene = bpy.context.scene
rig = bpy.data.objects["Beaded Warrior Rig"]
camera = bpy.data.objects["Action Camera"]
scene.frame_set(16)
bpy.context.view_layer.update()
head = rig.pose.bones["Head"]
neck = rig.pose.bones["neck"]
head_base = head.rotation_quaternion.copy()
neck_base = neck.rotation_quaternion.copy()
camera.location = (4.2, -0.05, 1.0)
camera.rotation_euler = (Vector((0, -0.05, 0.93)) - camera.location).to_track_quat("-Z", "Y").to_euler()
scene.render.resolution_x = 480
scene.render.resolution_y = 480
for angle in (-18, -10, 10, 18):
    head.rotation_quaternion = head_base @ Quaternion((1, 0, 0), math.radians(angle))
    neck.rotation_quaternion = neck_base @ Quaternion((1, 0, 0), math.radians(angle * 0.45))
    bpy.context.view_layer.update()
    scene.render.filepath = str(out / f"head_pitch_{angle:+d}.png")
    bpy.ops.render.render(write_still=True)
