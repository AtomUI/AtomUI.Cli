namespace AtomUI.Cli.Hosting.Metadata;

public sealed record DemoCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    DemoCommandMode Mode,
    DemoCodeLanguage CodeLanguage,
    DemoControlPayload Control,
    DemoSourceIdentityPayload Source,
    string? RequestedDemoKey,
    string? Scenario,
    string? Match,
    IReadOnlyList<string> AvailableScenarios,
    IReadOnlyList<DemoItemPayload> Demos,
    DemoItemPayload? SelectedDemo,
    IReadOnlyList<DemoSuggestionPayload> Suggestions,
    IReadOnlyList<DemoWarningPayload> Warnings) : IAtomUICliJsonPayload
{
    public IReadOnlyDictionary<string, object?> ToJsonPayload()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = SchemaVersion,
            ["command"] = Command,
            ["targetVersion"] = TargetVersion,
            ["mode"] = Mode.ToString(),
            ["codeLanguage"] = CodeLanguage.ToString(),
            ["control"] = Control.ToJson(),
            ["source"] = Source.ToJson(),
            ["requestedDemoKey"] = RequestedDemoKey,
            ["scenario"] = Scenario,
            ["match"] = Match,
            ["availableScenarios"] = AvailableScenarios.ToArray(),
            ["demos"] = Demos.Select(item => item.ToJson()).ToArray(),
            ["selectedDemo"] = SelectedDemo?.ToJson(),
            ["suggestions"] = Suggestions.Select(item => item.ToJson()).ToArray(),
            ["warnings"] = Warnings.Select(item => item.ToJson()).ToArray()
        };
    }
}

public sealed record DemoSourceIdentityPayload(
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

public sealed record DemoControlPayload(
    string Id,
    string Name,
    string DisplayName,
    string CategoryId,
    string ProductId,
    string PackageId,
    bool IsCommercial)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["name"] = Name,
            ["displayName"] = DisplayName,
            ["categoryId"] = CategoryId,
            ["productId"] = ProductId,
            ["packageId"] = PackageId,
            ["isCommercial"] = IsCommercial
        };
    }
}

public sealed record DemoItemPayload(
    string SourceKey,
    string Title,
    string Description,
    string Scenario,
    int Priority,
    string? BadgeText,
    IReadOnlyList<DemoCodeSnippetPayload> Snippets,
    string SourcePath,
    int? SourceLine,
    IReadOnlyList<DemoRelatedCommandPayload> RelatedCommands)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sourceKey"] = SourceKey,
            ["title"] = Title,
            ["description"] = Description,
            ["scenario"] = Scenario,
            ["priority"] = Priority,
            ["badgeText"] = BadgeText,
            ["snippets"] = Snippets.Select(item => item.ToJson()).ToArray(),
            ["sourcePath"] = SourcePath,
            ["sourceLine"] = SourceLine,
            ["relatedCommands"] = RelatedCommands.Select(item => item.ToJson()).ToArray()
        };
    }
}

public sealed record DemoCodeSnippetPayload(
    string Language,
    string Code,
    string? SourcePath,
    int? SourceLine)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["language"] = Language,
            ["code"] = Code,
            ["sourcePath"] = SourcePath,
            ["sourceLine"] = SourceLine
        };
    }
}

public sealed record DemoRelatedCommandPayload(string Command, string Description)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["command"] = Command,
            ["description"] = Description
        };
    }
}

public sealed record DemoSuggestionPayload(string SourceKey, string Title, string Scenario)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sourceKey"] = SourceKey,
            ["title"] = Title,
            ["scenario"] = Scenario
        };
    }
}

public sealed record DemoWarningPayload(string Code, string Message)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = Code,
            ["message"] = Message
        };
    }
}
