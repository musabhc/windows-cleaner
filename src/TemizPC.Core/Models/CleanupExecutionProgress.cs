namespace TemizPC.Core.Models;

public sealed record CleanupExecutionProgress(
    CleanupTaskId? CurrentTaskId,
    int CompletedTasks,
    int TotalTasks,
    string Message,
    bool IsCompleted = false);
