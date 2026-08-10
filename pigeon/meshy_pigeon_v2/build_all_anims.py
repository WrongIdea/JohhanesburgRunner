"""Author the full 12-clip pigeon animation library on PigeonV2_rigged.blend.
30 fps, in-place (no root translation across ground), loops where required.
Local-euler conventions (verified via pose tests):
  wings: rx=flap(up+/down-), ry=twist, rz=sweep/fold(back = negative)
  legs : rx=fore-aft swing / knee(Shin+) / ankle
  spine: rx=pitch, ry=roll, rz=yaw ; Tail rx (-up / +down)
Saves .blend + single FBX with all actions. Run headless."""
import bpy, math
from mathutils import Vector

BASE="/Users/thapelopilanyane/My project/pigeon/meshy_pigeon_v2"
bpy.ops.wm.open_mainfile(filepath=BASE+"/PigeonV2_rigged.blend")
arm=bpy.data.objects["PigeonArmature"]; mesh=bpy.data.objects["PigeonMesh"]
scene=bpy.context.scene; scene.render.fps=30
# constraints would clamp authored poses during bake (and don't export to Unity) -> mute
for pb in arm.pose.bones:
    pb.rotation_mode='XYZ'
    for c in pb.constraints: c.mute=True

WING=["Shoulder","UpperWing","LowerWing","Wrist","WingTip"]
FOLD={"Shoulder":(-30,3,-45),"UpperWing":(-16,0,-55),"LowerWing":(-13,0,-72),
      "Wrist":(-9,0,-62),"WingTip":(-6,0,-40)}
D=math.radians
def R(v): return (D(v[0]),D(v[1]),D(v[2]))

# ---- track container: name -> {bone: [(frame,(rx,ry,rz)|None,(lx,ly,lz)|None),...]} ----
A={}
def wingsym(dst, base, keys):
    """add same local keys to Left+Right<base> (symmetric world motion)."""
    for s in ("Left","Right"):
        dst[s+base]=keys

# ================= 1. IDLE (1..91 loop, 3s) =================
idle={}
idle["Body"]=[(1,(0,0,0),(0,0,0)),(30,(1.6,0,0),(0,0,0.004)),(60,(0.6,0.6,0),(0,0,0.002)),(91,(0,0,0),(0,0,0))]
idle["Neck"]=[(1,(0,0,0),None),(24,(2.5,0,2),None),(50,(-1,0,-2),None),(75,(1.5,0,1),None),(91,(0,0,0),None)]
idle["Head"]=[(1,(0,0,0),None),(18,(-2,0,-3),None),(33,(1,3,2),None),(52,(-1,-2,-4),None),(70,(2,1,3),None),(91,(0,0,0),None)]
idle["Tail"]=[(1,(0,0,0),None),(45,(-2,0,0),None),(91,(0,0,0),None)]
idle["Root"]=[(1,(0,0,0),None),(30,(0,1.2,0),None),(65,(0,-1.0,0),None),(91,(0,0,0),None)]  # gentle weight sway
A["Idle"]=("loop",91,"folded",idle)

# ================= 2. WALK (1..61 loop, 2s, 4 footfalls) =================
walk={}
# leg cycle: two 30f cycles over 1..61, right phased +15 (half cycle)
def legtracks(side, phase):
    thigh=[(0,-16),(8,16),(15,16),(22,-4),(30,-16)]
    knee =[(0,6),(8,6),(15,12),(22,40),(30,6)]
    ankle=[(0,2),(8,-10),(15,-2),(22,14),(30,2)]
    T={side+"Leg":[],side+"Shin":[],side+"Ankle":[]}
    for c0 in (0,30):
        for f,v in thigh: T[side+"Leg"].append((1+f+c0+phase,(v,0,0),None))
        for f,v in knee:  T[side+"Shin"].append((1+f+c0+phase,(v,0,0),None))
        for f,v in ankle: T[side+"Ankle"].append((1+f+c0+phase,(v,0,0),None))
    # clamp frames into 1..61 and ensure endpoints
    for k in T:
        T[k]=[(min(max(fr,1),61),rot,loc) for fr,rot,loc in T[k]]
    return T
