import bpy
from mathutils import Vector

path = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/pigeon.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=path)
obj = [o for o in bpy.data.objects if o.type=='MESH'][0]
me = obj.data
V = [obj.matrix_world @ v.co for v in me.vertices]

xs=[c.x for c in V]; ys=[c.y for c in V]; zs=[c.z for c in V]
minx,maxx=min(xs),max(xs); miny,maxy=min(ys),max(ys); minz,maxz=min(zs),max(zs)
print(f"BOUNDS X[{minx:.3f},{maxx:.3f}] Y[{miny:.3f},{maxy:.3f}] Z[{minz:.3f},{maxz:.3f}]")
# Note orientation: -Y = head/front, +Y = tail, +Z = up

def avg(pts):
    if not pts: return None
    return sum(pts, Vector())/len(pts)

# ---- FEET: lowest 6% of z ----
zt = minz + 0.10*(maxz-minz)
feet = [c for c in V if c.z < zt]
feetL = [c for c in feet if c.x > 0.02]
feetR = [c for c in feet if c.x < -0.02]
print("\nFEET (z<%.3f) n=%d"%(zt,len(feet)))
print("  L foot avg", tuple(round(v,3) for v in avg(feetL)) if feetL else None, "n=",len(feetL))
print("  R foot avg", tuple(round(v,3) for v in avg(feetR)) if feetR else None, "n=",len(feetR))
# toe front (most -Y) and heel
if feetL:
    print("  L foot Y range", round(min(c.y for c in feetL),3), round(max(c.y for c in feetL),3))

# ---- LEGS: thin region below body, z in lower third, |x|>0.02 ----
legzt = minz + 0.35*(maxz-minz)
legs = [c for c in V if c.z < legzt and abs(c.x)>0.03]
legsL=[c for c in legs if c.x>0]; legsR=[c for c in legs if c.x<0]
print("\nLEGS region z<%.3f"%legzt)
print("  L leg avgX=%.3f Zrange[%.3f,%.3f] Yrange[%.3f,%.3f] n=%d"%(
    avg(legsL).x, min(c.z for c in legsL), max(c.z for c in legsL),
    min(c.y for c in legsL), max(c.y for c in legsL), len(legsL)) if legsL else print("  none"))

# ---- HEAD/BEAK: most -Y verts (front) ----
headyt = miny + 0.18*(maxy-miny)
head = [c for c in V if c.y < headyt]
print("\nHEAD (y<%.3f) n=%d avg=%s Zrange[%.3f,%.3f]"%(headyt,len(head),
    tuple(round(v,3) for v in avg(head)), min(c.z for c in head), max(c.z for c in head)))
# beak tip = most -Y
beak = min(V, key=lambda c:c.y)
print("  BEAK tip", tuple(round(v,3) for v in beak))
# top of head
htop = max(head, key=lambda c:c.z)
print("  HEAD top", tuple(round(v,3) for v in htop))

# ---- TAIL: most +Y ----
tail = max(V, key=lambda c:c.y)
tailyt = maxy - 0.18*(maxy-miny)
tailcluster=[c for c in V if c.y>tailyt]
print("\nTAIL tip", tuple(round(v,3) for v in tail), " cluster avg", tuple(round(v,3) for v in avg(tailcluster)))

# ---- WINGS: max |x| region, and along Y ----
wingxt = 0.6*maxx
wingsL=[c for c in V if c.x>wingxt]; wingsR=[c for c in V if c.x< -wingxt]
print("\nWINGS")
if wingsL:
    print("  L wing outer avg=%s Xmax=%.3f Yrange[%.3f,%.3f] Zrange[%.3f,%.3f]"%(
        tuple(round(v,3) for v in avg(wingsL)), maxx,
        min(c.y for c in wingsL),max(c.y for c in wingsL),
        min(c.z for c in wingsL),max(c.z for c in wingsL)))
# wing tip = max x with its y,z
wtip = max(V, key=lambda c:c.x)
print("  L wingtip(maxX)", tuple(round(v,3) for v in wtip))

# ---- BODY center of mass (mid region) ----
body=[c for c in V if headyt<=c.y<=tailyt and c.z>legzt]
print("\nBODY avg", tuple(round(v,3) for v in avg(body)))
print("Total verts", len(V))
