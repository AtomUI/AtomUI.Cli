using AtomUI.Cli;

namespace AtomUI.Cli.Hosting.Commands.BuiltIn;

public sealed class VersionCommandHandler : IAtomUICliCommandHandler<BuiltInCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(
        BuiltInCommandOptions options,
        CliInvocationContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var version = typeof(VersionCommandHandler).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        return ValueTask.FromResult(AtomUICliResult.Success(version));
    }
}
