import bpy

scene = bpy.context.scene
scene.render.resolution_x = 480
scene.render.resolution_y = 480
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = "/Users/thapelopilanyane/My project/meshy_jump_roll/output/roll_frames/roll_"
bpy.ops.render.render(animation=True)
