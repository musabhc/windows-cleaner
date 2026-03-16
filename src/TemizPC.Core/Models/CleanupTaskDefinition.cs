namespace TemizPC.Core.Models;

public sealed record CleanupTaskDefinition(
    CleanupTaskId Id,
    string NameResourceKey,
    string DescriptionResourceKey,
    string? WarningResourceKey,
    CleanupPreset Preset,
    CleanupRiskLevel RiskLevel,
    bool IsDefaultSelected,
    bool RequiresAdministrator,
    CleanupExecutionStrategy Strategy,
    IReadOnlyList<string> TargetPaths);
