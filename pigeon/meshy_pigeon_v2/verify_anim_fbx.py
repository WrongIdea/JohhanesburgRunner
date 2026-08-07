"""Reimport the animations FBX and confirm all 12 takes baked with ranges."""
import bpy
FBX="/Users/thapelopilanyane/My project/pigeon/meshy_pigeon_v2/PigeonV2_animations.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, use_anim=True)
arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0]
mesh=[o for o in bpy.context.scene.objects if o.type=='MESH'][0]
print("bones:",len(arm.data.bones)," mesh verts:",len(mesh.data.vertices)," vgroups:",len(mesh.vertex_groups))
acts=sorted(bpy.data.actions, key=lambda a:a.name)
print("ACTIONS:",len(acts))
for a in acts:
    fr=a.frame_range
    print(f"  {a.name:28s} frames {fr[0]:.0f}..{fr[1]:.0f}  ({(fr[1]-fr[0]):.0f} intervals = {(fr[1]-fr[0])/30:.2f}s)")
# root transform sanity: any X/Y translation on Root bone fcurves?
print("root translation check:")
for a in bpy.data.actions:
    for fc in a.fcurves:
        if 'Root' in fc.data_path and fc.data_path.endswith('location') and fc.array_index in (0,1):
            vals=[abs(k.co[1]) for k in fc.keyframe_points]
            if max(vals)>1e-4:
                print(f"   {a.name}: Root loc[{fc.array_index}] max {max(vals):.4f}  (nonzero horizontal!)")
print("  (no lines above = root has no horizontal translation -> in-place OK)")
print("DONE")
