"""Temporary pose tests on the rigged pigeon to check deformation. Renders each
pose; NEVER saves. Poses are cleared between tests. Run headless."""
import bpy, math, os
from mathutils import Vector

BASE="/Users/thapelopilanyane/My project/pigeon/meshy_pigeon_v2"
BLEND=BASE+"/PigeonV2_rigged.blend"; OUT=BASE+"/pose_frames"; os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=BLEND)
arm=bpy.data.objects["PigeonArmature"]; mesh=bpy.data.objects["PigeonMesh"]

# temporarily MUTE limit-rotation constraints so tests can reach extreme poses
for pb in arm.pose.bones:
    for c in pb.constraints: c.mute=True

def clear():
    bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='POSE')
    for pb in arm.pose.bones:
        pb.rotation_mode='XYZ'; pb.rotation_euler=(0,0,0); pb.location=(0,0,0)
    bpy.ops.object.mode_set(mode='OBJECT')

def pose(d):
    clear(); bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='POSE')
    for name,(rx,ry,rz) in d.items():
        pb=arm.pose.bones.get(name)
        if pb: pb.rotation_mode='XYZ'; pb.rotation_euler=(math.radians(rx),math.radians(ry),math.radians(rz))
    bpy.ops.object.mode_set(mode='OBJECT'); bpy.context.view_layer.update()

# --- scene: lighting + world + camera ---
sc=bpy.context.scene; sc.render.engine='BLENDER_EEVEE'; sc.render.resolution_x=560; sc.render.resolution_y=560
w=bpy.data.worlds.new("w"); w.use_nodes=True; w.node_tree.nodes["Background"].inputs[0].default_value=(0.05,0.05,0.06,1); sc.world=w
sun=bpy.data.objects.new("sun",bpy.data.lights.new("s","SUN")); sc.collection.objects.link(sun)
sun.data.energy=4.0; sun.rotation_euler=(math.radians(55),0,math.radians(35))
cd=bpy.data.cameras.new("c"); cam=bpy.data.objects.new("cam",cd); sc.collection.objects.link(cam); sc.camera=cam
cen=Vector((0,0,0.16))
def shoot(name,dirv,scale=0.42):
    d=Vector(dirv).normalized(); cd.type='ORTHO'; cd.ortho_scale=scale
    cam.location=cen+d*2; cam.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=os.path.join(OUT,name); bpy.ops.render.render(write_still=True); print("  ",name)

# mirror helper: same local angle on Left/Right gives symmetric world motion (roll=0, X-mirrored)
def wings(shoulder,upper,lower,wrist,tip):
    d={}
    for s in ("Left","Right"):
        d[s+"Shoulder"]=shoulder; d[s+"UpperWing"]=upper; d[s+"LowerWing"]=lower
        d[s+"Wrist"]=wrist; d[s+"WingTip"]=tip
    return d

# ===== POSE TESTS =====
# rest control
clear(); shoot("00_rest_3q.png",(1,-0.7,0.4)); shoot("00_rest_front.png",(0.03,-1,0.12))

# 1. wings fully EXTENDED + flapped UP (rotate about local X raises/lowers tip)
pose(wings((25,0,0),(20,0,0),(15,0,0),(10,0,0),(8,0,0)))
shoot("1_extended_up_front.png",(0.03,-1,0.12)); shoot("1_extended_up_3q.png",(1,-0.7,0.4))

# 2. wings HALF-open (sweep back+down partly): local Z sweeps in plane, X drops
pose(wings((-20,0,-30),(-10,0,-35),(-8,0,-45),(-6,0,-40),(-4,0,-25)))
shoot("2_half_top.png",(0,0.001,1)); shoot("2_half_3q.png",(1,-0.7,0.4))

# 3. wings FULLY FOLDED (tuck along body)
pose(wings((-35,0,-55),(-18,0,-60),(-12,0,-80),(-8,0,-70),(-6,0,-45)))
shoot("3_folded_top.png",(0,0.001,1)); shoot("3_folded_side.png",(1,0.03,0.10))

# 4. WALK (legs swing fore/aft, knees bend) + slight body lean
pose({"LeftLeg":(-25,0,0),"LeftShin":(35,0,0),"LeftAnkle":(-20,0,0),
      "RightLeg":(25,0,0),"RightShin":(15,0,0),"RightAnkle":(-10,0,0),"Body":(4,0,0)})
shoot("4_walk_side.png",(1,0.03,0.10)); shoot("4_walk_3q.png",(1,-0.7,0.4))

# 5. TAIL up + spread-ish (single bone: raise + slight twist)
pose({"Tail":(-35,0,0)})
shoot("5_tail_side.png",(1,0.03,0.10)); shoot("5_tail_top.png",(0,0.001,1))

# 6. NECK + HEAD rotation (turn/look)
pose({"Neck":(0,0,40),"Head":(10,15,25)})
shoot("6_neckhead_3q.png",(1,-0.7,0.4)); shoot("6_neckhead_top.png",(0,0.001,1))

clear()
print("POSE TESTS DONE (not saved)")
