using System.Linq;
using JoburgRunner.Environment.Decor;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Imports the "Jozi Furniture" storefront (decimated to 3 LODs from the ~2M-tri
    /// Meshy source) plus its PBR textures, builds a hero-building prefab
    /// (LODGroup + BoxCollider, one URP/Lit material with base/normal/emission), and
    /// appends it as an entry to the shared <see cref="HeroBuildingSet"/> so the
    /// EnvironmentDecorDirector can spawn it at the CBD building line alongside the
    /// existing JHB heroes. Authored at real size (~21×13×8.5 m) so uniformScale = 1.
    ///
    /// Menu, or headless: <c>-executeMethod JoburgRunner.Editor.JoziFurnitureHeroBuilder.BuildAll</c>.
    /// </summary>
    public static class JoziFurnitureHeroBuilder
    {
        const string Dir = "Assets/Environment/Buildings/ImportedHeroes/JHB_JoziFurniture";
        const string Lod0 = Dir + "/Models/JHB_JoziFurniture_LOD0.fbx";
        const string Lod1 = Dir + "/Models/JHB_JoziFurniture_LOD1.fbx";
        const string Lod2 = Dir + "/Models/JHB_JoziFurniture_LOD2.fbx";
        const string BaseTex = Dir + "/Textures/JHB_JoziFurniture_BaseColor.png";
        const string NormalTex = Dir + "/Textures/JHB_JoziFurniture_Normal.png";
        const string EmissionTex = Dir + "/Textures/JHB_JoziFurniture_Emission.png";
        const string MatPath = Dir + "/Materials/M_JHB_JoziFurniture.mat";
        const string PrefabPath = Dir + "/Prefabs/JHB_JoziFurniture.prefab";
        const string SetPath = "Assets/Environment/Decor/HeroBuildingSet.asset";

        [MenuItem("Joburg Runner/Assets/Build & Add Jozi Furniture Hero")]
        public static void BuildAll()
        {
            ConfigureTextures();
            ConfigureModels();
            Material mat = CreateMaterial();
            GameObject prefab = BuildPrefab(mat);
            if (prefab == null)
            {
                return;
            }
            AddToHeroSet(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[JoziHero] Done — prefab built and added to HeroBuildingSet.");
        }

        // ------------------------------------------------------------- import
        static void ConfigureModels()
        {
            foreach (string p in new[] { Lod0, Lod1, Lod2 })
            {
                if (AssetImporter.GetAtPath(p) is ModelImporter mi)
                {
                    mi.animationType = ModelImporterAnimationType.None;
                    mi.importAnimation = false;
                    mi.importCameras = false;
                    mi.importLights = false;
                    mi.materialImportMode = ModelImporterMaterialImportMode.None;
                    mi.meshCompression = ModelImporterMeshCompression.Medium;
                    mi.isReadable = false;
                    mi.optimizeMeshPolygons = true;
                    mi.optimizeMeshVertices = true;
                    mi.generateSecondaryUV = false;
                    mi.useFileScale = true;
                    mi.SaveAndReimport();
                }
            }
        }

        static void ConfigureTextures()
        {
            SetTex(BaseTex, TextureImporterType.Default, true, 2048);
            SetTex(NormalTex, TextureImporterType.NormalMap, false, 2048);
            SetTex(EmissionTex, TextureImporterType.Default, true, 1024);
        }

        static void SetTex(string path, TextureImporterType type, bool srgb, int max)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter t)
            {
                t.textureType = type;
                t.sRGBTexture = srgb;
                t.maxTextureSize = max;
                t.textureCompression = TextureImporterCompression.Compressed;
                t.mipmapEnabled = true;
                t.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------- material
        static Material CreateMaterial()
        {
            EnsureFolder(Dir, "Materials");
            Material m = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, MatPath);
            }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BaseTex));
            Texture2D nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTex);
            if (nrm != null)
            {
                m.SetTexture("_BumpMap", nrm);
                m.EnableKeyword("_NORMALMAP");
            }
            Texture2D em = AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionTex);
            if (em != null)
            {
                m.SetTexture("_EmissionMap", em);
                m.SetColor("_EmissionColor", Color.white);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.25f);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------- prefab
        static GameObject BuildPrefab(Material mat)
        {
            GameObject lod0 = AssetDatabase.LoadAssetAtPath<GameObject>(Lod0);
            GameObject lod1 = AssetDatabase.LoadAssetAtPath<GameObject>(Lod1);
            GameObject lod2 = AssetDatabase.LoadAssetAtPath<GameObject>(Lod2);
            if (lod0 == null || lod1 == null || lod2 == null)
            {
                Debug.LogError("[JoziHero] one or more LOD FBX failed to import.");
                return null;
            }

            GameObject root = new GameObject("JHB_JoziFurniture");
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            Renderer[] AddLod(GameObject src, string name)
            {
                GameObject inst = Object.Instantiate(src, visual.transform);
                inst.name = name;
                inst.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                MeshRenderer[] rs = inst.GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer r in rs)
                {
                    Material[] ms = r.sharedMaterials;
                    for (int i = 0; i < ms.Length; i++) ms[i] = mat;
                    r.sharedMaterials = ms;
                }
                return rs.Cast<Renderer>().ToArray();
            }

            Renderer[] r0 = AddLod(lod0, "LOD0");
            Renderer[] r1 = AddLod(lod1, "LOD1");
            Renderer[] r2 = AddLod(lod2, "LOD2");

            LODGroup lg = root.AddComponent<LODGroup>();
            lg.SetLODs(new[]
            {
                new LOD(0.30f, r0),
                new LOD(0.10f, r1),
                new LOD(0.03f, r2),
            });
            lg.RecalculateBounds();

            // Box collider from the LOD0 footprint (stripped at runtime by the
            // decorator, kept for editor/authoring and placeholder overlap tests).
            Bounds b = r0[0].bounds;
            for (int i = 1; i < r0.Length; i++) b.Encapsulate(r0[i].bounds);
            BoxCollider bc = root.AddComponent<BoxCollider>();
            bc.center = b.center;   // root is at identity
            bc.size = b.size;

            EnsureFolder(Dir, "Prefabs");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[JoziHero] prefab footprint {b.size.x:F1} x {b.size.z:F1} m, height {b.size.y:F1} m");
            return prefab;
        }

        // ------------------------------------------------------------- catalogue
        static void AddToHeroSet(GameObject prefab)
        {
            HeroBuildingSet set = AssetDatabase.LoadAssetAtPath<HeroBuildingSet>(SetPath);
            if (set == null)
            {
                Debug.LogError("[JoziHero] HeroBuildingSet.asset not found.");
                return;
            }
            if (set.entries.Any(e => e != null && e.prefab == prefab))
            {
                Debug.Log("[JoziHero] already present in the set.");
                return;
            }
            HeroEntry entry = new HeroEntry
            {
                label = "JoziFurniture",
                prefab = prefab,
                weight = 1f,
                allowedSides = (HeroSide)3,   // Left | Right (Both)
                facePlayerDegrees = 0f,
                uniformScale = 1f,
                footprintRadius = 11f,        // ~half of the 21 m facade
                enabled = true,
            };
            set.entries = set.entries.Append(entry).ToArray();
            EditorUtility.SetDirty(set);
            Debug.Log("[JoziHero] added entry; set now has " + set.entries.Length + " entries.");
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
