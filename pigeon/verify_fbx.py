import bpy
from mathutils import Vector
FBX="/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/PigeonHD.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)
print("\n== OBJECTS ==")
for o in bpy.data.objects:
    print(" ",o.type,o.name)
me=[o for o in bpy.data.objects if o.type=='MESH'][0]
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
print("mesh verts",len(me.data.verts) if hasattr(me.data,'verts') else len(me.data.vertices),"polys",len(me.data.polygons))
print("materials",[m.name for m in me.data.materials])
print("bones",[b.name for b in arm.data.bones])
V=[me.matrix_world@v.co for v in me.data.vertices]
sz=Vector((max(c.x for c in V)-min(c.x for c in V),max(c.y for c in V)-min(c.y for c in V),max(c.z for c in V)-min(c.z for c in V)))
print("world size (m)",tuple(round(v,3) for v in sz))
# max influences per vertex
maxinf=max(len(v.groups) for v in me.data.vertices)
print("max influences/vertex",maxinf)
print("\n== ACTIONS (clips) ==")
for a in bpy.data.actions:
    print(f"  {a.name:10} range {tuple(round(v,1) for v in a.frame_range)}")
print("num actions",len(bpy.data.actions))
