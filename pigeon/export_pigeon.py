"""Optimize the rigged+animated pigeon and export one FBX with all 7 baked
clips (fwd -Z, up Y). Then re-import to verify. Run headless."""
import bpy, os

ANIM = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/pigeon_animated.blend"
FBX  = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/PigeonHD.fbx"

bpy.ops.wm.open_mainfile(filepath=ANIM)
obj = bpy.data.objects["PigeonMesh"]
arm = bpy.data.objects["PigeonArmature"]

# ---- OPTIMIZE ----
bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.objects.active = obj
obj.select_set(True)

# no shape keys
assert obj.data.shape_keys is None, "shape keys present!"

# single material
while len(obj.data.materials) > 1:
    obj.data.materials.pop(index=1)
if obj.data.materials:
    obj.data.materials[0].name = "PigeonMat"
print("materials:", [m.name for m in obj.data.materials])

# limit bone influences to 4 (mobile GPU skinning) + normalize
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.vertex_group_limit_total(limit=4)
bpy.ops.object.vertex_group_normalize_all()

# remove vertex groups that don't match a bone (none expected)
bones = {b.name for b in arm.data.bones}
for vg in list(obj.vertex_groups):
    if vg.name not in bones:
        print("removing stray vgroup", vg.name); obj.vertex_groups.remove(vg)
print("vgroups:", [vg.name for vg in obj.vertex_groups])

# ensure transforms applied / identity
for o in (obj, arm):
    print(o.name, "loc", tuple(round(v,4) for v in o.location), "scale", tuple(round(v,4) for v in o.scale))

# ---- EXPORT FBX ----
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True); arm.select_set(True)
bpy.context.view_layer.objects.active = arm

bpy.ops.export_scene.fbx(
    filepath=FBX,
    use_selection=True,
    object_types={'ARMATURE','MESH'},
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True,
    bake_anim_step=1.0,
    bake_anim_simplify_factor=0.0,
    apply_unit_scale=True,
    global_scale=1.0,
    apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',
    axis_up='Y',
    use_mesh_modifiers=True,
    mesh_smooth_type='FACE',
    path_mode='COPY',
    embed_textures=False,
)
print("EXPORTED", FBX, os.path.getsize(FBX), "bytes")
