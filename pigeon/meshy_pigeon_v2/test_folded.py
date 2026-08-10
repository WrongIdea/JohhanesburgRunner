"""Quick test: find a believable FOLDED resting-wing pose for ground clips."""
import bpy, math, os
BASE="/Users/thapelopilanyane/My project/pigeon/meshy_pigeon_v2"
bpy.ops.wm.open_mainfile(filepath=BASE+"/PigeonV2_rigged.blend")
arm=bpy.data.objects["PigeonArmature"]
for pb in arm.pose.bones:
    for c in pb.constraints: c.mute=True

# candidate folded baselines (per-wing local euler deg): shoulder,upper,lower,wrist,tip
CANDS = {
 "D":[(-58,0,-15),(-18,0,-30),(-14,0,-72),(-10,0,-60),(-6,0,-35)],
 "E":[(-70,0,-10),(-22,0,-22),(-16,0,-85),(-12,0,-72),(-8,0,-42)],
 "F":[(-64,-8,-18),(-20,-5,-40),(-15,0,-80),(-11,0,-66),(-7,0,-40)],
}
WNAMES=["Shoulder","UpperWing","LowerWing","Wrist","WingTip"]
def setpose(vals):
    bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='POSE')
    for pb in arm.pose.bones: pb.rotation_mode='XYZ'; pb.rotation_euler=(0,0,0)
    for s in ("Left","Right"):
        for nm,(rx,ry,rz) in zip(WNAMES,vals):
            pb=arm.pose.bones[s+nm]; pb.rotation_euler=(math.radians(rx),math.radians(ry),math.radians(rz))
    bpy.ops.object.mode_set(mode='OBJECT'); bpy.context.view_layer.update()

sc=bpy.context.scene; sc.render.engine='BLENDER_EEVEE'; sc.render.resolution_x=480;sc.render.resolution_y=480
w=bpy.data.worlds.new("w");w.use_nodes=True;w.node_tree.nodes["Background"].inputs[0].default_value=(0.05,0.05,0.06,1);sc.world=w
sun=bpy.data.objects.new("sun",bpy.data.lights.new("s","SUN"));sc.collection.objects.link(sun)
sun.data.energy=4.0;sun.rotation_euler=(math.radians(55),0,math.radians(35))
from mathutils import Vector
cd=bpy.data.cameras.new("c");cam=bpy.data.objects.new("cam",cd);sc.collection.objects.link(cam);sc.camera=cam
cen=Vector((0,0,0.16))
os.makedirs(BASE+"/fold_test",exist_ok=True)
def shoot(name,dirv):
    d=Vector(dirv).normalized();cd.type='ORTHO';cd.ortho_scale=0.40
    cam.location=cen+d*2;cam.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=BASE+"/fold_test/"+name;bpy.ops.render.render(write_still=True)
for k,vals in CANDS.items():
    setpose(vals); shoot(f"{k}_3q.png",(1,-0.6,0.35)); shoot(f"{k}_top.png",(0,0.001,1))
print("DONE")
