using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Ages the road surface: swaps the flat, untextured, smooth <c>Asphalt.mat</c>
    /// for a matte, weathered look driven by a seamless aged-asphalt albedo + normal
    /// (mottled warm grey, oil stains, faded patches, a thin crack network), tiled at
    /// a realistic ~3.5 m across the 8.45 × 30 m road slab. Also dulls the lane
    /// markings to a worn off-white so they read as sun-bleached, not freshly painted.
    ///
    /// Menu, or headless: <c>-executeMethod JoburgRunner.Editor.AgedRoadBuilder.Apply</c>.
    /// </summary>
    public static class AgedRoadBuilder
    {
        const string AlbedoPath = "Assets/Textures/AgedAsphalt_Albedo.png";
        const string NormalPath = "Assets/Textures/AgedAsphalt_Normal.png";
        const string AsphaltMat = "Assets/Materials/Asphalt.mat";
        const string MarkingMat = "Assets/Materials/RoadMarkingWhite.mat";

        // Road slab is 8.45 m wide (local X / UV u) by 30 m long (local Z / UV v).
        static readonly Vector2 Tiling = new Vector2(8.45f / 3.5f, 30f / 3.5f);

        [MenuItem("Joburg Runner/Assets/Age the Road")]
        public static void Apply()
        {
            ConfigureTextures();

            Material asphalt = AssetDatabase.LoadAssetAtPath<Material>(AsphaltMat);
            if (asphalt == null)
            {
                Debug.LogError("[AgedRoad] Asphalt.mat not found.");
                return;
            }
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);

            asphalt.SetTexture("_BaseMap", albedo);
            asphalt.SetTextureScale("_BaseMap", Tiling);
            asphalt.SetColor("_BaseColor", new Color(1f, 0.99f, 0.96f)); // let the grey texture read
            asphalt.SetColor("_Color", new Color(1f, 0.99f, 0.96f));
            if (normal != null)
            {
                asphalt.SetTexture("_BumpMap", normal);
                asphalt.SetTextureScale("_BumpMap", Tiling);
                asphalt.EnableKeyword("_NORMALMAP");
                asphalt.SetFloat("_BumpScale", 1f);
            }
            asphalt.SetFloat("_Metallic", 0f);
            asphalt.SetFloat("_Smoothness", 0.08f); // aged asphalt is matte, not glossy
            EditorUtility.SetDirty(asphalt);

            // Worn, sun-bleached lane paint (still visible, just not fresh white).
            Material marking = AssetDatabase.LoadAssetAtPath<Material>(MarkingMat);
            if (marking != null)
            {
                Color worn = new Color(0.72f, 0.71f, 0.66f, 1f);
                marking.SetColor("_BaseColor", worn);
                marking.SetColor("_Color", worn);
                marking.SetFloat("_Smoothness", 0.1f);
                EditorUtility.SetDirty(marking);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AgedRoad] Applied aged asphalt (tiling {Tiling.x:F1}x{Tiling.y:F1}) + worn markings.");
        }

        static void ConfigureTextures()
        {
            if (AssetImporter.GetAtPath(AlbedoPath) is TextureImporter a)
            {
                a.textureType = TextureImporterType.Default;
                a.sRGBTexture = true;
                a.wrapMode = TextureWrapMode.Repeat;
                a.maxTextureSize = 1024;
                a.textureCompression = TextureImporterCompression.Compressed;
                a.mipmapEnabled = true;
                a.SaveAndReimport();
            }
            if (AssetImporter.GetAtPath(NormalPath) is TextureImporter n)
            {
                n.textureType = TextureImporterType.NormalMap;
                n.wrapMode = TextureWrapMode.Repeat;
                n.maxTextureSize = 1024;
                n.textureCompression = TextureImporterCompression.Compressed;
                n.mipmapEnabled = true;
                n.SaveAndReimport();
            }
        }
    }
}
