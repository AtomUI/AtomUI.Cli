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
}
