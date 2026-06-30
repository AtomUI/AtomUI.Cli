namespace AtomUI.Cli.MetadataBuilder;

internal sealed record ExtractedSemanticSnapshot(
    string SchemaVersion,
    string SnapshotId,
    string TargetVersion,
    string SourceCommit,
    IReadOnlyList<ExtractedSemanticControl> Controls,
    IReadOnlyList<ExtractedDiagnostic> Diagnostics);

internal sealed record ExtractedSemanticControl(
    string Name,
    IReadOnlyList<ExtractedSemanticPart> Parts,
    IReadOnlyList<ExtractedSemanticPseudoClass> PseudoClasses,
    IReadOnlyList<ExtractedSemanticThemeTemplate> Templates);

internal sealed record ExtractedSemanticPart(
    string Name,
    string NodeType,
    string Description,
    IReadOnlyList<string> BoundApis,
    IReadOnlyList<string> PseudoClasses,
    IReadOnlyList<string> TokenUsages,
    string SourcePath,
    int? SourceLine);

internal sealed record ExtractedSemanticPseudoClass(
    string Name,
    string Description,
    IReadOnlyList<string> BoundApis,
    IReadOnlyList<string> Selectors,
    string SourcePath,
    int? SourceLine);

internal sealed record ExtractedSemanticThemeTemplate(
    string ThemeName,
    string Selector,
    string SourcePath,
    int? SourceLine,
    IReadOnlyList<ExtractedSemanticThemeNode> Roots);

internal sealed record ExtractedSemanticThemeNode(
    string ElementType,
    string? Name,
    IReadOnlyList<ExtractedSemanticThemeNode> Children);
