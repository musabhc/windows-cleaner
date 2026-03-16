using System.Diagnostics;
using System.Runtime.InteropServices;
using TemizPC.Core.Models;
using TemizPC.Core.Utilities;

namespace TemizPC.Core.Services;

public sealed class CleanupExecutor : ICleanupExecutor
{
    private readonly AppEnvironment _environment;
    private readonly IAppLogger _logger;

    public CleanupExecutor(AppEnvironment environment, IAppLogger logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<CleanupResult> ExecuteAsync(
        IEnumerable<CleanupTaskDefinition> tasks,
        IProgress<CleanupExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var selectedTasks = tasks
            .GroupBy(task => task.Id)
            .Select(group => group.First())
            .ToList();

        var taskResults = new List<CleanupTaskResult>(selectedTasks.Count);
        var aggregateErrors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < selectedTasks.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var task = selectedTasks[index];
            progress?.Report(new CleanupExecutionProgress(
                task.Id,
                index,
                selectedTasks.Count,
                $"Running {task.Id}"));

            _logger.Info("cleanup.task.started", new { task.Id, task.TargetPaths, task.Strategy });

            CleanupTaskResult taskResult;
            try
            {
                taskResult = await ExecuteTaskAsync(task, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.Error("cleanup.task.failed", exception, new { task.Id });
                taskResult = new CleanupTaskResult(
                    task.Id,
                    0,
                    0,
                    0,
                    [$"{task.Id} failed: {exception.Message}"],
                    $"{task.Id} failed.");
            }

            taskResults.Add(taskResult);
            foreach (var error in taskResult.Errors)
            {
                aggregateErrors.Add(error);
            }

            _logger.Info("cleanup.task.completed", new
            {
                task.Id,
                taskResult.FreedBytes,
                taskResult.DeletedCount,
                taskResult.SkippedCount,
                taskResult.Errors
            });
        }

        stopwatch.Stop();
        progress?.Report(new CleanupExecutionProgress(
            null,
            selectedTasks.Count,
            selectedTasks.Count,
            "Cleanup finished",
            true));

        return new CleanupResult(
            taskResults.Sum(result => result.FreedBytes),
            taskResults.Sum(result => result.DeletedCount),
            taskResults.Sum(result => result.SkippedCount),
            aggregateErrors.ToList(),
            taskResults,
            stopwatch.Elapsed);
    }

    private async Task<CleanupTaskResult> ExecuteTaskAsync(
        CleanupTaskDefinition task,
        CancellationToken cancellationToken)
    {
        return task.Id switch
        {
            CleanupTaskId.RecentFiles => ExecuteDirectoryTask(task, cancellationToken),
            CleanupTaskId.WindowsTemp => ExecuteDirectoryTask(task, cancellationToken),
            CleanupTaskId.LocalTemp => ExecuteDirectoryTask(task, cancellationToken),
            CleanupTaskId.ThumbnailCache => ExecutePatternTask(task, ["thumbcache*", "iconcache*"]),
            CleanupTaskId.WindowsErrorReports => ExecuteDirectoryTask(task, cancellationToken),
            CleanupTaskId.DeliveryOptimizationCache => ExecuteDirectoryTask(task, cancellationToken),
            CleanupTaskId.Prefetch => ExecuteDirectoryTask(task, cancellationToken),
            CleanupTaskId.WindowsUpdateDownloadCache => ExecuteDirectoryTask(task, cancellationToken),
            CleanupTaskId.CrashDumps => ExecuteCrashDumpTask(task, cancellationToken),
            CleanupTaskId.RecycleBin => ExecuteRecycleBinTask(task),
            CleanupTaskId.DismStartComponentCleanup => await ExecuteDismTaskAsync(task, cancellationToken),
            _ => new CleanupTaskResult(task.Id, 0, 0, 0, [$"Unsupported task {task.Id}."], "Task not supported.")
        };
    }

    private CleanupTaskResult ExecuteDirectoryTask(
        CleanupTaskDefinition task,
        CancellationToken cancellationToken)
    {
        var accumulator = new TaskAccumulator(task.Id);

        foreach (var rootPath in task.TargetPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteDirectoryContents(rootPath, task.TargetPaths, accumulator, cancellationToken);
        }

        return accumulator.ToResult("Completed directory cleanup.");
    }

    private CleanupTaskResult ExecutePatternTask(
        CleanupTaskDefinition task,
        IReadOnlyList<string> patterns)
    {
        var accumulator = new TaskAccumulator(task.Id);
        foreach (var rootPath in task.TargetPaths)
        {
            DeleteMatchingFiles(rootPath, task.TargetPaths, patterns, accumulator);
        }

        return accumulator.ToResult("Removed cache files.");
    }

    private CleanupTaskResult ExecuteCrashDumpTask(
        CleanupTaskDefinition task,
        CancellationToken cancellationToken)
    {
        var accumulator = new TaskAccumulator(task.Id);

        foreach (var targetPath in task.TargetPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Directory.Exists(targetPath))
            {
                DeleteDirectoryContents(targetPath, task.TargetPaths, accumulator, cancellationToken);
                continue;
            }

            if (File.Exists(targetPath))
            {
                DeleteFile(targetPath, task.TargetPaths, accumulator);
            }
        }

        return accumulator.ToResult("Removed crash dumps.");
    }

