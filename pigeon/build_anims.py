"""Author 7 baked animations on the rigged pigeon. Saves pigeon_animated.blend
and renders one validation frame per action. Run headless."""
import bpy, math, os
from mathutils import Euler

RIGGED = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/pigeon_rigged.blend"
ANIM   = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/pigeon_animated.blend"
OUT    = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/anim_frames"
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=RIGGED)
arm = bpy.data.objects["PigeonArmature"]
obj = bpy.data.objects["PigeonMesh"]
bpy.context.scene.render.fps = 30

for pb in arm.pose.bones:
    pb.rotation_mode = 'XYZ'

def mirrorZ(track):
    """mirror a Wing.L track to Wing.R: negate rot Z, keep x,y; negate loc x."""
    out=[]
    for f,rot,loc in track:
        rx,ry,rz=rot
        nloc=None
        if loc: nloc=(-loc[0],loc[1],loc[2])
        out.append((f,(rx,ry,-rz),nloc))
    return out

# ---- Animation data: action -> { bone: [ (frame, (rx,ry,rz)deg, (lx,ly,lz)|None), ... ] } ----
A = {}

A["Idle"] = {  # 1..75 loop  breathing + head bob + tail + foot shuffle
 "Body":[(1,(0,0,0),(0,0,0)),(37,(1.5,0,0),(0,0,0.004)),(75,(0,0,0),(0,0,0))],
 "Neck":[(1,(0,0,0),None),(18,(5,0,0),None),(40,(-2,0,2),None),(60,(3,0,0),None),(75,(0,0,0),None)],
 "Head":[(1,(0,0,0),None),(18,(-3,0,0),None),(40,(2,0,-2),None),(60,(-2,0,0),None),(75,(0,0,0),None)],
 "Tail":[(1,(0,0,0),None),(37,(-3,0,0),None),(75,(0,0,0),None)],
 "UpperLeg.R":[(1,(0,0,0),None),(45,(6,0,0),None),(55,(6,0,0),None),(65,(0,0,0),None),(75,(0,0,0),None)],
 "LowerLeg.R":[(1,(0,0,0),None),(45,(-8,0,0),None),(55,(-8,0,0),None),(65,(0,0,0),None),(75,(0,0,0),None)],
}

A["Walk"] = {  # 1..30 loop, two steps
 "Neck":[(1,(2,0,0),None),(8,(10,0,0),None),(15,(2,0,0),None),(23,(10,0,0),None),(30,(2,0,0),None)],
 "Head":[(1,(-1,0,0),None),(8,(-6,0,0),None),(15,(-1,0,0),None),(23,(-6,0,0),None),(30,(-1,0,0),None)],
 "UpperLeg.L":[(1,(-14,0,0),None),(8,(4,0,0),None),(15,(20,0,0),None),(23,(4,0,0),None),(30,(-14,0,0),None)],
 "LowerLeg.L":[(1,(-6,0,0),None),(8,(-10,0,0),None),(15,(-34,0,0),None),(23,(-14,0,0),None),(30,(-6,0,0),None)],
 "UpperLeg.R":[(1,(20,0,0),None),(8,(4,0,0),None),(15,(-14,0,0),None),(23,(4,0,0),None),(30,(20,0,0),None)],
 "LowerLeg.R":[(1,(-34,0,0),None),(8,(-14,0,0),None),(15,(-6,0,0),None),(23,(-10,0,0),None),(30,(-34,0,0),None)],
 "Wing.L":[(1,(0,0,0),None),(15,(0,0,-5),None),(30,(0,0,0),None)],
 "Wing.R":[(1,(0,0,0),None),(15,(0,0,5),None),(30,(0,0,0),None)],
 "Tail":[(1,(0,0,0),None),(8,(2,0,0),None),(15,(0,0,0),None),(23,(2,0,0),None),(30,(0,0,0),None)],
 "Body":[(1,(0,0,0),(0,0,0)),(8,(0,0,0),(0,0,-0.003)),(15,(0,0,0),(0,0,0)),(23,(0,0,0),(0,0,-0.003)),(30,(0,0,0),(0,0,0))],
}

A["Peck"] = {  # 1..30 look down, quick jab, return
 "Neck":[(1,(0,0,0),None),(9,(42,0,0),None),(20,(42,0,0),None),(27,(10,0,0),None),(30,(0,0,0),None)],
 "Head":[(1,(0,0,0),None),(9,(30,0,0),None),(12,(48,0,0),None),(15,(30,0,0),None),(20,(30,0,0),None),(27,(6,0,0),None),(30,(0,0,0),None)],
 "Tail":[(1,(0,0,0),None),(9,(-6,0,0),None),(30,(0,0,0),None)],
 "Body":[(1,(0,0,0),None),(9,(3,0,0),None),(30,(0,0,0),None)],
}

