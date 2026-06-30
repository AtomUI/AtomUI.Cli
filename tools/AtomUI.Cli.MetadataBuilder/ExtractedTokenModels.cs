namespace AtomUI.Cli.MetadataBuilder;

internal sealed record ExtractedTokenSnapshot(
    string SchemaVersion,
    string SnapshotId,
    string TargetVersion,
    string SourceCommit,
    IReadOnlyList<ExtractedProduct> Products,
    IReadOnlyList<ExtractedSharedToken> SharedTokens,
    IReadOnlyList<ExtractedControlTokenSet> ControlTokenSets,
    IReadOnlyList<ExtractedDiagnostic> Diagnostics);

internal sealed record ExtractedProduct(
    string Id,
    string Name,
    string PackageId,
    bool IsCommercial);

internal sealed record ExtractedSharedToken(
    string Name,
    string Kind,
    string Category,
    string Type,
    string? DefaultValue,
    string Description,
    string SourcePath,
    int SourceLine);

internal sealed record ExtractedControlTokenSet(
    string ControlName,
    string TokenId,
    string TokenTypeName,
    string ScopeProviderName,
    string PackageId,
    string ProductId,
    bool IsCommercial,
    IReadOnlyList<string> RegisteredControls,
    IReadOnlyList<ExtractedControlToken> Tokens,
    IReadOnlyList<string> SharedDependencies,
    IReadOnlyList<ExtractedTokenUsage> ThemeUsages);

internal sealed record ExtractedControlToken(
    string ControlName,
    string Name,
    string Category,
    string Type,
    string? DefaultValue,
    string Description,
    string TokenTypeName,
    string SourcePath,
    int SourceLine,
    string? GeneratedPath,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<ExtractedTokenUsage> Usages,
    string CustomizationLevel);

internal sealed record ExtractedTokenUsage(
    string ThemeName,
    string Selector,
    string TargetProperty,
    string? TargetNode,
    string ResourceExpression,
    string SourcePath,
    int SourceLine);

internal sealed record ExtractedDiagnostic(
    string Code,
    string Severity,
    string Message);
