namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis;

internal interface ISourceAnalysisProcessor
{
    string Id { get; }

    IReadOnlySet<SourceAnalysisFeature> Provides { get; }

    IReadOnlySet<SourceAnalysisFeature> Requires { get; }

    SourceAnalysisPhase Phase { get; }

    ValueTask ExecuteAsync(SourceAnalysisContext context, CancellationToken cancellationToken);
}
