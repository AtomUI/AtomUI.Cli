namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

internal sealed record ControlThemeFact(
    string ControlName,
    string ThemeName,
    SourceLocation SourceLocation);

internal sealed record ThemeNodeFact(
    string ControlName,
    string Name,
    string NodeType,
    string ThemeName,
    SourceLocation SourceLocation);

internal sealed record ThemeTemplateTreeFact(
    string ControlName,
    string ThemeName,
    string Selector,
    IReadOnlyList<ThemeTemplateTreeNodeFact> Roots,
    SourceLocation SourceLocation);

internal sealed record ThemeTemplateTreeNodeFact(
    string ElementType,
    string? Name,
    IReadOnlyList<ThemeTemplateTreeNodeFact> Children);

internal sealed record ThemeSelectorFact(
    string ControlName,
    string Selector,
    IReadOnlyList<string> PseudoClasses,
    IReadOnlyList<string> TargetParts,
    string ThemeName,
    SourceLocation SourceLocation);

internal sealed record TemplateBindingFact(
    string ControlName,
    string? PartName,
    string Property,
    string BoundApi,
    string ThemeName,
    SourceLocation SourceLocation);
