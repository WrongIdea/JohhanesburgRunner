import bpy

scene = bpy.context.scene
scene.render.resolution_x = 480
scene.render.resolution_y = 480
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = "/Users/thapelopilanyane/My project/meshy_jump_roll/output/fly_frames/fly_"
bpy.ops.render.render(animation=True)
