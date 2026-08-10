"""
Authors a stylised low-poly Johannesburg pigeon: mesh + 512 palette atlas +
7-bone rig + 6 animations + 3 LOD meshes, exported as one Unity-ready FBX.

Blender 5.2, run headless:
  Blender --background --python pigeon/create_pigeon.py

Design notes:
- Flat-colour look via a single material + one 512x512 atlas. Every mesh part's
  UVs collapse to a single point inside its colour cell, so there is exactly one
  material/texture and zero gradient authoring. This keeps the silhouette clean
  and the atlas trivially shareable/instanceable.
- Built in metres, Z-up, facing +Y (head forward), feet at Z=0. Exported with the
  project's standard axis conversion; final facing is corrected in the Unity
  prefab builder if needed.
"""

import bpy
import bmesh
import math
from pathlib import Path
from mathutils import Vector

OUT_DIR = Path("/Users/thapelopilanyane/My project/Assets/Characters/Pigeon")
OUT_DIR.mkdir(parents=True, exist_ok=True)
FBX_OUT = OUT_DIR / "Pigeon.fbx"
PNG_OUT = OUT_DIR / "PigeonAtlas.png"

FPS = 30
D2R = math.radians

# ----------------------------------------------------------------------------
# Clean slate
# ----------------------------------------------------------------------------
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = FPS

# ----------------------------------------------------------------------------
# Palette atlas (single 512 texture; parts point at cell centres)
# ----------------------------------------------------------------------------
SIZE = 512
img = bpy.data.images.new("PigeonAtlas", SIZE, SIZE, alpha=True)
BODY = (0.34, 0.35, 0.39)
WING = (0.50, 0.52, 0.55)
HEAD = (0.29, 0.30, 0.34)
BEAK = (0.16, 0.15, 0.14)
FEET = (0.86, 0.47, 0.52)
EYE = (0.02, 0.02, 0.02)
TAIL = (0.41, 0.42, 0.46)

# Cell centres in UV space.
CELL = {
    "Body": (0.125, 0.875, BODY),
    "Wing": (0.375, 0.875, WING),
    "Head": (0.625, 0.875, HEAD),
    "Beak": (0.875, 0.875, BEAK),
    "Feet": (0.125, 0.625, FEET),
    "Eye":  (0.375, 0.625, EYE),
    "Tail": (0.625, 0.625, TAIL),
}

px = list(BODY + (1.0,)) * (SIZE * SIZE)  # default fill = body grey


def paint(u, v, col, half=0.06):
    x0 = max(0, int((u - half) * SIZE))
    x1 = min(SIZE, int((u + half) * SIZE))
    y0 = max(0, int((v - half) * SIZE))
    y1 = min(SIZE, int((v + half) * SIZE))
    for y in range(y0, y1):
        base = y * SIZE * 4
        for x in range(x0, x1):
            i = base + x * 4
            px[i] = col[0]
            px[i + 1] = col[1]
            px[i + 2] = col[2]
            px[i + 3] = 1.0


for u, v, col in CELL.values():
    paint(u, v, col)

img.pixels = px
img.filepath_raw = str(PNG_OUT)
img.file_format = "PNG"
img.save()

# ----------------------------------------------------------------------------
# Shared material
# ----------------------------------------------------------------------------
mat = bpy.data.materials.new("PigeonMat")
mat.use_nodes = True
bsdf = mat.node_tree.nodes.get("Principled BSDF")
tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
tex.image = img
tex.interpolation = "Closest"
mat.node_tree.links.new(bsdf.inputs["Base Color"], tex.outputs["Color"])
bsdf.inputs["Roughness"].default_value = 0.75
bsdf.inputs["Metallic"].default_value = 0.0

# ----------------------------------------------------------------------------
# Mesh part helpers
# ----------------------------------------------------------------------------
part_objs = []


def finalize(obj, cell_key, group_name):
    """Assign shared material, collapse UVs to the cell centre, tag vertex group."""
    me = obj.data
    if not me.uv_layers:
        me.uv_layers.new(name="UV")
    u, v, _ = CELL[cell_key]
    uv = me.uv_layers.active.data
    for loop in me.loops:
        uv[loop.index].uv = (u, v)
    if me.materials:
        me.materials[0] = mat
    else:
        me.materials.append(mat)
    vg = obj.vertex_groups.new(name=group_name)
    vg.add([vtx.index for vtx in me.vertices], 1.0, "REPLACE")
    part_objs.append(obj)


