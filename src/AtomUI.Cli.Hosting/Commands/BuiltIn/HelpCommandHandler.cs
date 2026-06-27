using AtomUI.Cli;

namespace AtomUI.Cli.Hosting.Commands.BuiltIn;

public sealed class HelpCommandHandler : IAtomUICliCommandHandler<BuiltInCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(
        BuiltInCommandOptions options,
        CliInvocationContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(AtomUICliResult.Success("dotnet atomui <command> [options]"));
    }
}