for k,v in legtracks("Left",0).items(): walk[k]=v
for k,v in legtracks("Right",15).items(): walk[k]=v
# head bob (4 dips, period 15), synced to footfalls
walk["Head"]=[(1,(-6,0,0),None),(8,(5,0,0),None),(15,(-6,0,0),None),(23,(5,0,0),None),
              (30,(-6,0,0),None),(38,(5,0,0),None),(45,(-6,0,0),None),(53,(5,0,0),None),(61,(-6,0,0),None)]
walk["Neck"]=[(1,(4,0,0),None),(8,(-2,0,0),None),(15,(4,0,0),None),(23,(-2,0,0),None),
              (30,(4,0,0),None),(38,(-2,0,0),None),(45,(4,0,0),None),(53,(-2,0,0),None),(61,(4,0,0),None)]
walk["Body"]=[(1,(3,0,0),(0,0,0)),(8,(3,0,0),(0,0,0.003)),(15,(3,0,0),(0,0,0)),(23,(3,0,0),(0,0,0.003)),
              (30,(3,0,0),(0,0,0)),(38,(3,0,0),(0,0,0.003)),(45,(3,0,0),(0,0,0)),(53,(3,0,0),(0,0,0.003)),(61,(3,0,0),(0,0,0))]
walk["Tail"]=[(1,(1,0,0),None),(15,(2,0,1),None),(30,(1,0,0),None),(45,(2,0,-1),None),(61,(1,0,0),None)]
A["Walk"]=("loop",61,"folded",walk)

# ================= 3. PECK (1..61 loop, 2s) =================
peck={}
# neck/head: down (compress) -> jab -> up -> look around
peck["Neck"]=[(1,(6,0,0),None),(12,(40,0,0),None),(20,(44,0,0),None),(26,(40,0,0),None),
              (34,(4,0,0),None),(44,(2,0,8),None),(52,(2,0,-8),None),(61,(6,0,0),None)]
peck["Head"]=[(1,(2,0,0),None),(12,(30,0,0),None),(16,(50,0,0),None),(20,(30,0,0),None),(26,(34,0,0),None),
              (34,(-4,0,0),None),(44,(0,6,10),None),(52,(0,-6,-10),None),(61,(2,0,0),None)]
peck["Body"]=[(1,(0,0,0),None),(16,(4,0,0),None),(30,(1,0,0),None),(61,(0,0,0),None)]
peck["Tail"]=[(1,(0,0,0),None),(16,(-4,0,0),None),(30,(0,0,0),None),(61,(0,0,0),None)]
# slow shuffle: tiny leg
peck["LeftLeg"]=[(1,(-4,0,0),None),(30,(3,0,0),None),(61,(-4,0,0),None)]
peck["RightLeg"]=[(1,(4,0,0),None),(30,(-3,0,0),None),(61,(4,0,0),None)]
A["Peck"]=("loop",61,"folded",peck)

# ================= 4. ALERT (1..61 loop, 2s) =================
alert={}
alert["Neck"]=[(1,(-4,0,0),None),(10,(-8,0,35),None),(20,(-8,0,35),None),(30,(-6,0,-38),None),
               (40,(-6,0,-38),None),(50,(-4,0,4),None),(61,(-4,0,0),None)]
alert["Head"]=[(1,(-6,0,0),None),(8,(-10,0,48),None),(20,(-8,0,44),None),(28,(-10,0,-50),None),
               (40,(-8,0,-46),None),(50,(-6,0,6),None),(61,(-6,0,0),None)]
