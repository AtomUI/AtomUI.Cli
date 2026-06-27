namespace AtomUI.Cli;

public sealed record AtomUICliResult(
    bool IsSuccess,
    object? Payload,
    AtomUICliError? Error,
    IReadOnlyList<AtomUICliDiagnostic> Diagnostics,
    bool FailOnWarning)
{
    public static AtomUICliResult Success(object? payload = null)
    {
        return new AtomUICliResult(
            IsSuccess: true,
            Payload: payload,
            Error: null,
            Diagnostics: Array.Empty<AtomUICliDiagnostic>(),
            FailOnWarning: false);
    }

    public static AtomUICliResult Failure(AtomUICliError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AtomUICliResult(
            IsSuccess: false,
            Payload: null,
            Error: error,
            Diagnostics: Array.Empty<AtomUICliDiagnostic>(),
            FailOnWarning: false);
    }

    public static AtomUICliResult FromDiagnostics(
        IEnumerable<AtomUICliDiagnostic> diagnostics,
        bool failOnWarning,
        object? payload = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        var diagnosticArray = diagnostics as AtomUICliDiagnostic[] ?? diagnostics.ToArray();

        return new AtomUICliResult(
            IsSuccess: diagnosticArray.All(item => item.Severity != AtomUICliSeverity.Error)
                       && !(failOnWarning && diagnosticArray.Any(item => item.Severity == AtomUICliSeverity.Warning)),
            Payload: payload,
            Error: null,
            Diagnostics: Array.AsReadOnly(diagnosticArray),
            FailOnWarning: failOnWarning);
    }
}
