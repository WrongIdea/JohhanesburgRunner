"""Create 3 decimated LOD skinned meshes from PigeonV2_animated.blend (keeping
weights + armature + all 12 actions) and export a single Unity FBX with LODs +
animations. Names meshes Pigeon_LOD0/1/2 for Unity LODGroup wiring. Headless."""
import bpy, os
BASE="/Users/thapelopilanyane/My project/pigeon/meshy_pigeon_v2"
bpy.ops.wm.open_mainfile(filepath=BASE+"/PigeonV2_animated.blend")
mesh=bpy.data.objects["PigeonMesh"]; arm=bpy.data.objects["PigeonArmature"]

# single material name tidy
if mesh.data.materials:
    mesh.data.materials[0].name="PigeonMat"

def tris(o): return sum(len(p.vertices)-2 for p in o.data.polygons)
SRC_TRIS=tris(mesh)
print("SOURCE tris:",SRC_TRIS)

def optimize(o):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active=o; o.select_set(True)
    bpy.ops.object.vertex_group_limit_total(limit=4)
    bpy.ops.object.vertex_group_normalize_all()

def make_lod(name, target):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active=mesh; mesh.select_set(True)
    bpy.ops.object.duplicate()
    d=bpy.context.view_layer.objects.active; d.name=name; d.data.name=name
    m=d.modifiers.new("Dec","DECIMATE"); m.decimate_type='COLLAPSE'
    m.ratio=min(1.0, target/SRC_TRIS); m.use_collapse_triangulate=True
    # move Decimate above the Armature modifier so it applies to base mesh
    while d.modifiers.find("Dec")>0:
        with bpy.context.temp_override(object=d): bpy.ops.object.modifier_move_up(modifier="Dec")
    with bpy.context.temp_override(object=d): bpy.ops.object.modifier_apply(modifier="Dec")
    optimize(d)
    print(f"{name}: target~{target} actual tris={tris(d)}")
    return d

# LOD0 replaces original: rename original mesh to LOD0 then decimate it too
lod0=make_lod("Pigeon_LOD0",1100)
lod1=make_lod("Pigeon_LOD1",520)
lod2=make_lod("Pigeon_LOD2",240)
# remove the heavy original (we only ship LODs)
bpy.ops.object.select_all(action='DESELECT'); mesh.select_set(True)
bpy.ops.object.delete()

# ensure each LOD keeps armature modifier pointing at arm
for o in (lod0,lod1,lod2):
    if not any(md.type=='ARMATURE' for md in o.modifiers):
        md=o.modifiers.new("Armature","ARMATURE"); md.object=arm

# export: LODs + armature + all actions, NO textures (Unity assigns its own mat)
bpy.ops.object.select_all(action='DESELECT')
for o in (lod0,lod1,lod2,arm): o.select_set(True)
bpy.context.view_layer.objects.active=arm
FBX=BASE+"/Pigeon_Unity.fbx"
bpy.ops.export_scene.fbx(
    filepath=FBX, use_selection=True, object_types={'ARMATURE','MESH'},
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
    bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
    apply_unit_scale=True, global_scale=1.0, apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z', axis_up='Y', use_mesh_modifiers=False, mesh_smooth_type='FACE',
    path_mode='STRIP', embed_textures=False, use_armature_deform_only=True,
    primary_bone_axis='Y', secondary_bone_axis='X')
print("EXPORTED", FBX, os.path.getsize(FBX))
print("LOD tris:", tris(lod0), tris(lod1), tris(lod2))
print("actions:", len(bpy.data.actions))
print("DONE")
