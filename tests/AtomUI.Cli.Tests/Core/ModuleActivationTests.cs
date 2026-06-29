using AtomUI.Cli.Hosting;
using AtomUI.Cli.Hosting.Commands;
using AtomUI.Cli.Hosting.Metadata;
using AtomUI.Cli.Hosting.Setup;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class ModuleActivationTests
{
    [Fact]
    public void PreParserMapsVersionShortcutToVersionCommand()
    {
        var catalog = CommandManifestCatalog.CreateBuiltIn();
        var parser = new CommandPreParser();

        var result = parser.Parse(["--version"], catalog);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Manifest);
        Assert.Equal("version", result.Manifest.Name);
    }

    [Fact]
    public void ActivationPlannerIncludesCoreOwnerAndCommandRequiredModules()
    {
        var catalog = CommandManifestCatalog.CreateBuiltIn();
        var parser = new CommandPreParser();
        var parsed = parser.Parse(["add", "datagrid"], catalog);
        var planner = new ModuleActivationPlanner(typeof(AtomUICliCoreModule));

        var plan = planner.Plan(parsed.Manifest!);

        Assert.Contains(typeof(AtomUICliCoreModule), plan.ModuleTypes);
        Assert.Contains(typeof(AtomUICliSetupModule), plan.ModuleTypes);
        Assert.Contains(typeof(AtomUICliMetadataModule), plan.ModuleTypes);
    }

    [Fact]
    public void PreParserReturnsArgumentErrorForUnknownCommand()
    {
        var catalog = CommandManifestCatalog.CreateBuiltIn();
        var parser = new CommandPreParser();

        var result = parser.Parse(["missing"], catalog);

        Assert.False(result.IsSuccess);
        Assert.Equal(AtomUICliErrorCodes.ArgumentCommandNotFound, result.Error?.Code);
    }

    [Fact]
    public void PreParserMapsCommandHelpShortcutToHelpCommand()
    {
        var catalog = CommandManifestCatalog.CreateBuiltIn();
        var parser = new CommandPreParser();

        var result = parser.Parse(["info", "--help"], catalog);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Manifest);
        Assert.Equal("help", result.Manifest.Name);
    }

    [Fact]
    public void PreParserMapsHelpShortcutAfterGlobalOptionsToHelpCommand()
    {
        var catalog = CommandManifestCatalog.CreateBuiltIn();
        var parser = new CommandPreParser();

        var result = parser.Parse(["--format", "json", "-h"], catalog);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Manifest);
        Assert.Equal("help", result.Manifest.Name);
    }
}
