using System.Text.Json;

namespace TemizPC.Core.Services;

public sealed class JsonFileLogger : IAppLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly object _syncRoot = new();

    public JsonFileLogger(string appName)
    {
        var safeAppName = string.IsNullOrWhiteSpace(appName) ? "TemizPC" : appName.Trim();
        LogDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            safeAppName,
            "Logs");

        Directory.CreateDirectory(LogDirectoryPath);
        LogFilePath = Path.Combine(LogDirectoryPath, $"{DateTimeOffset.Now:yyyyMMdd-HHmmss}.jsonl");
    }

    public string LogDirectoryPath { get; }

    public string LogFilePath { get; }

    public void Info(string eventName, object? payload = null)
    {
        Write("info", eventName, payload);
    }

    public void Warning(string eventName, object? payload = null)
    {
        Write("warning", eventName, payload);
    }

    public void Error(string eventName, Exception exception, object? payload = null)
    {
        Write("error", eventName, new
        {
            payload,
            exception = new
            {
                exceptionType = exception.GetType().FullName,
                exception.Message,
                exception.StackTrace
            }
        });
    }

    private void Write(string level, string eventName, object? payload)
    {
        try
        {
            var entry = new
            {
                timestamp = DateTimeOffset.Now,
                level,
                eventName,
                payload
            };

            var line = JsonSerializer.Serialize(entry, JsonOptions);
            lock (_syncRoot)
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging failures should not affect the cleanup workflow.
        }
    }
}
