"""Extract anatomical landmark coordinates from the cleaned pigeon mesh so the
rig's bones can be placed correctly. Read-only. Prints a landmark dict + a
per-X-bin wing spine (centroid Y,Z) and per-Z leg profile. Run headless."""
import bpy
from mathutils import Vector

BLEND = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon_v2/PigeonV2_clean.blend"
bpy.ops.wm.open_mainfile(filepath=BLEND)
obj = max([o for o in bpy.context.scene.objects if o.type=='MESH'], key=lambda m: len(m.data.vertices))
me = obj.data
V = [obj.matrix_world @ v.co for v in me.vertices]

xs=[c.x for c in V]; ys=[c.y for c in V]; zs=[c.z for c in V]
mnx,mxx=min(xs),max(xs); mny,mxy=min(ys),max(ys); mnz,mxz=min(zs),max(zs)
print("BOUNDS X",round(mnx,3),round(mxx,3)," Y",round(mny,3),round(mxy,3)," Z",round(mnz,3),round(mxz,3))
print("  -Y is FORWARD (head). size:", round(mxx-mnx,3), round(mxy-mny,3), round(mxz-mnz,3))

def centroid(pts):
    n=len(pts);
    if n==0: return None
    s=Vector((0,0,0))
    for p in pts: s+=p
    return s/n

# ---- Beak tip: most forward (min Y), upper half ----
front = sorted(V, key=lambda c: c.y)[:80]
beak = centroid(front)
print("BEAK ~", tuple(round(v,3) for v in beak))

# ---- Tail tip: most back (max Y) ----
back = sorted(V, key=lambda c: -c.y)[:120]
tail_tip = centroid(back)
print("TAIL_TIP ~", tuple(round(v,3) for v in tail_tip))

# ---- Head top / crown: front third, highest Z ----
front_third = [c for c in V if c.y < mny + (mxy-mny)*0.33]
crown = centroid(sorted(front_third, key=lambda c:-c.z)[:60])
print("CROWN ~", tuple(round(v,3) for v in crown))

# ---- Body core: central mass |X|<0.05, mid Y ----
core = [c for c in V if abs(c.x)<0.05 and mny+(mxy-mny)*0.25<c.y<mny+(mxy-mny)*0.7]
body_c = centroid(core)
print("BODY_CORE ~", tuple(round(v,3) for v in body_c))

# ---- Wing spine (+X side): bin by X, centroid Y,Z of wing verts ----
# wing verts: |X|>0.06 (outside body), take +X
print("WING +X spine (x_bin -> centroid y,z ; count):")
import numpy as np
xw = 0.06
wing = [c for c in V if c.x>xw]
nb=10
xmax = max(c.x for c in wing)
for i in range(nb):
    lo=xw+(xmax-xw)*i/nb; hi=xw+(xmax-xw)*(i+1)/nb
    seg=[c for c in wing if lo<=c.x<hi]
    cc=centroid(seg)
    if cc: print(f"   x[{lo:.3f},{hi:.3f}] -> y={cc.y:.3f} z={cc.z:.3f}  n={len(seg)}")
print("WINGTIP_X_MAX", round(xmax,3))

# ---- Shoulder: wing root where wing meets body (x~0.04-0.07) ----
sh=[c for c in V if 0.03<c.x<0.07]
shoulder = centroid(sh)
print("SHOULDER ~", tuple(round(v,3) for v in shoulder))

# ---- Feet: lowest Z, split by X sign into two clusters ----
low = [c for c in V if c.z < mnz + 0.03]
lf = [c for c in low if c.x>0.005]; rf=[c for c in low if c.x<-0.005]
print("FOOT_L ~", tuple(round(v,3) for v in centroid(lf)) if lf else None, " n",len(lf))
print("FOOT_R ~", tuple(round(v,3) for v in centroid(rf)) if rf else None, " n",len(rf))

# ---- Leg profile (+X leg): bin by Z from foot up, centroid X,Y ----
print("LEG +X profile (z_bin -> centroid x,y ; count):")
legcol = [c for c in V if 0.005<c.x<0.06 and c.z < mnz+0.16]
zmn=min(c.z for c in legcol); zmx=max(c.z for c in legcol)
for i in range(6):
    lo=zmn+(zmx-zmn)*i/6; hi=zmn+(zmx-zmn)*(i+1)/6
    seg=[c for c in legcol if lo<=c.z<hi]
    cc=centroid(seg)
    if cc: print(f"   z[{lo:.3f},{hi:.3f}] -> x={cc.x:.3f} y={cc.y:.3f} n={len(seg)}")

# ---- Hip: top of leg column ----
hipcol=[c for c in legcol if c.z>zmn+(zmx-zmn)*0.75]
print("HIP_L ~", tuple(round(v,3) for v in centroid(hipcol)) if hipcol else None)

# ---- Neck base: between body core and crown ----
neckcol=[c for c in V if abs(c.x)<0.04 and mny+(mxy-mny)*0.12<c.y<mny+(mxy-mny)*0.35 and c.z>mnz+(mxz-mnz)*0.5]
print("NECK_BASE ~", tuple(round(v,3) for v in centroid(neckcol)) if neckcol else None)
print("DONE")
