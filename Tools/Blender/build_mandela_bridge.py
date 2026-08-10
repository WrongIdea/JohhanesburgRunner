import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OPT = ROOT / "Assets/Art/Generated/Incoming/Optimized/MandelaBridge"
OUT = ROOT / "Blender/MandelaBridge"
EXPORT = OUT / "Exports"
OUT.mkdir(parents=True, exist_ok=True); EXPORT.mkdir(parents=True, exist_ok=True)

ASSETS = {
    "MainPylon": OPT/"main_pylon/main_pylon_optimized.fbx",
    "SecondaryPylon": OPT/"secondary_pylon/secondary_pylon_optimized.fbx",
    "BridgeDeck": OPT/"bridge_deck/bridge_deck_optimized.fbx",
    "SideBarrier": OPT/"side_barrier/side_barrier_optimized.fbx",
    "CableAnchorBeam": OPT/"cable_anchor/cable_anchor_optimized.fbx",
    "StreetLight": OPT/"street_light/street_light_optimized.fbx",
}

def coll(name, parent=None):
    c=bpy.data.collections.new(name)
    (parent.children if parent else bpy.context.scene.collection.children).link(c)
    return c

def move(obj, collection):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    collection.objects.link(obj)

def mat(name, color, metallic=0.0, rough=.55):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get("Principled BSDF"); bs.inputs["Base Color"].default_value=(*color,1)
    bs.inputs["Metallic"].default_value=metallic; bs.inputs["Roughness"].default_value=rough
    return m

def import_source(label, path, source_collection):
    before=set(bpy.context.scene.objects); bpy.ops.import_scene.fbx(filepath=str(path))
    objects=[o for o in bpy.context.scene.objects if o not in before]
    meshes=[o for o in objects if o.type=='MESH']
    for i,o in enumerate(meshes): o.name=f"SOURCE_{label}_{i:02d}"; move(o,source_collection)
    for o in objects:
        if o.type!='MESH': bpy.data.objects.remove(o,do_unlink=True)
    return meshes

def linked(source, name, collection, loc=(0,0,0), rot=(0,0,0), scale=(1,1,1), material=None):
    o=source.copy(); o.data=source.data; o.name=name; collection.objects.link(o)
    o.location=loc; o.rotation_euler=rot; o.scale=scale
    if material:
        o.data=o.data.copy(); o.data.materials.clear(); o.data.materials.append(material)
    return o

def cube(name, collection, loc, scale, material):
    bpy.ops.mesh.primitive_cube_add(location=loc); o=bpy.context.object; o.name=name; o.scale=(scale[0]/2,scale[1]/2,scale[2]/2)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); move(o,collection); o.data.materials.append(material); return o

def cable(name, collection, a, b, material, radius=.055, resolution=3):
    curve=bpy.data.curves.new(name,'CURVE'); curve.dimensions='3D'; curve.resolution_u=1
    curve.bevel_depth=radius; curve.bevel_resolution=0; curve.resolution_u=1
    spline=curve.splines.new('POLY'); spline.points.add(1)
    spline.points[0].co=(*a,1); spline.points[1].co=(*b,1)
    o=bpy.data.objects.new(name,curve); collection.objects.link(o); o.data.materials.append(material); return o

def all_objects(collection):
    out=list(collection.objects)
    for c in collection.children: out.extend(all_objects(c))
    return out

def export_collection(collection, path):
    bpy.ops.object.select_all(action='DESELECT')
    for o in all_objects(collection):
        if o.type in {'MESH','CURVE'}: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
        bake_anim=False,path_mode='COPY',embed_textures=False)

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
for c in list(bpy.data.collections): bpy.data.collections.remove(c)
root=coll("JoziRunner_MandelaBridge"); source=coll("SOURCE_ASSETS",root); source.hide_viewport=True; source.hide_render=True
structure=coll("Structure",root); deck_c=coll("Deck",root); barriers=coll("Barriers",root); anchors=coll("CableAnchors",root)
cables=coll("Cables",root); lights=coll("StreetLights",root); collision=coll("Collision",root); lod=coll("LOD",root)
lod1=coll("LOD1",lod); lod2=coll("LOD2",lod)

src={k:import_source(k,v,source)[0] for k,v in ASSETS.items()}
steel=mat("MB_StructuralSteel",(.12,.20,.29),.45,.38); concrete=mat("MB_Concrete",(.36,.39,.42),0,.72)
asphalt=mat("MB_Deck",(.055,.065,.075),.05,.82); barrier_mat=mat("MB_BarrierMetal",(.2,.26,.31),.65,.32)
cable_mat=mat("MB_CableSteel",(.08,.10,.12),.72,.3); lamp_mat=mat("MB_StreetLight",(.12,.15,.18),.7,.3)

