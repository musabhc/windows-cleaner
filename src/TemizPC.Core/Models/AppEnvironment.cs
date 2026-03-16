namespace TemizPC.Core.Models;

public sealed record AppEnvironment(
    string UserProfileDirectory,
    string LocalAppDataDirectory,
    string RoamingAppDataDirectory,
    string CommonAppDataDirectory,
    string WindowsDirectory,
    string TempDirectory)
{
    public static AppEnvironment Current()
    {
        var windowsDirectory = Environment.GetEnvironmentVariable("WINDIR")
            ?? @"C:\Windows";

        return new AppEnvironment(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            windowsDirectory,
            Path.GetTempPath());
    }
}
