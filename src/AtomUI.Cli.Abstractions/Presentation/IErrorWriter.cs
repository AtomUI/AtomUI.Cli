namespace AtomUI.Cli;

public interface IErrorWriter
{
    ValueTask WriteErrorAsync(
        string commandName,
        AtomUICliError error,
        OutputFormat format,
        CancellationToken cancellationToken = default);
}