alert["Body"]=[(1,(0,0,0),(0,0,0)),(14,(6,0,0),(0,0,-0.006)),(46,(6,0,0),(0,0,-0.006)),(61,(0,0,0),(0,0,0))]  # slight crouch
alert["Tail"]=[(1,(0,0,0),None),(12,(-10,0,0),None),(18,(2,0,0),None),(34,(-8,0,0),None),(40,(2,0,0),None),(61,(0,0,0),None)]  # flicks
alert["Root"]=[(1,(0,0,0),None),(30,(0,0,0),None),(61,(0,0,0),None)]
A["Alert"]=("loop",61,"folded",alert)

# ================= 6. FLY (1..31 loop, 1s, 2 beats) =================
def flap_beat(basef, amp=1.0):
    """return per-bone lists for one 15f beat starting at frame basef."""
    b=basef
    sh=[(b+0,(32,0,0)),(b+4,(10,0,0)),(b+8,(-34,0,0)),(b+12,(-6,0,0)),(b+15,(32,0,0))]
    up=[(b+0,(22,0,-8)),(b+5,(2,0,-4)),(b+9,(-24,0,0)),(b+13,(-2,0,-4)),(b+15,(22,0,-8))]
    lo=[(b+0,(6,0,-30)),(b+8,(-8,0,-6)),(b+15,(6,0,-30))]
    wr=[(b+0,(4,4,-24)),(b+9,(-6,-4,-4)),(b+15,(4,4,-24))]
    ti=[(b+0,(2,8,-14)),(b+10,(-4,-6,-2)),(b+15,(2,8,-14))]
    return sh,up,lo,wr,ti
fly={}
for s in ("Left","Right"):
    fly[s+"Shoulder"]=[]; fly[s+"UpperWing"]=[]; fly[s+"LowerWing"]=[]; fly[s+"Wrist"]=[]; fly[s+"WingTip"]=[]
for beat in (1,16):
    sh,up,lo,wr,ti=flap_beat(beat)
    for s in ("Left","Right"):
        for fr,v in sh: fly[s+"Shoulder"].append((fr,v,None))
        for fr,v in up: fly[s+"UpperWing"].append((fr,v,None))
        for fr,v in lo: fly[s+"LowerWing"].append((fr,v,None))
        for fr,v in wr: fly[s+"Wrist"].append((fr,v,None))
        for fr,v in ti: fly[s+"WingTip"].append((fr,v,None))
fly["Body"]=[(1,(2,0,0),(0,0,0)),(8,(-2,0,0),(0,0,0.006)),(16,(2,0,0),(0,0,0)),(23,(-2,0,0),(0,0,0.006)),(31,(2,0,0),(0,0,0))]
fly["Head"]=[(1,(-2,0,0),None),(8,(2,0,0),None),(16,(-2,0,0),None),(23,(2,0,0),None),(31,(-2,0,0),None)]
fly["Neck"]=[(1,(-6,0,0),None),(31,(-6,0,0),None)]  # slight forward-reach, stable
fly["Tail"]=[(1,(-3,0,0),None),(8,(1,0,0),None),(16,(-3,0,0),None),(23,(1,0,0),None),(31,(-3,0,0),None)]
A["Fly"]=("loop",31,"open",fly)

# ================= 7. GLIDE (1..61 loop, 2s) =================
gl={}
wingsym(gl,"Shoulder",[(1,(-6,0,0),None),(30,(-2,0,-3),None),(61,(-6,0,0),None)])
wingsym(gl,"UpperWing",[(1,(-4,0,-4),None),(30,(0,0,-2),None),(61,(-4,0,-4),None)])
wingsym(gl,"LowerWing",[(1,(2,0,-6),None),(30,(4,0,-3),None),(61,(2,0,-6),None)])
wingsym(gl,"Wrist",[(1,(0,2,-4),None),(30,(2,0,-2),None),(61,(0,2,-4),None)])
wingsym(gl,"WingTip",[(1,(0,4,-2),None),(30,(2,-2,0),None),(61,(0,4,-2),None)])
gl["Body"]=[(1,(-2,0,0),(0,0,0)),(30,(-3,0.5,0),(0,0,0.003)),(61,(-2,0,0),(0,0,0))]
gl["Tail"]=[(1,(-2,0,0),None),(30,(-1,0,2),None),(45,(-3,0,-2),None),(61,(-2,0,0),None)]
gl["Head"]=[(1,(0,0,0),None),(30,(1,0,2),None),(61,(0,0,0),None)]
A["Glide"]=("loop",61,"open",gl)

