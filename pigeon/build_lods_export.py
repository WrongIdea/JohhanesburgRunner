"""Build 3 LOD skinned meshes, optimize, export single FBX with all clips.
Names LOD meshes Pigeon_LOD0/1/2 to match the existing PigeonBuilder.FindLod."""
import bpy, os

ANIM = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/pigeon_animated.blend"
FBX  = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/PigeonHD.fbx"

bpy.ops.wm.open_mainfile(filepath=ANIM)
obj = bpy.data.objects["PigeonMesh"]
arm = bpy.data.objects["PigeonArmature"]

# single material + name
while len(obj.data.materials) > 1:
    obj.data.materials.pop(index=1)
if obj.data.materials:
    obj.data.materials[0].name = "PigeonMat"

def optimize(o):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active=o; o.select_set(True)
    bpy.ops.object.vertex_group_limit_total(limit=4)
    bpy.ops.object.vertex_group_normalize_all()

# LOD0 = original
obj.name="Pigeon_LOD0"; obj.data.name="Pigeon_LOD0"
optimize(obj)

def make_lod(src, name, ratio):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active=src; src.select_set(True)
    bpy.ops.object.duplicate()
    d=bpy.context.view_layer.objects.active
    d.name=name; d.data.name=name
    m=d.modifiers.new("Dec","DECIMATE"); m.decimate_type='COLLAPSE'; m.ratio=ratio
    # move Decimate to top so it applies to base mesh (armature deform is identity at rest)
    while d.modifiers.find("Dec")>0:
        with bpy.context.temp_override(object=d):
            bpy.ops.object.modifier_move_up(modifier="Dec")
    with bpy.context.temp_override(object=d):
        bpy.ops.object.modifier_apply(modifier="Dec")
    optimize(d)
    print(f"{name}: tris~{sum(len(p.vertices)-2 for p in d.data.polygons)}")
    return d

lod1=make_lod(obj,"Pigeon_LOD1",0.5)
lod2=make_lod(obj,"Pigeon_LOD2",0.25)
print("LOD0 tris~", sum(len(p.vertices)-2 for p in obj.data.polygons))

# ---- EXPORT ----
bpy.ops.object.select_all(action='DESELECT')
for o in (obj,lod1,lod2,arm): o.select_set(True)
bpy.context.view_layer.objects.active=arm

bpy.ops.export_scene.fbx(
    filepath=FBX, use_selection=True,
    object_types={'ARMATURE','MESH'}, add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
    apply_unit_scale=True, global_scale=1.0, apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z', axis_up='Y', use_mesh_modifiers=True, mesh_smooth_type='FACE',
    path_mode='COPY', embed_textures=False,
)
print("EXPORTED", FBX, os.path.getsize(FBX))
