namespace AtomUI.Cli;

public interface IOutputWriter
{
    ValueTask WriteLineAsync(string text, CancellationToken cancellationToken = default);
}
