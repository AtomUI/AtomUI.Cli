namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

internal sealed record ControlTypeFact(
    string ControlName,
    string Namespace,
    string? BaseType,
    IReadOnlyList<string> Interfaces,
    SourceLocation SourceLocation);

internal sealed record ControlApiMemberFact(
    string ControlName,
    string Name,
    string Kind,
    string DeclaringType,
    string Accessibility,
    string Signature,
    string? Type,
    string? DefaultValue,
    string ContractLevel,
    string Description,
    SourceLocation SourceLocation);

internal sealed record ControlApiEventFact(
    string ControlName,
    string Name,
    string Kind,
    string DeclaringType,
    string Accessibility,
    string Signature,
    string? RoutingStrategy,
    string? EventArgsType,
    string Description,
    SourceLocation SourceLocation);
