import sys
from pathlib import Path

import bpy
from mathutils import Vector

render_args = sys.argv[sys.argv.index("--") + 1:]
source, output = render_args[:2]
view_axis = render_args[2] if len(render_args) > 2 else "Z"
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=source)

objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
low = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
high = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
centre = (low + high) * 0.5

camera_data = bpy.data.cameras.new("PreviewCamera")
camera = bpy.data.objects.new("PreviewCamera", camera_data)
bpy.context.scene.collection.objects.link(camera)
span = max(high.x - low.x, high.y - low.y, high.z - low.z)
if view_axis.upper() == "Y":
    camera.location = (centre.x, low.y - span * 1.8, centre.z)
elif view_axis.upper() == "X":
    camera.location = (low.x - span * 1.8, centre.y, centre.z)
else:
    camera.location = (centre.x, centre.y, high.z + span * 1.8)
direction = centre - camera.location
camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
camera_data.type = "ORTHO"
visible_height = (high.z - low.z) if view_axis.upper() in ("X", "Y") else (high.y - low.y)
visible_width = (high.y - low.y) if view_axis.upper() == "X" else (high.x - low.x)
camera_data.ortho_scale = max(visible_width, visible_height) * 1.15
bpy.context.scene.camera = camera

world = bpy.context.scene.world
world.color = (0.08, 0.08, 0.08)
for location, energy, size in [
    ((-8, 14, 12), 1800, 8),
    ((10, 8, 10), 1200, 6),
]:
    light_data = bpy.data.lights.new("Area", "AREA")
    light_data.energy = energy
    light_data.shape = "DISK"
    light_data.size = size
    light = bpy.data.objects.new("Area", light_data)
    light.location = location
    bpy.context.scene.collection.objects.link(light)
    light.rotation_euler = (centre - light.location).to_track_quat("-Z", "Y").to_euler()

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 900
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.render.filepath = output
scene.view_settings.look = "AgX - Medium High Contrast"
Path(output).parent.mkdir(parents=True, exist_ok=True)
bpy.ops.render.render(write_still=True)
