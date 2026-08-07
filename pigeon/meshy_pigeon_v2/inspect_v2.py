"""Non-destructive inspection of the new Meshy pigeon FBX.
Imports, reports transform/geometry/topology/UV/material facts, renders
multi-angle validation frames. Makes NO edits to the mesh. Run headless."""
import bpy, bmesh, math, os
from mathutils import Vector

BASE = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon_v2"
FBX  = BASE + "/raw/Meshy_AI_Create_a_realistic_lo_0806182151_texture.fbx"
OUT  = BASE + "/inspect_frames"
os.makedirs(OUT, exist_ok=True)

# ---- clean slate ----
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)

meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
arms   = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
empties= [o for o in bpy.context.scene.objects if o.type == 'EMPTY']
print("\n================ IMPORT ================")
print("mesh objects :", [m.name for m in meshes])
print("armatures    :", [a.name for a in arms])
print("empties      :", [e.name for e in empties])

obj = max(meshes, key=lambda m: len(m.data.vertices))  # main body

# ---- TRANSFORM ----
print("\n================ TRANSFORM ================")
for m in meshes:
    print(f"  {m.name}: loc={tuple(round(v,4) for v in m.location)} "
          f"rot_deg={tuple(round(math.degrees(v),2) for v in m.rotation_euler)} "
          f"scale={tuple(round(v,4) for v in m.scale)}")

# world-space bounds across all meshes
allc = []
for m in meshes:
    for c in m.bound_box:
        allc.append(m.matrix_world @ Vector(c))
mn = Vector((min(c.x for c in allc), min(c.y for c in allc), min(c.z for c in allc)))
mx = Vector((max(c.x for c in allc), max(c.y for c in allc), max(c.z for c in allc)))
size = mx - mn
print(f"  world bounds min = {tuple(round(v,4) for v in mn)}")
print(f"  world bounds max = {tuple(round(v,4) for v in mx)}")
print(f"  world size (X,Y,Z) = {tuple(round(v,4) for v in size)}  [metres]")
print(f"  --> width(X)={size.x:.3f}  depth(Y)={size.y:.3f}  height(Z)={size.z:.3f}")
print(f"  origin of main obj (world) = {tuple(round(v,4) for v in obj.matrix_world.translation)}")
print(f"  lowest point Z = {mn.z:.4f}  (feet-on-ground wants ~0)")
# guess longest horizontal axis = body length; tallest = height
horiz = sorted([('X',size.x),('Y',size.y)], key=lambda t:-t[1])
print(f"  longest horizontal axis = {horiz[0][0]} ({horiz[0][1]:.3f} m) -> likely body length")
print(f"  target: ~0.30 m tall, ~0.35 m long")

# ---- GEOMETRY (bmesh, read-only copy) ----
print("\n================ GEOMETRY ================")
def geom_report(o):
    me = o.data
    bm = bmesh.new(); bm.from_mesh(me)
    bm.verts.ensure_lookup_table(); bm.edges.ensure_lookup_table(); bm.faces.ensure_lookup_table()
    nverts = len(bm.verts); nedges = len(bm.edges); nfaces = len(bm.faces)
    tris = sum(len(f.verts)-2 for f in bm.faces)
    ngons = sum(1 for f in bm.faces if len(f.verts) > 4)
    quads = sum(1 for f in bm.faces if len(f.verts) == 4)
    tri_faces = sum(1 for f in bm.faces if len(f.verts) == 3)
    # non-manifold edges (not exactly 2 faces)
    nonman_e = sum(1 for e in bm.edges if not e.is_manifold)
    boundary_e = sum(1 for e in bm.edges if e.is_boundary)
    wire_e = sum(1 for e in bm.edges if len(e.link_faces) == 0)
    # loose verts
    loose_v = sum(1 for v in bm.verts if len(v.link_edges) == 0)
    # flipped/consistent normals: count edges where linked faces disagree
    flipped_pairs = 0
    for e in bm.edges:
        if len(e.link_faces) == 2:
            if e.link_faces[0].normal.dot(e.link_faces[1].normal) < -0.5:
                flipped_pairs += 1
    # duplicate verts within 0.0001
    kd_dupes = 0
    import mathutils
    size_bb = (o.dimensions)
    merge_dist = max(1e-5, min([d for d in size_bb if d>0]) * 0.001)
    # cheap dupe check via dict rounding
    seen = {}
    for v in bm.verts:
        key = (round(v.co.x,5), round(v.co.y,5), round(v.co.z,5))
        seen[key] = seen.get(key,0)+1
    kd_dupes = sum(c-1 for c in seen.values() if c>1)
    # loose parts (islands) via face walk
    visited = set(); islands = 0
    for f in bm.faces:
        if f.index in visited: continue
        islands += 1; stack=[f]
        while stack:
            cf = stack.pop()
            if cf.index in visited: continue
            visited.add(cf.index)
            for e in cf.edges:
                for lf in e.link_faces:
                    if lf.index not in visited: stack.append(lf)
    bm.free()
    print(f"  [{o.name}]")
    print(f"    verts={nverts}  edges={nedges}  faces={nfaces}  TRIS={tris}")
    print(f"    face mix: tris={tri_faces} quads={quads} ngons(>4)={ngons}")
    print(f"    non-manifold edges = {nonman_e}   boundary(open) edges = {boundary_e}   wire edges = {wire_e}")
    print(f"    loose verts = {loose_v}   duplicate-position verts(<1e-5) = {kd_dupes}")
    print(f"    normal-disagreement edge pairs = {flipped_pairs}")
    print(f"    connected surface islands = {islands}")
    return dict(tris=tris, verts=nverts, ngons=ngons, nonman=nonman_e,
                boundary=boundary_e, loose=loose_v, dupes=kd_dupes,
                islands=islands, flipped=flipped_pairs)
