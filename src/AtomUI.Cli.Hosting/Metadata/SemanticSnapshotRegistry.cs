namespace AtomUI.Cli.Hosting.Metadata;

public sealed class SemanticSnapshotRegistry
{
    private SemanticSnapshotRegistry(SemanticSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public SemanticSnapshot Snapshot { get; }

    public static SemanticSnapshotRegistry CreateDefault()
    {
        return new SemanticSnapshotRegistry(BuiltInSemanticSnapshotFactory.Create());
    }
}