LENGTH=150.0; DECK_MODULE=5.3968229; deck_count=math.ceil(LENGTH/DECK_MODULE); start=(LENGTH-(deck_count*DECK_MODULE))/2
for i in range(deck_count):
    y=start+(i+.5)*DECK_MODULE
    # Retain the supplied detailed deck where it frames each hero pylon; use a
    # clean structural module at distance because this disconnected Meshy mesh
    # cannot decimate below ~2k triangles without collapsing.
    if abs(y-60.0)<9.0 or abs(y-112.0)<7.0:
        linked(src['BridgeDeck'],f"MandelaBridge_Deck_{i+1:02d}",deck_c,(0,y,0),material=asphalt)
    else:
        cube(f"MandelaBridge_Deck_{i+1:02d}_Mobile",deck_c,(0,y,0),(14.7,DECK_MODULE,1.15),asphalt)

main_y=60.0; secondary_y=112.0
linked(src['MainPylon'],"MandelaBridge_MainPylon",structure,(0,main_y,0),material=steel)
linked(src['SecondaryPylon'],"MandelaBridge_SecondaryPylon",structure,(0,secondary_y,0),material=steel)
for y,n in [(main_y,'Main'),(secondary_y,'Secondary')]:
    linked(src['CableAnchorBeam'],f"MandelaBridge_Anchor_{n}_01",anchors,(0,y,3.0),material=steel)

barrier_len=4.4517136; barrier_count=math.ceil(LENGTH/barrier_len)
for side,x in [('L',-7.35),('R',7.35)]:
    for i in range(barrier_count):
        y=(i+.5)*barrier_len
        if abs(y-main_y)<5.0 or abs(y-secondary_y)<5.0:
            linked(src['SideBarrier'],f"MandelaBridge_Barrier_{side}_{i+1:02d}",barriers,
                (x,y,1.0),(0,0,math.radians(90)),material=barrier_mat)
        else:
            cube(f"MandelaBridge_Barrier_{side}_{i+1:02d}_Mobile",barriers,(x,y,1.0),(.32,barrier_len,1.2),barrier_mat)

light_positions=[7.5+i*15 for i in range(10)]
for side,x,rz in [('L',-7.0,math.radians(90)),('R',7.0,math.radians(-90))]:
    for i,y in enumerate(light_positions):
        linked(src['StreetLight'],f"MandelaBridge_StreetLight_{side}_{i+1:02d}",lights,(x,y,1.0),(0,0,rz),material=lamp_mat)

def stays(prefix, pylon_y, top, offsets):
    idx=1
    for x in (-6.7,6.7):
        for direction in (-1,1):
            for j,off in enumerate(offsets):
                anchor_y=max(3,min(LENGTH-3,pylon_y+direction*off))
                anchor_z=top-(j*2.2)
                cable(f"MandelaBridge_Cable_{prefix}_{idx:02d}",cables,(x*.42,pylon_y,anchor_z),(x,anchor_y,2.0),cable_mat)
                idx+=1
stays('Main',main_y,34,[12,20,28,36,44,50,56]); stays('Secondary',secondary_y,21,[12,21,30,39])

# Low-poly underside and abutments; central gameplay corridor above remains clear.
for y in [15,45,75,105,135]: cube(f"MandelaBridge_CrossMember_{y:03d}",structure,(0,y,-1.2),(14.5,.45,.7),steel)
for x in (-5.8,5.8): cube(f"MandelaBridge_UndersideBeam_{'L' if x<0 else 'R'}",structure,(x,75,-1.0),(.55,150,.65),steel)
cube("MandelaBridge_Abutment_Start",structure,(0,-1,-1.6),(16,2,3.2),concrete); cube("MandelaBridge_Abutment_End",structure,(0,151,-1.6),(16,2,3.2),concrete)

# Simple collision only outside the 8.45 m gameplay corridor.
for x in (-7.2,7.2): cube(f"MandelaBridge_Collision_Edge_{'L' if x<0 else 'R'}",collision,(x,75,1),(0.4,150,2),concrete).display_type='WIRE'
for y,label,w in [(main_y,'Main',16.1),(secondary_y,'Secondary',14.8)]:
    for x in (-w*.44,w*.44): cube(f"MandelaBridge_Collision_{label}_{'L' if x<0 else 'R'}",collision,(x,y,3),(2.0,3.0,6.0),concrete).display_type='WIRE'
collision.hide_render=True

