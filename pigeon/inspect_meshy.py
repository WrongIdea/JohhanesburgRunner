import bpy, sys, os
from mathutils import Vector

path = "/Users/thapelopilanyane/My project/pigeon/meshy_pigeon/pigeon.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=path)

print("\n==== SCENE OBJECTS ====")
meshes = []
for o in bpy.data.objects:
    print(f"  {o.type:8} '{o.name}'  loc={tuple(round(v,3) for v in o.location)}  scale={tuple(round(v,3) for v in o.scale)}  rot={tuple(round(v,3) for v in o.rotation_euler)}")
    if o.type == 'MESH':
        meshes.append(o)

for o in meshes:
    me = o.data
    print(f"\n==== MESH '{o.name}' ====")
    print(f"  verts={len(me.vertices)}  polys={len(me.polygons)}  tris(est)={sum(len(p.vertices)-2 for p in me.polygons)}")
    print(f"  materials={[m.name if m else None for m in me.materials]}")
    print(f"  uv_layers={[l.name for l in me.uv_layers]}")
    print(f"  shape_keys={me.shape_keys is not None}")
    print(f"  vertex_groups on obj={[g.name for g in o.vertex_groups]}")
    # world-space bounds
    mw = o.matrix_world
    cos = [mw @ v.co for v in me.vertices]
    xs=[c.x for c in cos]; ys=[c.y for c in cos]; zs=[c.z for c in cos]
    print(f"  WORLD bounds X[{min(xs):.3f},{max(xs):.3f}] Y[{min(ys):.3f},{max(ys):.3f}] Z[{min(zs):.3f},{max(zs):.3f}]")
    print(f"  WORLD size  ({max(xs)-min(xs):.3f}, {max(ys)-min(ys):.3f}, {max(zs)-min(zs):.3f})")
    # which axis is longest (body length), which is up
    size = Vector((max(xs)-min(xs), max(ys)-min(ys), max(zs)-min(zs)))
    print(f"  longest axis idx={max(range(3), key=lambda i: size[i])} (0=X,1=Y,2=Z)")

    # Distribution: sample extreme points to guess head/tail/wings/feet
    # print centroid
    cen = sum(cos, Vector())/len(cos)
    print(f"  centroid world=({cen.x:.3f},{cen.y:.3f},{cen.z:.3f})")
    # extreme verts along each axis
    def extreme(axis, sign):
        return max(cos, key=lambda c: sign*c[axis])
    for ax,nm in [(0,'X'),(1,'Y'),(2,'Z')]:
        pmax=extreme(ax,1); pmin=extreme(ax,-1)
        print(f"    +{nm} extreme=({pmax.x:.2f},{pmax.y:.2f},{pmax.z:.2f})  -{nm} extreme=({pmin.x:.2f},{pmin.y:.2f},{pmin.z:.2f})")
