"""Reimport the rigged FBX into an empty scene and confirm Unity-readiness."""
import bpy
FBX="/Users/thapelopilanyane/My project/pigeon/meshy_pigeon_v2/PigeonV2_rigged.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)
arms=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
arm=arms[0]; mesh=meshes[0]
print("ARMATURES:",[a.name for a in arms]," bones:",len(arm.data.bones))
print("MESH:",mesh.name," verts:",len(mesh.data.vertices)," vgroups:",len(mesh.vertex_groups))
mods=[m.type for m in mesh.modifiers]
print("MESH modifiers:",mods)
roots=[b.name for b in arm.data.bones if b.parent is None]
print("ROOT bones:",roots)
r=arm.data.bones.get("Root")
if r: print("Root head(world)~",tuple(round(v,4) for v in (arm.matrix_world@r.head_local)))
print("armature obj loc:",tuple(round(v,4) for v in arm.location)," rot(deg):",tuple(round(__import__('math').degrees(v),2) for v in arm.rotation_euler)," scale:",tuple(round(v,4) for v in arm.scale))
# dims
c=[mesh.matrix_world@v.co for v in mesh.data.vertices]
import mathutils
mnz=min(p.z for p in c); mxz=max(p.z for p in c)
print("mesh height Z:",round(mxz-mnz,4)," lowest Z:",round(mnz,4))
# any unweighted?
unw=sum(1 for v in mesh.data.vertices if len(v.groups)==0)
print("unweighted verts:",unw)
print("DONE")
