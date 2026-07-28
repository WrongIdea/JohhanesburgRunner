import bpy
import json
from pathlib import Path


rig = bpy.data.objects.get("Beaded Warrior Rig")
mesh = bpy.data.objects.get("Beaded Warrior")
if not rig or not mesh:
    raise RuntimeError("Expected Beaded Warrior rig and mesh were not found")

report = {
    "rig": {
        "name": rig.name,
        "type": rig.type,
        "location": list(rig.location),
        "rotation_mode": rig.rotation_mode,
        "rotation": list(rig.rotation_euler),
        "scale": list(rig.scale),
        "constraints": [c.type for c in rig.constraints],
    },
    "mesh": {
        "name": mesh.name,
        "parent": mesh.parent.name if mesh.parent else None,
        "modifiers": [{"name": m.name, "type": m.type, "object": getattr(getattr(m, "object", None), "name", None)} for m in mesh.modifiers],
        "dimensions": list(mesh.dimensions),
        "vertex_groups": len(mesh.vertex_groups),
    },
    "actions": [],
    "bones": [],
}

for action in bpy.data.actions:
    report["actions"].append({
        "name": action.name,
        "frame_range": list(action.frame_range),
        "fake_user": action.use_fake_user,
        "slots": [slot.identifier for slot in getattr(action, "slots", [])],
    })

for pb in rig.pose.bones:
    b = pb.bone
    report["bones"].append({
        "name": pb.name,
        "parent": pb.parent.name if pb.parent else None,
        "head": [round(x, 5) for x in b.head_local],
        "tail": [round(x, 5) for x in b.tail_local],
        "use_deform": b.use_deform,
        "rotation_mode": pb.rotation_mode,
        "constraints": [{"name": c.name, "type": c.type, "mute": c.mute} for c in pb.constraints],
    })

out = Path("/Users/thapelopilanyane/My project/meshy_jump_roll/rig_inspection.json")
out.write_text(json.dumps(report, indent=2))
print(f"WROTE {out}")
print("ACTIONS", [a["name"] for a in report["actions"]])
print("BONES", [b["name"] for b in report["bones"]])
