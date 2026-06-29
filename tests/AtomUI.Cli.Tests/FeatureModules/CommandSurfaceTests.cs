using AtomUI.Cli.Hosting;
using Xunit;

namespace AtomUI.Cli.Tests.FeatureModules;

public sealed class CommandSurfaceTests
{
    [Fact]
    public async Task ApplicationRegistersAllDesignedCommands()
    {
        var application = AtomUICliApplication.CreateBuilder(["help"]).Build();

        await application.RunAsync(["help"], TestContext.Current.CancellationToken);

        var names = application.CommandManifests.Manifests.Select(command => command.Name).Order().ToArray();
        Assert.Equal(
            [
                "add", "changelog", "demo", "design.md", "doc", "doctor", "env", "help",
                "info", "init", "lint", "list", "mcp", "migrate", "package", "semantic",
                "setup", "token", "upgrade", "usage", "version"
            ],
            names);
    }
}
