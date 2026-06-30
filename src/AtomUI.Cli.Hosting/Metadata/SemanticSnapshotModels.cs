namespace AtomUI.Cli.Hosting.Metadata;

public sealed record SemanticSnapshot(
    string SchemaVersion,
    string SnapshotId,
    string TargetVersion,
    string SourceCommit,
    IReadOnlyList<SemanticControlDocument> Controls,
    IReadOnlyList<SemanticDiagnosticDocument> Diagnostics);

public sealed record SemanticControlDocument(
    string Name,
    IReadOnlyList<SemanticPartDocument> Parts,
    IReadOnlyList<SemanticPseudoClassDocument> PseudoClasses,
    IReadOnlyList<SemanticThemeTemplateDocument> Templates);

public sealed record SemanticPartDocument(
    string Name,
    string NodeType,
    string Description,
    IReadOnlyList<string> BoundApis,
    IReadOnlyList<string> PseudoClasses,
    IReadOnlyList<string> TokenUsages,
    SemanticSourceDocument Source);

public sealed record SemanticPseudoClassDocument(
    string Name,
    string Description,
    IReadOnlyList<string> BoundApis,
    IReadOnlyList<string> Selectors,
    SemanticSourceDocument Source);

public sealed record SemanticThemeTemplateDocument(
    string ThemeName,
    string Selector,
    SemanticSourceDocument Source,
    IReadOnlyList<SemanticThemeNodeDocument> Roots);

public sealed record SemanticThemeNodeDocument(
    string ElementType,
    string? Name,
    IReadOnlyList<SemanticThemeNodeDocument> Children);

public sealed record SemanticSourceDocument(string Path, int? Line);

public sealed record SemanticDiagnosticDocument(string Code, string Severity, string Message);
