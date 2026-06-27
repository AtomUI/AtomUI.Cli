using AtomUI.Cli.Entry;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class ProgramSmokeTests
{
    [Fact]
    public async Task MainReturnsZeroForVersion()
    {
        var exitCode = await Program.Main(["--version"]);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task MainReturnsArgumentExitCodeForUnknownCommand()
    {
        var exitCode = await Program.Main(["missing"]);

        Assert.Equal(2, exitCode);
    }
}
