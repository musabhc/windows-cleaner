using System.IO;
using System.Text.Json;

namespace TemizPC.App.Services;

public sealed record ReleaseSettings(string GithubRepositoryUrl, bool AllowPrerelease)
{
    public bool IsConfigured =>
        Uri.TryCreate(GithubRepositoryUrl, UriKind.Absolute, out var uri)
        && uri.Host.Contains("github.com", StringComparison.OrdinalIgnoreCase);

    public static ReleaseSettings Load(string baseDirectory)
    {
        var filePath = Path.Combine(baseDirectory, "release-settings.json");
        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<ReleaseSettingsDto>(json);
                if (data is not null)
                {
                    return new ReleaseSettings(
                        data.GithubRepositoryUrl?.Trim() ?? string.Empty,
                        data.AllowPrerelease);
                }
            }
            catch
            {
                // Fall through to environment variables.
            }
        }

        return new ReleaseSettings(
            Environment.GetEnvironmentVariable("TEMIZPC_GITHUB_REPOSITORY")?.Trim() ?? string.Empty,
            bool.TryParse(Environment.GetEnvironmentVariable("TEMIZPC_ALLOW_PRERELEASE"), out var allowPrerelease)
                && allowPrerelease);
    }

    private sealed record ReleaseSettingsDto(string? GithubRepositoryUrl, bool AllowPrerelease);
}
