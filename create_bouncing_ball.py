import bpy
import math
from pathlib import Path
from mathutils import Vector


OUTPUT_DIR = Path(__file__).resolve().parent / "blender_animation"
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
(OUTPUT_DIR / "frames").mkdir(parents=True, exist_ok=True)


def material(name, color, metallic=0.0, roughness=0.45):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def keyframe(obj, frame, location, scale):
    obj.location = location
    obj.scale = scale
    obj.keyframe_insert("location", frame=frame)
    obj.keyframe_insert("scale", frame=frame)


# Clean the default scene.
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.materials, bpy.data.curves, bpy.data.cameras, bpy.data.lights):
    pass

# Ground and curved-looking studio backdrop.
bpy.ops.mesh.primitive_plane_add(size=30, location=(0, 0, 0))
ground = bpy.context.object
ground.name = "Studio Floor"
ground.data.materials.append(material("Warm Floor", (0.055, 0.065, 0.085), metallic=0.05, roughness=0.28))

# Ball.
bpy.ops.mesh.primitive_uv_sphere_add(segments=64, ring_count=32, location=(-4.3, 0, 1.0))
ball = bpy.context.object
ball.name = "AI Assisted Bouncing Ball"
ball.data.materials.append(material("Coral Ball", (0.95, 0.12, 0.07), metallic=0.15, roughness=0.24))
bpy.ops.object.shade_smooth()

# Animation poses: anticipation, impacts, squash and stretch, then settle.
poses = [
    (1,   (-4.3, 0, 1.0), (1.00, 1.00, 1.00)),
    (8,   (-4.3, 0, 0.72), (1.18, 1.18, 0.72)),
    (12,  (-4.0, 0, 1.35), (0.86, 0.86, 1.22)),
    (28,  (-1.9, 0, 5.2), (0.94, 0.94, 1.08)),
    (44,  (0.0, 0, 0.72), (1.28, 1.28, 0.66)),
    (48,  (0.4, 0, 1.25), (0.88, 0.88, 1.18)),
    (62,  (1.7, 0, 3.55), (0.96, 0.96, 1.05)),
    (76,  (2.8, 0, 0.72), (1.20, 1.20, 0.74)),
    (80,  (3.05, 0, 1.15), (0.91, 0.91, 1.13)),
    (91,  (3.65, 0, 2.25), (0.98, 0.98, 1.03)),
    (102, (4.15, 0, 0.72), (1.12, 1.12, 0.82)),
    (106, (4.25, 0, 1.05), (0.96, 0.96, 1.06)),
    (113, (4.45, 0, 1.45), (0.99, 0.99, 1.02)),
    (120, (4.6, 0, 1.0), (1.00, 1.00, 1.00)),
]
for pose in poses:
    keyframe(ball, *pose)

# Blender uses Bezier interpolation by default. Closely spaced impact poses sharpen
# the contacts while remaining compatible with Blender 5.x's layered Actions API.

# Area lights.
def add_area(name, location, energy, color, size):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.color = color
    data.shape = "DISK"
    data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    look_at(obj, (0, 0, 1.5))
    return obj


add_area("Key Light", (-4, -4, 8), 1250, (1.0, 0.72, 0.55), 5.0)
add_area("Fill Light", (5, -1, 5), 900, (0.45, 0.65, 1.0), 4.0)
add_area("Rim Light", (2, 5, 7), 1100, (0.75, 0.35, 1.0), 3.0)

# Camera with a gentle push-in.
camera_data = bpy.data.cameras.new("Camera")
camera = bpy.data.objects.new("Camera", camera_data)
bpy.context.collection.objects.link(camera)
bpy.context.scene.camera = camera
camera.data.lens = 46
camera.location = (10.8, -18.5, 8.2)
look_at(camera, (0.3, 0, 1.9))
camera.keyframe_insert("location", frame=1)
camera.location = (10.0, -17.2, 7.7)
look_at(camera, (0.6, 0, 1.75))
camera.keyframe_insert("location", frame=120)
camera.keyframe_insert("rotation_euler", frame=1)
camera.keyframe_insert("rotation_euler", frame=120)

# World and render settings.
world = bpy.context.scene.world or bpy.data.worlds.new("World")
bpy.context.scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.012, 0.018, 0.035, 1.0)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.22

scene = bpy.context.scene
scene.frame_start = 1
scene.frame_end = 120
scene.render.fps = 24
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = str(OUTPUT_DIR / "frames" / "frame_")
scene.render.film_transparent = False

# Color management.
scene.view_settings.look = "AgX - Medium High Contrast"

# Save the finished project and a preview still.
scene.frame_set(28)
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_DIR / "bouncing_ball.blend"))
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = str(OUTPUT_DIR / "preview.png")
bpy.ops.render.render(write_still=True)
scene.render.filepath = str(OUTPUT_DIR / "frames" / "frame_")
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_DIR / "bouncing_ball.blend"))

print(f"Created: {OUTPUT_DIR / 'bouncing_ball.blend'}")
print(f"Preview: {OUTPUT_DIR / 'preview.png'}")
print("Open the .blend file and choose Render > Render Animation to create PNG frames.")
