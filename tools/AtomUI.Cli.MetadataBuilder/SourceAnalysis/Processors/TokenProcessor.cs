namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;

internal sealed class TokenProcessor : ISourceAnalysisProcessor
{
    public const string SnapshotCacheKey = "token.snapshot";

    public string Id => "token";

    public IReadOnlySet<SourceAnalysisFeature> Provides { get; } =
        new HashSet<SourceAnalysisFeature>
        {
            SourceAnalysisFeature.TokenDefinition,
            SourceAnalysisFeature.TokenGraph,
            SourceAnalysisFeature.TokenUsage
        };

    public IReadOnlySet<SourceAnalysisFeature> Requires { get; } = new HashSet<SourceAnalysisFeature>();

    public SourceAnalysisPhase Phase => SourceAnalysisPhase.ExtractFacts;

    public ValueTask ExecuteAsync(SourceAnalysisContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var extractor = new TokenSourceExtractor();
        var snapshot = extractor.Extract(context.Paths.SourceRoot, context.Identity.TargetVersion);
        context.Cache[SnapshotCacheKey] = snapshot;

        foreach (var tokenSet in snapshot.ControlTokenSets)
        {
            foreach (var token in tokenSet.Tokens)
            {
                context.Facts.Add($"token:{tokenSet.ControlName}:{token.Name}", Id, token);
            }
        }

        foreach (var token in snapshot.SharedTokens)
        {
            context.Facts.Add($"shared-token:{token.Name}", Id, token);
        }

        foreach (var diagnostic in snapshot.Diagnostics)
        {
            context.Diagnostics.Add(diagnostic.Code, diagnostic.Severity, diagnostic.Message, Id);
        }

        return ValueTask.CompletedTask;
    }
}
