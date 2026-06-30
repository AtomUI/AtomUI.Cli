namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

internal sealed record SourceIdentity(
    string SourceRootConvention,
    string SourceRef,
    string SourceCommit,
    string TargetVersion,
    string SnapshotSchemaVersion);
