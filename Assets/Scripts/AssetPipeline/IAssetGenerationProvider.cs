using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JoziRunner.AssetPipeline
{
    public interface IAssetGenerationProvider
    {
        bool IsLiveProvider { get; }
        bool IsConfigured { get; }
        Task<GenerationTaskInfo> CreateTextTo3DTask(GeneratedAssetRequest request, CancellationToken token = default);
        Task<GenerationTaskInfo> CreateImageTo3DTask(GeneratedAssetRequest request, string imagePath, CancellationToken token = default);
        Task<GenerationTaskInfo> CreateMultiImageTo3DTask(GeneratedAssetRequest request, IReadOnlyList<string> imagePaths, CancellationToken token = default);
        Task<GenerationTaskInfo> GetTaskStatus(string taskId, CancellationToken token = default);
        Task<string> DownloadResult(string taskId, string destinationDirectory, CancellationToken token = default);
        Task<decimal> GetCreditBalance(CancellationToken token = default);
        Task<bool> CancelTask(string taskId, CancellationToken token = default);
    }
}
