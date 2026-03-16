namespace TemizPC.Core.Models;

public sealed record CleanupTaskResult(
    CleanupTaskId TaskId,
    long FreedBytes,
    int DeletedCount,
    int SkippedCount,
    IReadOnlyList<string> Errors,
    string Summary);