# ================= 8/9. BANK LEFT / RIGHT (1..31 loop, 1s) =================
def bank(sign):  # sign +1 = bank LEFT (roll left, left wing folds, right extends)
    bk={}
    # roll whole body via Root ry (local Y ~ world -Y fore-aft = roll)
    roll=12*sign
    bk["Root"]=[(1,(0,0,0),None),(12,(0,roll,0),None),(20,(0,roll,0),None),(31,(0,0,0),None)]
    bk["Body"]=[(1,(0,0,0),None),(12,(0,roll*0.4,0),None),(31,(0,0,0),None)]
    # inner wing (left when banking left) folds a bit; outer extends + slight flap
    innerL = (sign>0)  # left is inner when banking left
    for s in ("Left","Right"):
        inner = (s=="Left") if sign>0 else (s=="Right")
        if inner:
            keys_sh=[(1,(0,0,0),None),(14,(-14,0,-18),None),(22,(-14,0,-18),None),(31,(0,0,0),None)]
            keys_lo=[(1,(0,0,0),None),(14,(-6,0,-26),None),(31,(0,0,0),None)]
        else:
            keys_sh=[(1,(0,0,0),None),(14,(10,0,10),None),(22,(8,0,10),None),(31,(0,0,0),None)]
            keys_lo=[(1,(0,0,0),None),(14,(4,0,8),None),(31,(0,0,0),None)]
        bk[s+"Shoulder"]=keys_sh; bk[s+"LowerWing"]=keys_lo
    # tail assists turn (yaw + slight roll)
    bk["Tail"]=[(1,(0,0,0),None),(14,(-4,0,10*sign),None),(31,(0,0,0),None)]
    bk["Head"]=[(1,(0,0,0),None),(14,(0,-4*sign,8*sign),None),(31,(0,0,0),None)]  # look into turn
    return bk
A["BankLeft"]=("loop",31,"open",bank(+1))
A["BankRight"]=("loop",31,"open",bank(-1))

# ================= 5. TAKEOFF (1..22 non-loop, 0.7s) =================
tk={}
# crouch (1-5) -> push+rise (5-12) -> wings sweep up open + downstroke (5-18) -> up ready (22)
tk["Body"]=[(1,(0,0,0),(0,0,0)),(5,(8,0,0),(0,0,-0.012)),(11,(-10,0,0),(0,0,0.02)),(18,(-16,0,0),(0,0,0.05)),(22,(-14,0,0),(0,0,0.06))]
tk["Root"]=[(1,(0,0,0),(0,0,0)),(5,(0,0,0),(0,0,-0.006)),(12,(0,0,0),(0,0,0.02)),(22,(0,0,0),(0,0,0.05))]
# legs: bend then extend (push) then tuck
for s in ("Left","Right"):
    tk[s+"Leg"]=[(1,(0,0,0),None),(5,(18,0,0),None),(11,(-30,0,0),None),(22,(24,0,0),None)]
    tk[s+"Shin"]=[(1,(6,0,0),None),(5,(45,0,0),None),(11,(-10,0,0),None),(22,(50,0,0),None)]
    tk[s+"Ankle"]=[(1,(0,0,0),None),(5,(-30,0,0),None),(11,(20,0,0),None),(22,(-40,0,0),None)]
