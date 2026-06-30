using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis;

internal sealed class SourceDiagnosticBag
{
    private readonly List<SourceDiagnostic> _diagnostics = [];

    public IReadOnlyList<SourceDiagnostic> Diagnostics => _diagnostics;

    public void Add(
        string code,
        string severity,
        string message,
        string processorId,
        SourceLocation? location = null,
        string? targetKind = null,
        string? targetId = null)
    {
        _diagnostics.Add(new SourceDiagnostic(
            code,
            severity,
            message,
            processorId,
            location,
            targetKind,
            targetId));
    }
}

internal sealed record SourceDiagnostic(
    string Code,
    string Severity,
    string Message,
    string ProcessorId,
    SourceLocation? Location,
    string? TargetKind,
    string? TargetId);
