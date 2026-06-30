namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

internal sealed record PseudoClassFact(
    string ControlName,
    string Name,
    string SourceExpression,
    SourceLocation SourceLocation);

internal sealed record StateFlowFact(
    string ControlName,
    string PseudoClass,
    string Condition,
    IReadOnlyList<string> ReferencedApis,
    SourceLocation SourceLocation);

internal sealed record NameScopeLookupFact(
    string ControlName,
    string PartName,
    string? RequestedType,
    SourceLocation SourceLocation);
