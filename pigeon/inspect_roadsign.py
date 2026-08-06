import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
fbx = "/Users/thapelopilanyane/My project/Assets/Art/Generated/Incoming/road_sign_new/Meshy_AI_Create_a_low_poly_mod_0802182911_texture_fbx/Meshy_AI_Create_a_low_poly_mod_0802182911_texture.fbx"
bpy.ops.import_scene.fbx(filepath=fbx)
for o in bpy.data.objects:
    if o.type == 'MESH':
        d = o.dimensions
        print(f"ROADSIGN-DEBUG obj={o.name} dims=({d.x:.3f},{d.y:.3f},{d.z:.3f}) rot_euler={tuple(round(a,3) for a in o.rotation_euler)} verts={len(o.data.vertices)}")
