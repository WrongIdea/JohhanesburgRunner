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
ROOT = "/Users/thapelopilanyane/My project/blender_buildings/production"
TARGET_HEIGHT = 18.0
LOD_TARGETS = [40000, 12000, 3000]

def reset():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    for image in list(bpy.data.images):
        bpy.data.images.remove(image)
    for collection in list(bpy.data.collections):
        if collection.name != "Collection":
            bpy.data.collections.remove(collection)

def mesh_bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    mn = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    mx = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return mn, mx

def tri_count(obj):
    return sum(len(poly.vertices) - 2 for poly in obj.data.polygons)

def select_only(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.hide_set(False)
    obj.hide_viewport = False
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

def export_fbx(obj, path):
    select_only(obj)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="STRIP",
    )

def extract_textures(texture_dir):
    os.makedirs(texture_dir, exist_ok=True)
    written = []
    for image in bpy.data.images:
        lower = image.name.lower()
        if not any(key in lower for key in ("base_color", "normal", "metallic_roughness", "emissive")):
            continue
        name = next(key for key in ("base_color", "normal", "metallic_roughness", "emissive") if key in lower)
        path = os.path.join(texture_dir, name + ".png")
        image.file_format = "PNG"
        image.filepath_raw = path
        image.save()
        written.append(path)
    return written

def create_collision(name, dimensions):
    bpy.ops.mesh.primitive_cube_add(location=(0, 0, dimensions.z * 0.5))
    col = bpy.context.object
    col.name = name + "_Collision"
    col.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return col

reports = {}
for source, name in SOURCES:
    reset()
    out = os.path.join(ROOT, name)
    fbx_dir = os.path.join(out, "FBX")
    tex_dir = os.path.join(out, "Textures")
    os.makedirs(fbx_dir, exist_ok=True)
    os.makedirs(tex_dir, exist_ok=True)

    bpy.ops.import_scene.gltf(filepath=source)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if len(meshes) != 1:
        for obj in meshes:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = meshes[0]
        bpy.ops.object.join()
    master = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"][0]
    master.name = name + "_SourceMaster"

    mn, mx = mesh_bounds(master)
    scale = TARGET_HEIGHT / (mx.z - mn.z)
    master.scale = (scale, scale, scale)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    mn, mx = mesh_bounds(master)
    master.location += Vector((-(mn.x + mx.x) * 0.5, -(mn.y + mx.y) * 0.5, -mn.z))
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    mn, mx = mesh_bounds(master)
    dimensions = mx - mn
    source_tris = tri_count(master)
    textures = extract_textures(tex_dir)

    lod_objects = []
    lod_stats = []
    for index, target in enumerate(LOD_TARGETS):
        lod = master.copy()
        lod.data = master.data.copy()
        bpy.context.collection.objects.link(lod)
        lod.name = name + "_LOD" + str(index)
        ratio = min(1.0, target / max(1, source_tris))
        modifier = lod.modifiers.new("MobileDecimate", "DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = ratio
        modifier.use_collapse_triangulate = True
        select_only(lod)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        for poly in lod.data.polygons:
            poly.use_smooth = False
        lod.data.update()
        lod_min, _ = mesh_bounds(lod)
        lod.location.z -= lod_min.z
        bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
        lod_objects.append(lod)
        lod_stats.append(tri_count(lod))
        export_fbx(lod, os.path.join(fbx_dir, lod.name + ".fbx"))
        lod.hide_render = True
        lod.hide_set(True)

    collision = create_collision(name, dimensions)
    export_fbx(collision, os.path.join(fbx_dir, collision.name + ".fbx"))
    collision.hide_render = True
    collision.hide_set(True)
    master.hide_render = True
    master.hide_set(True)

    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, name + ".blend"))

    report = {
        "source": source,
        "blend": os.path.join(out, name + ".blend"),
        "dimensions_m": list(dimensions),
        "source_triangles": source_tris,
        "lod_triangles": lod_stats,
        "collision_triangles": tri_count(collision),
        "textures": textures,
        "pivot": "ground centre",
        "base_y_unity": 0,
        "front": "Blender -Y / Unity +Z",
    }
    reports[name] = report
    with open(os.path.join(out, name + "_REPORT.json"), "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)

with open(os.path.join(ROOT, "production_summary.json"), "w", encoding="utf-8") as f:
    json.dump(reports, f, indent=2)
