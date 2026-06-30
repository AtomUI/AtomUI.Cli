namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis;

internal sealed record SourceAnalysisPlan(IReadOnlyList<ISourceAnalysisProcessor> Processors);
