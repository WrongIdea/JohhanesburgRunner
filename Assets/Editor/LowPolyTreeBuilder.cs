using System.Linq;
using JoburgRunner.Environment.Decor;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Imports the low-poly tree extracted from the donated scene GLB (one tree,
    /// Bark + alpha-masked Leaves), builds a <c>DecorTree_LowPoly</c> prefab
    /// height-matched to the jacaranda, then wires it into the roadside tree
    /// sockets: it is added to the random-pick tree rules of the non-jacaranda-palette
    /// districts and every tree rule is densified so the pavement no longer has empty
    /// gaps between trees. The purple JacarandaAvenue pair is left prefab-unchanged
    /// (its palette tint would muddy a green tree) but is densified too.
    ///
    /// Run via the menu, or headless:
    /// <c>-executeMethod JoburgRunner.Editor.LowPolyTreeBuilder.BuildAll</c>.
    /// </summary>
    public static class LowPolyTreeBuilder
    {
        const string Folder = "Assets/Environment/Roadside";
        const string ModelPath = Folder + "/LowPolyTree.fbx";
        const string BarkTex = Folder + "/Tree_Bark.png";
        const string LeavesTex = Folder + "/Tree_Leaves.png";
        const string BarkMatPath = "Assets/Materials/LowPolyTree_Bark.mat";
        const string LeavesMatPath = "Assets/Materials/LowPolyTree_Leaves.mat";
        const string PrefabPath = "Assets/Prefabs/Decor/DecorTree_LowPoly.prefab";
        const string RefJacaranda = "Assets/Prefabs/Decor/DecorTree_JacarandaA.prefab";

        // Random-pick tree rules that may take the green tree as an extra candidate.
        static readonly string[] AddToDistricts =
        {
            "Assets/Environment/Decor/District_Park.asset",
            "Assets/Environment/Decor/District_Business.asset",
            "Assets/Environment/Decor/District_CBD.asset",
            "Assets/Environment/Decor/District_Commissioner.asset",
        };

        [MenuItem("Joburg Runner/Assets/Build & Place Low-Poly Tree")]
        public static void BuildAll()
        {
            ConfigureTextures();
            ConfigureModel();

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError("[LowPolyTree] FBX not imported at " + ModelPath);
                return;
            }

            Material bark = MakeBark();
            Material leaves = MakeLeaves();
            GameObject prefab = BuildPrefab(model, bark, leaves);
            WireIntoDistricts(prefab);
            DensifyTreeRules();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LowPolyTree] Done — prefab built, added to 4 districts, all tree rules densified.");
        }

        // ------------------------------------------------------------- import
        static void ConfigureModel()
        {
            if (!(AssetImporter.GetAtPath(ModelPath) is ModelImporter importer))
            {
                return;
            }
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.generateSecondaryUV = false;
            importer.useFileScale = true;
            importer.SaveAndReimport();
        }

        static void ConfigureTextures()
        {
            if (AssetImporter.GetAtPath(BarkTex) is TextureImporter bark)
            {
                bark.textureType = TextureImporterType.Default;
                bark.sRGBTexture = true;
                bark.maxTextureSize = 512;
                bark.textureCompression = TextureImporterCompression.Compressed;
                bark.mipmapEnabled = true;
                bark.SaveAndReimport();
            }
            if (AssetImporter.GetAtPath(LeavesTex) is TextureImporter leaves)
            {
                leaves.textureType = TextureImporterType.Default;
                leaves.sRGBTexture = true;
                leaves.alphaSource = TextureImporterAlphaSource.FromInput; // keep the leaf mask
                leaves.alphaIsTransparency = true;
                leaves.maxTextureSize = 512;
                leaves.textureCompression = TextureImporterCompression.Compressed;
                leaves.mipmapEnabled = true;
                leaves.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------- materials
        static Material MakeBark()
        {
            Material m = LoadOrCreate(BarkMatPath);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BarkTex));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.05f);
            m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off); // GLB bark is double-sided
            m.doubleSidedGI = true;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material MakeLeaves()
        {
            Material m = LoadOrCreate(LeavesMatPath);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(LeavesTex));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.05f);
            // Alpha-clipped leaf cards (glTF alphaMode MASK), double-sided.
            m.SetFloat("_AlphaClip", 1f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cutoff", 0.5f);
            m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            m.doubleSidedGI = true;
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material LoadOrCreate(string path)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        // ------------------------------------------------------------- prefab
        static GameObject BuildPrefab(GameObject model, Material bark, Material leaves)
        {
            GameObject root = new GameObject("DecorTree_LowPoly");
            GameObject visual = Object.Instantiate(model, root.transform);
            visual.name = "LowPolyTree_Mesh";
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (LODGroup stray in visual.GetComponentsInChildren<LODGroup>(true))
            {
                Object.DestroyImmediate(stray);
            }

            MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;
                // Deterministic FBX slot order: 0 = Bark, 1 = Leaves.
                if (mats.Length >= 1) mats[0] = bark;
                if (mats.Length >= 2) mats[1] = leaves;
                r.sharedMaterials = mats;
            }

            // Height-match the jacaranda so the mix reads consistently, whatever the
            // FBX's native units. Falls back to ~6.5 m if the reference is missing.
            float myHeight = CombinedHeight(renderers);
            float refHeight = JacarandaHeight();
            float target = refHeight > 0.01f ? refHeight : 6.5f;
            float scale = myHeight > 0.01f ? target / myHeight : 1f;
            root.transform.localScale = Vector3.one * scale;
            Debug.Log($"[LowPolyTree] tree height={myHeight:F2} m, jacaranda={refHeight:F2} m -> scale {scale:F3}");

            LODGroup lod = root.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(0.02f, renderers.Cast<Renderer>().ToArray()) });
            lod.RecalculateBounds();

            EnsureFolder("Assets/Prefabs", "Decor");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static float CombinedHeight(Renderer[] renderers)
        {
            if (renderers.Length == 0) return 0f;
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b.size.y;
        }

        static float JacarandaHeight()
        {
            GameObject jac = AssetDatabase.LoadAssetAtPath<GameObject>(RefJacaranda);
            if (jac == null) return 0f;
            GameObject inst = Object.Instantiate(jac);
            float h = CombinedHeight(inst.GetComponentsInChildren<Renderer>(true));
            Object.DestroyImmediate(inst);
            return h;
        }

        // ------------------------------------------------------------- wiring
        static void WireIntoDistricts(GameObject prefab)
        {
            foreach (string path in AddToDistricts)
            {
                DistrictDecorProfile profile = AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>(path);
                DecorRule rule = profile != null ? profile.GetRule(DecorSocketType.Tree) : null;
                if (rule == null)
                {
                    Debug.LogWarning("[LowPolyTree] no Tree rule in " + path);
                    continue;
                }
                if (rule.allowedPrefabs.Contains(prefab))
                {
                    continue;
                }
                rule.allowedPrefabs = rule.allowedPrefabs.Append(prefab).ToArray();
                EditorUtility.SetDirty(profile);
            }
        }

        static void DensifyTreeRules()
        {
            // path, spawnProbability, maxCountPerSegment, minSpacing, maxSpacing
            Densify("Assets/Environment/Decor/District_JacarandaAvenue.asset", 1.0f, 4, 9f, 14f);
            Densify("Assets/Environment/Decor/District_Park.asset", 0.9f, 4, 8f, 15f);
            Densify("Assets/Environment/Decor/District_Business.asset", 0.8f, 3, 9f, 16f);
            Densify("Assets/Environment/Decor/District_CBD.asset", 0.8f, 3, 9f, 16f);
            Densify("Assets/Environment/Decor/District_Commissioner.asset", 0.85f, 3, 9f, 16f);
        }

        static void Densify(string path, float spawn, int maxCount, float minSpacing, float maxSpacing)
        {
            DistrictDecorProfile profile = AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>(path);
            DecorRule rule = profile != null ? profile.GetRule(DecorSocketType.Tree) : null;
            if (rule == null)
            {
                Debug.LogWarning("[LowPolyTree] no Tree rule to densify in " + path);
                return;
            }
            rule.spawnProbability = spawn;
            rule.maxCountPerSegment = maxCount;
            rule.minSpacing = minSpacing;
            rule.maxSpacing = maxSpacing;
            EditorUtility.SetDirty(profile);
        }

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
