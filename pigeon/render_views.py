import bpy, math, os
from mathutils import Vector

path = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/pigeon.fbx"
outdir = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/views"
os.makedirs(outdir, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=path)
obj = [o for o in bpy.data.objects if o.type=='MESH'][0]

# neutral clay material so we read form, not texture
mat = bpy.data.materials.new("clay")
mat.use_nodes = True
bsdf = mat.node_tree.nodes.get("Principled BSDF")
bsdf.inputs["Base Color"].default_value = (0.7,0.7,0.72,1)
bsdf.inputs["Roughness"].default_value = 0.6
obj.data.materials.clear()
obj.data.materials.append(mat)

# center of bounds
me = obj.data
cos = [obj.matrix_world @ v.co for v in me.vertices]
cen = sum(cos, Vector())/len(cos)
size = Vector((max(c.x for c in cos)-min(c.x for c in cos),
               max(c.y for c in cos)-min(c.y for c in cos),
               max(c.z for c in cos)-min(c.z for c in cos)))
R = max(size)*1.4

# lighting
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun","SUN"))
bpy.context.collection.objects.link(sun)
sun.data.energy = 3
sun.rotation_euler = (math.radians(50), 0, math.radians(30))
bpy.context.scene.world = bpy.data.worlds.new("w")
bpy.context.scene.world.use_nodes = True
bpy.context.scene.world.node_tree.nodes["Background"].inputs[0].default_value=(0.05,0.05,0.06,1)

cam_data = bpy.data.cameras.new("cam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = max(size)*1.2
cam = bpy.data.objects.new("cam", cam_data)
bpy.context.collection.objects.link(cam)
bpy.context.scene.camera = cam

scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 500
scene.render.resolution_y = 500
scene.render.film_transparent = False

def look(cam, target, dir_vec):
    cam.location = target + dir_vec.normalized()*R*3
    d = -dir_vec.normalized()
    cam.rotation_euler = d.to_track_quat('-Z','Y').to_euler()

views = {
    "posY_from_+Y": Vector((0,1,0)),
    "negY_from_-Y": Vector((0,-1,0)),
    "posX_from_+X": Vector((1,0,0)),
    "posZ_from_+Z_top": Vector((0,0,1)),
    "iso": Vector((1,1,0.6)),
}
for nm, dv in views.items():
    look(cam, cen, dv)
    scene.render.filepath = os.path.join(outdir, nm+".png")
    bpy.ops.render.render(write_still=True)
    print("rendered", nm)
print("DONE")
