using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;
using Xunit;

namespace AtomUI.Cli.Tests.MetadataBuilder;

public sealed class ControlThemeProcessorTests
{
    [Fact]
    public async Task ControlThemeProcessorExtractsButtonNamedNodesAndSelectors()
    {
        var sourceRoot = MetadataBuilderTestPaths.ResolveAtomUISourceRoot();
        var context = MetadataBuilderTestPaths.CreateContext(sourceRoot);

        await new ControlThemeProcessor().ExecuteAsync(context, TestContext.Current.CancellationToken);

        var nodes = context.Facts.GetByPrefix("theme-node:Button:").Select(fact => fact.Key).ToArray();
        Assert.Contains("theme-node:Button:PART_LoadingIcon", nodes);
        Assert.Contains("theme-node:Button:PART_ContentPresenter", nodes);
        Assert.Contains("theme-node:Button:PART_ButtonIcon", nodes);

        var selectors = context.Facts.GetByPrefix("theme-selector:Button:").Select(fact => fact.Value.ToString()).ToArray();
        Assert.Contains(selectors, selector => selector is not null && selector.Contains(":loading", StringComparison.Ordinal));
    }
}
