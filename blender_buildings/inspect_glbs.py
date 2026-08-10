import bpy
import json
import math
import os
from mathutils import Vector

SOURCES = [
    ("/Users/thapelopilanyane/Downloads/Building3.glb", "JHB_HeroBuilding03"),
    ("/Users/thapelopilanyane/Downloads/building4.glb", "JHB_HeroBuilding04"),
    ("/Users/thapelopilanyane/Downloads/building5.glb", "JHB_HeroBuilding05"),
    ("/Users/thapelopilanyane/Downloads/Building6.glb", "JHB_HeroBuilding06"),
    ("/Users/thapelopilanyane/Downloads/building7.glb", "JHB_HeroBuilding07"),
]
ONLY = os.environ.get("JOZI_BUILDING_ONLY")
if ONLY:
    SOURCES = [item for item in SOURCES if item[1] == ONLY]
OUT = "/Users/thapelopilanyane/My project/blender_buildings/inspection"
os.makedirs(OUT, exist_ok=True)

def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.images):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)

def bounds(objects):
    points = []
    for obj in objects:
        if obj.type != "MESH":
            continue
        points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    mn = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    mx = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return mn, mx

def render(name, mesh_objects, mn, mx):
    world = bpy.data.worlds.new("ReviewWorld")
    bpy.context.scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.12, 0.15, 0.2, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8

    ground = bpy.data.meshes.new("GroundMesh")
    ground.from_pydata([(-100,-100,0),(100,-100,0),(100,100,0),(-100,100,0)], [], [(0,1,2,3)])
    ground_obj = bpy.data.objects.new("Ground", ground)
    bpy.context.collection.objects.link(ground_obj)
    mat = bpy.data.materials.new("GroundMaterial")
    mat.diffuse_color = (0.08, 0.09, 0.1, 1)
    ground_obj.data.materials.append(mat)

    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 3.0
    sun = bpy.data.objects.new("Sun", sun_data)
    bpy.context.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(35), math.radians(-25), math.radians(-35))

    size = mx - mn
    center = (mn + mx) * 0.5
    target = Vector((center.x, center.y, mn.z + size.z * 0.45))
    distance = max(size.x, size.y, size.z) * 1.9
    cam_data = bpy.data.cameras.new("ReviewCamera")
    cam = bpy.data.objects.new("ReviewCamera", cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = target + Vector((distance * 0.85, -distance, distance * 0.55))
    direction = target - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = 52
    bpy.context.scene.camera = cam

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 720
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = os.path.join(OUT, name + "_review.png")
    scene.render.film_transparent = False
    bpy.ops.render.render(write_still=True)

report = {}
for source, name in SOURCES:
    reset()
    bpy.ops.import_scene.gltf(filepath=source)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    mn, mx = bounds(meshes)
    triangles = sum(len(poly.vertices) - 2 for obj in meshes for poly in obj.data.polygons)
    images = []
    for image in bpy.data.images:
        images.append({
            "name": image.name,
            "size": list(image.size),
            "packed": image.packed_file is not None,
            "filepath": image.filepath,
        })
    report[name] = {
        "source": source,
        "objects": len(meshes),
        "triangles": triangles,
        "materials": [mat.name for mat in bpy.data.materials],
        "images": images,
        "bounds_min": list(mn),
        "bounds_max": list(mx),
        "dimensions": list(mx - mn),
    }
    render(name, meshes, mn, mx)

with open(os.path.join(OUT, "inspection.json"), "w", encoding="utf-8") as f:
    json.dump(report, f, indent=2)
