using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace JoziRunner.AssetPipeline
{
    public sealed class MockMeshyProvider : IAssetGenerationProvider
    {
        readonly Dictionary<string, GenerationTaskInfo> tasks = new Dictionary<string, GenerationTaskInfo>();
        public bool IsLiveProvider => false;
        public bool IsConfigured => true;

        public Task<GenerationTaskInfo> CreateTextTo3DTask(GeneratedAssetRequest request, CancellationToken token = default)
            => Create(request, "text");

        public Task<GenerationTaskInfo> CreateImageTo3DTask(GeneratedAssetRequest request, string imagePath, CancellationToken token = default)
            => Create(request, "image");

        public Task<GenerationTaskInfo> CreateMultiImageTo3DTask(GeneratedAssetRequest request, IReadOnlyList<string> imagePaths, CancellationToken token = default)
            => Create(request, "multi-image");

        Task<GenerationTaskInfo> Create(GeneratedAssetRequest request, string mode)
        {
            string id = "mock-" + Guid.NewGuid().ToString("N");
            var task = new GenerationTaskInfo
            {
                taskId = id,
                state = GenerationTaskState.AwaitingApproval,
                progress = 0f,
                message = $"Mock {mode} task only. No API request or credit use occurred."
            };
            tasks[id] = task;
            return Task.FromResult(task);
        }

        public Task<GenerationTaskInfo> GetTaskStatus(string taskId, CancellationToken token = default)
            => Task.FromResult(tasks.TryGetValue(taskId, out var task) ? task : null);

        public Task<string> DownloadResult(string taskId, string destinationDirectory, CancellationToken token = default)
        {
            Directory.CreateDirectory(destinationDirectory);
            return Task.FromResult(string.Empty);
        }

        public Task<decimal> GetCreditBalance(CancellationToken token = default) => Task.FromResult(0m);

        public Task<bool> CancelTask(string taskId, CancellationToken token = default)
        {
            if (!tasks.TryGetValue(taskId, out var task)) return Task.FromResult(false);
            task.state = GenerationTaskState.Cancelled;
            task.message = "Mock task cancelled.";
            return Task.FromResult(true);
        }
    }
}
