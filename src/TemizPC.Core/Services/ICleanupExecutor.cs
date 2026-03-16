using TemizPC.Core.Models;

namespace TemizPC.Core.Services;

public interface ICleanupExecutor
{
    Task<CleanupResult> ExecuteAsync(
        IEnumerable<CleanupTaskDefinition> tasks,
        IProgress<CleanupExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
