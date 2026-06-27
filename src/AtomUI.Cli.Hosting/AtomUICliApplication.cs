using AtomUI.Cli.Hosting.Commands;
using AtomUI.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AtomUI.Cli.Hosting;

public sealed class AtomUICliApplication : IAsyncDisposable
{
    private readonly IReadOnlyList<string> _args;
    private readonly IReadOnlyList<ModuleRegistration> _moduleRegistrations;
    private readonly IReadOnlyList<Type> _enabledModuleTypes;
    private readonly IReadOnlyList<Action<IServiceCollection>> _serviceConfigurations;

    internal AtomUICliApplication(
        IReadOnlyList<string> args,
        IReadOnlyList<ModuleRegistration> moduleRegistrations,
        IReadOnlyList<Type> enabledModuleTypes,
        IReadOnlyList<Action<IServiceCollection>> serviceConfigurations)
    {
        _args = args;
        _moduleRegistrations = moduleRegistrations;
        _enabledModuleTypes = enabledModuleTypes;
        _serviceConfigurations = serviceConfigurations;
    }

    public ModuleHost? ModuleHost { get; private set; }

    public IHost? Host { get; private set; }

    public CliCommandDescriptorCatalog Commands { get; private set; } = CliCommandDescriptorCatalog.Empty;

    public static AtomUICliApplicationBuilder CreateBuilder(string[] args)
    {
        return new AtomUICliApplicationBuilder(Array.AsReadOnly(args.ToArray()));
    }

    public async ValueTask<int> RunAsync(
        IReadOnlyList<string>? args = null,
        CancellationToken cancellationToken = default)
    {
        var runArgs = args ?? _args;
        ModuleHost = new ModuleHost(_moduleRegistrations, new ModuleHostOptions(_enabledModuleTypes));

        try
        {
            var lifecycleResult = ModuleHost.ResolveModules();
            if (!lifecycleResult.IsSuccess)
            {
                return 1;
            }

            cancellationToken.ThrowIfCancellationRequested();
            lifecycleResult = ModuleHost.CreateModules();
            if (!lifecycleResult.IsSuccess)
            {
                return 1;
            }

            lifecycleResult = await ModuleHost.PreConfigureServicesAsync(cancellationToken);
            if (!lifecycleResult.IsSuccess)
            {
                return 1;
            }

            lifecycleResult = await ModuleHost.ConfigureServicesAsync(cancellationToken);
            if (!lifecycleResult.IsSuccess)
            {
                return 1;
            }

            lifecycleResult = await ModuleHost.PostConfigureServicesAsync(cancellationToken);
            if (!lifecycleResult.IsSuccess)
            {
                return 1;
            }

            lifecycleResult = ModuleHost.FreezeServices();
            if (!lifecycleResult.IsSuccess)
            {
                return 1;
            }

            Commands = await ConfigureCommandsAsync(ModuleHost, cancellationToken);
            Host = AtomUICliApplicationBuilder.BuildHost(runArgs, ModuleHost.Services, Commands, _serviceConfigurations);

            lifecycleResult = await ModuleHost.InitializeModulesAsync(cancellationToken);
            if (!lifecycleResult.IsSuccess)
            {
                return 1;
            }

            await Host.StartAsync(cancellationToken);

            var dispatcher = Host.Services.GetRequiredService<CliCommandDispatcher>();
            var result = await dispatcher.DispatchAsync(runArgs, Commands, cancellationToken);
            var mapper = Host.Services.GetRequiredService<IExitCodeMapper>();
            return mapper.Map(result);
        }
        finally
        {
            if (Host is not null)
            {
                await Host.StopAsync(CancellationToken.None);
                Host.Dispose();
                Host = null;
            }

            if (ModuleHost is not null)
            {
                await ModuleHost.ShutdownAsync(CancellationToken.None);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Host is not null)
        {
            await Host.StopAsync(CancellationToken.None);
            Host.Dispose();
            Host = null;
        }

        if (ModuleHost is not null)
        {
            await ModuleHost.ShutdownAsync(CancellationToken.None);
            ModuleHost = null;
        }
    }

    private static async ValueTask<CliCommandDescriptorCatalog> ConfigureCommandsAsync(
        ModuleHost moduleHost,
        CancellationToken cancellationToken)
    {
        var context = new Modularity.AtomUICliCommandContributionContext();

        foreach (var entry in moduleHost.EnabledModules)
        {
            if (entry.Module is Modularity.AtomUICliModule cliModule)
            {
                await cliModule.ConfigureAtomUICliCommandsAsync(context, cancellationToken);
            }
        }

        return CliCommandDescriptorCatalog.Create(
            context.Commands.Select(command => command.ToCliCommandDescriptor()));
    }
}
