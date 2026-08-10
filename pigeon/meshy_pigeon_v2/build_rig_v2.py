"""Build a lightweight production bird rig on the cleaned pigeon and skin it.
Hierarchy per spec (Root hub; Neck>Head; each Wing & Leg a chain). Perfect
X-symmetry (left built, right mirrored). Robust distance-based skinning (bone
heat fails on this dense Meshy soup) + smoothing. Limit-Rotation constraints on
wing joints. Saves rig .blend (REST pose) + Unity FBX. No animation. Headless."""
import bpy, math, numpy as np
from mathutils import Vector

BASE  = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon_v2"
BLEND_IN = BASE + "/PigeonV2_clean.blend"
BLEND_OUT= BASE + "/PigeonV2_rigged.blend"
FBX_OUT  = BASE + "/PigeonV2_rigged.fbx"

bpy.ops.wm.open_mainfile(filepath=BLEND_IN)
mesh = max([o for o in bpy.context.scene.objects if o.type=='MESH'], key=lambda m: len(m.data.vertices))
mesh.name = "PigeonMesh"

# ---------------- bone table (world space, forward=-Y up=+Z) ----------------
CENTER = [
 ("Root", (0,0,0),               (0,-0.05,0.0),        None,   False),
 ("Body", (0,0.055,0.105),       (0,-0.06,0.150),      "Root", False),
 ("Neck", (0,-0.055,0.155),      (0,-0.100,0.235),     "Root", False),
 ("Head", (0,-0.100,0.235),      (0,-0.165,0.255),     "Neck", True),
 ("Tail", (0,0.060,0.100),       (0,0.168,0.020),      "Root", False),
]
WING = [
 ("Wing",      (0.025,0.000,0.135), (0.055,0.000,0.128), None,        False),
 ("Shoulder",  (0.055,0.000,0.128), (0.100,-0.006,0.150),"Wing",      True),
 ("UpperWing", (0.100,-0.006,0.150),(0.160,-0.012,0.172),"Shoulder",  True),
 ("LowerWing", (0.160,-0.012,0.172),(0.240,-0.020,0.196),"UpperWing", True),
 ("Wrist",     (0.240,-0.020,0.196),(0.310,-0.016,0.200),"LowerWing", True),
 ("WingTip",   (0.310,-0.016,0.200),(0.382,-0.028,0.207),"Wrist",     True),
]
LEG = [
 ("Leg",   (0.036,-0.020,0.135), (0.036,0.000,0.085), None,   False),
 ("Shin",  (0.036,0.000,0.085),  (0.036,0.030,0.045), "Leg",  True),
 ("Ankle", (0.036,0.030,0.045),  (0.036,0.045,0.014), "Shin", True),
 ("Foot",  (0.036,0.045,0.014),  (0.036,0.005,0.004), "Ankle",True),
]
def mir(p): return (-p[0], p[1], p[2])

bones = []
for n,h,t,p,c in CENTER: bones.append((n,h,t,p,c))
for side,sgn in (("Left",1),("Right",-1)):
    for suf,h,t,par,c in WING+LEG:
        bones.append((side+suf, h if sgn>0 else mir(h), t if sgn>0 else mir(t),
                      (side+par) if par else "Root", c))

# ---------------- create armature ----------------
arm_data = bpy.data.armatures.new("PigeonArmature")
arm = bpy.data.objects.new("PigeonArmature", arm_data)
bpy.context.scene.collection.objects.link(arm); arm.location=(0,0,0)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='EDIT')
eb = arm_data.edit_bones; created={}
for name,h,t,parent,connect in bones:
    b=eb.new(name); b.head=Vector(h); b.tail=Vector(t); b.roll=0.0; created[name]=b
for name,h,t,parent,connect in bones:
    if parent: created[name].parent=created[parent]; created[name].use_connect=connect
for b in eb: b.use_deform=True
bpy.ops.object.mode_set(mode='OBJECT')
print("BONES:", len(arm_data.bones))

# ---------------- distance-based skinning (numpy) ----------------
# per-vertex distance to each bone SEGMENT; weight = (1/d)^power over K nearest.
segA = np.array([b[1] for b in bones], dtype=np.float64)   # (B,3) heads
segB = np.array([b[2] for b in bones], dtype=np.float64)   # (B,3) tails
names = [b[0] for b in bones]
B = len(bones)
P = np.array([ (mesh.matrix_world @ v.co)[:] for v in mesh.data.vertices ], dtype=np.float64)  # (N,3)
N = P.shape[0]
AB = segB - segA                      # (B,3)
ab2 = np.einsum('bi,bi->b', AB, AB)   # (B,)
ab2[ab2 < 1e-12] = 1e-12
# distance matrix D (N,B)
D = np.empty((N,B), dtype=np.float64)
for j in range(B):
    AP = P - segA[j]                          # (N,3)
    t = (AP @ AB[j]) / ab2[j]                  # (N,)
    t = np.clip(t, 0.0, 1.0)
    proj = segA[j] + np.outer(t, AB[j])        # (N,3)
    D[:,j] = np.linalg.norm(P - proj, axis=1)

