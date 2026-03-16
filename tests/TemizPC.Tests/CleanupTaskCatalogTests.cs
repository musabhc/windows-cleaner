using TemizPC.Core.Models;
using TemizPC.Core.Services;

namespace TemizPC.Tests;

public sealed class CleanupTaskCatalogTests
{
    [Fact]
    public void Default_catalog_marks_recommended_tasks_as_selected()
    {
        var environment = new AppEnvironment(
            @"C:\Users\Test",
            @"C:\Users\Test\AppData\Local",
            @"C:\Users\Test\AppData\Roaming",
            @"C:\ProgramData",
            @"C:\Windows",
            @"C:\Users\Test\AppData\Local\Temp");

        var tasks = CleanupTaskCatalog.CreateDefault(environment);

        Assert.Equal(11, tasks.Count);

        var selectedIds = tasks
            .Where(task => task.IsDefaultSelected)
            .Select(task => task.Id)
            .ToHashSet();

        Assert.Contains(CleanupTaskId.RecentFiles, selectedIds);
        Assert.Contains(CleanupTaskId.WindowsTemp, selectedIds);
        Assert.Contains(CleanupTaskId.LocalTemp, selectedIds);
        Assert.Contains(CleanupTaskId.RecycleBin, selectedIds);
        Assert.Contains(CleanupTaskId.ThumbnailCache, selectedIds);
        Assert.Contains(CleanupTaskId.WindowsErrorReports, selectedIds);
        Assert.Contains(CleanupTaskId.DeliveryOptimizationCache, selectedIds);

        var prefetch = tasks.Single(task => task.Id == CleanupTaskId.Prefetch);
        Assert.Equal(CleanupPreset.Advanced, prefetch.Preset);
        Assert.False(prefetch.IsDefaultSelected);
    }
}
