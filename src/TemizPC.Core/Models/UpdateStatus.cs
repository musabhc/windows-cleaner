namespace TemizPC.Core.Models;

public sealed record UpdateStatus(
    string CurrentVersion,
    bool IsConfigured,
    bool IsInstalled,
    bool IsUpdateAvailable,
    string? AvailableVersion,
    string Message);
