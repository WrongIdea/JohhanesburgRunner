import bpy
from pathlib import Path


OUTPUT = Path("/Users/thapelopilanyane/Downloads/jumpandroll.fbx")

scene = bpy.context.scene
scene.frame_start = 18
scene.frame_end = 83

bpy.ops.object.select_all(action="DESELECT")
character = bpy.data.objects.get("Beaded Warrior")
rig = bpy.data.objects.get("Beaded Warrior Rig")

if character is None or rig is None:
    raise RuntimeError("Beaded Warrior mesh or rig was not found in the Blender project")

character.hide_set(False)
rig.hide_set(False)
character.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig

bpy.ops.export_scene.fbx(
    filepath=str(OUTPUT),
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
    bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True,
    bake_anim_step=1.0,
    bake_anim_simplify_factor=0.0,
    path_mode="COPY",
    embed_textures=True,
)

print(f"UNITY_FBX={OUTPUT}")
