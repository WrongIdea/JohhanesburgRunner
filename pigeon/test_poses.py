import bpy, math, os
from mathutils import Vector, Euler

BLEND = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/pigeon_rigged.blend"
OUT = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/poses"
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=BLEND)

obj=bpy.data.objects["PigeonMesh"]
arm=bpy.data.objects["PigeonArmature"]

# clay material
mat=bpy.data.materials.new("clay"); mat.use_nodes=True
b=mat.node_tree.nodes["Principled BSDF"]
b.inputs["Base Color"].default_value=(0.72,0.72,0.75,1); b.inputs["Roughness"].default_value=0.6
obj.data.materials.clear(); obj.data.materials.append(mat)

# lighting/world
sun=bpy.data.objects.new("sun",bpy.data.lights.new("s","SUN")); bpy.context.collection.objects.link(sun)
sun.data.energy=3.5; sun.rotation_euler=(math.radians(55),0,math.radians(35))
w=bpy.data.worlds.new("w"); w.use_nodes=True; w.node_tree.nodes["Background"].inputs[0].default_value=(0.04,0.04,0.05,1)
bpy.context.scene.world=w
sc=bpy.context.scene; sc.render.engine='BLENDER_EEVEE'; sc.render.resolution_x=480; sc.render.resolution_y=480

# camera
cd=bpy.data.cameras.new("c"); cd.type='ORTHO'; cd.ortho_scale=0.42
cam=bpy.data.objects.new("cam",cd); bpy.context.collection.objects.link(cam); sc.camera=cam
cen=Vector((0,-0.02,0.0))
def look(dir_vec):
    cam.location=cen+dir_vec.normalized()*2
    cam.rotation_euler=(-dir_vec.normalized()).to_track_quat('-Z','Y').to_euler()

def setpose(rots):
    bpy.context.view_layer.objects.active=arm
    bpy.ops.object.mode_set(mode='POSE')
    for pb in arm.pose.bones:
        pb.rotation_mode='XYZ'; pb.rotation_euler=(0,0,0); pb.location=(0,0,0)
    for name,(rx,ry,rz) in rots.items():
        pb=arm.pose.bones[name]
        pb.rotation_euler=Euler((math.radians(rx),math.radians(ry),math.radians(rz)),'XYZ')
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.view_layer.update()

poses={
 "rest": {},
 "wings_up": {"Wing.L":(0,0,-70),"Wing.R":(0,0,70)},
 "legs_bend": {"UpperLeg.L":(35,0,0),"LowerLeg.L":(-45,0,0),"UpperLeg.R":(35,0,0),"LowerLeg.R":(-45,0,0)},
 "head_down": {"Neck":(40,0,0),"Head":(30,0,0)},
 "tail_up": {"Tail":(-35,0,0)},
}
for nm,rots in poses.items():
    setpose(rots)
    for vlabel,dv in [("side",Vector((1,0,0))),("iso",Vector((1,1,0.5)))]:
        look(dv)
        sc.render.filepath=os.path.join(OUT,f"{nm}_{vlabel}.png")
        bpy.ops.render.render(write_still=True)
    print("posed",nm)
print("DONE")