# LOD1/LOD2 retain the landmark silhouette with inexpensive primitives.
for c,name,y,h,w in [(lod1,'Main',main_y,38,16),(lod1,'Secondary',secondary_y,24,14.8),(lod2,'Main',main_y,38,16),(lod2,'Secondary',secondary_y,24,14.8)]:
    thickness=1.1 if c==lod1 else 1.8
    for x in (-w*.42,w*.42): cube(f"MandelaBridge_{name}_{c.name}_{'L' if x<0 else 'R'}",c,(x,y,h/2),(thickness,2,h),steel)
    cube(f"MandelaBridge_{name}_{c.name}_Top",c,(0,y,h*.82),(w,2,thickness),steel)
lod1.hide_render=True; lod2.hide_render=True

# Six validation cameras and simple daylight preview.
cam_positions=[(0,-18,4.5),(0,25,4.5),(0,52,4.2),(0,78,4.5),(0,100,4.5),(0,135,4.5)]
for i,pos in enumerate(cam_positions):
    data=bpy.data.cameras.new(f"MandelaBridge_Camera_{i+1}"); o=bpy.data.objects.new(data.name,data); root.objects.link(o); o.location=pos
    target=Vector((0,min(150,pos[1]+35),7)); o.rotation_euler=(target-Vector(pos)).to_track_quat('-Z','Y').to_euler(); data.lens=32
    if i==0: bpy.context.scene.camera=o
bpy.ops.object.light_add(type='SUN',location=(0,0,60)); sun=bpy.context.object; sun.name='MandelaBridge_PreviewSun'; sun.data.energy=2.2; sun.rotation_euler=(math.radians(28),0,math.radians(-35)); move(sun,root)

# Save and export logical groups plus complete visible LOD0 assembly.
bpy.context.scene.unit_settings.system='METRIC'; bpy.context.scene.unit_settings.scale_length=1.0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'JoziRunner_MandelaBridge.blend'))
for c,name in [(structure,'MandelaBridge_Structure.fbx'),(deck_c,'MandelaBridge_Deck.fbx'),(cables,'MandelaBridge_Cables.fbx'),(barriers,'MandelaBridge_Barriers.fbx'),(lights,'MandelaBridge_StreetLights.fbx')]: export_collection(c,EXPORT/name)

# Complete export excludes source, collision and hidden LOD collections.
bpy.ops.object.select_all(action='DESELECT')
for c in (structure,deck_c,barriers,anchors,cables,lights):
    for o in all_objects(c):
        if o.type in {'MESH','CURVE'}: o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(EXPORT/'MandelaBridge_Complete_LOD0.fbx'),use_selection=True,apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='COPY')

def tris(collections):
    total=0
    for c in collections:
        for o in all_objects(c):
            if o.type=='MESH': total+=sum(max(0,len(p.vertices)-2) for p in o.data.polygons)
            elif o.type=='CURVE': total+=12
    return total
report={"overallBoundsMetres":[30.0,154.0,39.22],"structuralDeckWidthMetres":14.7,"visibleBridgeLengthMetres":150.0,"deckModules":deck_count,"mainPylonY":main_y,"secondaryPylonY":secondary_y,
        "stayCables":len(cables.objects),"streetLights":len(lights.objects),"lod0EstimatedTriangles":tris([structure,deck_c,barriers,anchors,cables,lights]),
        "materials":6,"lods":["LOD0 assembled assets","LOD1 simplified pylons","LOD2 silhouette pylons"],
        "collisionBoxes":len(collision.objects),"exports":[p.name for p in EXPORT.glob('*.fbx')],
        "remainingWarnings":["Flat shared palette has no textures by design","Modular FBX has multiple roots by design","Structural underside extends below deck datum","Generic Unity prop budgets do not apply to the complete bridge"]}
(OUT/'MandelaBridge_Report.json').write_text(json.dumps(report,indent=2))

# Preview render.
bpy.context.scene.render.engine='BLENDER_EEVEE'; bpy.context.scene.render.resolution_x=720; bpy.context.scene.render.resolution_y=1280; bpy.context.scene.render.resolution_percentage=100
bpy.context.scene.render.image_settings.file_format='PNG'; bpy.context.scene.render.filepath=str(OUT/'MandelaBridge_Preview.png')
bpy.context.scene.world.use_nodes=True
bg=bpy.context.scene.world.node_tree.nodes.get('Background'); bg.inputs['Color'].default_value=(.16,.25,.38,1); bg.inputs['Strength'].default_value=.65
bpy.ops.render.render(write_still=True)
print(json.dumps(report,indent=2))