    private CleanupTaskResult ExecuteRecycleBinTask(CleanupTaskDefinition task)
    {
        var accumulator = new TaskAccumulator(task.Id);

        try
        {
            var result = SHEmptyRecycleBin(
                IntPtr.Zero,
                null,
                RecycleBinFlags.NoConfirmation | RecycleBinFlags.NoProgressUi | RecycleBinFlags.NoSound);

            if (result != 0)
            {
                accumulator.AddError("Recycle Bin could not be emptied.");
            }
            else
            {
                accumulator.AddDeletion(1, 0);
            }
        }
        catch (Exception exception)
        {
            accumulator.AddError($"Recycle Bin cleanup failed: {exception.Message}");
        }

        return accumulator.ToResult("Recycle Bin emptied.");
    }

    private async Task<CleanupTaskResult> ExecuteDismTaskAsync(
        CleanupTaskDefinition task,
        CancellationToken cancellationToken)
    {
        var accumulator = new TaskAccumulator(task.Id);
        var dismPath = Path.Combine(_environment.WindowsDirectory, "System32", "dism.exe");

        if (!File.Exists(dismPath))
        {
            return new CleanupTaskResult(
                task.Id,
                0,
                0,
                0,
                ["DISM is not available on this Windows installation."],
                "DISM unavailable.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = dismPath,
            Arguments = "/Online /Cleanup-Image /StartComponentCleanup /Quiet /NoRestart",
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            accumulator.AddError(
                string.IsNullOrWhiteSpace(error)
                    ? $"DISM exited with code {process.ExitCode}."
                    : $"DISM exited with code {process.ExitCode}: {error.Trim()}");
        }
        else
        {
            accumulator.AddDeletion(1, 0);
        }

        _logger.Info("cleanup.task.dism.output", new { output, error, process.ExitCode });
        return accumulator.ToResult("DISM component cleanup completed.");
    }

    private void DeleteMatchingFiles(
        string rootPath,
        IReadOnlyList<string> allowedRoots,
        IReadOnlyList<string> patterns,
        TaskAccumulator accumulator)
    {
        if (!Directory.Exists(rootPath))
        {
            return;
        }

        EnsureSafeRoot(rootPath, allowedRoots);

        foreach (var pattern in patterns)
        {
            IEnumerable<string> filePaths;
            try
            {
                filePaths = Directory.EnumerateFiles(rootPath, pattern, SearchOption.TopDirectoryOnly);
            }
            catch (Exception exception)
            {
                accumulator.AddError($"Unable to enumerate files in {rootPath}: {exception.Message}");
                return;
            }

            foreach (var filePath in filePaths)
            {
                DeleteFile(filePath, allowedRoots, accumulator);
            }
        }
    }

    private void DeleteDirectoryContents(
        string rootPath,
        IReadOnlyList<string> allowedRoots,
        TaskAccumulator accumulator,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(rootPath))
        {
            return;
        }

        EnsureSafeRoot(rootPath, allowedRoots);
        var rootDirectory = new DirectoryInfo(rootPath);

        foreach (var file in EnumerateFiles(rootDirectory, accumulator))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteFile(file.FullName, allowedRoots, accumulator);
        }