stats = {m.name: geom_report(m) for m in meshes}

# ---- SYMMETRY (X mirror test) ----
print("\n================ SYMMETRY (X) ================")
def symmetry(o, tol=None):
    me = o.data
    co = [o.matrix_world @ v.co for v in me.vertices]
    xs = [c.x for c in co]
    cx = sum(xs)/len(xs)
    span = max(1e-6, max(xs)-min(xs))
    tol = tol or span*0.01
    # bucket by (y,z) rounded, look for mirrored x
    keys = {}
    for c in co:
        keys[(round(c.y,3), round(c.z,3), round(abs(c.x-cx),3))] = keys.get((round(c.y,3),round(c.z,3),round(abs(c.x-cx),3)),0)+1
    paired = sum(v for v in keys.values() if v>=2)
    frac = paired/len(co)
    print(f"  [{o.name}] center X≈{cx:.4f} span={span:.3f} tol={tol:.4f}  mirror-paired verts ≈ {frac*100:.1f}%")
symmetry(obj)

# ---- UV ----
print("\n================ UV ================")
for m in meshes:
    me = m.data
    if not me.uv_layers:
        print(f"  [{m.name}] NO UV layers"); continue
    uvl = me.uv_layers.active
    us=[]; vs=[]
    for loop in me.loops:
        uv = uvl.data[loop.index].uv
        us.append(uv.x); vs.append(uv.y)
    out01 = sum(1 for u in us if u<-0.001 or u>1.001) + sum(1 for v in vs if v<-0.001 or v>1.001)
    print(f"  [{m.name}] uv layers={[l.name for l in me.uv_layers]} "
          f"U[{min(us):.3f},{max(us):.3f}] V[{min(vs):.3f},{max(vs):.3f}] "
          f"loops-outside-0..1={out01}")

# ---- MATERIALS ----
print("\n================ MATERIALS ================")
for m in meshes:
    slots = [s.material.name if s.material else "<none>" for s in m.material_slots]
    print(f"  [{m.name}] materials = {slots}")
for mat in bpy.data.materials:
    imgs=[]
    if mat.use_nodes:
        for n in mat.node_tree.nodes:
            if n.type=='TEX_IMAGE' and n.image: imgs.append(n.image.name)
    print(f"  material '{mat.name}' textures={imgs}")
for img in bpy.data.images:
    if img.size[0]>0:
        print(f"  image '{img.name}' {img.size[0]}x{img.size[1]}")

# ---- RENDERS (clay + textured, multi-angle) ----
print("\n================ RENDER ================")
sc = bpy.context.scene
sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x = 640; sc.render.resolution_y = 640
sc.render.film_transparent = False
w = bpy.data.worlds.new("w"); w.use_nodes=True
w.node_tree.nodes["Background"].inputs[0].default_value=(0.05,0.05,0.06,1)
sc.world = w
sun = bpy.data.objects.new("sun", bpy.data.lights.new("s","SUN"))
sc.collection.objects.link(sun); sun.data.energy=4.0
sun.rotation_euler=(math.radians(55),0,math.radians(35))

cen = (mn+mx)*0.5
rad = max(size) * 1.4
cam_data = bpy.data.cameras.new("c"); cam = bpy.data.objects.new("cam", cam_data)
sc.collection.objects.link(cam); sc.camera = cam

def shoot(name, dirv, ortho=True):
    d = Vector(dirv).normalized()
    if ortho:
        cam_data.type='ORTHO'; cam_data.ortho_scale = max(size)*1.25
    cam.location = cen + d*rad*2
    cam.rotation_euler = (-d).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath = os.path.join(OUT, name)
    bpy.ops.render.render(write_still=True)
    print("  rendered", name)

shoot("side.png",   (1, 0.05, 0.10))
shoot("front.png",  (0.05, -1, 0.10))
shoot("top.png",    (0.0, 0.0, 1.0))
shoot("threequarter.png", (1, -0.7, 0.35))
shoot("back.png",   (0.05, 1, 0.10))
print("DONE")