# wings: start folded (baseline) -> open + first big downstroke -> up
for s in ("Left","Right"):
    tk[s+"Shoulder"]=[(1,FOLD["Shoulder"],None),(6,(15,0,0),None),(12,(-30,0,0),None),(18,(40,0,0),None),(22,(34,0,0),None)]
    tk[s+"UpperWing"]=[(1,FOLD["UpperWing"],None),(6,(8,0,-6),None),(12,(-22,0,0),None),(18,(26,0,-6),None),(22,(22,0,-8),None)]
    tk[s+"LowerWing"]=[(1,FOLD["LowerWing"],None),(7,(0,0,-20),None),(12,(-6,0,-4),None),(18,(6,0,-28),None),(22,(6,0,-30),None)]
    tk[s+"Wrist"]=[(1,FOLD["Wrist"],None),(7,(0,0,-16),None),(18,(4,4,-22),None),(22,(4,4,-24),None)]
    tk[s+"WingTip"]=[(1,FOLD["WingTip"],None),(7,(0,0,-10),None),(18,(2,8,-12),None),(22,(2,8,-14),None)]
tk["Tail"]=[(1,(0,0,0),None),(8,(-14,0,0),None),(22,(-10,0,0),None)]  # spread/raise
tk["Neck"]=[(1,(6,0,0),None),(8,(-6,0,0),None),(22,(-8,0,0),None)]
tk["Head"]=[(1,(2,0,0),None),(22,(4,0,0),None)]
A["Takeoff"]=("noloop",22,"folded",tk)

# ================= 10. LANDING (1..31 non-loop, 1s) -> folds to idle =================
ld={}
# start airborne open, wings cup wide+forward (brake) -> legs extend -> contact -> fold
ld["Body"]=[(1,(-14,0,0),(0,0,0.04)),(8,(-18,0,0),(0,0,0.03)),(18,(-6,0,0),(0,0,0.008)),(24,(3,0,0),(0,0,-0.006)),(31,(0,0,0),(0,0,0))]
ld["Root"]=[(1,(0,0,0),(0,0,0.04)),(10,(0,0,0),(0,0,0.02)),(20,(0,0,0),(0,0,0.002)),(31,(0,0,0),(0,0,0))]
for s in ("Left","Right"):
    # wings open wide+forward then fold to FOLD
    ld[s+"Shoulder"]=[(1,(30,0,0),None),(8,(44,0,20),None),(16,(20,0,0),None),(24,(-10,0,-28),None),(31,FOLD["Shoulder"],None)]
    ld[s+"UpperWing"]=[(1,(18,0,-6),None),(8,(30,0,6),None),(16,(6,0,-20),None),(24,(-12,0,-40),None),(31,FOLD["UpperWing"],None)]
    ld[s+"LowerWing"]=[(1,(6,0,-24),None),(8,(2,0,10),None),(16,(-4,0,-40),None),(24,(-10,0,-60),None),(31,FOLD["LowerWing"],None)]
    ld[s+"Wrist"]=[(1,(4,4,-20),None),(8,(0,0,6),None),(16,(-4,0,-44),None),(31,FOLD["Wrist"],None)]
    ld[s+"WingTip"]=[(1,(2,8,-12),None),(8,(0,0,4),None),(16,(-4,0,-30),None),(31,FOLD["WingTip"],None)]
    # legs reach down, plant, absorb
    ld[s+"Leg"]=[(1,(20,0,0),None),(10,(-20,0,0),None),(20,(-6,0,0),None),(25,(8,0,0),None),(31,(0,0,0),None)]
    ld[s+"Shin"]=[(1,(40,0,0),None),(10,(6,0,0),None),(20,(20,0,0),None),(25,(40,0,0),None),(31,(6,0,0),None)]
    ld[s+"Ankle"]=[(1,(-30,0,0),None),(10,(10,0,0),None),(20,(-6,0,0),None),(25,(-18,0,0),None),(31,(0,0,0),None)]
