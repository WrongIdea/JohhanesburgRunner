import bpy
import math
from pathlib import Path
from mathutils import Vector


ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "source/Meshy_AI_Beaded_Warrior_biped/Meshy_AI_Beaded_Warrior_biped_Animation_Run_Jump_and_Roll_withSkin.glb"
OUTPUT = ROOT / "output"
FRAMES = OUTPUT / "frames"
OUTPUT.mkdir(parents=True, exist_ok=True)
FRAMES.mkdir(parents=True, exist_ok=True)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def make_material(name, color, metallic=0.0, roughness=0.5):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


def add_area(name, location, energy, color, size, target=(0, -2.4, 0.9)):
    light_data = bpy.data.lights.new(name, "AREA")
    light_data.energy = energy
    light_data.color = color
    light_data.shape = "DISK"
    light_data.size = size
    light = bpy.data.objects.new(name, light_data)
    bpy.context.collection.objects.link(light)
    light.location = location
    look_at(light, target)
    return light


# Import the rigged Meshy character and its embedded action.
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))

armature = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
armature.name = "Beaded Warrior Rig"
character = bpy.data.objects.get("char1")
character.name = "Beaded Warrior"

# Meshy includes an untextured helper icosphere in this export; it is not part of the character.
helper = bpy.data.objects.get("Icosphere")
if helper:
    bpy.data.objects.remove(helper, do_unlink=True)

# Improve surface shading without changing the imported texture or rig.
for polygon in character.data.polygons:
    polygon.use_smooth = True

# Animation range: short anticipation, vertical jump, hard landing, forward roll, recovery.
scene = bpy.context.scene
scene.frame_start = 18
scene.frame_end = 83
scene.render.fps = 24
scene.timeline_markers.new("TAKEOFF", frame=21)
scene.timeline_markers.new("JUMP PEAK", frame=36)
scene.timeline_markers.new("IMPACT", frame=46)
scene.timeline_markers.new("FORWARD ROLL", frame=53)
scene.timeline_markers.new("RECOVERY", frame=76)

# Ground at the character's contact height.
bpy.ops.mesh.primitive_plane_add(size=24, location=(0, -2.4, -0.065))
ground = bpy.context.object
ground.name = "Ground"
ground.data.materials.append(make_material("Ground Material", (0.035, 0.045, 0.065), metallic=0.08, roughness=0.3))

# A subtle landing target helps sell the impact and direction of travel.
bpy.ops.mesh.primitive_torus_add(major_radius=0.72, minor_radius=0.012, major_segments=96, minor_segments=8, location=(0, -2.65, -0.045))
ring = bpy.context.object
ring.name = "Impact Ring"
ring.data.materials.append(make_material("Impact Accent", (0.78, 0.38, 0.08), metallic=0.15, roughness=0.24))

# Three-point lighting.
add_area("Warm Key", (4.2, 1.0, 6.5), 1150, (1.0, 0.66, 0.42), 4.0)
add_area("Cool Fill", (-3.8, -4.5, 4.0), 850, (0.36, 0.58, 1.0), 4.0)
add_area("Rim Light", (1.5, -7.0, 6.0), 1250, (0.74, 0.38, 1.0), 3.0)

# Static side/three-quarter camera showing the entire forward trajectory.
camera_data = bpy.data.cameras.new("Action Camera")
camera = bpy.data.objects.new("Action Camera", camera_data)
bpy.context.collection.objects.link(camera)
camera.location = (7.3, -2.5, 3.2)
camera.data.lens = 58
look_at(camera, (0, -2.5, 0.9))
scene.camera = camera

# Render configuration compatible with this Blender 5.2 installation.
world = scene.world or bpy.data.worlds.new("World")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.006, 0.01, 0.025, 1.0)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.18
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 960
scene.render.resolution_y = 540
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = str(FRAMES / "jump_roll_")
scene.render.film_transparent = False
scene.view_settings.look = "AgX - Medium High Contrast"

# Save the editable project and representative preview frames.
blend_path = OUTPUT / "beaded_warrior_jump_roll.blend"
scene.frame_set(scene.frame_start)
bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
for frame, name in [(36, "preview_jump.png"), (46, "preview_impact.png"), (61, "preview_roll.png")]:
    scene.frame_set(frame)
    scene.render.filepath = str(OUTPUT / name)
    bpy.ops.render.render(write_still=True)

scene.render.filepath = str(FRAMES / "jump_roll_")
scene.frame_set(scene.frame_start)
bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
print(f"Created {blend_path}")
