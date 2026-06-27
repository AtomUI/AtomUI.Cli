using AtomUI.Cli;

namespace AtomUI.Cli.Modularity;

public sealed class AtomUICliCommandContributionContext
{
    private readonly List<AtomUICliCommandContribution> _commands = [];

    public IReadOnlyList<AtomUICliCommandContribution> Commands => _commands;

    public void Add<TOptions, THandler>(
        string name,
        Func<GlobalCliOptions, IReadOnlyList<string>, TOptions> optionsFactory,
        CommandGroup group,
        IReadOnlySet<OutputFormat> supportedFormats,
        bool isReadOnly = true,
        bool requiresProject = false,
        bool requiresWriteConfirmation = false)
        where TOptions : IAtomUICliCommandOptions
        where THandler : class, IAtomUICliCommandHandler<TOptions>
    {
        _commands.Add(AtomUICliCommandContribution.Create<TOptions, THandler>(
            name,
            optionsFactory,
            group,
            supportedFormats,
            isReadOnly,
            requiresProject,
            requiresWriteConfirmation));
    }
}
