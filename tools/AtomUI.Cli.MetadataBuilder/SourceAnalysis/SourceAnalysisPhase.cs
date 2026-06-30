namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis;

internal enum SourceAnalysisPhase
{
    ReadWorkspace,
    ExtractFacts,
    AssembleContracts,
    ProjectSnapshots,
    WriteArtifacts
}
