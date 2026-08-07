"""Report per-bone weight coverage and any unweighted verts on the rigged mesh."""
import bpy
BLEND = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon_v2/PigeonV2_rigged.blend"
bpy.ops.wm.open_mainfile(filepath=BLEND)
mesh = bpy.data.objects["PigeonMesh"]
arm  = bpy.data.objects["PigeonArmature"]
me = mesh.data
vgs = mesh.vertex_groups
gname = {vg.index: vg.name for vg in vgs}

tot = {vg.name:0 for vg in vgs}
sumw = {vg.name:0.0 for vg in vgs}
unweighted = 0
maxw_zero = 0
for v in me.vertices:
    if len(v.groups)==0:
        unweighted += 1; continue
    s=sum(g.weight for g in v.groups)
    if s < 1e-4:
        maxw_zero += 1
    for g in v.groups:
        if g.weight>1e-4:
            tot[gname[g.group]] += 1
            sumw[gname[g.group]] += g.weight

print("VERTS:", len(me.vertices), " UNWEIGHTED:", unweighted, " ~zero-sum:", maxw_zero)
order=[b.name for b in arm.data.bones]
print("per-bone (verts with weight>0, total weight):")
empties=[]
for n in order:
    c=tot.get(n,0); w=sumw.get(n,0.0)
    flag = "  <-- EMPTY" if c==0 else ""
    if c==0: empties.append(n)
    print(f"   {n:14s} verts={c:6d} sumW={w:8.1f}{flag}")
print("EMPTY BONES:", empties)
print("DONE")
