using AtomUI.Cli;

namespace AtomUI.Cli.Hosting.Presentation;

public sealed class ConsoleOutputWriter(TextWriter stdout) : IOutputWriter
{
    public ValueTask WriteLineAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        stdout.WriteLine(text);
        stdout.Flush();
        return ValueTask.CompletedTask;
    }
}
