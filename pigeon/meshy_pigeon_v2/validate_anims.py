"""Render key frames of several clips from the animated blend to validate
sign conventions + deformation. Read-only."""
import bpy, math, os
from mathutils import Vector
BASE="/Users/thapelopilanyane/My project/pigeon/meshy_pigeon_v2"
bpy.ops.wm.open_mainfile(filepath=BASE+"/PigeonV2_animated.blend")
arm=bpy.data.objects["PigeonArmature"]
OUT=BASE+"/anim_val"; os.makedirs(OUT,exist_ok=True)
sc=bpy.context.scene; sc.render.engine='BLENDER_EEVEE'; sc.render.resolution_x=440;sc.render.resolution_y=440
w=bpy.data.worlds.new("w");w.use_nodes=True;w.node_tree.nodes["Background"].inputs[0].default_value=(0.05,0.05,0.06,1);sc.world=w
sun=bpy.data.objects.new("sun",bpy.data.lights.new("s","SUN"));sc.collection.objects.link(sun)
sun.data.energy=4.0;sun.rotation_euler=(math.radians(55),0,math.radians(35))
cd=bpy.data.cameras.new("c");cam=bpy.data.objects.new("cam",cd);sc.collection.objects.link(cam);sc.camera=cam
cen=Vector((0,0,0.16))
def shoot(name,frame,action,dirv=(1,-0.55,0.32),scale=0.42):
    arm.animation_data.action=bpy.data.actions[action]
    sc.frame_set(frame); bpy.context.view_layer.update()
    d=Vector(dirv).normalized();cd.type='ORTHO';cd.ortho_scale=scale
    cam.location=cen+d*2;cam.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=os.path.join(OUT,name);bpy.ops.render.render(write_still=True);print(" ",name)
# side view for pitch/nod checks
S=(1,0.03,0.10)
shoot("walk_f8_side.png",8,"Walk",S); shoot("walk_f23_side.png",23,"Walk",S)
shoot("peck_down_f16_side.png",16,"Peck",S); shoot("peck_up_f34_side.png",34,"Peck",S)
shoot("alert_left_f15.png",15,"Alert"); shoot("alert_right_f34.png",34,"Alert")
shoot("fly_up_f1_front.png",1,"Fly",(0.03,-1,0.12),0.85); shoot("fly_down_f8_front.png",8,"Fly",(0.03,-1,0.12),0.85)
shoot("takeoff_crouch_f5_side.png",5,"Takeoff",S); shoot("takeoff_push_f12_side.png",12,"Takeoff",S); shoot("takeoff_up_f22.png",22,"Takeoff")
shoot("landing_cup_f8.png",8,"Landing"); shoot("landing_plant_f25_side.png",25,"Landing",S)
shoot("glide_f30_front.png",30,"Glide",(0.03,-1,0.12),0.85)
shoot("bankL_f14_front.png",14,"BankLeft",(0.03,-1,0.12),0.85)
shoot("hop_f7_side.png",7,"ShortHop",S)
shoot("flutter_f13_front.png",13,"FrightenedFlutter",(0.03,-1,0.12),0.85)
print("DONE")
