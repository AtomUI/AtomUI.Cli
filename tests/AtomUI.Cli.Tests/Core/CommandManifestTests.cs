using AtomUI.Cli.Hosting;
using AtomUI.Cli.Hosting.Commands;
using AtomUI.Cli.Hosting.Metadata;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class CommandManifestTests
{
    [Fact]
    public void BuiltInCatalogMapsInfoToMetadataModule()
    {
        var catalog = CommandManifestCatalog.CreateBuiltIn();

        Assert.True(catalog.TryFind("info", out var manifest));
        Assert.NotNull(manifest);
        Assert.Equal(typeof(AtomUICliMetadataModule), manifest.OwnerModuleType);
        Assert.Equal(CommandGroup.Knowledge, manifest.Group);
        Assert.Contains(OutputFormat.Json, manifest.SupportedFormats);
    }

    [Fact]
    public void CatalogRejectsDuplicateCommandNames()
    {
        var duplicate = new CommandManifest(
            "fake",
            typeof(AtomUICliCoreModule),
            CommandGroup.Knowledge,
            new HashSet<OutputFormat> { OutputFormat.Text });

        var exception = Assert.Throws<ArgumentException>(() => CommandManifestCatalog.Create([duplicate, duplicate]));

        Assert.Contains("fake", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuiltInCatalogProvidesHelpMetadataForEveryCommand()
    {
        var catalog = CommandManifestCatalog.CreateBuiltIn();

        foreach (var manifest in catalog.Manifests)
        {
            Assert.False(string.IsNullOrWhiteSpace(manifest.Help.Summary), manifest.Name);
            Assert.False(string.IsNullOrWhiteSpace(manifest.Help.Usage), manifest.Name);
            Assert.NotEmpty(manifest.Help.Examples);
        }
    }

    [Fact]
    public void BuiltInCatalogDescribesProductGradeDemoOptions()
    {
        var catalog = CommandManifestCatalog.CreateBuiltIn();

        Assert.True(catalog.TryFind("demo", out var manifest));
        Assert.NotNull(manifest);
        Assert.Equal("dotnet atomui demo <control> [demo-key] [options]", manifest.Help.Usage);
        Assert.Contains(manifest.Help.Arguments, argument => argument.Name == "demo-key");
        Assert.Contains(manifest.Help.Options, option => option.Name == "--all");
        Assert.Contains(manifest.Help.Options, option => option.Name == "--scenario");
        Assert.Contains(manifest.Help.Options, option => option.Name == "--match");
        Assert.Contains(manifest.Help.Options, option => option.Name == "--code-language");
        Assert.Contains(manifest.Help.Options, option => option.Name == "--source");
        Assert.Contains(manifest.Help.Options, option => option.Name == "--related" && option.ValueName == "true|false");
        Assert.DoesNotContain(manifest.Help.Options, option => option.Name == "--language");
    }
}
