namespace AtomUI.Cli;

public interface IAtomUICliCommandHandler<in TOptions>
    where TOptions : IAtomUICliCommandOptions
{
    ValueTask<AtomUICliResult> ExecuteAsync(
        TOptions options,
        CliInvocationContext context,
        CancellationToken cancellationToken);
}
