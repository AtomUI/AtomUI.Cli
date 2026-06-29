using AtomUI.Cli.Hosting.DependencyInjection;
using AtomUI.Cli.Hosting.Commands;
using AtomUI.Cli.Hosting.Commercial;
using AtomUI.Cli.Hosting.Errors;
using AtomUI.Cli.Hosting.Mcp;
using AtomUI.Cli.Hosting.Metadata;
using AtomUI.Cli.Hosting.Output;
using AtomUI.Cli.Hosting.ProjectAnalysis;
using AtomUI.Cli.Hosting.Setup;
using AtomUI.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AtomUI.Cli.Hosting;

public sealed class AtomUICliApplicationBuilder
{
    private readonly List<ModuleRegistration> _moduleRegistrations =
    [
        CreateModuleRegistration<AtomUICliCoreModule>("AtomUI Cli Core"),
        CreateModuleRegistration<AtomUICliMetadataModule>("AtomUI Cli Metadata"),
        CreateModuleRegistration<AtomUICliProjectAnalysisModule>("AtomUI Cli Project Analysis"),
        CreateModuleRegistration<AtomUICliMcpModule>("AtomUI Cli MCP"),
        CreateModuleRegistration<AtomUICliSetupModule>("AtomUI Cli Setup"),
        CreateModuleRegistration<AtomUICliCommercialDataModule>("AtomUI Cli Commercial Data")
    ];

    private readonly List<Action<IServiceCollection>> _serviceConfigurations = [];
    private readonly List<CommandManifest> _commandManifests = [.. CommandManifestCatalog.CreateBuiltIn().Manifests];

    internal AtomUICliApplicationBuilder(IReadOnlyList<string> args)
    {
        Args = args;
    }

    public IReadOnlyList<string> Args { get; }

    public AtomUICliApplicationBuilder AddModuleRegistration(ModuleRegistration registration, Type moduleType)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(moduleType);
        _moduleRegistrations.Add(registration);
        return this;
    }

    public AtomUICliApplicationBuilder AddCommandManifest(CommandManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        _commandManifests.Add(manifest);
        return this;
    }

    public AtomUICliApplicationBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(configureServices);
        _serviceConfigurations.Add(configureServices);
        return this;
    }

    public AtomUICliApplication Build()
    {
        return new AtomUICliApplication(
            Args,
            _moduleRegistrations.ToArray(),
            CommandManifestCatalog.Create(_commandManifests),
            _serviceConfigurations.ToArray());
    }

    internal static IHost BuildHost(
        IReadOnlyList<string> args,
        ModuleServiceRegistry moduleServices,
        CliCommandDescriptorCatalog commandCatalog,
        CommandManifestCatalog commandManifestCatalog,
        IReadOnlyList<Action<IServiceCollection>> serviceConfigurations)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = []
        });

        ModuleServiceDescriptorMapper.AddModuleServices(builder.Services, moduleServices.Descriptors);

        builder.Services.AddSingleton(commandCatalog);
        builder.Services.AddSingleton(commandManifestCatalog);
        builder.Services.AddSingleton<Commands.CliCommandParser>();
        builder.Services.AddSingleton<Commands.CliCommandDispatcher>();
        builder.Services.AddSingleton<IJsonOutputSerializer, JsonOutputSerializer>();
        builder.Services.AddSingleton<IOutputWriter>(_ => new ConsoleOutputWriter(Console.Out));
        builder.Services.AddSingleton<IErrorWriter>(provider => new ConsoleErrorWriter(
            Console.Error,
            provider.GetRequiredService<IJsonOutputSerializer>()));

        foreach (var configureServices in serviceConfigurations)
        {
            configureServices(builder.Services);
        }

        return builder.Build();
    }

    private static ModuleRegistration CreateModuleRegistration<TModule>(string displayName)
        where TModule : IModule, new()
    {
        return ModuleRegistration.For(
            ModuleDescriptor.For<TModule>(displayName),
            static () => new TModule());
    }
}
