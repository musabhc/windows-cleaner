using TemizPC.Core.Models;
using TemizPC.Core.Services;
using Velopack;
using Velopack.Sources;

namespace TemizPC.App.Services;

public sealed class VelopackUpdateService : IUpdateService
{
    private readonly ReleaseSettings _releaseSettings;
    private readonly string _currentVersion;
    private readonly IAppLogger _logger;
    private UpdateInfo? _pendingUpdate;

    public VelopackUpdateService(ReleaseSettings releaseSettings, string currentVersion, IAppLogger logger)
    {
        _releaseSettings = releaseSettings;
        _currentVersion = currentVersion;
        _logger = logger;
    }

    public async Task<UpdateStatus> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        if (!_releaseSettings.IsConfigured)
        {
            return new UpdateStatus(_currentVersion, false, false, false, null, string.Empty);
        }

        try
        {
            var manager = CreateManager();
            if (!manager.IsInstalled)
            {
                return new UpdateStatus(_currentVersion, true, false, false, null, string.Empty);
            }

            _pendingUpdate = await manager.CheckForUpdatesAsync();
            if (_pendingUpdate is null)
            {
                return new UpdateStatus(_currentVersion, true, true, false, null, string.Empty);
            }

            var targetVersion = _pendingUpdate.TargetFullRelease.Version?.ToString() ?? "unknown";
            return new UpdateStatus(_currentVersion, true, true, true, targetVersion, string.Empty);
        }
        catch (Exception exception)
        {
            _logger.Error("update.check.failed", exception);
            return new UpdateStatus(_currentVersion, true, true, false, null, exception.Message);
        }
    }

    public async Task<UpdateApplyResult> DownloadAndApplyAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingUpdate is null)
        {
            return new UpdateApplyResult(false, "No update is ready to install.");
        }

        try
        {
            var manager = CreateManager();
            await manager.DownloadUpdatesAsync(_pendingUpdate);
            manager.ApplyUpdatesAndRestart(_pendingUpdate.TargetFullRelease);
            return new UpdateApplyResult(true, "Update downloaded. Restarting.");
        }
        catch (Exception exception)
        {
            _logger.Error("update.apply.failed", exception);
            return new UpdateApplyResult(false, exception.Message);
        }
    }

    private UpdateManager CreateManager()
    {
        return new UpdateManager(new GithubSource(_releaseSettings.GithubRepositoryUrl, null, _releaseSettings.AllowPrerelease));
    }
}
