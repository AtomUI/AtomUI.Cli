using AtomUI.Cli;

namespace AtomUI.Cli.Hosting.Errors;

public sealed class ExitCodeMapper(IErrorCodeCatalog errorCodeCatalog) : IExitCodeMapper
{
    public int Map(AtomUICliResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Error is not null)
        {
            return MapError(result.Error);
        }

        if (HasDiagnosticFailure(result))
        {
            return 5;
        }

        return 0;
    }

    private int MapError(AtomUICliError error)
    {
        var descriptor = errorCodeCatalog.Find(error.Code);
        return descriptor?.DefaultExitCode ?? 1;
    }

    private static bool HasDiagnosticFailure(AtomUICliResult result)
    {
        return result.Diagnostics.Any(item => item.Severity == AtomUICliSeverity.Error)
               || (result.FailOnWarning && result.Diagnostics.Any(item => item.Severity == AtomUICliSeverity.Warning));
    }
}
