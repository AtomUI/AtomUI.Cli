namespace AtomUI.Cli.Hosting.Metadata;

public sealed record DesignCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string Section,
    string Audience,
    string Doc,
    DesignDocumentSourcePayload Source,
    IReadOnlyList<DesignDocumentWarningPayload> Warnings) : IAtomUICliJsonPayload
{
    public IReadOnlyDictionary<string, object?> ToJsonPayload()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = SchemaVersion,
            ["command"] = Command,
            ["targetVersion"] = TargetVersion,
            ["section"] = Section,
            ["audience"] = Audience,
            ["doc"] = Doc,
            ["source"] = Source.ToJson(),
            ["warnings"] = Warnings.Select(warning => warning.ToJson()).ToArray()
        };
    }
}

public sealed record DesignDocumentSourcePayload(
    string SnapshotSchemaVersion,
    string SnapshotId,
    string SourceRootConvention,
    string SourceRef,
    string SourceCommit,
    string GeneratedAt)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["snapshotSchemaVersion"] = SnapshotSchemaVersion,
            ["snapshotId"] = SnapshotId,
            ["sourceRootConvention"] = SourceRootConvention,
            ["sourceRef"] = SourceRef,
            ["sourceCommit"] = SourceCommit,
            ["generatedAt"] = GeneratedAt
        };
    }
}

public sealed record DesignDocumentWarningPayload(string Code, string Message, string? Section)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = Code,
            ["message"] = Message,
            ["section"] = Section
        };
    }
}
