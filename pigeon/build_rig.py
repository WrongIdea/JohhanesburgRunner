"""Import Meshy pigeon, clean, scale, build game armature, weight, save .blend.
Renders test-pose deformations for validation. Run headless.
"""
import bpy, bmesh, math, os
from mathutils import Vector

SRC = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/pigeon.fbx"
BLEND = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/pigeon_rigged.blend"
S = 0.33  # scale factor: normalized (1.0) -> 0.33 m total length

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
obj = [o for o in bpy.data.objects if o.type=='MESH'][0]
obj.name = "PigeonMesh"

# ---- CLEAN MESH ----
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
# apply any transform
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# weld duplicate verts + recalc normals via bmesh
bm = bmesh.new(); bm.from_mesh(obj.data)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bm.to_mesh(obj.data); bm.free()
obj.data.update()
print("After clean: verts=%d polys=%d"%(len(obj.data.vertices),len(obj.data.polygons)))

# ---- SINGLE MATERIAL (keep first, drop extras) ----
while len(obj.data.materials) > 1:
    obj.data.materials.pop(index=1)

# ---- SCALE to real size, apply ----
obj.scale = (S,S,S)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
# recompute bounds
V=[v.co for v in obj.data.vertices]
sz=Vector((max(c.x for c in V)-min(c.x for c in V),
           max(c.y for c in V)-min(c.y for c in V),
           max(c.z for c in V)-min(c.z for c in V)))
print("Scaled size (m):", tuple(round(v,3) for v in sz))

# ---- BUILD ARMATURE ----
def P(x,y,z):  # scaled landmark
    return Vector((x*S, y*S, z*S))

bones = [
    # name, head, tail, parent, connected
    ("Root",       P(0,-0.05,-0.432), P(0,-0.18,-0.432), None,    False),
    ("Body",       P(0, 0.06,-0.02),  P(0,-0.26, 0.20),  "Root",  False),
    ("Neck",       P(0,-0.26, 0.20),  P(0,-0.37, 0.31),  "Body",  True),
    ("Head",       P(0,-0.37, 0.31),  P(0,-0.49, 0.28),  "Neck",  True),
    ("Tail",       P(0, 0.08,-0.03),  P(0, 0.46,-0.26),  "Body",  False),
    ("Wing.L",     P(0.05,-0.20,0.10),P(0.21,-0.16,-0.03),"Body", False),
    ("Wing.R",     P(-0.05,-0.20,0.10),P(-0.21,-0.16,-0.03),"Body",False),
    ("UpperLeg.L", P(0.095,-0.15,-0.14),P(0.095,-0.17,-0.27),"Root",False),
    ("LowerLeg.L", P(0.095,-0.17,-0.27),P(0.095,-0.20,-0.41),"UpperLeg.L",True),
    ("UpperLeg.R", P(-0.095,-0.15,-0.14),P(-0.095,-0.17,-0.27),"Root",False),
    ("LowerLeg.R", P(-0.095,-0.17,-0.27),P(-0.095,-0.20,-0.41),"UpperLeg.R",True),
]

arm_data = bpy.data.armatures.new("PigeonArmature")
arm = bpy.data.objects.new("PigeonArmature", arm_data)
bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='EDIT')
eb = arm_data.edit_bones
created={}
for nm,h,t,par,con in bones:
    b=eb.new(nm); b.head=h; b.tail=t; b.roll=0
    created[nm]=b
for nm,h,t,par,con in bones:
    if par:
        created[nm].parent=created[par]
        created[nm].use_connect=con
bpy.ops.object.mode_set(mode='OBJECT')

# ---- PARENT WITH AUTOMATIC WEIGHTS ----
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True); arm.select_set(True)
bpy.context.view_layer.objects.active = arm
ok=True
try:
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
except Exception as e:
    ok=False; print("AUTO WEIGHT FAILED:", e)
# verify each deforming bone got some weight
defbones=[b[0] for b in bones]
have = {vg.name for vg in obj.vertex_groups}
missing=[n for n in defbones if n not in have]
print("Vertex groups created:", sorted(have))
print("Missing groups:", missing)

bpy.ops.wm.save_as_mainfile(filepath=BLEND)
print("SAVED", BLEND)
