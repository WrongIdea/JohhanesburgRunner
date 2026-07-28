import bpy

blend_path = "/Users/thapelopilanyane/My project/meshy_jump_roll/output/beaded_warrior_jump_roll.blend"

character = bpy.data.objects.get("Beaded Warrior")
rig = bpy.data.objects.get("Beaded Warrior Rig")
camera = bpy.data.objects.get("Action Camera")

print("character", character, "hidden", character.hide_viewport if character else None, character.hide_render if character else None)
print("rig", rig, "hidden", rig.hide_viewport if rig else None)
print("camera", camera)

if character:
    character.hide_set(False)
    character.hide_viewport = False
    character.hide_render = False
    character.select_set(True)
    bpy.context.view_layer.objects.active = character
if rig:
    rig.hide_set(False)
    rig.hide_viewport = False

# Open on the jump peak, where the character is unmistakably visible.
bpy.context.scene.frame_set(36)
bpy.context.scene.camera = camera

# Save every 3D viewport in camera view. This state is retained when the file opens.
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == "VIEW_3D":
            space = area.spaces.active
            space.region_3d.view_perspective = "CAMERA"
            space.overlay.show_floor = True

bpy.ops.wm.save_as_mainfile(filepath=blend_path)
print("Saved visible camera view at frame 36")