def add_sphere(name, loc, scale, segs, rings, cell, group):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=rings, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    finalize(o, cell, group)
    return o


def add_cylinder(name, loc, radius, depth, verts, cell, group, rot=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc)
    o = bpy.context.active_object
    o.name = name
    if rot:
        o.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    finalize(o, cell, group)
    return o


def add_cone(name, loc, radius, depth, verts, cell, group, rot=None):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=radius, radius2=0.0, depth=depth, location=loc)
    o = bpy.context.active_object
    o.name = name
    if rot:
        o.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    finalize(o, cell, group)
    return o


def add_box(name, loc, scale, cell, group, rot=None):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    if rot:
        o.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    finalize(o, cell, group)
    return o


# Body (ellipsoid). Centre ~0.15 up; length along Y.
add_sphere("Body", (0, 0.0, 0.15), (0.072, 0.115, 0.078), 10, 6, "Body", "Body")
# Tail (flattened wedge back+up)
add_box("Tail", (0, -0.15, 0.175), (0.06, 0.11, 0.012), "Tail", "Body",
        rot=(D2R(18), 0, 0))
# Head (compact, slightly forward)
add_sphere("Head", (0, 0.115, 0.20), (0.046, 0.048, 0.046), 8, 6, "Head", "Head")
# Beak (points +Y)
add_cone("Beak", (0, 0.165, 0.195), 0.015, 0.045, 6, "Beak", "Head",
         rot=(D2R(90), 0, 0))
# Eyes (tiny quads on head sides)
add_box("EyeL", (0.038, 0.135, 0.21), (0.006, 0.013, 0.013), "Eye", "Head")
add_box("EyeR", (-0.038, 0.135, 0.21), (0.006, 0.013, 0.013), "Eye", "Head")
# Wings — thin plates folded along the flanks (vertical, swept back), so the
# resting silhouette is clean; the flap animation swings them up from here.
add_box("WingL", (0.070, -0.02, 0.155), (0.012, 0.15, 0.065), "Wing", "WingL",
        rot=(0, D2R(-14), 0))
add_box("WingR", (-0.070, -0.02, 0.155), (0.012, 0.15, 0.065), "Wing", "WingR",
        rot=(0, D2R(14), 0))
# Legs (thin cylinders down to feet)
add_cylinder("LegL", (0.035, -0.01, 0.05), 0.008, 0.10, 5, "Feet", "LegL")
add_cylinder("LegR", (-0.035, -0.01, 0.05), 0.008, 0.10, 5, "Feet", "LegR")
# Feet (small flat pads)
add_box("FootL", (0.035, 0.006, 0.006), (0.028, 0.05, 0.008), "Feet", "LegL")
add_box("FootR", (-0.035, 0.006, 0.006), (0.028, 0.05, 0.008), "Feet", "LegR")

# ----------------------------------------------------------------------------
# Join into one mesh (vertex groups merge by name)
# ----------------------------------------------------------------------------
bpy.ops.object.select_all(action="DESELECT")
for o in part_objs:
    o.select_set(True)
bpy.context.view_layer.objects.active = part_objs[0]
bpy.ops.object.join()
mesh_obj = bpy.context.active_object
mesh_obj.name = "Pigeon_LOD0"
# Recentre origin at feet (world origin already at feet; keep object origin at 0).
mesh_obj.location = (0, 0, 0)

tri_count = sum(len(p.vertices) - 2 for p in mesh_obj.data.polygons)
print(f"PIGEON-DEBUG LOD0 faces={len(mesh_obj.data.polygons)} tris~={tri_count}")

# ----------------------------------------------------------------------------
# Armature (Root/Body/Head/WingL/WingR/LegL/LegR)
# ----------------------------------------------------------------------------
bpy.ops.object.armature_add(location=(0, 0, 0))
arm = bpy.context.active_object
arm.name = "PigeonRig"
amt = arm.data
amt.name = "PigeonArmature"

