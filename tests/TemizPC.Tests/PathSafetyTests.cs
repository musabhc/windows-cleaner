using TemizPC.Core.Utilities;

namespace TemizPC.Tests;

public sealed class PathSafetyTests
{
    [Fact]
    public void IsUnderRoot_returns_true_for_nested_path()
    {
        var result = PathSafety.IsUnderRoot(
            @"C:\Windows\Temp\foo\bar.tmp",
            @"C:\Windows\Temp");

        Assert.True(result);
    }

    [Fact]
    public void IsUnderRoot_returns_false_for_sibling_path()
    {
        var result = PathSafety.IsUnderRoot(
            @"C:\Windows\TempBackup\bar.tmp",
            @"C:\Windows\Temp");

        Assert.False(result);
    }

    [Fact]
    public void Normalize_resolves_relative_segments()
    {
        var result = PathSafety.Normalize(
            @"C:\Windows\Temp\sub\..\file.tmp");

        Assert.Equal(@"C:\Windows\Temp\file.tmp", result);
    }
}
