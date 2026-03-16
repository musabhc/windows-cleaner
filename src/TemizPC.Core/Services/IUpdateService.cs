using TemizPC.Core.Models;

namespace TemizPC.Core.Services;

public interface IUpdateService
{
    Task<UpdateStatus> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    Task<UpdateApplyResult> DownloadAndApplyAsync(CancellationToken cancellationToken = default);
}
