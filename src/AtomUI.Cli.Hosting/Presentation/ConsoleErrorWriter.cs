using AtomUI.Cli;

namespace AtomUI.Cli.Hosting.Presentation;

public sealed class ConsoleErrorWriter(
    TextWriter stderr,
    IJsonOutputSerializer jsonOutputSerializer) : IErrorWriter
{
    public ValueTask WriteErrorAsync(
        string commandName,
        AtomUICliError error,
        OutputFormat format,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(error);
        cancellationToken.ThrowIfCancellationRequested();

        var text = format == OutputFormat.Json
            ? jsonOutputSerializer.SerializeError(commandName, error)
            : FormatTextError(error);

        stderr.WriteLine(text);
        stderr.Flush();
        return ValueTask.CompletedTask;
    }

    private static string FormatTextError(AtomUICliError error)
    {
        if (string.IsNullOrWhiteSpace(error.Suggestion))
        {
            return $"{error.Code}: {error.Message}";
        }

        return $"{error.Code}: {error.Message}{Environment.NewLine}{error.Suggestion}";
    }
}
