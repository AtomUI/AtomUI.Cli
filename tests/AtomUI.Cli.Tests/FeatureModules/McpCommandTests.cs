using AtomUI.Cli.Entry;
using AtomUI.Cli.Hosting;
using Xunit;

namespace AtomUI.Cli.Tests.FeatureModules;

public sealed class McpCommandTests
{
    [Fact]
    public async Task McpCommandIsRegistered()
    {
        var application = AtomUICliApplication.CreateBuilder(["help"]).Build();

        await application.RunAsync(["help"], TestContext.Current.CancellationToken);

        Assert.True(application.CommandManifests.TryFind("mcp", out _));
    }

    [Fact]
    public async Task McpCommandReturnsAvailableTools()
    {
        var exitCode = await Program.Main(["mcp"]);

        Assert.Equal(0, exitCode);
    }
}
