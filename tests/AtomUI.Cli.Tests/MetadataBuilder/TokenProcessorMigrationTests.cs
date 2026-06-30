using AtomUI.Cli.MetadataBuilder.SourceAnalysis;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;
using Xunit;

namespace AtomUI.Cli.Tests.MetadataBuilder;

public sealed class TokenProcessorMigrationTests
{
    [Fact]
    public async Task TokenProcessorKeepsButtonTokenCompleteness()
    {
        var sourceRoot = ResolveAtomUISourceRoot();
        var context = new SourceAnalysisContext(
            new SourceIdentity("default-reference-project", "release/6.0", "unknown", "6.0", "1.0"),
            new SourcePathIndex(sourceRoot));
        var processor = new TokenProcessor();

        await processor.ExecuteAsync(context, TestContext.Current.CancellationToken);

        var buttonTokens = context.Facts.GetByPrefix("token:Button:").ToArray();
        Assert.Equal(53, buttonTokens.Length);
        Assert.Contains(buttonTokens, fact => fact.Key.Equals("token:Button:Padding", StringComparison.Ordinal));
    }

    private static string ResolveAtomUISourceRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.GetFullPath(Path.Combine(current.FullName, "..", "ReferenceProjects", "AtomUI"));
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Cannot locate ../ReferenceProjects/AtomUI from test output.");
    }
}
