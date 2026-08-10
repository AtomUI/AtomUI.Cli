using AtomUI.Cli.MetadataBuilder.SourceAnalysis;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;
using Xunit;

namespace AtomUI.Cli.Tests.MetadataBuilder;

public sealed class ControlSourceProcessorTests
{
    [Fact]
    public async Task ControlSourceProcessorExtractsButtonPseudoClassesAndTemplateLookups()
    {
        var sourceRoot = MetadataBuilderTestPaths.ResolveAtomUISourceRoot();
        var context = MetadataBuilderTestPaths.CreateContext(sourceRoot);

        await new ControlSourceProcessor().ExecuteAsync(context, TestContext.Current.CancellationToken);

        var pseudoClasses = context.Facts.GetByPrefix("pseudo-class:Button:").Select(fact => fact.Key).ToArray();
        Assert.Contains("pseudo-class:Button::loading", pseudoClasses);
        Assert.Contains("pseudo-class:Button::danger", pseudoClasses);
        Assert.Contains("pseudo-class:Button::icononly", pseudoClasses);

        var lookups = context.Facts.GetByPrefix("name-scope-lookup:Button:").Select(fact => fact.Key).ToArray();
        Assert.Contains("name-scope-lookup:Button:PART_WaveSpirit", lookups);

        var loadingState = Assert.Single(context.Facts.Get("state-flow:Button::loading"));
        Assert.Contains("IsLoading", loadingState.Value.ToString(), StringComparison.Ordinal);
    }
}

internal static class MetadataBuilderTestPaths
{
    public static SourceAnalysisContext CreateContext(string sourceRoot)
    {
        return new SourceAnalysisContext(
            new SourceIdentity(".workspace/AtomUI", "release/6.0", "unknown", "6.0", "1.0"),
            new SourcePathIndex(sourceRoot));
    }

    public static string ResolveAtomUISourceRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.GetFullPath(Path.Combine(current.FullName, ".workspace", "AtomUI"));
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Cannot locate .workspace/AtomUI from test output.");
    }
}