bpy.ops.object.mode_set(mode="EDIT")
ebones = amt.edit_bones
# Remove the default bone.
for b in list(ebones):
    ebones.remove(b)


def mkbone(name, head, tail, parent=None):
    b = ebones.new(name)
    b.head = head
    b.tail = tail
    if parent:
        b.parent = parent
        b.use_connect = False
    return b


root = mkbone("Root", (0, 0, 0.0), (0, 0, 0.06))
body = mkbone("Body", (0, 0, 0.09), (0, 0.0, 0.20), root)
head = mkbone("Head", (0, 0.10, 0.20), (0, 0.16, 0.225), body)
wingL = mkbone("WingL", (0.05, 0, 0.17), (0.14, 0, 0.17), body)
wingR = mkbone("WingR", (-0.05, 0, 0.17), (-0.14, 0, 0.17), body)
legL = mkbone("LegL", (0.035, 0, 0.09), (0.035, 0, 0.0), root)
legR = mkbone("LegR", (-0.035, 0, 0.09), (-0.035, 0, 0.0), root)
bpy.ops.object.mode_set(mode="OBJECT")

# ----------------------------------------------------------------------------
# Bind mesh to armature (vertex groups already set => rigid skin)
# ----------------------------------------------------------------------------
mesh_obj.parent = arm
mod = mesh_obj.modifiers.new("Armature", "ARMATURE")
mod.object = arm

# ----------------------------------------------------------------------------
# LOD1 / LOD2 by decimation (keep vertex groups + armature bind)
# ----------------------------------------------------------------------------
def make_lod(name, ratio):
    dup = mesh_obj.copy()
    dup.data = mesh_obj.data.copy()
    dup.name = name
    scene.collection.objects.link(dup)
    dec = dup.modifiers.new("Decimate", "DECIMATE")
    dec.ratio = ratio
    # Apply only the decimate (keep the armature modifier for skinning).
    bpy.context.view_layer.objects.active = dup
    bpy.ops.object.modifier_apply(modifier="Decimate")
    tris = sum(len(p.vertices) - 2 for p in dup.data.polygons)
    print(f"PIGEON-DEBUG {name} tris~={tris}")
    return dup


lod1 = make_lod("Pigeon_LOD1", 0.55)
lod2 = make_lod("Pigeon_LOD2", 0.22)

# ----------------------------------------------------------------------------
# Animations
# ----------------------------------------------------------------------------
arm.animation_data_create()
POSE = arm.pose.bones
for pb in POSE:
    pb.rotation_mode = "XYZ"


def new_action(name):
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    return act


def key_rot(bone, frame, x=0.0, y=0.0, z=0.0):
    pb = POSE[bone]
    pb.rotation_euler = (D2R(x), D2R(y), D2R(z))
    pb.keyframe_insert("rotation_euler", frame=frame)


def key_loc(bone, frame, x=0.0, y=0.0, z=0.0):
    pb = POSE[bone]
    pb.location = (x, y, z)
    pb.keyframe_insert("location", frame=frame)


def reset_pose():
    for pb in POSE:
        pb.rotation_euler = (0, 0, 0)
        pb.location = (0, 0, 0)


# --- 1. Idle (1..90, loop) ---
new_action("Idle")
reset_pose()
key_loc("Root", 1, z=0.0); key_loc("Root", 45, z=0.004); key_loc("Root", 90, z=0.0)
key_rot("Head", 1, z=0); key_rot("Head", 30, z=10); key_rot("Head", 60, z=-10); key_rot("Head", 90, z=0)
key_rot("Head", 1, x=0); key_rot("Head", 45, x=6); key_rot("Head", 90, x=0)
key_rot("Body", 1, y=0); key_rot("Body", 45, y=3); key_rot("Body", 90, y=0)

# --- 2. Peck (1..60, loop) ---
new_action("Peck")
reset_pose()
key_rot("Head", 1, x=0); key_rot("Head", 15, x=48); key_rot("Head", 24, x=52)
key_rot("Head", 40, x=0); key_rot("Head", 60, x=0)
key_rot("Body", 1, x=0); key_rot("Body", 15, x=10); key_rot("Body", 40, x=0); key_rot("Body", 60, x=0)

