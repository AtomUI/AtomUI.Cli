using AtomUI.Cli;

namespace AtomUI.Cli.Modularity;

public sealed class AtomUICliCommandContribution
{
    private readonly Func<GlobalCliOptions, IReadOnlyList<string>, IAtomUICliCommandOptions> _optionsFactory;
    private readonly Func<IServiceProvider, IAtomUICliCommandOptions, CliInvocationContext, CancellationToken, ValueTask<AtomUICliResult>> _executor;

    internal AtomUICliCommandContribution(
        string name,
        Type optionsType,
        Type handlerType,
        CommandGroup group,
        IReadOnlySet<OutputFormat> supportedFormats,
        bool isReadOnly,
        bool requiresProject,
        bool requiresWriteConfirmation,
        Func<GlobalCliOptions, IReadOnlyList<string>, IAtomUICliCommandOptions> optionsFactory,
        Func<IServiceProvider, IAtomUICliCommandOptions, CliInvocationContext, CancellationToken, ValueTask<AtomUICliResult>> executor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(optionsType);
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentNullException.ThrowIfNull(supportedFormats);
        ArgumentNullException.ThrowIfNull(optionsFactory);
        ArgumentNullException.ThrowIfNull(executor);

        Name = name;
        OptionsType = optionsType;
        HandlerType = handlerType;
        Group = group;
        SupportedFormats = supportedFormats;
        IsReadOnly = isReadOnly;
        RequiresProject = requiresProject;
        RequiresWriteConfirmation = requiresWriteConfirmation;
        _optionsFactory = optionsFactory;
        _executor = executor;
    }

    public string Name { get; }

    public Type OptionsType { get; }

    public Type HandlerType { get; }

    public CommandGroup Group { get; }

    public IReadOnlySet<OutputFormat> SupportedFormats { get; }

    public bool IsReadOnly { get; }

    public bool RequiresProject { get; }

    public bool RequiresWriteConfirmation { get; }

    public IAtomUICliCommandOptions CreateOptions(GlobalCliOptions globalOptions, IReadOnlyList<string> commandArguments)
    {
        return _optionsFactory(globalOptions, commandArguments);
    }

    public ValueTask<AtomUICliResult> ExecuteAsync(
        IServiceProvider serviceProvider,
        IAtomUICliCommandOptions options,
        CliInvocationContext context,
        CancellationToken cancellationToken)
    {
        return _executor(serviceProvider, options, context, cancellationToken);
    }

    internal static AtomUICliCommandContribution Create<TOptions, THandler>(
        string name,
        Func<GlobalCliOptions, IReadOnlyList<string>, TOptions> optionsFactory,
        CommandGroup group,
        IReadOnlySet<OutputFormat> supportedFormats,
        bool isReadOnly,
        bool requiresProject,
        bool requiresWriteConfirmation)
        where TOptions : IAtomUICliCommandOptions
        where THandler : class, IAtomUICliCommandHandler<TOptions>
    {
        return new AtomUICliCommandContribution(
            name,
            typeof(TOptions),
            typeof(THandler),
            group,
            supportedFormats,
            isReadOnly,
            requiresProject,
            requiresWriteConfirmation,
            (globalOptions, commandArguments) => optionsFactory(globalOptions, commandArguments),
            ExecuteAsync<TOptions, THandler>);
    }

    private static ValueTask<AtomUICliResult> ExecuteAsync<TOptions, THandler>(
        IServiceProvider serviceProvider,
        IAtomUICliCommandOptions options,
        CliInvocationContext context,
        CancellationToken cancellationToken)
        where TOptions : IAtomUICliCommandOptions
        where THandler : class, IAtomUICliCommandHandler<TOptions>
    {
        var handler = serviceProvider.GetService(typeof(THandler)) as THandler;
        if (handler is null)
        {
            throw new InvalidOperationException($"Command handler '{typeof(THandler).FullName}' is not registered.");
        }

        return handler.ExecuteAsync((TOptions)options, context, cancellationToken);
    }
}
