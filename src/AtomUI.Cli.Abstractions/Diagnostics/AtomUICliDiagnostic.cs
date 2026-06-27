namespace AtomUI.Cli;

public sealed record AtomUICliDiagnostic(
    string Code,
    AtomUICliSeverity Severity,
    string Category,
    string Message,
    string? File = null,
    int? Line = null,
    string? Suggestion = null);
