using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;
using Xunit;

namespace AtomUI.Cli.Tests.MetadataBuilder;

public sealed class SemanticContractProcessorTests
{
    [Fact]
    public async Task SemanticContractCombinesSourceAndThemeFacts()
    {
        var sourceRoot = MetadataBuilderTestPaths.ResolveAtomUISourceRoot();
        var context = MetadataBuilderTestPaths.CreateContext(sourceRoot);

        await new ControlSourceProcessor().ExecuteAsync(context, TestContext.Current.CancellationToken);
        await new ControlThemeProcessor().ExecuteAsync(context, TestContext.Current.CancellationToken);
        await new TokenProcessor().ExecuteAsync(context, TestContext.Current.CancellationToken);
        await new SemanticContractProcessor().ExecuteAsync(context, TestContext.Current.CancellationToken);

        var loadingIcon = Assert.Single(context.Facts.Get("semantic-part:Button:PART_LoadingIcon"));
        Assert.Contains("IsLoading", loadingIcon.Value.ToString(), StringComparison.Ordinal);
        Assert.Contains(":loading", loadingIcon.Value.ToString(), StringComparison.Ordinal);

        var loadingState = Assert.Single(context.Facts.Get("semantic-pseudo-class:Button::loading"));
        Assert.Contains("IsLoading", loadingState.Value.ToString(), StringComparison.Ordinal);
    }
}