# --- 3. Walk (1..60, loop) ---
new_action("Walk")
reset_pose()
key_rot("LegL", 1, x=22); key_rot("LegL", 30, x=-22); key_rot("LegL", 60, x=22)
key_rot("LegR", 1, x=-22); key_rot("LegR", 30, x=22); key_rot("LegR", 60, x=-22)
key_loc("Root", 1, z=0); key_loc("Root", 15, z=0.009); key_loc("Root", 30, z=0)
key_loc("Root", 45, z=0.009); key_loc("Root", 60, z=0)
key_rot("Head", 1, x=0); key_rot("Head", 15, x=7); key_rot("Head", 30, x=0)
key_rot("Head", 45, x=7); key_rot("Head", 60, x=0)
key_rot("Body", 1, y=2); key_rot("Body", 30, y=-2); key_rot("Body", 60, y=2)

# --- 4. Take Off (1..18, no loop) ---
new_action("TakeOff")
reset_pose()
key_loc("Root", 1, z=0); key_loc("Root", 3, z=-0.012); key_loc("Root", 7, z=0.02); key_loc("Root", 18, z=0.04)
key_rot("Body", 1, x=0); key_rot("Body", 18, x=-20)
for b, s in (("WingL", 1), ("WingR", -1)):
    key_rot(b, 1, x=10 * s); key_rot(b, 5, x=-40 * s); key_rot(b, 10, x=40 * s)
    key_rot(b, 14, x=-40 * s); key_rot(b, 18, x=20 * s)

# --- 5. Fly (1..15, loop) ---
new_action("Fly")
reset_pose()
key_loc("Root", 1, z=0); key_loc("Root", 8, z=0.006); key_loc("Root", 15, z=0)
key_rot("Body", 1, x=-4); key_rot("Body", 8, x=-7); key_rot("Body", 15, x=-4)
for b, s in (("WingL", 1), ("WingR", -1)):
    key_rot(b, 1, x=-32 * s); key_rot(b, 8, x=38 * s); key_rot(b, 15, x=-32 * s)

# --- 6. Land (1..15, no loop) ---
new_action("Land")
reset_pose()
key_loc("Root", 1, z=0.05); key_loc("Root", 9, z=0.0); key_loc("Root", 11, z=0.015); key_loc("Root", 15, z=0)
key_rot("Body", 1, x=-15); key_rot("Body", 9, x=0); key_rot("Body", 15, x=0)
for b, s in (("WingL", 1), ("WingR", -1)):
    key_rot(b, 1, x=32 * s); key_rot(b, 6, x=10 * s); key_rot(b, 10, x=-10 * s); key_rot(b, 15, x=0)

# Set frame ranges on actions so use_all_actions exports correct lengths.
ranges = {"Idle": (1, 90), "Peck": (1, 60), "Walk": (1, 60),
          "TakeOff": (1, 18), "Fly": (1, 15), "Land": (1, 15)}
for name, (f0, f1) in ranges.items():
    act = bpy.data.actions.get(name)
    if act:
        act.frame_range  # noqa (touch)
        act.use_frame_range = True
        act.frame_start = f0
        act.frame_end = f1

arm.animation_data.action = bpy.data.actions.get("Idle")
scene.frame_start = 1
scene.frame_end = 90
scene.frame_set(1)

# ----------------------------------------------------------------------------
# Export
# ----------------------------------------------------------------------------
bpy.ops.object.select_all(action="DESELECT")
for o in (arm, mesh_obj, lod1, lod2):
    o.select_set(True)
bpy.context.view_layer.objects.active = arm

bpy.ops.export_scene.fbx(
    filepath=str(FBX_OUT),
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    use_space_transform=True,
    bake_space_transform=True,
    axis_forward="-Z",
    axis_up="Y",
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    use_armature_deform_only=True,
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True,
    bake_anim_step=1.0,
    bake_anim_simplify_factor=0.0,
    path_mode="COPY",
    embed_textures=False,
)
print(f"PIGEON-DEBUG exported {FBX_OUT}")