# anatomical guard: forbid cross-side bleed. Left* bones can't weight x<-e verts.
eps = 0.012
for j,nm in enumerate(names):
    if nm.startswith("Left"):  D[P[:,0] < -eps, j] = 1e6
    if nm.startswith("Right"): D[P[:,0] >  eps, j] = 1e6

K = 4
POWER = 3.2
W = np.zeros((N,B), dtype=np.float64)
order = np.argsort(D, axis=1)[:, :K]           # (N,K) nearest bone idx
rows = np.arange(N)[:,None]
dk = D[rows, order]                             # (N,K)
wk = 1.0 / np.power(dk + 1e-4, POWER)
wk[dk > 5e5] = 0.0                              # drop the forbidden 1e6 slots
wk_sum = wk.sum(axis=1, keepdims=True); wk_sum[wk_sum<1e-12]=1e-12
wk = wk / wk_sum
for k in range(K):
    W[rows[:,0], order[:,k]] = np.maximum(W[rows[:,0], order[:,k]], 0)  # ensure init
    W[np.arange(N), order[:,k]] = wk[:,k]

# ---- write to vertex groups (quantized buckets to keep op-count low) ----
vgs = {nm: mesh.vertex_groups.new(name=nm) for nm in names}
STEP = 0.02
for j,nm in enumerate(names):
    col = W[:,j]
    idx = np.where(col > 0.01)[0]
    if len(idx)==0: continue
    q = np.round(col[idx]/STEP).astype(int)
    for level in np.unique(q):
        if level<=0: continue
        vlist = idx[q==level].tolist()
        vgs[nm].add(vlist, float(level*STEP), 'REPLACE')
print("SKIN: distance-based weights assigned (K=%d power=%.1f)" % (K,POWER))

# add armature modifier + parent (no auto weights this time)
mesh.parent = arm
mod = mesh.modifiers.new("Armature","ARMATURE"); mod.object = arm; mod.use_vertex_groups=True

# ---------------- smooth + normalize + cap influences ----------------
bpy.context.view_layer.objects.active = mesh; mesh.select_set(True)
bpy.ops.object.mode_set(mode='WEIGHT_PAINT')
bpy.ops.object.vertex_group_smooth(group_select_mode='ALL', factor=0.5, repeat=3)
bpy.ops.object.vertex_group_limit_total(limit=4)
bpy.ops.object.vertex_group_normalize_all(lock_active=False)
bpy.ops.object.mode_set(mode='OBJECT')
print("WEIGHTS: smoothed(3x0.5), limited to 4, normalized")

# ---------------- constraints: Limit Rotation on wing joints ----------------
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='POSE')
def limit(bone, x, y, z):
    pb = arm.pose.bones.get(bone)
    if not pb: return
    c=pb.constraints.new('LIMIT_ROTATION'); c.owner_space='LOCAL'
    c.use_limit_x=True; c.min_x=math.radians(x[0]); c.max_x=math.radians(x[1])
    c.use_limit_y=True; c.min_y=math.radians(y[0]); c.max_y=math.radians(y[1])
    c.use_limit_z=True; c.min_z=math.radians(z[0]); c.max_z=math.radians(z[1])
for s in ("Left","Right"):
    limit(s+"Shoulder", (-70,80),(-30,30),(-60,60))
    limit(s+"UpperWing",(-60,70),(-20,20),(-40,50))
    limit(s+"LowerWing",(-20,20),(-10,10),(-90,10))
    limit(s+"Wrist",    (-20,20),(-10,10),(-80,10))
    limit(s+"Shin",     (-10,90),(-5,5),(-5,5))
    limit(s+"Ankle",    (-90,10),(-5,5),(-5,5))
bpy.ops.object.mode_set(mode='OBJECT')
print("CONSTRAINTS: Limit Rotation on wing joints + knee/ankle hinges")

# ---------------- validation ----------------
me=mesh.data; unw=sum(1 for v in me.vertices if len(v.groups)==0)
print("UNWEIGHTED VERTS:", unw)
print("--- HIERARCHY ---")
def dump(b,d=0):
    print("  "*d+b.name)
    for c in b.children: dump(c,d+1)
for b in arm_data.bones:
    if b.parent is None: dump(b)
bad=0
for b in arm_data.bones:
    if b.name.startswith("Left"):
        r=arm_data.bones.get("Right"+b.name[4:])
        if not r or abs(b.head_local.x+r.head_local.x)>1e-5 or abs(b.head_local.y-r.head_local.y)>1e-5: bad+=1
print("SYMMETRY mismatches:", bad)

# ---------------- save + export (REST pose) ----------------
bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT); print("SAVED", BLEND_OUT)
bpy.ops.object.select_all(action='DESELECT')
mesh.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(
    filepath=FBX_OUT, use_selection=True, object_types={'ARMATURE','MESH'},
    add_leaf_bones=False, bake_anim=False, apply_unit_scale=True, global_scale=1.0,
    apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
    use_mesh_modifiers=False, mesh_smooth_type='FACE', path_mode='COPY', embed_textures=True,
    use_armature_deform_only=True, primary_bone_axis='Y', secondary_bone_axis='X')
import os; print("EXPORTED", FBX_OUT, os.path.getsize(FBX_OUT)); print("DONE")