        foreach (var directory in EnumerateDirectories(rootDirectory, accumulator))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteSubdirectory(directory, allowedRoots, accumulator, cancellationToken);
        }
    }

    private void DeleteSubdirectory(
        DirectoryInfo directory,
        IReadOnlyList<string> allowedRoots,
        TaskAccumulator accumulator,
        CancellationToken cancellationToken)
    {
        if (IsReparsePoint(directory.Attributes))
        {
            accumulator.AddSkipped("Link targets were skipped for safety.");
            return;
        }

        foreach (var file in EnumerateFiles(directory, accumulator))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteFile(file.FullName, allowedRoots, accumulator);
        }

        foreach (var child in EnumerateDirectories(directory, accumulator))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteSubdirectory(child, allowedRoots, accumulator, cancellationToken);
        }

        TryDeleteDirectory(directory.FullName, allowedRoots, accumulator);
    }

    private void TryDeleteDirectory(
        string directoryPath,
        IReadOnlyList<string> allowedRoots,
        TaskAccumulator accumulator)
    {
        if (!PathSafety.IsWithinAllowedRoots(directoryPath, allowedRoots))
        {
            accumulator.AddError($"Blocked unsafe directory delete: {directoryPath}");
            return;
        }

        try
        {
            var info = new DirectoryInfo(directoryPath);
            if (info.Exists)
            {
                info.Attributes &= ~FileAttributes.ReadOnly;
                info.Delete();
                accumulator.AddDeletion(1, 0);
            }
        }
        catch (UnauthorizedAccessException)
        {
            accumulator.AddSkipped("Protected folders were skipped.");
        }
        catch (IOException)
        {
            accumulator.AddSkipped("Files currently in use were skipped.");
        }
    }

    private void DeleteFile(
        string filePath,
        IReadOnlyList<string> allowedRoots,
        TaskAccumulator accumulator)
    {
        if (!PathSafety.IsWithinAllowedRoots(filePath, allowedRoots))
        {
            accumulator.AddError($"Blocked unsafe file delete: {filePath}");
            return;
        }

        try
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                return;
            }

            if (IsReparsePoint(fileInfo.Attributes))
            {
                accumulator.AddSkipped("Link targets were skipped for safety.");
                return;
            }

            var length = fileInfo.Length;
            fileInfo.Attributes = FileAttributes.Normal;
            fileInfo.Delete();
            accumulator.AddDeletion(1, length);
        }
        catch (UnauthorizedAccessException)
        {
            accumulator.AddSkipped("Protected files were skipped.");
        }
        catch (IOException)
        {
            accumulator.AddSkipped("Files currently in use were skipped.");
        }
    }

    private static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directory, TaskAccumulator accumulator)
    {
        try
        {
            return directory.EnumerateFiles();
        }
        catch (Exception exception)
        {
            accumulator.AddError($"Unable to enumerate files in {directory.FullName}: {exception.Message}");
            return [];
        }
    }

    private static IEnumerable<DirectoryInfo> EnumerateDirectories(DirectoryInfo directory, TaskAccumulator accumulator)
    {
        try
        {
            return directory.EnumerateDirectories();
        }
        catch (Exception exception)
        {
            accumulator.AddError($"Unable to enumerate folders in {directory.FullName}: {exception.Message}");
            return [];
        }
    }

    private static bool IsReparsePoint(FileAttributes attributes)
    {
        return attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    private static void EnsureSafeRoot(string rootPath, IReadOnlyList<string> allowedRoots)
    {
        if (!PathSafety.IsWithinAllowedRoots(rootPath, allowedRoots))
        {
            throw new InvalidOperationException($"Unsafe cleanup root blocked: {rootPath}");
        }
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(
        IntPtr hwnd,
        string? pszRootPath,
        RecycleBinFlags flags);

    [Flags]
    private enum RecycleBinFlags
    {
        NoConfirmation = 0x00000001,
        NoProgressUi = 0x00000002,
        NoSound = 0x00000004
    }

    private sealed class TaskAccumulator
    {
        private readonly HashSet<string> _errors = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _skipMessages = new(StringComparer.OrdinalIgnoreCase);

        public TaskAccumulator(CleanupTaskId taskId)
        {
            TaskId = taskId;
        }

        public CleanupTaskId TaskId { get; }

        public long FreedBytes { get; private set; }

        public int DeletedCount { get; private set; }

        public int SkippedCount { get; private set; }

        public void AddDeletion(int deletedCount, long freedBytes)
        {
            DeletedCount += deletedCount;
            FreedBytes += freedBytes;
        }

        public void AddSkipped(string message)
        {
            SkippedCount++;
            _skipMessages.Add(message);
        }

        public void AddError(string message)
        {
            _errors.Add(message);
        }

        public CleanupTaskResult ToResult(string defaultSummary)
        {
            var summary = DeletedCount > 0 || FreedBytes > 0
                ? $"{DeletedCount} items removed, {ByteSizeFormatter.Format(FreedBytes)} reclaimed."
                : defaultSummary;

            var allMessages = _errors.Concat(_skipMessages).ToList();
            return new CleanupTaskResult(
                TaskId,
                FreedBytes,
                DeletedCount,
                SkippedCount,
                allMessages,
                summary);
        }
    }
}
