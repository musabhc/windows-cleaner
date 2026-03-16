using TemizPC.Core.Models;

namespace TemizPC.Core.Services;

public static class CleanupTaskCatalog
{
    public static IReadOnlyList<CleanupTaskDefinition> CreateDefault(AppEnvironment environment)
    {
        var recentPath = Path.Combine(
            environment.RoamingAppDataDirectory,
            "Microsoft",
            "Windows",
            "Recent");

        var windowsTempPath = Path.Combine(environment.WindowsDirectory, "Temp");
        var windowsPrefetchPath = Path.Combine(environment.WindowsDirectory, "Prefetch");
        var softwareDistributionDownloadPath = Path.Combine(
            environment.WindowsDirectory,
            "SoftwareDistribution",
            "Download");

        var thumbnailCachePath = Path.Combine(
            environment.LocalAppDataDirectory,
            "Microsoft",
            "Windows",
            "Explorer");

        var localWerPath = Path.Combine(
            environment.LocalAppDataDirectory,
            "Microsoft",
            "Windows",
            "WER");

        var globalWerArchivePath = Path.Combine(
            environment.CommonAppDataDirectory,
            "Microsoft",
            "Windows",
            "WER",
            "ReportArchive");

        var globalWerQueuePath = Path.Combine(
            environment.CommonAppDataDirectory,
            "Microsoft",
            "Windows",
            "WER",
            "ReportQueue");

        var deliveryOptimizationPath = Path.Combine(
            environment.CommonAppDataDirectory,
            "Microsoft",
            "Windows",
            "DeliveryOptimization",
            "Cache");

        var minidumpPath = Path.Combine(environment.WindowsDirectory, "Minidump");
        var memoryDumpPath = Path.Combine(environment.WindowsDirectory, "MEMORY.DMP");
        var localCrashDumpPath = Path.Combine(environment.LocalAppDataDirectory, "CrashDumps");

        return
        [
            new(
                CleanupTaskId.RecentFiles,
                "Task_Recent_Name",
                "Task_Recent_Description",
                null,
                CleanupPreset.Recommended,
                CleanupRiskLevel.Safe,
                true,
                true,
                CleanupExecutionStrategy.FileSystem,
                [recentPath]),
            new(
                CleanupTaskId.WindowsTemp,
                "Task_WindowsTemp_Name",
                "Task_WindowsTemp_Description",
                null,
                CleanupPreset.Recommended,
                CleanupRiskLevel.Safe,
                true,
                true,
                CleanupExecutionStrategy.FileSystem,
                [windowsTempPath]),
            new(
                CleanupTaskId.LocalTemp,
                "Task_LocalTemp_Name",
                "Task_LocalTemp_Description",
                null,
                CleanupPreset.Recommended,
                CleanupRiskLevel.Safe,
                true,
                true,
                CleanupExecutionStrategy.FileSystem,
                [environment.TempDirectory]),
            new(
                CleanupTaskId.RecycleBin,
                "Task_RecycleBin_Name",
                "Task_RecycleBin_Description",
                null,
                CleanupPreset.Recommended,
                CleanupRiskLevel.Safe,
                true,
                true,
                CleanupExecutionStrategy.RecycleBin,
                []),
            new(
                CleanupTaskId.ThumbnailCache,
                "Task_ThumbnailCache_Name",
                "Task_ThumbnailCache_Description",
                null,
                CleanupPreset.Recommended,
                CleanupRiskLevel.Safe,
                true,
                true,
                CleanupExecutionStrategy.FileSystem,
                [thumbnailCachePath]),
            new(
                CleanupTaskId.WindowsErrorReports,
                "Task_WindowsErrorReports_Name",
                "Task_WindowsErrorReports_Description",
                null,
                CleanupPreset.Recommended,
                CleanupRiskLevel.Safe,
                true,
                true,
                CleanupExecutionStrategy.FileSystem,
                [globalWerArchivePath, globalWerQueuePath, localWerPath]),
            new(
                CleanupTaskId.DeliveryOptimizationCache,
                "Task_DeliveryOptimization_Name",
                "Task_DeliveryOptimization_Description",
                null,
                CleanupPreset.Recommended,
                CleanupRiskLevel.Review,
                true,
                true,
                CleanupExecutionStrategy.FileSystem,
                [deliveryOptimizationPath]),
            new(
                CleanupTaskId.Prefetch,
                "Task_Prefetch_Name",
                "Task_Prefetch_Description",
                "Task_Prefetch_Warning",
                CleanupPreset.Advanced,
                CleanupRiskLevel.Advanced,
                false,
                true,
                CleanupExecutionStrategy.FileSystem,
                [windowsPrefetchPath]),
            new(
                CleanupTaskId.WindowsUpdateDownloadCache,
                "Task_WindowsUpdateCache_Name",
                "Task_WindowsUpdateCache_Description",
                "Task_WindowsUpdateCache_Warning",
                CleanupPreset.Advanced,
                CleanupRiskLevel.Review,
                false,
                true,
                CleanupExecutionStrategy.FileSystem,
                [softwareDistributionDownloadPath]),
            new(
                CleanupTaskId.CrashDumps,
                "Task_CrashDumps_Name",
                "Task_CrashDumps_Description",
                null,
                CleanupPreset.Advanced,
                CleanupRiskLevel.Review,
                false,
                true,
                CleanupExecutionStrategy.FileSystem,
                [minidumpPath, memoryDumpPath, localCrashDumpPath]),
            new(
                CleanupTaskId.DismStartComponentCleanup,
                "Task_Dism_Name",
                "Task_Dism_Description",
                "Task_Dism_Warning",
                CleanupPreset.Advanced,
                CleanupRiskLevel.Advanced,
                false,
                true,
                CleanupExecutionStrategy.Command,
                ["DISM /Online /Cleanup-Image /StartComponentCleanup /NoRestart"])
        ];
    }
}
