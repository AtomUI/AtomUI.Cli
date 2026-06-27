using AtomUI.Cli.Hosting.DependencyInjection;
using AtomUI.Cli.Hosting.Commands;
using AtomUI.Cli.Hosting.Errors;
using AtomUI.Cli.Hosting.Output;
using AtomUI.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AtomUI.Cli.Hosting;

public sealed class AtomUICliApplicationBuilder
{
    private readonly List<ModuleRegistration> _moduleRegistrations = [CreateCoreModuleRegistration()];
    private readonly List<Type> _enabledModuleTypes = [typeof(AtomUICliCoreModule)];
    private readonly List<Action<IServiceCollection>> _serviceConfigurations = [];

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
        _enabledModuleTypes.Add(moduleType);
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
            _enabledModuleTypes.ToArray(),
            _serviceConfigurations.ToArray());
    }

    internal static IHost BuildHost(
        IReadOnlyList<string> args,
        ModuleServiceRegistry moduleServices,
        CliCommandDescriptorCatalog commandCatalog,
        IReadOnlyList<Action<IServiceCollection>> serviceConfigurations)
    {
        var builder = Host.CreateApplicationBuilder(args.ToArray());

        ModuleServiceDescriptorMapper.AddModuleServices(builder.Services, moduleServices.Descriptors);

        builder.Services.AddSingleton(commandCatalog);
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

    private static ModuleRegistration CreateCoreModuleRegistration()
    {
        return ModuleRegistration.For(
            ModuleDescriptor.For<AtomUICliCoreModule>("AtomUI Cli Core"),
            static () => new AtomUICliCoreModule());
    }
}