ld["Tail"]=[(1,(-16,0,0),None),(10,(-20,0,0),None),(24,(-4,0,0),None),(31,(0,0,0),None)]
ld["Neck"]=[(1,(-8,0,0),None),(20,(4,0,0),None),(31,(0,0,0),None)]
ld["Head"]=[(1,(6,0,0),None),(31,(0,0,0),None)]
A["Landing"]=("noloop",31,"open",ld)

# ================= 11. SHORT HOP (1..16 non-loop, 0.5s) =================
hp={}
hp["Root"]=[(1,(0,0,0),(0,0,0)),(3,(0,0,0),(0,0,-0.008)),(7,(0,0,0),(0,0,0.03)),(10,(0,0,0),(0,0,0.026)),(13,(0,0,0),(0,0,-0.004)),(16,(0,0,0),(0,0,0))]
hp["Body"]=[(1,(0,0,0),None),(3,(6,0,0),None),(7,(-8,0,0),None),(13,(4,0,0),None),(16,(0,0,0),None)]
for s in ("Left","Right"):
    hp[s+"Leg"]=[(1,(0,0,0),None),(3,(20,0,0),None),(7,(-24,0,0),None),(11,(10,0,0),None),(16,(0,0,0),None)]
    hp[s+"Shin"]=[(1,(6,0,0),None),(3,(40,0,0),None),(7,(-8,0,0),None),(11,(38,0,0),None),(16,(6,0,0),None)]
    hp[s+"Ankle"]=[(1,(0,0,0),None),(3,(-30,0,0),None),(7,(16,0,0),None),(11,(-24,0,0),None),(16,(0,0,0),None)]
    # quick single flap: folded->open->folded
    hp[s+"Shoulder"]=[(1,FOLD["Shoulder"],None),(5,(20,0,-6),None),(9,(-16,0,-10),None),(16,FOLD["Shoulder"],None)]
    hp[s+"UpperWing"]=[(1,FOLD["UpperWing"],None),(5,(10,0,-16),None),(9,(-10,0,-20),None),(16,FOLD["UpperWing"],None)]
    hp[s+"LowerWing"]=[(1,FOLD["LowerWing"],None),(5,(2,0,-24),None),(16,FOLD["LowerWing"],None)]
    hp[s+"Wrist"]=[(1,FOLD["Wrist"],None),(5,(0,0,-20),None),(16,FOLD["Wrist"],None)]
    hp[s+"WingTip"]=[(1,FOLD["WingTip"],None),(5,(0,0,-12),None),(16,FOLD["WingTip"],None)]
hp["Tail"]=[(1,(0,0,0),None),(7,(-8,0,0),None),(16,(0,0,0),None)]
A["ShortHop"]=("noloop",16,"folded",hp)

# ================= 12. FRIGHTENED FLUTTER (1..25 non-loop, 0.8s) -> Fly =================
ff={}
ff["Body"]=[(1,(0,0,0),(0,0,0)),(4,(6,0,0),(0,0,-0.006)),(10,(-8,1.5,0),(0,0,0.02)),(17,(-12,-1.5,0),(0,0,0.04)),(25,(-12,0,0),(0,0,0.05))]
ff["Root"]=[(1,(0,0,0),(0,0,0)),(6,(0,1.5,0),(0,0,0.01)),(14,(0,-1.5,0),(0,0,0.03)),(25,(0,0,0),(0,0,0.05))]
# 3 frantic quick beats over 24f (period ~8): up->down->up
def frantic(b):
    return [(b,(30,0,-6)),(b+4,(-30,0,-10)),(b+8,(30,0,-6))]