A["Hop"] = {  # 1..15 small jump
 "Root":[(1,(0,0,0),(0,0,0)),(3,(0,0,0),(0,0,-0.008)),(7,(0,0,0),(0,0,0.035)),(10,(0,0,0),(0,0,0.030)),(13,(0,0,0),(0,0,-0.004)),(15,(0,0,0),(0,0,0))],
 "Body":[(1,(0,0,0),None),(7,(-6,0,0),None),(15,(0,0,0),None)],
 "UpperLeg.L":[(1,(0,0,0),None),(4,(20,0,0),None),(7,(28,0,0),None),(11,(28,0,0),None),(13,(6,0,0),None),(15,(0,0,0),None)],
 "LowerLeg.L":[(1,(0,0,0),None),(4,(-20,0,0),None),(7,(-38,0,0),None),(11,(-38,0,0),None),(13,(-8,0,0),None),(15,(0,0,0),None)],
 "UpperLeg.R":[(1,(0,0,0),None),(4,(20,0,0),None),(7,(28,0,0),None),(11,(28,0,0),None),(13,(6,0,0),None),(15,(0,0,0),None)],
 "LowerLeg.R":[(1,(0,0,0),None),(4,(-20,0,0),None),(7,(-38,0,0),None),(11,(-38,0,0),None),(13,(-8,0,0),None),(15,(0,0,0),None)],
 "Wing.L":[(1,(0,0,0),None),(5,(0,0,-28),None),(9,(0,0,-4),None),(13,(0,0,-12),None),(15,(0,0,0),None)],
 "Tail":[(1,(0,0,0),None),(7,(-6,0,0),None),(15,(0,0,0),None)],
}
A["Hop"]["Wing.R"]=mirrorZ(A["Hop"]["Wing.L"])

A["Takeoff"] = {  # 1..30 rapid flaps + push
 "Root":[(1,(0,0,0),(0,0,0)),(5,(0,0,0),(0,0,-0.008)),(10,(0,0,0),(0,0,0.02)),(20,(0,0,0),(0,0,0.045)),(30,(0,0,0),(0,0,0.06))],
 "Body":[(1,(0,0,0),None),(8,(-12,0,0),None),(30,(-9,0,0),None)],
 "Wing.L":[(1,(0,0,15),None),(5,(0,0,-72),None),(9,(0,0,15),None),(13,(0,0,-72),None),(17,(0,0,10),None),(21,(0,0,-68),None),(25,(0,0,5),None),(30,(0,0,-30),None)],
 "UpperLeg.L":[(1,(28,0,0),None),(6,(-10,0,0),None),(12,(40,0,0),None),(30,(42,0,0),None)],
 "LowerLeg.L":[(1,(-40,0,0),None),(6,(5,0,0),None),(12,(-50,0,0),None),(30,(-52,0,0),None)],
 "UpperLeg.R":[(1,(28,0,0),None),(6,(-10,0,0),None),(12,(40,0,0),None),(30,(42,0,0),None)],
 "LowerLeg.R":[(1,(-40,0,0),None),(6,(5,0,0),None),(12,(-50,0,0),None),(30,(-52,0,0),None)],
 "Tail":[(1,(0,0,0),None),(10,(10,0,0),None),(30,(8,0,0),None)],
}
A["Takeoff"]["Wing.R"]=mirrorZ(A["Takeoff"]["Wing.L"])

A["Glide"] = {  # 1..30 loop, wings extended
 "Wing.L":[(1,(0,0,-48),None),(8,(3,0,-50),None),(15,(0,0,-53),None),(23,(-3,0,-50),None),(30,(0,0,-48),None)],
 "Body":[(1,(-3,0,0),None),(15,(-4,0,0),None),(30,(-3,0,0),None)],
 "Tail":[(1,(4,0,0),None),(15,(6,0,0),None),(30,(4,0,0),None)],
 "UpperLeg.L":[(1,(38,0,0),None),(30,(38,0,0),None)],
 "LowerLeg.L":[(1,(-46,0,0),None),(30,(-46,0,0),None)],
 "UpperLeg.R":[(1,(38,0,0),None),(30,(38,0,0),None)],
 "LowerLeg.R":[(1,(-46,0,0),None),(30,(-46,0,0),None)],
}
A["Glide"]["Wing.R"]=mirrorZ(A["Glide"]["Wing.L"])

