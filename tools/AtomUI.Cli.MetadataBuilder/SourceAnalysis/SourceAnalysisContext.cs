using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis;

internal sealed class SourceAnalysisContext
{
    public SourceAnalysisContext(SourceIdentity identity, SourcePathIndex paths)
    {
        Identity = identity;
        Paths = paths;
    }

    public SourceIdentity Identity { get; }

    public SourcePathIndex Paths { get; }

    public SourceFactStore Facts { get; } = new();

    public SourceDiagnosticBag Diagnostics { get; } = new();

    public IDictionary<string, object> Cache { get; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
