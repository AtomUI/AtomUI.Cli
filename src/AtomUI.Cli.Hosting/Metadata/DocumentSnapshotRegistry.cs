namespace AtomUI.Cli.Hosting.Metadata;

public sealed class DocumentSnapshotRegistry(IReadOnlyList<DocumentSnapshot> snapshots)
{
    public IReadOnlyList<DocumentSnapshot> Snapshots { get; } = snapshots;

    public static DocumentSnapshotRegistry CreateDefault()
    {
        return new DocumentSnapshotRegistry(BuiltInDocumentSnapshotFactory.Create());
    }
}
