using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JoziRunner.AssetPipeline;
using UnityEditor;
using UnityEngine;

namespace JoziRunner.AssetPipeline.Editor
{
    public static class GeneratedAssetValidator
    {
        public const string IncomingRoot = "Assets/Art/Generated/Incoming";

        public static AssetValidationReport Validate(GameObject model, string sourcePath, GeneratedAssetRequest request = null)
        {
            var report = new AssetValidationReport
            {
                sourcePath = sourcePath,
                generatedUtc = DateTime.UtcNow.ToString("O"),
                rootObjectCount = model == null ? 0 : model.transform.childCount
            };
            if (model == null)
            {
                Warn(report, "MODEL_MISSING", "The model could not be loaded as a GameObject.");
                return report;
            }

            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            var skinned = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var meshes = filters.Select(x => x.sharedMesh).Concat(skinned.Select(x => x.sharedMesh)).Where(x => x != null).Distinct().ToArray();

            foreach (var mesh in meshes)
            {
                report.meshNames.Add(mesh.name);
                report.vertexCount += mesh.vertexCount;
                for (int s = 0; s < mesh.subMeshCount; s++)
                    report.triangleCount += (int)(mesh.GetIndexCount(s) / 3);
            }

            var materials = renderers.SelectMany(x => x.sharedMaterials).Where(x => x != null).Distinct().ToArray();
            report.materialCount = materials.Length;
            report.materialNames.AddRange(materials.Select(x => x.name));
            if (renderers.Any(x => x.sharedMaterials.Any(m => m == null)))
                Warn(report, "MISSING_MATERIAL", "One or more renderer material slots are empty.");

            var textures = new HashSet<Texture>();
            foreach (var material in materials)
            {
                foreach (string property in material.GetTexturePropertyNames())
                {
                    Texture texture = material.GetTexture(property);
                    if (texture != null) textures.Add(texture);
                }
            }
            report.textureCount = textures.Count;
            foreach (Texture texture in textures)
            {
                report.textureDimensions.Add($"{texture.name}: {texture.width}x{texture.height}");
                report.estimatedTextureMemoryBytes += EstimateTextureBytes(texture);
                int limit = request != null ? request.textureResolution : Settings.defaultTextureResolution;
                if (texture.width > limit || texture.height > limit)
                    Warn(report, "TEXTURE_OVERSIZE", $"{texture.name} is {texture.width}x{texture.height}; requested limit is {limit}.");
            }
            if (materials.Length > 0 && textures.Count == 0)
                Warn(report, "MISSING_TEXTURE", "Materials were found but no assigned textures were detected.");

            Bounds bounds = CalculateBounds(renderers, model.transform);
            report.boundsMetres.width = bounds.size.x;
            report.boundsMetres.height = bounds.size.y;
            report.boundsMetres.depth = bounds.size.z;
            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxDimension < 0.05f || maxDimension > 200f)
                Warn(report, "ABNORMAL_SCALE", $"Model bounds {Format(bounds.size)} are outside the default 0.05–200 m sanity range.");
            if (bounds.min.y < -0.01f)
                Warn(report, "BELOW_GROUND", $"Model extends {Mathf.Abs(bounds.min.y):0.###} m below its root ground plane.");
            if (Quaternion.Angle(model.transform.localRotation, Quaternion.identity) > 0.1f)
                Warn(report, "ROOT_ROTATION", "Root rotation is not identity; confirm the model faces Unity +Z with +Y up.");
            if (model.transform.childCount > 1)
                Warn(report, "MULTIPLE_ROOTS", $"The imported model has {model.transform.childCount} top-level objects.");
            if (HasNegativeScale(model.transform))
                Warn(report, "NEGATIVE_SCALE", "One or more transforms use negative scale.");

            var duplicates = materials.GroupBy(NormalizeMaterialName).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            if (duplicates.Length > 0)
                Warn(report, "DUPLICATE_MATERIALS", "Likely duplicate materials: " + string.Join(", ", duplicates));

            int maxTriangles = request != null ? request.maximumTriangleCount : Settings.MaximumTriangles(GuessCategory(sourcePath));
            if (maxTriangles > 0 && report.triangleCount > maxTriangles)
                Warn(report, "TRIANGLE_BUDGET", $"{report.triangleCount:N0} triangles exceed the {maxTriangles:N0} warning threshold.");
            int maxMaterials = request != null && request.assetCategory == GeneratedAssetCategory.Vehicle
                ? Settings.vehicleMaxMaterials : Settings.ordinaryPropMaxMaterials;
            if (report.materialCount > maxMaterials)
                Warn(report, "MATERIAL_BUDGET", $"{report.materialCount} materials exceed the {maxMaterials} material warning threshold.");

            return report;
        }

        public static string ReportPath(string sourcePath)
        {
            string directory = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/') ?? IncomingRoot;
            return $"{directory}/{Path.GetFileNameWithoutExtension(sourcePath)}.validation.json";
        }

        public static void SaveReport(AssetValidationReport report)
        {
            if (report == null || string.IsNullOrEmpty(report.sourcePath)) return;
            File.WriteAllText(ReportPath(report.sourcePath), JsonUtility.ToJson(report, true));
        }

        static Bounds CalculateBounds(Renderer[] renderers, Transform root)
        {
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
            Bounds world = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) world.Encapsulate(renderers[i].bounds);
            Vector3 centre = root.InverseTransformPoint(world.center);
            Vector3 size = root.InverseTransformVector(world.size);
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            return new Bounds(centre, size);
        }

        static bool HasNegativeScale(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.localScale.x < 0f || t.localScale.y < 0f || t.localScale.z < 0f) return true;
            return false;
        }

        static string NormalizeMaterialName(Material material)
            => material.name.ToLowerInvariant().Replace("(instance)", "").Replace("_", "").Replace(" ", "").Replace("material", "");

        static long EstimateTextureBytes(Texture texture)
        {
            // Conservative uncompressed RGBA estimate including a 4/3 mip-chain factor.
            return (long)Math.Ceiling(texture.width * texture.height * 4d * 4d / 3d);
        }

        static GeneratedAssetCategory GuessCategory(string path)
        {
            string p = path.ToLowerInvariant();
            if (p.Contains("vehicle") || p.Contains("taxi") || p.Contains("car")) return GeneratedAssetCategory.Vehicle;
            if (p.Contains("obstacle") || p.Contains("barrier")) return GeneratedAssetCategory.Obstacle;
            if (p.Contains("building")) return GeneratedAssetCategory.StandardBuilding;
            return GeneratedAssetCategory.SmallProp;
        }

        static AssetPipelineSettings Settings
        {
            get
            {
                string[] guids = AssetDatabase.FindAssets("t:AssetPipelineSettings");
                if (guids.Length > 0)
                    return AssetDatabase.LoadAssetAtPath<AssetPipelineSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
                return ScriptableObject.CreateInstance<AssetPipelineSettings>();
            }
        }

        static string Format(Vector3 v) => $"{v.x:0.###} x {v.y:0.###} x {v.z:0.###} m";
        static void Warn(AssetValidationReport report, string code, string message)
            => report.warnings.Add(new AssetValidationWarning { code = code, message = message });
    }
}
