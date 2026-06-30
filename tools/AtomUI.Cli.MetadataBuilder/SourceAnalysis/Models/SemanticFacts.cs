namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

internal sealed record SemanticPartFact(
    string ControlName,
    string Name,
    string NodeType,
    string Description,
    IReadOnlyList<string> BoundApis,
    IReadOnlyList<string> PseudoClasses,
    IReadOnlyList<string> TokenUsages,
    SourceLocation SourceLocation);

internal sealed record SemanticPseudoClassFact(
    string ControlName,
    string Name,
    string Description,
    IReadOnlyList<string> BoundApis,
    IReadOnlyList<string> Selectors,
    SourceLocation SourceLocation);
