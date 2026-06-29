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
        return ValueTask.FromResult(AtomUICliResult.Success(CliProductInfo.Current.Version));
    }
}
