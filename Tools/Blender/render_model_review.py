import bpy
import math
import os
import sys
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
source, output = args[0], args[1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.abspath(source))
meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
centre = (minimum + maximum) * 0.5
size = maximum - minimum

camera_data = bpy.data.cameras.new("ReviewCamera")
camera = bpy.data.objects.new("ReviewCamera", camera_data)
bpy.context.collection.objects.link(camera)
camera.location = centre + Vector((size.x * 1.7, size.y * 0.8, size.z * 2.2))
bpy.context.scene.camera = camera

def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()

look_at(camera, centre)
camera.data.lens = 52

light_data = bpy.data.lights.new("Key", "AREA")
light_data.energy = 1400
light_data.shape = "DISK"
light_data.size = max(size) * 2
light = bpy.data.objects.new("Key", light_data)
bpy.context.collection.objects.link(light)
light.location = centre + Vector((-size.x, size.y * 2, size.z * 1.5))
look_at(light, centre)

world = bpy.context.scene.world
if world is None:
    world = bpy.data.worlds.new("ReviewWorld")
    bpy.context.scene.world = world
world.color = (0.12, 0.12, 0.12)
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 700
scene.render.resolution_y = 700
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = os.path.abspath(output)
scene.render.film_transparent = False
bpy.ops.render.render(write_still=True)
