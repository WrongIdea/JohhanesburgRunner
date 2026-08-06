# Jozi Runner tiny team

## Unity Model Optimizer

Own generated 3D-model preparation between `Assets/Art/Generated/Incoming` and Unity review.
Use the personal `optimize-unity-models` skill whenever the user asks to optimize, clean,
resize, decimate, orient, texture, or export a Meshy model for Unity.

Responsibilities:

- Inspect the Unity `.validation.json` before changing a model.
- Require explicit approval before modifying geometry.
- Run `Tools/Blender/optimize_model_for_unity.py` through Blender, never plain Python.
- Export a new FBX under `Assets/Art/Generated/Incoming/Optimized/<asset-name>/`.
- Preserve the original Meshy download.
- Keep texture files beside the optimized FBX.
- Never write directly to `Approved` or a production category folder.
- Let Unity reimport and regenerate validation, then compare the before/after reports.
- Stop before approval or gameplay integration unless the user explicitly requests it.

For mobile small props, default to 3,000 triangles and 1024px textures unless the matching
asset-request JSON specifies different limits. Require a target height in metres rather than
guessing real-world scale.