for s in ("Left","Right"):
    ff[s+"Shoulder"]=[(1,FOLD["Shoulder"],None),(5,(28,0,-6),None)]
    ff[s+"UpperWing"]=[(1,FOLD["UpperWing"],None),(5,(18,0,-8),None)]
    ff[s+"LowerWing"]=[(1,FOLD["LowerWing"],None),(5,(4,0,-24),None)]
    ff[s+"Wrist"]=[(1,FOLD["Wrist"],None),(5,(2,4,-20),None)]
    ff[s+"WingTip"]=[(1,FOLD["WingTip"],None),(5,(0,8,-12),None)]
    for beat in (5,13,21):
        for fr,v in frantic(beat):
            ff[s+"Shoulder"].append((fr,v,None))
            ff[s+"UpperWing"].append((fr,(v[0]*0.7,0,-8),None))
            ff[s+"LowerWing"].append((fr,(4,0,-24),None))
            ff[s+"Wrist"].append((fr,(2,4,-20),None))
            ff[s+"WingTip"].append((fr,(0,8,-12),None))
ff["Tail"]=[(1,(0,0,0),None),(6,(-14,0,0),None),(25,(-10,0,0),None)]
ff["Neck"]=[(1,(4,0,0),None),(25,(-6,0,0),None)]
ff["Head"]=[(1,(0,0,0),None),(10,(-6,3,0),None),(25,(2,0,0),None)]
A["FrightenedFlutter"]=("noloop",25,"folded",ff)

# ============================================================
# build actions
# ============================================================
if not arm.animation_data: arm.animation_data_create()
CLIP_ORDER=["Idle","Walk","Peck","Alert","Takeoff","Fly","Glide","BankLeft","BankRight","Landing","ShortHop","FrightenedFlutter"]
report=[]
for name in CLIP_ORDER:
    kind,last,wingbase,tracks=A[name]
    act=bpy.data.actions.new(name); act.use_fake_user=True
    arm.animation_data.action=act
    s,e=1,last
    # baseline: reset all bones; wings folded if requested
    for pb in arm.pose.bones:
        base=(0,0,0)
        for w in WING:
            if pb.name.endswith(w) and wingbase=="folded":
                base=FOLD[w]
        pb.rotation_euler=R(base); pb.location=(0,0,0)
        for f in (s,e):
            pb.keyframe_insert('rotation_euler',frame=f)
        if pb.name in ("Root","Body"):
            for f in (s,e): pb.keyframe_insert('location',frame=f)
    # tracks
    for bone,keys in tracks.items():
        pb=arm.pose.bones.get(bone)
        if not pb: print("  MISSING BONE",bone); continue
        for item in keys:
            f=item[0]; rot=item[1]; loc=item[2] if len(item)>2 else None
            f=min(max(int(f),s),e)
            if rot is not None:
                pb.rotation_euler=R(rot); pb.keyframe_insert('rotation_euler',frame=f)
            if loc is not None:
                pb.location=loc; pb.keyframe_insert('location',frame=f)
    act.use_frame_range=True; act.frame_start=s; act.frame_end=e
    dur=(e-1)/30.0
    report.append((name,kind,e,round(dur,3)))
    print(f"built {name:18s} {kind:6s} frames 1..{e}  {dur:.2f}s")

# set a neutral current action range
scene.frame_start=1; scene.frame_end=91
bpy.ops.wm.save_as_mainfile(filepath=BASE+"/PigeonV2_animated.blend")
print("SAVED animated blend")

# ---- export single FBX with all actions ----
bpy.ops.object.select_all(action='DESELECT')
mesh.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active=arm
import os
bpy.ops.export_scene.fbx(
    filepath=BASE+"/PigeonV2_animations.fbx", use_selection=True,
    object_types={'ARMATURE','MESH'}, add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
    apply_unit_scale=True, global_scale=1.0, apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z', axis_up='Y', use_mesh_modifiers=False, mesh_smooth_type='FACE',
    path_mode='COPY', embed_textures=True, use_armature_deform_only=True,
    primary_bone_axis='Y', secondary_bone_axis='X')
print("EXPORTED", os.path.getsize(BASE+"/PigeonV2_animations.fbx"))
print("CLIPS:")
for n,k,e,d in report: print(f"  {n:18s} {d:.2f}s  {e}f  {k}")
print("DONE")
