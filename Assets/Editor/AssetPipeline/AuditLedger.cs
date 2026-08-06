using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JoziRunner.AssetPipeline.Editor
{
    [Serializable]
    public sealed class AuditEvent
    {
        public string schemaVersion = "1.0";
        public string actionId;
        public string correlationId;
        public string parentActionId;
        public string assetId;
        public string timestampUtc;
        public string actor;
        public string provider;
        public string operation;
        public string model;
        public string authenticationType;
        public string status;
        public bool approvalRequired;
        public string approvalStatus;
        public string approvedBy;
        public string approvalTimestampUtc;
        public string startTimestampUtc;
        public string finishTimestampUtc;
        public long? durationMilliseconds;
        public long? inputTokens;
        public long? cachedInputTokens;
        public long? outputTokens;
        public long? totalTokens;
        public string tokenSource;
        public decimal? estimatedCost;
        public decimal? actualCost;
        public string currency;
        public string costType = "free-local-operation";
        public string pricingVersion;
        public decimal? meshyBalanceBefore;
        public decimal? meshyBalanceAfter;
        public decimal? meshyCreditsConsumed;
        public decimal? estimatedMeshyCredits;
        public List<string> filesRead = new List<string>();
        public List<string> filesCreated = new List<string>();
        public List<string> filesModified = new List<string>();
        public List<string> commandsExecuted = new List<string>();
        public string gitCommitBefore;
        public string gitCommitAfter;
        public string promptHash;
        public string resultHash;
        public string previousRecordHash;
        public string recordHash;
        public string errorCode;
        public string redactedErrorMessage;
        public string rawEventFilePath;
    }

    public static class AuditLedger
    {
        public static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        public static string LedgerPath => Path.Combine(ProjectRoot, "Audit/Ledger/actions.jsonl");

        public static AuditEvent NewUnityEvent(string operation, string assetId = null)
        {
            string id = Guid.NewGuid().ToString();
            return new AuditEvent
            {
                actionId = id,
                correlationId = id,
                assetId = assetId,
                timestampUtc = DateTime.UtcNow.ToString("O"),
                actor = Environment.UserName,
                provider = "unity",
                operation = operation,
                authenticationType = "none",
                status = "completed",
                approvalRequired = false,
                costType = "free-local-operation",
                currency = "USD",
                estimatedCost = 0m,
                actualCost = 0m
            };
        }

        public static void Append(AuditEvent auditEvent)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LedgerPath));
            auditEvent.timestampUtc = string.IsNullOrEmpty(auditEvent.timestampUtc) ? DateTime.UtcNow.ToString("O") : auditEvent.timestampUtc;
            auditEvent.redactedErrorMessage = Redact(auditEvent.redactedErrorMessage);
            auditEvent.previousRecordHash = LastRecordHash();
            auditEvent.recordHash = null;
            string canonical = Serialize(auditEvent, false);
            auditEvent.recordHash = HashText(canonical);
            string line = Serialize(auditEvent, true) + "\n";
            using (var stream = new FileStream(LedgerPath, FileMode.Append, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                writer.Write(line);
        }

        public static void VerifyAppendFromBatch()
        {
            var audit = NewUnityEvent("audit-ledger-smoke-test");
            audit.filesRead.Add("Audit/Schemas/audit-event.v1.schema.json");
            audit.resultHash = "unity-to-jsonl-cross-runtime-verification";
            Append(audit);
            Debug.Log("Appended Unity audit ledger smoke-test event: " + audit.actionId);
        }

        public static string Redact(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            value = Regex.Replace(value, @"(?i)(authorization\s*[:=]\s*)(?:bearer\s+)?[^\s,;]+", "$1[REDACTED]");
            value = Regex.Replace(value, @"(?i)((?:api[_-]?key|access[_-]?token|secret|password)\s*[:=]\s*)[^\s,;]+", "$1[REDACTED]");
            return Regex.Replace(value, @"\bsk-[A-Za-z0-9_-]{12,}\b", "[REDACTED]");
        }

        static string LastRecordHash()
        {
            if (!File.Exists(LedgerPath)) return null;
            string last = File.ReadLines(LedgerPath).LastOrDefault(x => !string.IsNullOrWhiteSpace(x));
            if (last == null) return null;
            Match match = Regex.Match(last, "\"recordHash\":\"([a-fA-F0-9]{64})\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        static string Serialize(AuditEvent value, bool includeRecordHash)
        {
            var fields = typeof(AuditEvent).GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => includeRecordHash || x.Name != nameof(AuditEvent.recordHash))
                .OrderBy(x => x.Name, StringComparer.Ordinal);
            return "{" + string.Join(",", fields.Select(x => Quote(x.Name) + ":" + JsonValue(x.GetValue(value)))) + "}";
        }

        static string JsonValue(object value)
        {
            if (value == null) return "null";
            if (value is string text) return Quote(text);
            if (value is bool flag) return flag ? "true" : "false";
            if (value is decimal money) return money.ToString(CultureInfo.InvariantCulture);
            if (value is float || value is double) return Convert.ToDouble(value).ToString("R", CultureInfo.InvariantCulture);
            if (value is byte || value is short || value is int || value is long ||
                value is sbyte || value is ushort || value is uint || value is ulong)
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            if (value is IEnumerable enumerable)
            {
                var items = new List<string>();
                foreach (object item in enumerable) items.Add(JsonValue(item));
                return "[" + string.Join(",", items) + "]";
            }
            return Quote(value.ToString());
        }

        static string Quote(string value)
        {
            if (value == null) return "null";
            var builder = new StringBuilder("\"");
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 32) builder.Append("\\u" + ((int)c).ToString("x4"));
                        else builder.Append(c);
                        break;
                }
            }
            return builder.Append('"').ToString();
        }

        public static string HashText(string value)
        {
            using (SHA256 hash = SHA256.Create())
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(x => x.ToString("x2")));
        }
    }
}
