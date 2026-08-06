using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JoziRunner.AssetPipeline;

namespace JoziRunner.AssetPipeline.Editor
{
    /// <summary>
    /// The only intended gateway for a future live Meshy provider. One approval token authorises
    /// one paid submission. Polling and download calls do not consume another approval.
    /// </summary>
    public sealed class AuditedMeshyOperationService
    {
        readonly IAssetGenerationProvider provider;
        readonly HashSet<string> consumedApprovals = new HashSet<string>();

        public AuditedMeshyOperationService(IAssetGenerationProvider provider)
            => this.provider = provider ?? throw new ArgumentNullException(nameof(provider));

        public async Task<GenerationTaskInfo> SubmitApproved(
            GeneratedAssetRequest request,
            string operation,
            string approvalId,
            string approvedBy,
            DateTime approvalTimestampUtc,
            IReadOnlyList<string> imagePaths = null,
            bool ownerBudgetOverride = false,
            CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (provider.IsLiveProvider && !provider.IsConfigured)
                return Blocked(request, operation, "LIVE_PROVIDER_DISABLED", "Live Meshy provider is not explicitly configured.");
            if (string.IsNullOrWhiteSpace(approvalId) || consumedApprovals.Contains(approvalId))
                return Blocked(request, operation, "APPROVAL_MISSING_OR_USED", "A fresh explicit approval is required for exactly one paid generation.");

            decimal? estimate = EstimatedCredits(operation);
            if (provider.IsLiveProvider && estimate == null)
                return Blocked(request, operation, "PRICING_UNKNOWN", "Verified Meshy credit pricing must be configured before a paid submission.");
            if (!GenerationAllowed(request.assetId))
                return Blocked(request, operation, "MAX_GENERATIONS", "Maximum generations per asset has been reached.");

            decimal before = await provider.GetCreditBalance(token);
            BudgetResult budget = EvaluateBudget(ownerBudgetOverride);
            if (!budget.allowed)
                return Blocked(request, operation, "BUDGET_BLOCK", budget.message);
            if (budget.level == "warning" || budget.level == "owner-override")
            {
                var budgetAudit = NewEvent(request, "budget-policy-decision", Guid.NewGuid().ToString());
                budgetAudit.status = budget.level;
                budgetAudit.approvalRequired = budget.level == "owner-override";
                budgetAudit.approvalStatus = ownerBudgetOverride ? "approved" : "not-required";
                budgetAudit.approvedBy = ownerBudgetOverride ? approvedBy : null;
                budgetAudit.approvalTimestampUtc = ownerBudgetOverride ? approvalTimestampUtc.ToUniversalTime().ToString("O") : null;
                budgetAudit.redactedErrorMessage = budget.message;
                AuditLedger.Append(budgetAudit);
            }

            string actionId = Guid.NewGuid().ToString();
            var pending = NewEvent(request, operation, actionId);
            pending.status = "pending";
            pending.approvalRequired = true;
            pending.approvalStatus = "approved";
            pending.approvedBy = approvedBy;
            pending.approvalTimestampUtc = approvalTimestampUtc.ToUniversalTime().ToString("O");
            pending.meshyBalanceBefore = before;
            pending.estimatedMeshyCredits = estimate;
            pending.costType = provider.IsLiveProvider ? "fixed-credit-cost" : "free-local-operation";
            AuditLedger.Append(pending);

            // Consume before submitting so an exception cannot permit an automatic retry.
            consumedApprovals.Add(approvalId);
            try
            {
                GenerationTaskInfo task;
                switch (operation)
                {
                    case "CreateTextTo3DTask":
                        task = await provider.CreateTextTo3DTask(request, token);
                        break;
                    case "CreateImageTo3DTask":
                        task = await provider.CreateImageTo3DTask(request, imagePaths?.FirstOrDefault(), token);
                        break;
                    case "CreateMultiImageTo3DTask":
                        task = await provider.CreateMultiImageTo3DTask(request, imagePaths ?? Array.Empty<string>(), token);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(operation), operation, "Not a paid generation operation.");
                }
                decimal after = await provider.GetCreditBalance(token);
                var completed = NewEvent(request, operation + "-result", Guid.NewGuid().ToString());
                completed.correlationId = pending.correlationId;
                completed.parentActionId = actionId;
                completed.status = task == null ? "failed" : "submitted";
                completed.meshyBalanceBefore = before;
                completed.meshyBalanceAfter = after;
                completed.meshyCreditsConsumed = Math.Max(0m, before - after);
                completed.estimatedMeshyCredits = estimate;
                if (!string.IsNullOrEmpty(task?.taskId))
                {
                    string rawRelative = $"Audit/Raw/Meshy/{completed.actionId}.json";
                    string rawAbsolute = Path.Combine(AuditLedger.ProjectRoot, rawRelative);
                    Directory.CreateDirectory(Path.GetDirectoryName(rawAbsolute));
                    string raw = "{\"actionId\":\"" + completed.actionId + "\",\"providerTaskId\":\"" +
                                 task.taskId.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}";
                    File.WriteAllText(rawAbsolute, raw);
                    completed.rawEventFilePath = rawRelative;
                    completed.resultHash = AuditLedger.HashText(raw);
                    completed.filesCreated.Add(rawRelative);
                }
                completed.costType = provider.IsLiveProvider ? "fixed-credit-cost" : "free-local-operation";
                AuditLedger.Append(completed);
                return task;
            }
            catch (Exception e)
            {
                var failed = NewEvent(request, operation + "-result", Guid.NewGuid().ToString());
                failed.correlationId = pending.correlationId;
                failed.parentActionId = actionId;
                failed.status = "failed";
                failed.errorCode = "MESHY_SUBMISSION_FAILED";
                failed.redactedErrorMessage = e.Message;
                failed.costType = provider.IsLiveProvider ? "fixed-credit-cost" : "free-local-operation";
                AuditLedger.Append(failed);
                return new GenerationTaskInfo { state = GenerationTaskState.Failed, message = AuditLedger.Redact(e.Message) };
            }
        }

        public Task<GenerationTaskInfo> GetTaskStatus(string taskId, CancellationToken token = default)
            => provider.GetTaskStatus(taskId, token);

        decimal? EstimatedCredits(string operation)
        {
            string path = Path.Combine(AuditLedger.ProjectRoot, "Audit/Pricing/meshy-pricing.v1.json");
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            Match block = Regex.Match(json, "\"" + Regex.Escape(operation) + "\"\\s*:\\s*\\{[^}]*\"estimatedCredits\"\\s*:\\s*(null|-?[0-9.]+)");
            if (!block.Success || block.Groups[1].Value == "null") return null;
            return decimal.TryParse(block.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value) ? value : null;
        }

        bool GenerationAllowed(string assetId)
        {
            string policy = File.ReadAllText(Path.Combine(AuditLedger.ProjectRoot, "Audit/Pricing/budget-policy.v1.json"));
            int maximum = (int)Number(policy, "maximumGenerationsPerAsset");
            if (!File.Exists(AuditLedger.LedgerPath)) return true;
            return File.ReadLines(AuditLedger.LedgerPath).Count(line =>
                line.Contains("\"provider\":\"meshy\"") && line.Contains("\"assetId\":\"" + assetId + "\"") &&
                Regex.IsMatch(line, "\"operation\":\"Create(?:Text|Image|MultiImage)To3DTask\"") &&
                !line.Contains("\"status\":\"blocked\"")) < maximum;
        }

        BudgetResult EvaluateBudget(bool ownerOverride)
        {
            string policy = File.ReadAllText(Path.Combine(AuditLedger.ProjectRoot, "Audit/Pricing/budget-policy.v1.json"));
            decimal daily = Number(policy, "dailyMeshyCreditBudget");
            decimal monthly = Number(policy, "monthlyMeshyCreditBudget");
            decimal hard = Number(policy, "hardStopThresholdPercentage");
            decimal approval = Number(policy, "approvalThresholdPercentage");
            decimal warning = Number(policy, "warningThresholdPercentage");
            DateTime now = DateTime.UtcNow;
            decimal usedDaily = CreditsSince(now.Date);
            decimal usedMonthly = CreditsSince(new DateTime(now.Year, now.Month, 1));
            decimal percentage = Math.Max(daily > 0 ? usedDaily / daily * 100 : 0, monthly > 0 ? usedMonthly / monthly * 100 : 0);
            if (percentage >= hard)
                return new BudgetResult { allowed = false, level = "blocked", message = "Meshy hard budget threshold has been reached." };
            if (percentage >= approval)
                return new BudgetResult { allowed = ownerOverride, level = ownerOverride ? "owner-override" : "approval-required", message = "Meshy owner budget approval is required at or above the approval threshold." };
            if (percentage >= warning)
                return new BudgetResult { allowed = true, level = "warning", message = $"Meshy budget warning: {percentage:0.0}% used." };
            return new BudgetResult { allowed = true, level = "normal", message = string.Empty };
        }

        decimal CreditsSince(DateTime since)
        {
            if (!File.Exists(AuditLedger.LedgerPath)) return 0m;
            decimal total = 0m;
            foreach (string line in File.ReadLines(AuditLedger.LedgerPath))
            {
                if (!line.Contains("\"provider\":\"meshy\"")) continue;
                Match timestamp = Regex.Match(line, "\"timestampUtc\":\"([^\"]+)\"");
                Match credits = Regex.Match(line, "\"meshyCreditsConsumed\":([0-9.]+)");
                if (DateTime.TryParse(timestamp.Groups[1].Value, out DateTime time) && time.ToUniversalTime() >= since &&
                    decimal.TryParse(credits.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                    total += amount;
            }
            return total;
        }

        GenerationTaskInfo Blocked(GeneratedAssetRequest request, string operation, string code, string message)
        {
            var audit = NewEvent(request, operation, Guid.NewGuid().ToString());
            audit.status = "blocked";
            audit.approvalRequired = true;
            audit.approvalStatus = "required";
            audit.errorCode = code;
            audit.redactedErrorMessage = message;
            audit.costType = provider.IsLiveProvider ? "unknown" : "free-local-operation";
            AuditLedger.Append(audit);
            return new GenerationTaskInfo { state = GenerationTaskState.Failed, message = message };
        }

        static AuditEvent NewEvent(GeneratedAssetRequest request, string operation, string actionId)
        {
            var audit = AuditLedger.NewUnityEvent(operation, request.assetId);
            audit.actionId = actionId;
            audit.correlationId = actionId;
            audit.provider = "meshy";
            audit.authenticationType = "redacted-provider-configuration";
            return audit;
        }

        static decimal Number(string json, string key)
        {
            Match match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*([0-9.]+)");
            return match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value) ? value : 0m;
        }

        sealed class BudgetResult { public bool allowed; public string level; public string message; }
    }
}
