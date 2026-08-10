import bpy
import math
import random
from pathlib import Path
from mathutils import Vector


random.seed(2701)
ROOT = Path("/Users/thapelopilanyane/My project/jozi_runner")
OUTPUT = ROOT / "output"
OUTPUT.mkdir(parents=True, exist_ok=True)


def clean_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.name != "Collection":
            bpy.data.collections.remove(collection)


def collection(name):
    col = bpy.data.collections.get(name) or bpy.data.collections.new(name)
    if col.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(col)
    return col


def move_to_collection(obj, col):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    col.objects.link(obj)


def mat(name, color, metallic=0.0, roughness=0.5, emission=None, emission_strength=0.0):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    material.diffuse_color = (*color, 1.0)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    return material


def cube(name, location, scale, material, col, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (scale[0] / 2, scale[1] / 2, scale[2] / 2)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if material:
        obj.data.materials.append(material)
    if bevel:
        modifier = obj.modifiers.new("Edge Softening", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    move_to_collection(obj, col)
    return obj


def cylinder(name, location, radius, depth, material, col, vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    if material:
        obj.data.materials.append(material)
    move_to_collection(obj, col)
    return obj


def torus(name, location, major, minor, material, col, rotation=(math.pi / 2, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=24, minor_segments=8, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    move_to_collection(obj, col)
    return obj


def empty(name, location, col, display="CUBE", size=0.75):
    obj = bpy.data.objects.new(name, None)
    obj.location = location
    obj.empty_display_type = display
    obj.empty_display_size = size
    col.objects.link(obj)
    return obj


def text_mesh(body, name, location, rotation, size, material, col, align="CENTER"):
    curve = bpy.data.curves.new(name + "_Curve", "FONT")
    curve.body = body
    curve.align_x = align
    curve.size = size
    curve.extrude = 0.035
    curve.bevel_depth = 0.008
    obj = bpy.data.objects.new(name, curve)
    col.objects.link(obj)
    obj.location = location
    obj.rotation_euler = rotation
    obj.data.materials.append(material)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj.select_set(False)
    return obj


clean_scene()
scene = bpy.context.scene
geometry = collection("ENV_Geometry")
buildings_col = collection("ENV_Buildings")
props = collection("ENV_Props")
landmarks = collection("ENV_Landmarks")
spawn = collection("UNITY_SpawnPoints")

# Palette inspired by Johannesburg's concrete, gold-mining heritage, taxis and dusk light.
road_mat = mat("Road Asphalt", (0.025, 0.032, 0.043), metallic=0.05, roughness=0.72)
sidewalk_mat = mat("Concrete Pavement", (0.24, 0.255, 0.27), roughness=0.82)
curb_mat = mat("Jozi Curb", (0.82, 0.72, 0.18), roughness=0.6)
white = mat("Road White", (0.88, 0.88, 0.82), roughness=0.5)
yellow = mat("Jozi Gold", (0.95, 0.58, 0.045), metallic=0.18, roughness=0.32, emission=(1.0, 0.28, 0.02), emission_strength=0.25)
green = mat("South Africa Green", (0.01, 0.32, 0.15), roughness=0.4)
red = mat("Signal Red", (0.55, 0.015, 0.01), emission=(1.0, 0.01, 0.0), emission_strength=4.0)
amber = mat("Signal Amber", (0.65, 0.20, 0.005), emission=(1.0, 0.22, 0.0), emission_strength=3.5)
window_lit = mat("Warm Windows", (0.15, 0.09, 0.025), metallic=0.05, roughness=0.28, emission=(1.0, 0.35, 0.08), emission_strength=2.2)
window_dark = mat("Dark Windows", (0.012, 0.03, 0.055), metallic=0.45, roughness=0.18)
taxi_white = mat("Taxi White", (0.74, 0.78, 0.75), metallic=0.15, roughness=0.32)
rubber = mat("Tyre", (0.008, 0.008, 0.01), roughness=0.8)
barrier_mat = mat("Barrier Orange", (0.95, 0.18, 0.015), roughness=0.45)

# Six repeatable 20 m runner modules, each with consistent connection points.
for module_index in range(6):
    y = -10.0 - module_index * 20.0
    cube(f"Road_Module_{module_index:02d}", (0, y, -0.11), (12, 20, 0.2), road_mat, geometry)
    cube(f"Sidewalk_L_{module_index:02d}", (-7.2, y, 0.02), (2.4, 20, 0.28), sidewalk_mat, geometry)
    cube(f"Sidewalk_R_{module_index:02d}", (7.2, y, 0.02), (2.4, 20, 0.28), sidewalk_mat, geometry)
    cube(f"Curb_L_{module_index:02d}", (-6.05, y, 0.12), (0.18, 20, 0.25), curb_mat, geometry)
    cube(f"Curb_R_{module_index:02d}", (6.05, y, 0.12), (0.18, 20, 0.25), curb_mat, geometry)
    for lane_x in (-2.0, 2.0):
        for dash in range(4):
            cube(f"Lane_{module_index}_{lane_x}_{dash}", (lane_x, y - 7.5 + dash * 5.0, 0.015), (0.10, 2.4, 0.025), white, geometry)
    empty(f"UNITY_RoadSocket_In_{module_index:02d}", (0, y + 10, 0), spawn, "ARROWS", 1.0)
    empty(f"UNITY_RoadSocket_Out_{module_index:02d}", (0, y - 10, 0), spawn, "ARROWS", 1.0)

# City blocks with inward-facing window bands.
building_palette = [
    mat("Concrete Sand", (0.34, 0.29, 0.23), roughness=0.75),
    mat("Concrete Grey", (0.20, 0.23, 0.27), roughness=0.7),
    mat("Brick Brown", (0.27, 0.12, 0.07), roughness=0.78),
    mat("CBD Blue", (0.08, 0.15, 0.23), metallic=0.18, roughness=0.35),
    mat("CBD Cream", (0.43, 0.39, 0.30), roughness=0.65),
]

for side in (-1, 1):
    for index, y in enumerate(range(5, -120, -10)):
        width = random.uniform(7.0, 11.0)
        depth = random.uniform(7.0, 9.5)
        height = random.uniform(10.0, 30.0)
        x = side * random.uniform(11.0, 14.5)
        body = cube(f"CBD_Block_{'L' if side < 0 else 'R'}_{index:02d}", (x, y, height / 2), (depth, width, height), random.choice(building_palette), buildings_col, 0.12)
        inward_x = x - side * (depth / 2 + 0.035)
        rows = max(3, min(10, int(height / 2.7)))
        for row in range(rows):
            z = 2.2 + row * ((height - 3.0) / rows)
            window = cube(f"Windows_{body.name}_{row:02d}", (inward_x, y, z), (0.055, width * 0.72, 0.62), window_lit if (row + index) % 3 == 0 else window_dark, buildings_col)

# Landmark silhouettes: Ponte-inspired cylinder, Carlton-inspired tower, Hillbrow mast.
ponte = cylinder("Ponte_Inspired_Tower", (20, -92, 18), 5.3, 36, building_palette[1], landmarks, 48)
for z in range(3, 35, 3):
    torus(f"Ponte_Window_Ring_{z}", (20, -92, z), 5.32, 0.055, window_dark if z % 6 else window_lit, landmarks, rotation=(0, 0, 0))
cube("Carlton_Inspired_Tower", (-18, -76, 25), (9, 11, 50), building_palette[3], landmarks, 0.18)
for z in range(4, 48, 4):
    cube(f"Carlton_WindowBand_{z}", (-13.47, -76, z), (0.06, 8.5, 0.55), window_lit if z % 8 == 0 else window_dark, landmarks)
cylinder("Hillbrow_Tower_Shaft", (-22, -108, 28), 1.15, 56, sidewalk_mat, landmarks, 24)
cylinder("Hillbrow_Tower_Deck", (-22, -108, 51), 4.0, 5.0, building_palette[1], landmarks, 32)
cylinder("Hillbrow_Tower_Mast", (-22, -108, 67), 0.34, 30, red, landmarks, 16)

# Nelson Mandela Bridge-inspired gold arch near the horizon.
for x in (-5.7, 5.7):
    cube(f"Bridge_Pylon_{x}", (x, -112, 8.5), (0.45, 0.6, 17), yellow, landmarks)
for step in range(7):
    x = -5.7 + step * 1.9
    z = 16.5 - abs(step - 3) * 1.15
    cube(f"Bridge_Cable_{step}", (x, -112, z / 2), (0.07, 0.08, z), yellow, landmarks)


def make_taxi(name, location, rotation_z=0.0):
    root = empty(name, location, props, "CUBE", 1.0)
    root.rotation_euler.z = rotation_z
    body = cube(name + "_Body", (location[0], location[1], 0.85), (2.15, 4.6, 1.65), taxi_white, props, 0.18)
    stripe = cube(name + "_Stripe", (location[0] - 1.09, location[1], 0.72), (0.035, 4.0, 0.22), green, props)
    windshield = cube(name + "_Windshield", (location[0], location[1] - 2.31, 1.15), (1.72, 0.035, 0.62), window_dark, props)
    for x in (-0.88, 0.88):
        for y in (-1.45, 1.45):
            wheel = cylinder(name + f"_Wheel_{x}_{y}", (location[0] + x, location[1] + y, 0.38), 0.37, 0.23, rubber, props, 20)
            wheel.rotation_euler.y = math.pi / 2
    return root


make_taxi("Taxi_Obstacle_A", (-4.0, -36.0, 0))
make_taxi("Taxi_Obstacle_B", (4.0, -72.0, 0))
make_taxi("Taxi_Background", (0.0, -105.0, 0))

# Runner obstacles and collectible line.
for lane_x, y in [(0, -24), (4, -52), (-4, -88)]:
    cube(f"Construction_Barrier_{lane_x}_{y}", (lane_x, y, 0.65), (2.5, 0.38, 1.2), barrier_mat, props, 0.08)
    for offset in (-0.9, 0.0, 0.9):
        cube(f"Barrier_Stripe_{lane_x}_{y}_{offset}", (lane_x + offset, y - 0.205, 0.7), (0.32, 0.025, 0.88), white, props)
    empty(f"UNITY_ObstacleSpawn_{lane_x}_{abs(y)}", (lane_x, y, 0), spawn, "CUBE", 0.8)

coin_positions = [(-4, -12), (-2, -16), (0, -20), (2, -28), (4, -32), (2, -44), (0, -48), (-2, -60), (-4, -64)]
for index, (x, y) in enumerate(coin_positions):
    coin = torus(f"Gold_Coin_{index:02d}", (x, y, 1.25), 0.34, 0.085, yellow, props, rotation=(math.pi / 2, 0, 0))
    empty(f"UNITY_CoinSpawn_{index:02d}", (x, y, 1.25), spawn, "SPHERE", 0.45)

# Streetlights and traffic signals.
for side in (-1, 1):
    for index, y in enumerate(range(4, -111, -16)):
        x = side * 6.9
        cylinder(f"Streetlight_Pole_{side}_{index}", (x, y, 3.3), 0.075, 6.6, window_dark, props, 12)
        cube(f"Streetlight_Arm_{side}_{index}", (x - side * 0.55, y, 6.45), (1.1, 0.08, 0.08), window_dark, props)
        light_data = bpy.data.lights.new(f"Streetlight_Light_{side}_{index}", "AREA")
        light_data.energy = 110
        light_data.color = (1.0, 0.48, 0.20)
        light_data.shape = "DISK"
        light_data.size = 1.0
        light = bpy.data.objects.new(light_data.name, light_data)
        props.objects.link(light)
        light.location = (x - side * 1.0, y, 6.3)
        light.rotation_euler = (0, 0, 0)

for side in (-1, 1):
    x = side * 5.5
    cylinder(f"TrafficPole_{side}", (x, -18, 2.5), 0.09, 5.0, window_dark, props, 12)
    cube(f"TrafficBox_{side}", (x, -18, 4.6), (0.62, 0.38, 1.55), window_dark, props, 0.08)
    cylinder(f"TrafficRed_{side}", (x, -18.21, 5.05), 0.13, 0.05, red, props, 16).rotation_euler.x = math.pi / 2
    cylinder(f"TrafficAmber_{side}", (x, -18.21, 4.58), 0.13, 0.05, amber, props, 16).rotation_euler.x = math.pi / 2
    cylinder(f"TrafficGreen_{side}", (x, -18.21, 4.12), 0.13, 0.05, green, props, 16).rotation_euler.x = math.pi / 2

# Hero start pad and city signage.
cylinder("Runner_StartPad", (0, 1.5, 0.05), 1.45, 0.10, yellow, props, 48)
empty("UNITY_PlayerSpawn", (0, 1.5, 0.1), spawn, "ARROWS", 1.2)
text_mesh("JOZI RUNNER", "Jozi_Runner_Sign", (0, -8.2, 8.55), (math.pi / 2, 0, 0), 1.05, yellow, props)
cube("Jozi_Sign_Frame", (0, -8.5, 8.25), (8.8, 0.22, 2.2), window_dark, props, 0.12)

# Ground beyond the road corridor.
cube("City_Ground", (0, -55, -0.38), (80, 150, 0.5), mat("Ground Dark", (0.018, 0.024, 0.032), roughness=0.9), geometry)

# Camera and lighting.
camera_data = bpy.data.cameras.new("Runner Gameplay Camera")
camera = bpy.data.objects.new("Runner Gameplay Camera", camera_data)
scene.collection.objects.link(camera)
camera.location = (0, 12.5, 5.8)
camera.data.lens = 52
camera.rotation_euler = (Vector((0, -12, 1.2)) - camera.location).to_track_quat("-Z", "Y").to_euler()
scene.camera = camera

sun_data = bpy.data.lights.new("Jozi Sunset", "SUN")
sun_data.energy = 2.0
sun_data.color = (1.0, 0.42, 0.18)
sun = bpy.data.objects.new("Jozi Sunset", sun_data)
scene.collection.objects.link(sun)
sun.rotation_euler = (math.radians(35), 0, math.radians(-28))

fill_data = bpy.data.lights.new("CBD Sky Fill", "AREA")
fill_data.energy = 1700
fill_data.color = (0.16, 0.30, 0.72)
fill_data.shape = "DISK"
fill_data.size = 18
fill = bpy.data.objects.new("CBD Sky Fill", fill_data)
scene.collection.objects.link(fill)
fill.location = (0, -35, 35)
fill.rotation_euler = (0, 0, 0)

world = scene.world or bpy.data.worlds.new("Jozi World")
scene.world = world
world.use_nodes = True
background = world.node_tree.nodes["Background"]
background.inputs["Color"].default_value = (0.008, 0.018, 0.055, 1)
background.inputs["Strength"].default_value = 0.34

scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = str(OUTPUT / "jozi_runner_gameplay.png")
scene.view_settings.look = "AgX - Medium High Contrast"
scene.render.film_transparent = False
scene.frame_start = 1
scene.frame_end = 120
scene.render.fps = 30

# Save, render gameplay preview, then a high three-quarter overview.
blend_path = OUTPUT / "Jozi_Runner_CBD.blend"
bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
bpy.ops.render.render(write_still=True)

camera.location = (0, 23, 29)
camera.data.lens = 48
camera.rotation_euler = (Vector((0, -48, 3.5)) - camera.location).to_track_quat("-Z", "Y").to_euler()
scene.render.filepath = str(OUTPUT / "jozi_runner_overview.png")
bpy.ops.render.render(write_still=True)

# Leave the file opening in the gameplay camera.
camera.location = (0, 12.5, 5.8)
camera.data.lens = 52
camera.rotation_euler = (Vector((0, -12, 1.2)) - camera.location).to_track_quat("-Z", "Y").to_euler()
scene.render.filepath = str(OUTPUT / "jozi_runner_gameplay.png")
bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

# Export environment geometry, props, landmarks, and Unity spawn empties only.
bpy.ops.object.select_all(action="DESELECT")
for col in (geometry, buildings_col, props, landmarks, spawn):
    for obj in col.objects:
        if obj.type in {"MESH", "EMPTY"}:
            obj.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=str(OUTPUT / "Jozi_Runner_CBD_Environment.fbx"),
    use_selection=True,
    object_types={"MESH", "EMPTY"},
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    use_space_transform=True,
    bake_space_transform=True,
    axis_forward="-Z",
    axis_up="Y",
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    path_mode="COPY",
    embed_textures=True,
    bake_anim=False,
)

print("BLEND", blend_path)
print("FBX", OUTPUT / "Jozi_Runner_CBD_Environment.fbx")
