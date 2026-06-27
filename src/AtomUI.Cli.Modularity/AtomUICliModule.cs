using AtomUI.Modularity;

namespace AtomUI.Cli.Modularity;

public abstract class AtomUICliModule : ModuleBase
{
    public virtual ValueTask ConfigureAtomUICliCommandsAsync(
        AtomUICliCommandContributionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        ConfigureAtomUICliCommands(context);
        return ValueTask.CompletedTask;
    }

    public virtual void ConfigureAtomUICliCommands(AtomUICliCommandContributionContext context)
    {
    }
}
