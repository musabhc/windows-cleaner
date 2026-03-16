using TemizPC.Core.Utilities;

namespace TemizPC.Tests;

public sealed class ByteSizeFormatterTests
{
    [Fact]
    public void Format_uses_human_readable_units()
    {
        var result = ByteSizeFormatter.Format(1_572_864);

        Assert.Equal("1.5 MB", result);
    }
}
