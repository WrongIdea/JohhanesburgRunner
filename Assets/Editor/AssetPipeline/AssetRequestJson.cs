using System;
using System.Text;
using System.Text.RegularExpressions;
using JoziRunner.AssetPipeline;
using UnityEngine;

namespace JoziRunner.AssetPipeline.Editor
{
    public static class AssetRequestJson
    {
        public static GeneratedAssetRequest Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            string normalized = ReplaceEnum<GeneratedAssetCategory>(json, "assetCategory");
            normalized = ReplaceEnum<GeneratedColliderType>(normalized, "colliderType");
            normalized = ReplaceEnum<AssetRequestStatus>(normalized, "status");
            normalized = ReplaceEnum<AssetApprovalState>(normalized, "approvalState");
            return JsonUtility.FromJson<GeneratedAssetRequest>(normalized);
        }

        public static string UpdateReview(string json, AssetRequestStatus status, AssetApprovalState state, string notes)
        {
            string updated = ReplaceString(json, "status", status.ToString());
            updated = ReplaceString(updated, "approvalState", state.ToString());
            updated = ReplaceString(updated, "reviewNotes", Escape(notes ?? string.Empty));
            return updated;
        }

        static string ReplaceString(string json, string field, string value)
        {
            string pattern = "(\\\"" + Regex.Escape(field) + "\\\"\\s*:\\s*)\\\"(?:\\\\.|[^\\\"])*\\\"";
            return Regex.Replace(json, pattern, match => match.Groups[1].Value + "\"" + value + "\"");
        }

        static string Escape(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default: builder.Append(c); break;
                }
            }
            return builder.ToString();
        }

        static string ReplaceEnum<T>(string json, string field) where T : struct, Enum
        {
            string pattern = "(\\\"" + Regex.Escape(field) + "\\\"\\s*:\\s*)\\\"([^\\\"]+)\\\"";
            return Regex.Replace(json, pattern, match =>
            {
                if (!Enum.TryParse(match.Groups[2].Value, true, out T value)) return match.Value;
                return match.Groups[1].Value + Convert.ToInt32(value);
            });
        }
    }
}
