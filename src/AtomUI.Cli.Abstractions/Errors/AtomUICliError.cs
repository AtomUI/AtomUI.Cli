namespace AtomUI.Cli;

public sealed record AtomUICliError(
    string Code,
    AtomUICliSeverity Severity,
    string Message,
    string? Suggestion,
    string Stage,
    CliLocation? Location,
    IReadOnlyDictionary<string, string>? Details);
