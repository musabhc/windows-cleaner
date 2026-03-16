using System.Globalization;

namespace TemizPC.Core.Utilities;

public static class ByteSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public static string Format(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        var unitIndex = 0;
        double value = bytes;

        while (value >= 1024 && unitIndex < Units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{value:0.#} {Units[unitIndex]}");
    }
}
