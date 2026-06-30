namespace AtomUI.Cli.Hosting.Metadata;

public sealed record TokenSnapshot(
    string SchemaVersion,
    string SnapshotId,
    string TargetVersion,
    string SourceCommit,
    IReadOnlyList<TokenProductDocument> Products,
    IReadOnlyList<TokenDocument> SharedTokens,
    IReadOnlyList<ControlTokenSetDocument> ControlTokenSets,
    IReadOnlyList<TokenGraphEdgeDocument> GraphEdges,
    IReadOnlyList<TokenDiagnosticDocument> Diagnostics);

public sealed record TokenProductDocument(
    string Id,
    string Name,
    string PackageId,
    bool IsCommercial);

public sealed record ControlTokenSetDocument(
    string ControlName,
    string TokenId,
    string TokenTypeName,
    string ScopeProviderName,
    string PackageId,
    string ProductId,
    bool IsCommercial,
    IReadOnlyList<string> RegisteredControls,
    IReadOnlyList<TokenDocument> Tokens,
    IReadOnlyList<string> SharedDependencies,
    IReadOnlyList<TokenUsageDocument> ThemeUsages);

public sealed record TokenDocument(
    string Id,
    string Name,
    string Scope,
    string Kind,
    string Category,
    string Type,
    string? DefaultValue,
    string? Description,
    TokenResourceDocument Resource,
    TokenSourceDocument Source,
    IReadOnlyList<TokenValueVariantDocument> Values,
    IReadOnlyList<TokenGraphEdgeDocument> Dependencies,
    IReadOnlyList<TokenUsageDocument> Usages,
    TokenCustomizationDocument Customization);

public sealed record TokenResourceDocument(
    string? ResourceKind,
    string? ResourceKey,
    string? MarkupExtension,
    string? XamlSnippet,
    string Stability);

public sealed record TokenSourceDocument(
    string? DefinitionPath,
    int? DefinitionLine,
    string? GeneratedPath,
    int? GeneratedLine);

public sealed record TokenValueVariantDocument(
    string Theme,
    string? Value,
    string Source);

public sealed record TokenGraphEdgeDocument(
    string From,
    string To,
    string Relation,
    string Description);

public sealed record TokenUsageDocument(
    string ThemeName,
    string Selector,
    string TargetProperty,
    string? TargetNode,
    string ResourceExpression,
    string SourcePath,
    int? SourceLine);

public sealed record TokenCustomizationDocument(
    string Level,
    string Recommendation,
    IReadOnlyList<string> Notes);

public sealed record TokenDiagnosticDocument(
    string Code,
    string Severity,
    string Message);
