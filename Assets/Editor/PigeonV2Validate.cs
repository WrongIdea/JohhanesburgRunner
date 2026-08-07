using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>Headless validation of the built v2 pigeon assets (Part 16).</summary>
    public static class PigeonV2Validate
    {
        const string Dir = "Assets/Characters/Pigeon";
        const string FbxPath = Dir + "/Models/Pigeon_Unity.fbx";
        const string MatPath = Dir + "/Materials/Pigeon.mat";
        const string ControllerPath = Dir + "/Controllers/PigeonAnimator.controller";
        const string PrefabPath = Dir + "/Prefabs/Pigeon.prefab";

        [MenuItem("Joburg Runner/Pigeon v2/Validate")]
        public static void Validate()
        {
            var log = new System.Text.StringBuilder("\n===== PIGEON V2 VALIDATION =====\n");

            // --- import / rig ---
            var imp = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            log.AppendLine($"AnimationType: {imp.animationType}  (want Generic)");
            log.AppendLine($"materialImportMode: {imp.materialImportMode}  meshCompression: {imp.meshCompression}");
            log.AppendLine($"readable:{imp.isReadable} blendshapes:{imp.importBlendShapes} cameras:{imp.importCameras} lights:{imp.importLights} collider:{imp.addCollider}");

            // --- clips + loop settings ---
            var clips = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<AnimationClip>()
                        .Where(c => !c.name.StartsWith("__preview__")).OrderBy(c => c.name).ToArray();
            log.AppendLine($"CLIPS: {clips.Length}");
            foreach (var c in clips)
                log.AppendLine($"   {c.name,-18} len={c.length:F2}s  loop={c.isLooping}");

            // --- model dims ---
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            var inst = Object.Instantiate(model);
            var rends = inst.GetComponentsInChildren<Renderer>();
            var b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);
            log.AppendLine($"MODEL bounds size = {b.size}  (height Y should be ~0.30)");
            log.AppendLine($"lowest Y = {b.min.y:F3} (feet ~0)");
            int totalTris = inst.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Sum(s => s.sharedMesh != null ? s.sharedMesh.triangles.Length / 3 : 0);
            Object.DestroyImmediate(inst);

            // --- material ---
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            log.AppendLine($"MATERIAL shader={mat.shader.name} instancing={mat.enableInstancing} " +
                           $"metallic={mat.GetFloat("_Metallic")} smooth={mat.GetFloat("_Smoothness")} baseMap={(mat.GetTexture("_BaseMap") != null)}");
            var tex = mat.GetTexture("_BaseMap") as Texture2D;
            if (tex != null) log.AppendLine($"   albedo {tex.width}x{tex.height}");

            // --- controller ---
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            log.AppendLine($"CONTROLLER params={ac.parameters.Length} states={ac.layers[0].stateMachine.states.Length} " +
                           $"default={ac.layers[0].stateMachine.defaultState.name}");
            int emptyMotion = ac.layers[0].stateMachine.states.Count(s => s.state.motion == null);
            log.AppendLine($"   states with NO motion: {emptyMotion} (want 0)");
            log.AppendLine("   params: " + string.Join(", ", ac.parameters.Select(p => p.name + ":" + p.type)));

            // --- prefab ---
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var pi = Object.Instantiate(prefab);
            var an = pi.GetComponent<Animator>();
            var lg = pi.GetComponent<LODGroup>();
            var pc = pi.GetComponent<JoburgRunner.Characters.Pigeon.PigeonController>();
            var col = pi.GetComponent<CapsuleCollider>();
            var au = pi.GetComponent<AudioSource>();
            log.AppendLine($"PREFAB: Animator={an != null}(ctrl={(an != null && an.runtimeAnimatorController != null)},rootMotion={an?.applyRootMotion}) " +
                           $"LODGroup={lg != null}(lods={lg?.lodCount}) PigeonController={pc != null} Capsule={col != null}(trigger={col?.isTrigger}) Audio={au != null}");
            var so = new SerializedObject(pc);
            log.AppendLine($"   PC wiring: animator={so.FindProperty("animator").objectReferenceValue != null} " +
                           $"groundCheck={so.FindProperty("groundCheck").objectReferenceValue != null} " +
                           $"audio={so.FindProperty("audioSource").objectReferenceValue != null} " +
                           $"collider={so.FindProperty("bodyCollider").objectReferenceValue != null}");
            log.AppendLine($"   materials shared (no per-instance): {pi.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r => r.sharedMaterial).Distinct().Count()} unique material(s)");
            log.AppendLine($"   total LOD0+1+2 tris = {totalTris}");
            Object.DestroyImmediate(pi);

            log.AppendLine("===== END =====");
            Debug.Log(log.ToString());
        }
    }
}
