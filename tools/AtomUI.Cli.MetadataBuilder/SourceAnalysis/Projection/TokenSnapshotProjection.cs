using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Projection;

internal static class TokenSnapshotProjection
{
    public static ExtractedTokenSnapshot Create(SourceAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Cache.TryGetValue(TokenProcessor.SnapshotCacheKey, out var snapshot)
            && snapshot is ExtractedTokenSnapshot tokenSnapshot)
        {
            return tokenSnapshot;
        }

        throw new InvalidOperationException("Token snapshot has not been produced by the source analysis pipeline.");
    }
}
