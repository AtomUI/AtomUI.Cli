using AtomUI.Cli;
using Microsoft.Extensions.DependencyInjection;

namespace AtomUI.Cli.Hosting.Commands;

public sealed class CliCommandDescriptor
{
    private readonly Func<GlobalCliOptions, IReadOnlyList<string>, IAtomUICliCommandOptions> _optionsFactory;
    private readonly Func<IServiceProvider, IAtomUICliCommandOptions, CliInvocationContext, CancellationToken, ValueTask<AtomUICliResult>> _executor;

    internal CliCommandDescriptor(
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

    public static CliCommandDescriptor Create<TOptions, THandler>(
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
        return new CliCommandDescriptor(
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

    private static ValueTask<AtomUICliResult> ExecuteAsync<TOptions, THandler>(
        IServiceProvider serviceProvider,
        IAtomUICliCommandOptions options,
        CliInvocationContext context,
        CancellationToken cancellationToken)
        where TOptions : IAtomUICliCommandOptions
        where THandler : class, IAtomUICliCommandHandler<TOptions>
    {
        var handler = serviceProvider.GetRequiredService<THandler>();
        return handler.ExecuteAsync((TOptions)options, context, cancellationToken);
    }
}
