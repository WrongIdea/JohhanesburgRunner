using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace JoziRunner.AssetPipeline.Editor
{
    public sealed class CostAuditDashboard : EditorWindow
    {
        string dateFilter = "", assetFilter = "", correlationFilter = "", providerFilter = "";
        string operationFilter = "", actorFilter = "", modelFilter = "", statusFilter = "";
        string approvalFilter = "", costTypeFilter = "";
        Vector2 scroll;
        List<Row> rows = new List<Row>();

        [MenuItem("Jozi Runner/AI Pipeline/Cost & Audit Dashboard")]
        static void Open() => GetWindow<CostAuditDashboard>("Cost & Audit");

        void OnEnable() => Reload();

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload", GUILayout.Width(90))) Reload();
                if (GUILayout.Button("Validate Hash Chain", GUILayout.Width(150))) RunAuditTool("validate");
                if (GUILayout.Button("Rebuild SQLite Index", GUILayout.Width(150))) RunAuditTool("project");
                if (GUILayout.Button("Export Filtered CSV", GUILayout.Width(150))) ExportCsv();
            }

            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope()) { dateFilter = Field("Date", dateFilter); assetFilter = Field("Asset ID", assetFilter); correlationFilter = Field("Correlation", correlationFilter); }
            using (new EditorGUILayout.HorizontalScope()) { providerFilter = Field("Provider", providerFilter); operationFilter = Field("Operation", operationFilter); actorFilter = Field("Actor", actorFilter); }
            using (new EditorGUILayout.HorizontalScope()) { modelFilter = Field("Model", modelFilter); statusFilter = Field("Status", statusFilter); approvalFilter = Field("Approval", approvalFilter); costTypeFilter = Field("Cost type", costTypeFilter); }

            List<Row> filtered = Filtered().ToList();
            DateTime today = DateTime.UtcNow.Date;
            DateTime month = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            decimal openAiToday = Sum(filtered, r => r.provider == "openai" && r.timestamp >= today, r => r.estimatedCost);
            decimal openAiMonth = Sum(filtered, r => r.provider == "openai" && r.timestamp >= month, r => r.estimatedCost);
            decimal meshyToday = Sum(filtered, r => r.provider == "meshy" && r.timestamp >= today, r => r.meshyCredits);
            decimal meshyMonth = Sum(filtered, r => r.provider == "meshy" && r.timestamp >= month, r => r.meshyCredits);
            int approved = filtered.Count(r => r.operation == "asset-approval" && r.approvalStatus == "approved");
            int rejected = filtered.Count(r => r.operation == "asset-rejection");
            int assets = filtered.Where(r => !string.IsNullOrEmpty(r.assetId)).Select(r => r.assetId).Distinct().Count();
            decimal approvedAverage = approved == 0 ? 0 : filtered.Where(r => r.approvalStatus == "approved").Sum(r => r.estimatedCost) / approved;
            int pending = filtered.Count(r => r.approvalStatus == "pending");
            int failures = filtered.Count(r => r.status == "failed");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"OpenAI estimate today: ${openAiToday:0.000000}   |   month: ${openAiMonth:0.000000}");
            EditorGUILayout.LabelField($"Meshy credits today: {meshyToday:0.###}   |   month: {meshyMonth:0.###}");
            EditorGUILayout.LabelField($"Assets attempted: {assets}   |   approved: {approved}   |   rejected: {rejected}   |   avg estimated cost/approved asset: ${approvedAverage:0.000000}");
            EditorGUILayout.LabelField($"Pending approvals: {pending}   |   recent failures: {failures}");
            EditorGUILayout.LabelField("Actions by provider: " + GroupSummary(filtered, r => r.provider));
            EditorGUILayout.LabelField("Actions by status: " + GroupSummary(filtered, r => r.status));
            DrawBudget(openAiToday, openAiMonth, meshyToday, meshyMonth);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Events ({filtered.Count})", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (Row row in filtered.OrderByDescending(r => r.timestamp).Take(250))
                EditorGUILayout.LabelField($"{row.timestamp:yyyy-MM-dd HH:mm:ss}  {row.provider}/{row.operation}  {row.status}  {row.assetId}  {row.actionId}", EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
        }

        void Reload()
        {
            rows.Clear();
            if (!File.Exists(AuditLedger.LedgerPath)) return;
            int line = 0;
            foreach (string json in File.ReadLines(AuditLedger.LedgerPath))
            {
                line++;
                if (string.IsNullOrWhiteSpace(json)) continue;
                try { rows.Add(Row.Parse(json)); }
                catch (Exception e) { Debug.LogWarning($"Audit dashboard skipped malformed line {line}: {e.Message}"); }
            }
            Repaint();
        }

        IEnumerable<Row> Filtered() => rows.Where(r =>
            Match(r.timestampText, dateFilter) && Match(r.assetId, assetFilter) &&
            Match(r.correlationId, correlationFilter) && Match(r.provider, providerFilter) &&
            Match(r.operation, operationFilter) && Match(r.actor, actorFilter) &&
            Match(r.model, modelFilter) && Match(r.status, statusFilter) &&
            Match(r.approvalStatus, approvalFilter) && Match(r.costType, costTypeFilter));

        void ExportCsv()
        {
            string defaultName = "audit-filtered-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".csv";
            string path = EditorUtility.SaveFilePanel("Export filtered audit CSV", Path.Combine(AuditLedger.ProjectRoot, "Audit/Reports"), defaultName, "csv");
            if (string.IsNullOrEmpty(path)) return;
            var builder = new StringBuilder("timestampUtc,actionId,correlationId,parentActionId,assetId,actor,provider,operation,model,status,approvalStatus,costType,estimatedCost,meshyCreditsConsumed\n");
            foreach (Row r in Filtered())
                builder.AppendLine(string.Join(",", new[] { r.timestampText, r.actionId, r.correlationId, r.parentActionId, r.assetId, r.actor, r.provider, r.operation, r.model, r.status, r.approvalStatus, r.costType, r.estimatedCost.ToString(CultureInfo.InvariantCulture), r.meshyCredits.ToString(CultureInfo.InvariantCulture) }.Select(Csv)));
            File.WriteAllText(path, builder.ToString());
            Debug.Log("Exported filtered audit CSV: " + path);
        }

        void RunAuditTool(string command)
        {
            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{Path.Combine(AuditLedger.ProjectRoot, "Tools/Audit/audit_cli.py")}\" --project-root \"{AuditLedger.ProjectRoot}\" {command}",
                WorkingDirectory = AuditLedger.ProjectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using (var process = System.Diagnostics.Process.Start(start))
            {
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                string error = AuditLedger.Redact(process.StandardError.ReadToEnd());
                EditorUtility.DisplayDialog("Audit utility", process.ExitCode == 0 ? output : error, "OK");
            }
            Reload();
        }

        void DrawBudget(decimal openAiToday, decimal openAiMonth, decimal meshyToday, decimal meshyMonth)
        {
            string path = Path.Combine(AuditLedger.ProjectRoot, "Audit/Pricing/budget-policy.v1.json");
            if (!File.Exists(path)) return;
            string json = File.ReadAllText(path);
            decimal dailyOpenAi = Number(json, "dailyOpenAIBudgetUsd");
            decimal monthlyOpenAi = Number(json, "monthlyOpenAIBudgetUsd");
            decimal dailyMeshy = Number(json, "dailyMeshyCreditBudget");
            decimal monthlyMeshy = Number(json, "monthlyMeshyCreditBudget");
            EditorGUILayout.LabelField($"Budget used — OpenAI daily: {Percent(openAiToday, dailyOpenAi)}; monthly: {Percent(openAiMonth, monthlyOpenAi)}; Meshy daily: {Percent(meshyToday, dailyMeshy)}; monthly: {Percent(meshyMonth, monthlyMeshy)}");
        }

        static string Field(string label, string value) { EditorGUILayout.LabelField(label, GUILayout.Width(70)); return EditorGUILayout.TextField(value); }
        static bool Match(string value, string filter) => string.IsNullOrEmpty(filter) || (value ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        static decimal Sum(IEnumerable<Row> source, Func<Row, bool> predicate, Func<Row, decimal> selector) => source.Where(predicate).Sum(selector);
        static string GroupSummary(IEnumerable<Row> rows, Func<Row, string> key) => string.Join(", ", rows.GroupBy(key).OrderBy(g => g.Key).Select(g => $"{g.Key ?? "unknown"}={g.Count()}"));
        static string Csv(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        static string Percent(decimal used, decimal budget) => budget <= 0 ? "disabled" : $"{used / budget * 100:0.0}%";
        static decimal Number(string json, string key) { decimal.TryParse(Row.Value(json, key), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result); return result; }

        sealed class Row
        {
            public string actionId, correlationId, parentActionId, assetId, actor, provider, operation, model, status, approvalStatus, costType, timestampText;
            public DateTime timestamp;
            public decimal estimatedCost, meshyCredits;

            public static Row Parse(string json)
            {
                var row = new Row
                {
                    actionId = Value(json, "actionId"), correlationId = Value(json, "correlationId"),
                    parentActionId = Value(json, "parentActionId"), assetId = Value(json, "assetId"),
                    actor = Value(json, "actor"), provider = Value(json, "provider"),
                    operation = Value(json, "operation"), model = Value(json, "model"),
                    status = Value(json, "status"), approvalStatus = Value(json, "approvalStatus"),
                    costType = Value(json, "costType"), timestampText = Value(json, "timestampUtc")
                };
                DateTime.TryParse(row.timestampText, null, DateTimeStyles.AdjustToUniversal, out row.timestamp);
                decimal.TryParse(Value(json, "estimatedCost"), NumberStyles.Any, CultureInfo.InvariantCulture, out row.estimatedCost);
                decimal.TryParse(Value(json, "meshyCreditsConsumed"), NumberStyles.Any, CultureInfo.InvariantCulture, out row.meshyCredits);
                return row;
            }

            public static string Value(string json, string key)
            {
                Match match = Regex.Match(json, "\"" + Regex.Escape(key) + "\":(?:\"((?:\\\\.|[^\"])*)\"|([^,}\\]]+))");
                if (!match.Success) return "";
                string value = match.Groups[1].Success ? Regex.Unescape(match.Groups[1].Value) : match.Groups[2].Value.Trim();
                return value == "null" ? "" : value;
            }
        }
    }
}
