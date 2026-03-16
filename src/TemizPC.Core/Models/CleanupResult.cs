namespace TemizPC.Core.Models;

public sealed record CleanupResult(
    long FreedBytes,
    int DeletedCount,
    int SkippedCount,
    IReadOnlyList<string> Errors,
    IReadOnlyList<CleanupTaskResult> TaskResults,
    TimeSpan Duration);
