using AtomUI.Cli.Entry;
using Xunit;

namespace AtomUI.Cli.Tests.FeatureModules;

public sealed class MetadataCommandTests
{
    [Theory]
    [InlineData("list", "controls")]
    [InlineData("info", "Button")]
    [InlineData("doc", "Button")]
    [InlineData("demo", "Button", "--list")]
    [InlineData("token")]
    [InlineData("semantic", "Button")]
    [InlineData("design.md")]
    [InlineData("package", "AtomUI.Controls")]
    [InlineData("changelog")]
    public async Task MetadataCommandReturnsSuccess(params string[] args)
    {
        var exitCode = await Program.Main(args);

        Assert.Equal(0, exitCode);
    }
}
