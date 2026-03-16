namespace TemizPC.Core.Services;

public interface IAppLogger
{
    string LogDirectoryPath { get; }
    string LogFilePath { get; }

    void Info(string eventName, object? payload = null);
    void Warning(string eventName, object? payload = null);
    void Error(string eventName, Exception exception, object? payload = null);
}
