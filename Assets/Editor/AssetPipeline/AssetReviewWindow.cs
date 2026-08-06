using System;
using System.IO;
using System.Linq;
using JoziRunner.AssetPipeline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoziRunner.AssetPipeline.Editor
{
    public sealed class AssetReviewWindow : EditorWindow
    {
        GameObject incomingModel;
        TextAsset requestJson;
        GeneratedAssetRequest request;
        AssetValidationReport report;
        Vector2 scroll;
        string rejectionNotes;

        [MenuItem("Jozi Runner/Asset Pipeline/Asset Review")]
        static void Open() => GetWindow<AssetReviewWindow>("Asset Review");

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Review is non-destructive. Import never implies approval, and no production scene or spawn set is changed.", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            incomingModel = (GameObject)EditorGUILayout.ObjectField("Incoming Model", incomingModel, typeof(GameObject), false);
            requestJson = (TextAsset)EditorGUILayout.ObjectField("Request JSON", requestJson, typeof(TextAsset), false);
            if (EditorGUI.EndChangeCheck()) RefreshReport();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Validation")) RefreshReport();
                if (GUILayout.Button("Open In Review Scene")) OpenInReviewScene();
            }

            if (report == null) return;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Source", report.sourcePath);
            EditorGUILayout.LabelField("Triangles", report.triangleCount.ToString("N0"));
            EditorGUILayout.LabelField("Vertices", report.vertexCount.ToString("N0"));
            EditorGUILayout.LabelField("Materials", report.materialCount.ToString());
            EditorGUILayout.LabelField("Texture memory estimate", EditorUtility.FormatBytes(report.estimatedTextureMemoryBytes));
            EditorGUILayout.LabelField("Dimensions", $"{report.boundsMetres.width:0.###} × {report.boundsMetres.height:0.###} × {report.boundsMetres.depth:0.###} m");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation warnings", EditorStyles.boldLabel);
            if (report.warnings.Count == 0) EditorGUILayout.HelpBox("No warnings. Manual visual approval is still required.", MessageType.Info);
            foreach (var warning in report.warnings)
                EditorGUILayout.HelpBox($"[{warning.code}] {warning.message}", MessageType.Warning);
            EditorGUILayout.Space();
            rejectionNotes = EditorGUILayout.TextField("Rejection notes", rejectionNotes);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = incomingModel != null;
                if (GUILayout.Button("Approve")) Approve();
                if (GUILayout.Button("Reject")) Reject();
                GUI.enabled = true;
            }
            EditorGUILayout.EndScrollView();
        }

        void RefreshReport()
        {
            request = null;
            if (requestJson != null)
            {
                try { request = AssetRequestJson.Parse(requestJson.text); }
                catch (Exception e) { Debug.LogWarning("Could not parse request JSON: " + e.Message); }
            }
            string path = incomingModel == null ? string.Empty : AssetDatabase.GetAssetPath(incomingModel);
            report = incomingModel == null ? null : GeneratedAssetValidator.Validate(incomingModel, path, request);
            if (report != null)
            {
                GeneratedAssetValidator.SaveReport(report);
                AssetDatabase.Refresh();
            }
            Repaint();
        }

        void OpenInReviewScene()
        {
            if (incomingModel == null) return;
            if (!File.Exists(AssetReviewSceneBuilder.ScenePath)) AssetReviewSceneBuilder.CreateOrReset();
            EditorSceneManager.OpenScene(AssetReviewSceneBuilder.ScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("REVIEW_MODEL_ROOT") ?? new GameObject("REVIEW_MODEL_ROOT");
            foreach (Transform child in root.transform) DestroyImmediate(child.gameObject);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(incomingModel, root.transform);
            if (instance == null) instance = Instantiate(incomingModel, root.transform);
            instance.name = incomingModel.name;
            instance.transform.localPosition = Vector3.zero;
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        void Approve()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            RefreshReport();
            string sourcePath = AssetDatabase.GetAssetPath(incomingModel);
            if (!IsIncoming(sourcePath))
            {
                EditorUtility.DisplayDialog("Approval blocked", "Select a model from Assets/Art/Generated/Incoming.", "OK");
                return;
            }
            GeneratedAssetCategory category = request?.assetCategory ?? GuessCategory(sourcePath);
            string categoryFolder = CategoryFolder(category);
            EnsureFolder(categoryFolder);
            EnsureFolder("Assets/Art/Generated/Approved");

            string approvedSource = AssetDatabase.GenerateUniqueAssetPath($"Assets/Art/Generated/Approved/{Path.GetFileName(sourcePath)}");
            if (!AssetDatabase.CopyAsset(sourcePath, approvedSource))
                throw new InvalidOperationException("Could not preserve the approved source copy.");
            string categorySource = AssetDatabase.GenerateUniqueAssetPath($"{categoryFolder}/{Path.GetFileName(sourcePath)}");
            if (!AssetDatabase.CopyAsset(sourcePath, categorySource))
                throw new InvalidOperationException("Could not copy the processed model to its category folder.");
            AssetDatabase.ImportAsset(categorySource, ImportAssetOptions.ForceSynchronousImport);

            GameObject copiedModel = AssetDatabase.LoadAssetAtPath<GameObject>(categorySource);
            GameObject root = new GameObject("PF_" + SafeName(request?.assetName ?? incomingModel.name));
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(copiedModel, root.transform);
            if (visual == null) visual = Instantiate(copiedModel, root.transform);
            visual.name = "Visual";
            ApplyUrpMaterials(visual);
            AddCollider(root, visual, request?.colliderType ?? GeneratedColliderType.Box);
            if (request?.lodsRequired == true && visual.GetComponentsInChildren<Renderer>(true).Length > 0)
                ConfigureNonDestructiveLod(root, visual);
            GameObjectUtility.SetStaticEditorFlags(root, category == GeneratedAssetCategory.Vehicle ? 0 : StaticEditorFlags.BatchingStatic);

            var metadata = root.AddComponent<GeneratedAssetMetadata>();
            metadata.assetId = request?.assetId ?? Path.GetFileNameWithoutExtension(sourcePath);
            metadata.sourceAssetPath = sourcePath;
            metadata.category = category;
            metadata.intendedDistrict = request?.intendedDistrict ?? string.Empty;
            metadata.spawnRole = request?.spawnRole ?? string.Empty;
            metadata.triangleCount = report.triangleCount;
            metadata.materialCount = report.materialCount;
            metadata.estimatedTextureMemoryBytes = report.estimatedTextureMemoryBytes;
            metadata.approvedUtc = DateTime.UtcNow.ToString("O");
            metadata.spawnMetadataEnabled = true;
            string approvedAssetId = metadata.assetId;

            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{categoryFolder}/{root.name}.prefab");
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);
            UpdateRequest(AssetRequestStatus.Approved, AssetApprovalState.Approved, string.Empty);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            timer.Stop();
            var approval = AuditLedger.NewUnityEvent("asset-approval", approvedAssetId);
            approval.approvalRequired = true;
            approval.approvalStatus = "approved";
            approval.approvedBy = Environment.UserName;
            approval.approvalTimestampUtc = DateTime.UtcNow.ToString("O");
            approval.durationMilliseconds = timer.ElapsedMilliseconds;
            approval.filesRead.Add(sourcePath);
            approval.filesCreated.Add(approvedSource);
            AuditLedger.Append(approval);
            var integration = AuditLedger.NewUnityEvent("prefab-integration", approvedAssetId);
            integration.correlationId = approval.correlationId;
            integration.parentActionId = approval.actionId;
            integration.filesCreated.Add(prefabPath);
            integration.resultHash = $"{report.triangleCount}:{report.materialCount}:{request?.colliderType}:{request?.lodsRequired}";
            AuditLedger.Append(integration);
            EditorUtility.DisplayDialog("Asset approved", $"Created isolated prefab:\n{prefabPath}\n\nIt was not added to a production scene or spawn set.", "OK");
        }

        void Reject()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            RefreshReport();
            string sourcePath = AssetDatabase.GetAssetPath(incomingModel);
            if (!IsIncoming(sourcePath)) return;
            EnsureFolder("Assets/Art/Generated/Rejected");
            string name = Path.GetFileNameWithoutExtension(sourcePath);
            var rejection = new
            {
                assetId = request?.assetId ?? name,
                sourcePath,
                rejectedUtc = DateTime.UtcNow.ToString("O"),
                notes = rejectionNotes,
                validationWarnings = report.warnings.Select(x => $"[{x.code}] {x.message}").ToArray(),
                recommendedMeshyRegenerationInstructions = BuildRegenerationInstructions()
            };
            string json = JsonUtility.ToJson(new RejectionReport(rejection.assetId, rejection.sourcePath, rejection.rejectedUtc,
                rejection.notes, rejection.validationWarnings, rejection.recommendedMeshyRegenerationInstructions), true);
            File.WriteAllText($"Assets/Art/Generated/Rejected/{name}.rejection.json", json);
            UpdateRequest(AssetRequestStatus.Rejected, AssetApprovalState.Rejected, rejectionNotes);
            AssetDatabase.Refresh();
            timer.Stop();
            var audit = AuditLedger.NewUnityEvent("asset-rejection", rejection.assetId);
            audit.approvalRequired = true;
            audit.approvalStatus = "rejected";
            audit.approvedBy = Environment.UserName;
            audit.approvalTimestampUtc = DateTime.UtcNow.ToString("O");
            audit.durationMilliseconds = timer.ElapsedMilliseconds;
            audit.filesRead.Add(sourcePath);
            audit.filesCreated.Add($"Assets/Art/Generated/Rejected/{name}.rejection.json");
            audit.redactedErrorMessage = rejectionNotes;
            AuditLedger.Append(audit);
            EditorUtility.DisplayDialog("Asset rejected", "The source remains in Incoming and a rejection report was created.", "OK");
        }

        string BuildRegenerationInstructions()
        {
            string issues = string.Join("; ", report.warnings.Select(x => x.message));
            return "Regenerate only after a new explicit approval. Preserve silhouette and intended dimensions. Address: " +
                   (string.IsNullOrEmpty(issues) ? "the reviewer notes and visual-quality concerns." : issues);
        }

        void UpdateRequest(AssetRequestStatus status, AssetApprovalState state, string notes)
        {
            if (request == null || requestJson == null) return;
            request.status = status;
            request.approvalState = state;
            request.reviewNotes = notes;
            string path = AssetDatabase.GetAssetPath(requestJson);
            File.WriteAllText(path, AssetRequestJson.UpdateReview(requestJson.text, status, state, notes));
        }

        static void AddCollider(GameObject root, GameObject visual, GeneratedColliderType type)
        {
            Bounds b = BoundsFor(visual);
            switch (type)
            {
                case GeneratedColliderType.Box:
                    var box = root.AddComponent<BoxCollider>(); box.center = b.center; box.size = b.size; break;
                case GeneratedColliderType.Capsule:
                    var capsule = root.AddComponent<CapsuleCollider>(); capsule.center = b.center; capsule.height = b.size.y; capsule.radius = Mathf.Max(b.size.x, b.size.z) * 0.5f; break;
                case GeneratedColliderType.Sphere:
                    var sphere = root.AddComponent<SphereCollider>(); sphere.center = b.center; sphere.radius = Mathf.Max(b.extents.x, b.extents.y, b.extents.z); break;
                case GeneratedColliderType.Mesh:
                    var filter = visual.GetComponentInChildren<MeshFilter>(); if (filter != null) { var mesh = root.AddComponent<MeshCollider>(); mesh.sharedMesh = filter.sharedMesh; mesh.convex = false; } break;
            }
        }

        static void ConfigureNonDestructiveLod(GameObject root, GameObject visual)
        {
            // No geometry is generated or decimated. The source renderers form LOD0 only;
            // developers can add approved lower-detail meshes later.
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            var group = root.AddComponent<LODGroup>();
            group.SetLODs(new[] { new LOD(0.1f, renderers) });
            group.RecalculateBounds();
        }

        static void ApplyUrpMaterials(GameObject visual)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                foreach (Material material in renderer.sharedMaterials.Where(x => x != null))
                    if (material.shader == null || material.shader.name == "Standard") material.shader = urpLit;
        }

        static Bounds BoundsFor(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return new Bounds(go.transform.InverseTransformPoint(b.center), b.size);
        }

        static bool IsIncoming(string path) => !string.IsNullOrEmpty(path) && path.Replace('\\', '/').StartsWith(GeneratedAssetValidator.IncomingRoot + "/", StringComparison.OrdinalIgnoreCase);
        static string SafeName(string value) => string.Concat(value.Select(c => char.IsLetterOrDigit(c) ? c : '_')).Trim('_');
        static GeneratedAssetCategory GuessCategory(string path) => path.ToLowerInvariant().Contains("vehicle") || path.ToLowerInvariant().Contains("taxi") ? GeneratedAssetCategory.Vehicle : GeneratedAssetCategory.SmallProp;
        static string CategoryFolder(GeneratedAssetCategory category)
        {
            switch (category)
            {
                case GeneratedAssetCategory.HeroBuilding:
                case GeneratedAssetCategory.StandardBuilding: return "Assets/Art/Buildings";
                case GeneratedAssetCategory.Vehicle: return "Assets/Art/Vehicles";
                case GeneratedAssetCategory.Obstacle: return "Assets/Art/Obstacles";
                default: return "Assets/Art/Props";
            }
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        [Serializable]
        sealed class RejectionReport
        {
            public string assetId, sourcePath, rejectedUtc, notes;
            public string[] validationWarnings;
            public string recommendedMeshyRegenerationInstructions;
            public RejectionReport(string id, string source, string utc, string reviewNotes, string[] warnings, string instructions)
            {
                assetId = id; sourcePath = source; rejectedUtc = utc; notes = reviewNotes;
                validationWarnings = warnings; recommendedMeshyRegenerationInstructions = instructions;
            }
        }
    }
}
