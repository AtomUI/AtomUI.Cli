namespace AtomUI.Cli.Hosting.Metadata;

public sealed class PackageSnapshotRegistry(IReadOnlyList<PackageSnapshot> snapshots)
{
    public IReadOnlyList<PackageSnapshot> Snapshots { get; } = snapshots;

    public static PackageSnapshotRegistry CreateDefault()
    {
        return new PackageSnapshotRegistry(BuiltInPackageSnapshotFactory.Create());
    }
}
