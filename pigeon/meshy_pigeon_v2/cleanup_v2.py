"""Non-destructive cleanup + correct transform for the new Meshy pigeon.
Preserves geometry/appearance. Fixes: normals, scale-to-Unity, origin-at-feet,
centring. Saves .blend + textured FBX, renders validation frames. Run headless."""
import bpy, bmesh, math, os
from mathutils import Vector, kdtree

BASE = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon_v2"
FBX_IN  = BASE + "/raw/Meshy_AI_Create_a_realistic_lo_0806182151_texture.fbx"
BLEND   = BASE + "/PigeonV2_clean.blend"
FBX_OUT = BASE + "/PigeonV2_clean.fbx"
OUT     = BASE + "/validate_frames"
TARGET_HEIGHT = 0.30   # metres, top-of-head to feet
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX_IN)
obj = max([o for o in bpy.context.scene.objects if o.type=='MESH'],
          key=lambda m: len(m.data.vertices))
obj.name = "Pigeon"
bpy.context.view_layer.objects.active = obj
obj.select_set(True)

# --- 1. apply existing object transform (bake rot/scale to identity) ---
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

# --- 2. fix normals: recalculate consistently outward ---
bpy.ops.object.mode_set(mode='EDIT')
bm = bmesh.from_edit_mesh(obj.data)
for f in bm.faces: f.select = True
bmesh.update_edit_mesh(obj.data)
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.normals_make_consistent(inside=False)
# safety weld of exact-coincident verts only (won't move real geometry)
bpy.ops.mesh.remove_doubles(threshold=1e-6)
bpy.ops.object.mode_set(mode='OBJECT')

# --- 3. uniform scale to target height (preserves proportions) ---
def world_bounds():
    cs=[obj.matrix_world @ Vector(c) for c in obj.bound_box]
    mn=Vector((min(c.x for c in cs),min(c.y for c in cs),min(c.z for c in cs)))
    mx=Vector((max(c.x for c in cs),max(c.y for c in cs),max(c.z for c in cs)))
    return mn,mx
mn,mx = world_bounds()
h = (mx.z-mn.z)
factor = TARGET_HEIGHT / h
obj.scale = (factor,factor,factor)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# --- 4. origin to feet (bottom-centre), then sit on world origin ---
mn,mx = world_bounds()
feet = Vector(((mn.x+mx.x)*0.5, (mn.y+mx.y)*0.5, mn.z))
bpy.context.scene.cursor.location = feet
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
obj.location = (0.0, 0.0, 0.0)
bpy.context.view_layer.update()

# --- validation numbers ---
mn,mx = world_bounds()
size = mx-mn
print("\n================ VALIDATION ================")
print(f"  final size (X,Y,Z) = {tuple(round(v,4) for v in size)} m")
print(f"  height(Z)={size.z:.3f}  length(Y)={size.y:.3f}  wingspan(X)={size.x:.3f}")
print(f"  origin(world) = {tuple(round(v,5) for v in obj.matrix_world.translation)}")
print(f"  feet lowest Z = {mn.z:.5f}  (want 0)   centre X={(mn.x+mx.x)/2:.4f} Y={(mn.y+mx.y)/2:.4f}")
me = obj.data
print(f"  verts={len(me.vertices)}  tris={sum(len(p.vertices)-2 for p in me.polygons)}")

# symmetry via KDTree: for a sample of +X verts, distance to nearest mirrored(-X) vert
co=[v.co for v in me.vertices]
size_kd = kdtree.KDTree(len(co))
for i,c in enumerate(co): size_kd.insert(c,i)
size_kd.balance()
cx = sum(c.x for c in co)/len(co)
import random
random.seed(0)
samp=[c for c in co if c.x-cx>size.x*0.08]
random.shuffle(samp); samp=samp[:2000]
dists=[]
for c in samp:
    mc=Vector((2*cx-c.x, c.y, c.z))
    _,_,d = size_kd.find(mc)
    dists.append(d)
if dists:
    dists.sort()
    med=dists[len(dists)//2]; p90=dists[int(len(dists)*0.9)]
    print(f"  symmetry: median mirror-match dist={med*1000:.2f} mm, p90={p90*1000:.2f} mm "
          f"(vs body length {size.y*1000:.0f} mm)")

# --- save blend ---
bpy.ops.wm.save_as_mainfile(filepath=BLEND)
print("SAVED", BLEND)

# --- export FBX (project-standard axis, textures embedded) ---
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True); bpy.context.view_layer.objects.active=obj
bpy.ops.export_scene.fbx(
    filepath=FBX_OUT, use_selection=True, object_types={'MESH'},
    add_leaf_bones=False, apply_unit_scale=True, global_scale=1.0,
    apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
    use_mesh_modifiers=True, mesh_smooth_type='FACE',
    path_mode='COPY', embed_textures=True)
print("EXPORTED", FBX_OUT, os.path.getsize(FBX_OUT))

# --- validation renders (textured) ---
sc=bpy.context.scene; sc.render.engine='BLENDER_EEVEE'
sc.render.resolution_x=640; sc.render.resolution_y=640
w=bpy.data.worlds.new("w"); w.use_nodes=True
w.node_tree.nodes["Background"].inputs[0].default_value=(0.05,0.05,0.06,1); sc.world=w
sun=bpy.data.objects.new("sun",bpy.data.lights.new("s","SUN")); sc.collection.objects.link(sun)
sun.data.energy=4.0; sun.rotation_euler=(math.radians(55),0,math.radians(35))
cen=(mn+mx)*0.5; rad=max(size)*1.4
cd=bpy.data.cameras.new("c"); cam=bpy.data.objects.new("cam",cd)
sc.collection.objects.link(cam); sc.camera=cam
def shoot(name,dirv):
    d=Vector(dirv).normalized(); cd.type='ORTHO'; cd.ortho_scale=max(size)*1.25
    cam.location=cen+d*rad*2; cam.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=os.path.join(OUT,name); bpy.ops.render.render(write_still=True)
shoot("v_side.png",(1,0.05,0.10)); shoot("v_front.png",(0.05,-1,0.10))
shoot("v_threequarter.png",(1,-0.7,0.35))
print("DONE")
