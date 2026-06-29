using AtomUI.Cli.Entry;
using Xunit;

namespace AtomUI.Cli.Tests.FeatureModules;

public sealed class SetupCommandTests
{
    [Theory]
    [InlineData("setup")]
    [InlineData("init")]
    [InlineData("add", "datagrid")]
    [InlineData("upgrade")]
    public async Task WriteCommandsDefaultToDryRun(params string[] args)
    {
        var exitCode = await Program.Main(args);

        Assert.Equal(0, exitCode);
    }
}
