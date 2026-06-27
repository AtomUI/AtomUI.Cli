using AtomUI.Cli.Hosting.Commands.BuiltIn;
using AtomUI.Cli.Hosting.Errors;
using AtomUI.Modularity;

namespace AtomUI.Cli.Hosting;

[Module(DisplayName = "AtomUI Cli Core")]
public sealed partial class AtomUICliCoreModule : Modularity.AtomUICliModule
{
    public override void ConfigureServices(ModuleServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IErrorCodeCatalog>(ErrorCodeCatalog.Default);
        context.Services.AddSingleton<IExitCodeMapper, ExitCodeMapper>();
        context.Services.AddTransient<VersionCommandHandler, VersionCommandHandler>();
        context.Services.AddTransient<HelpCommandHandler, HelpCommandHandler>();
    }

    public override void ConfigureAtomUICliCommands(Modularity.AtomUICliCommandContributionContext context)
    {
        context.Add<BuiltInCommandOptions, VersionCommandHandler>(
            "version",
            (global, _) => new BuiltInCommandOptions(global),
            CommandGroup.Integration,
            new HashSet<OutputFormat> { OutputFormat.Text, OutputFormat.Json });

        context.Add<BuiltInCommandOptions, HelpCommandHandler>(
            "help",
            (global, _) => new BuiltInCommandOptions(global),
            CommandGroup.Integration,
            new HashSet<OutputFormat> { OutputFormat.Text, OutputFormat.Json, OutputFormat.Markdown });
    }
}
