using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Security.Principal;

namespace TemizPC.App.Services;

public static class AdminGuard
{
    public static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static bool TryRestartAsAdministrator()
    {
        if (IsRunningAsAdministrator())
        {
            return false;
        }

        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = processPath,
            Arguments = BuildArguments(Environment.GetCommandLineArgs().Skip(1)),
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = AppContext.BaseDirectory
        };

        try
        {
            Process.Start(startInfo);
            return true;
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            // The user canceled the UAC prompt.
            return false;
        }
    }

    private static string BuildArguments(IEnumerable<string> arguments)
    {
        var builder = new StringBuilder();

        foreach (var argument in arguments)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(QuoteArgument(argument));
        }

        return builder.ToString();
    }

    private static string QuoteArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return "\"\"";
        }

        if (!argument.Any(ch => char.IsWhiteSpace(ch) || ch == '"'))
        {
            return argument;
        }

        return "\"" + argument.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