A["Landing"] = {  # 1..30 brake + reach + settle to rest
 "Wing.L":[(1,(0,0,-46),None),(8,(0,0,-82),None),(16,(0,0,-60),None),(24,(0,0,-28),None),(30,(0,0,0),None)],
 "UpperLeg.L":[(1,(38,0,0),None),(8,(20,0,0),None),(16,(-5,0,0),None),(22,(2,0,0),None),(30,(0,0,0),None)],
 "LowerLeg.L":[(1,(-46,0,0),None),(8,(-30,0,0),None),(16,(8,0,0),None),(22,(-4,0,0),None),(30,(0,0,0),None)],
 "UpperLeg.R":[(1,(38,0,0),None),(8,(20,0,0),None),(16,(-5,0,0),None),(22,(2,0,0),None),(30,(0,0,0),None)],
 "LowerLeg.R":[(1,(-46,0,0),None),(8,(-30,0,0),None),(16,(8,0,0),None),(22,(-4,0,0),None),(30,(0,0,0),None)],
 "Body":[(1,(-12,0,0),None),(8,(-14,0,0),None),(16,(4,0,0),None),(30,(0,0,0),None)],
 "Root":[(1,(0,0,0),(0,0,0.05)),(8,(0,0,0),(0,0,0.03)),(16,(0,0,0),(0,0,0.005)),(22,(0,0,0),(0,0,-0.004)),(30,(0,0,0),(0,0,0))],
 "Tail":[(1,(8,0,0),None),(8,(18,0,0),None),(16,(10,0,0),None),(30,(0,0,0),None)],
}
A["Landing"]["Wing.R"]=mirrorZ(A["Landing"]["Wing.L"])

RANGES={"Idle":(1,75),"Walk":(1,30),"Peck":(1,30),"Hop":(1,15),"Takeoff":(1,30),"Glide":(1,30),"Landing":(1,30)}

if not arm.animation_data:
    arm.animation_data_create()

for name, tracks in A.items():
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    s0,e0=RANGES[name]
    # reset pose, then key EVERY bone to rest at start+end so each clip is
    # fully self-contained (no pose leaking between actions / into FBX bake)
    for pb in arm.pose.bones:
        pb.rotation_euler=(0,0,0); pb.location=(0,0,0)
        for f in (s0,e0):
            pb.keyframe_insert('rotation_euler', frame=f)
        if pb.name in ("Root","Body"):
            for f in (s0,e0):
                pb.keyframe_insert('location', frame=f)
    for bone, keys in tracks.items():
        pb = arm.pose.bones[bone]
        for f,rot,loc in keys:
            pb.rotation_euler = Euler((math.radians(rot[0]),math.radians(rot[1]),math.radians(rot[2])),'XYZ')
            pb.keyframe_insert('rotation_euler', frame=f)
            if loc is not None:
                pb.location = loc
                pb.keyframe_insert('location', frame=f)
    # frame range is computed from keyframes; also set manual range for exporter
    s,e=RANGES[name]
    try:
        act.use_frame_range=True; act.frame_start=s; act.frame_end=e
    except Exception as ex:
        print("  (frame_range set skipped:",ex,")")
    print(f"built action {name} frames {s}-{e} range={tuple(round(v,1) for v in act.frame_range)}")

bpy.ops.wm.save_as_mainfile(filepath=ANIM)
print("SAVED", ANIM)

# ---- VALIDATION RENDERS ----
mat=bpy.data.materials.new("clay"); mat.use_nodes=True
b=mat.node_tree.nodes["Principled BSDF"]
b.inputs["Base Color"].default_value=(0.72,0.72,0.75,1); b.inputs["Roughness"].default_value=0.6
obj.data.materials.clear(); obj.data.materials.append(mat)
sun=bpy.data.objects.new("sun",bpy.data.lights.new("s","SUN")); bpy.context.collection.objects.link(sun)
sun.data.energy=3.5; sun.rotation_euler=(math.radians(55),0,math.radians(35))
w=bpy.data.worlds.new("w"); w.use_nodes=True; w.node_tree.nodes["Background"].inputs[0].default_value=(0.04,0.04,0.05,1)
bpy.context.scene.world=w
sc=bpy.context.scene; sc.render.engine='BLENDER_EEVEE'; sc.render.resolution_x=440; sc.render.resolution_y=440
cd=bpy.data.cameras.new("c"); cd.type='ORTHO'; cd.ortho_scale=0.46
cam=bpy.data.objects.new("cam",cd); bpy.context.collection.objects.link(cam); sc.camera=cam
from mathutils import Vector
cen=Vector((0,-0.02,0.02)); dv=Vector((1,0.25,0.15))
cam.location=cen+dv.normalized()*2; cam.rotation_euler=(-dv.normalized()).to_track_quat('-Z','Y').to_euler()

checkframes={"Idle":18,"Walk":15,"Peck":12,"Hop":7,"Takeoff":5,"Glide":15,"Landing":8}
for name in A:
    arm.animation_data.action=bpy.data.actions[name]
    sc.frame_set(checkframes[name])
    sc.render.filepath=os.path.join(OUT,f"{name}_f{checkframes[name]}.png")
    bpy.ops.render.render(write_still=True)
    print("rendered",name)
print("DONE")
