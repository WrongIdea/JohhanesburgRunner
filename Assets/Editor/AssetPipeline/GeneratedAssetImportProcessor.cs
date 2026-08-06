using System;
using System.IO;
using JoziRunner.AssetPipeline;
using UnityEditor;
using UnityEngine;

namespace JoziRunner.AssetPipeline.Editor
{
    public sealed class GeneratedAssetImportProcessor : AssetPostprocessor
    {
        static bool IsIncomingModel(string path)
        {
            if (!path.Replace('\\', '/').StartsWith(GeneratedAssetValidator.IncomingRoot + "/", StringComparison.OrdinalIgnoreCase))
                return false;
            string extension = Path.GetExtension(path);
            return extension.Equals(".fbx", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".glb", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".gltf", StringComparison.OrdinalIgnoreCase);
        }

        void OnPreprocessModel()
        {
            if (!IsIncomingModel(assetPath)) return;
            if (assetImporter is ModelImporter importer)
            {
                importer.isReadable = true;
            }
        }

        void OnPostprocessModel(GameObject importedRoot)
        {
            if (!IsIncomingModel(assetPath)) return;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            AssetValidationReport report = GeneratedAssetValidator.Validate(importedRoot, assetPath);
            GeneratedAssetValidator.SaveReport(report);
            timer.Stop();
            var audit = AuditLedger.NewUnityEvent("import-validation", Path.GetFileNameWithoutExtension(assetPath));
            audit.startTimestampUtc = DateTime.UtcNow.Subtract(timer.Elapsed).ToString("O");
            audit.finishTimestampUtc = DateTime.UtcNow.ToString("O");
            audit.durationMilliseconds = timer.ElapsedMilliseconds;
            audit.filesRead.Add(assetPath);
            audit.filesCreated.Add(GeneratedAssetValidator.ReportPath(assetPath));
            audit.resultHash = report.triangleCount + ":" + report.materialCount + ":" + report.warnings.Count;
            audit.redactedErrorMessage = report.warnings.Count == 0 ? null : string.Join("; ", report.warnings.ConvertAll(x => x.message));
            AuditLedger.Append(audit);
            Debug.Log($"Generated asset validation: {assetPath} — {report.triangleCount:N0} triangles, " +
                      $"{report.materialCount} materials, {report.warnings.Count} warnings. Asset remains unapproved.");
        }
    }
}
