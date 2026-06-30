namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis;

internal sealed class SourceAnalysisPipeline
{
    public async ValueTask ExecuteAsync(
        SourceAnalysisPlan plan,
        SourceAnalysisContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var processor in plan.Processors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await processor.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }
}
