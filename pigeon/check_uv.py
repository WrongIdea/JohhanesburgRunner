import bpy
for tag, path in [("ORIG","Assets/Art/Generated/Incoming/road_sign_new/Meshy_AI_Create_a_low_poly_mod_0802182911_texture_fbx/Meshy_AI_Create_a_low_poly_mod_0802182911_texture.fbx"),
                  ("REEXPORT","Assets/Art/Generated/Incoming/road_sign_new/road_sign_unity.fbx")]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath="/Users/thapelopilanyane/My project/"+path)
    for o in bpy.data.objects:
        if o.type=='MESH':
            uvs = [l.name for l in o.data.uv_layers]
            print(f"UVCHECK {tag} obj={o.name} uv_layers={uvs} verts={len(o.data.vertices)}")
