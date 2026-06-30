namespace AtomUI.Cli.Hosting.Metadata;

public sealed class TokenSnapshotRegistry(TokenSnapshot snapshot)
{
    public TokenSnapshot Snapshot { get; } = snapshot;

    public static TokenSnapshotRegistry CreateDefault()
    {
        return new TokenSnapshotRegistry(BuiltInTokenSnapshotFactory.Create());
    }
}
