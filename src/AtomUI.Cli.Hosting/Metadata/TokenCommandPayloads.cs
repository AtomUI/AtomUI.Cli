namespace AtomUI.Cli.Hosting.Metadata;

public sealed record TokenCommandPayload(
    string SchemaVersion,
    string Command,
    string TargetVersion,
    string SnapshotId,
    TokenQuerySummaryPayload Query,
    TokenControlPayload? Control,
    IReadOnlyList<TokenGroupPayload> Groups,
    IReadOnlyList<TokenDocumentPayload> Tokens,
    IReadOnlyList<TokenGraphEdgePayload> Graph,
    IReadOnlyList<TokenUsagePayload> Usages,
    IReadOnlyList<TokenDiagnosticPayload> Diagnostics,
    IReadOnlyList<TokenNextActionPayload> NextActions) : IAtomUICliJsonPayload
{
    public IReadOnlyDictionary<string, object?> ToJsonPayload()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = SchemaVersion,
            ["command"] = Command,
            ["targetVersion"] = TargetVersion,
            ["snapshotId"] = SnapshotId,
            ["query"] = Query.ToJson(),
            ["control"] = Control?.ToJson(),
            ["groups"] = Groups.Select(group => group.ToJson()).ToArray(),
            ["tokens"] = Tokens.Select(token => token.ToJson()).ToArray(),
            ["graph"] = Graph.Select(edge => edge.ToJson()).ToArray(),
            ["usages"] = Usages.Select(usage => usage.ToJson()).ToArray(),
            ["diagnostics"] = Diagnostics.Select(diagnostic => diagnostic.ToJson()).ToArray(),
            ["nextActions"] = NextActions.Select(action => action.ToJson()).ToArray()
        };
    }
}

public sealed record TokenQuerySummaryPayload(
    string? Control,
    string? Token,
    string Scope,
    string Kind,
    string? Category,
    string? Match,
    string Theme,
    IReadOnlyList<string> Include)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["control"] = Control,
            ["token"] = Token,
            ["scope"] = Scope,
            ["kind"] = Kind,
            ["category"] = Category,
            ["match"] = Match,
            ["theme"] = Theme,
            ["include"] = Include.ToArray()
        };
    }
}

public sealed record TokenControlPayload(
    string Name,
    string TokenId,
    string TokenTypeName,
    string ScopeProviderName,
    string PackageId,
    string ProductId,
    bool IsCommercial,
    string ResourceKind,
    string ResourceKeyKind,
    IReadOnlyList<string> RegisteredControls)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["tokenId"] = TokenId,
            ["tokenTypeName"] = TokenTypeName,
            ["scopeProviderName"] = ScopeProviderName,
            ["packageId"] = PackageId,
            ["productId"] = ProductId,
            ["isCommercial"] = IsCommercial,
            ["resourceKind"] = ResourceKind,
            ["resourceKeyKind"] = ResourceKeyKind,
            ["registeredControls"] = RegisteredControls.ToArray()
        };
    }
}

public sealed record TokenGroupPayload(
    string Key,
    string Title,
    IReadOnlyList<TokenDocumentPayload> Tokens)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["key"] = Key,
            ["title"] = Title,
            ["tokens"] = Tokens.Select(token => token.ToJson()).ToArray()
        };
    }
}

public sealed record TokenDocumentPayload(
    string Id,
    string Name,
    string Scope,
    string Kind,
    string Category,
    string Type,
    string? DefaultValue,
    string? Description,
    TokenResourcePayload Resource,
    TokenSourcePayload Source,
    IReadOnlyList<TokenValueVariantPayload> Values,
    IReadOnlyList<TokenGraphEdgePayload> Dependencies,
    IReadOnlyList<TokenUsagePayload> Usages,
    TokenCustomizationPayload Customization)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = Id,
            ["name"] = Name,
            ["scope"] = Scope,
            ["kind"] = Kind,
            ["category"] = Category,
            ["type"] = Type,
            ["defaultValue"] = DefaultValue,
            ["description"] = Description,
            ["resource"] = Resource.ToJson(),
            ["source"] = Source.ToJson(),
            ["values"] = Values.Select(value => value.ToJson()).ToArray(),
            ["dependencies"] = Dependencies.Select(edge => edge.ToJson()).ToArray(),
            ["usages"] = Usages.Select(usage => usage.ToJson()).ToArray(),
            ["customization"] = Customization.ToJson()
        };
    }
}

public sealed record TokenResourcePayload(
    string? ResourceKind,
    string? ResourceKey,
    string? MarkupExtension,
    string? XamlSnippet,
    string Stability)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["resourceKind"] = ResourceKind,
            ["resourceKey"] = ResourceKey,
            ["markupExtension"] = MarkupExtension,
            ["xamlSnippet"] = XamlSnippet,
            ["stability"] = Stability
        };
    }
}

public sealed record TokenSourcePayload(
    string? DefinitionPath,
    int? DefinitionLine,
    string? GeneratedPath,
    int? GeneratedLine)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["definitionPath"] = DefinitionPath,
            ["definitionLine"] = DefinitionLine,
            ["generatedPath"] = GeneratedPath,
            ["generatedLine"] = GeneratedLine
        };
    }
}

public sealed record TokenValueVariantPayload(string Theme, string? Value, string Source)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["theme"] = Theme,
            ["value"] = Value,
            ["source"] = Source
        };
    }
}

public sealed record TokenGraphEdgePayload(string From, string To, string Relation, string Description)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["from"] = From,
            ["to"] = To,
            ["relation"] = Relation,
            ["description"] = Description
        };
    }
}

public sealed record TokenUsagePayload(
    string ThemeName,
    string Selector,
    string TargetProperty,
    string? TargetNode,
    string ResourceExpression,
    string SourcePath,
    int? SourceLine)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["themeName"] = ThemeName,
            ["selector"] = Selector,
            ["targetProperty"] = TargetProperty,
            ["targetNode"] = TargetNode,
            ["resourceExpression"] = ResourceExpression,
            ["sourcePath"] = SourcePath,
            ["sourceLine"] = SourceLine
        };
    }
}

public sealed record TokenCustomizationPayload(string Level, string Recommendation, IReadOnlyList<string> Notes)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["level"] = Level,
            ["recommendation"] = Recommendation,
            ["notes"] = Notes.ToArray()
        };
    }
}

public sealed record TokenDiagnosticPayload(string Code, string Severity, string Message)
{
    public IReadOnlyDictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = Code,
            ["severity"] = Severity,
            ["message"] = Message
        };
    }
}

public sealed record TokenNextActionPayload(string Command, string Description)
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
