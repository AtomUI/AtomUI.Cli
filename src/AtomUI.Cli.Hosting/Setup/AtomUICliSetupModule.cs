using AtomUI.Cli.Modularity;
using AtomUI.Modularity;

namespace AtomUI.Cli.Hosting.Setup;

[Module(DisplayName = "AtomUI Cli Setup")]
public sealed partial class AtomUICliSetupModule : AtomUICliModule
{
    private static readonly HashSet<OutputFormat> WriteFormats =
    [
        OutputFormat.Text,
        OutputFormat.Json,
        OutputFormat.Markdown
    ];

    public override void ConfigureServices(ModuleServiceConfigurationContext context)
    {
        context.Services.AddSingleton<SetupPlanService, SetupPlanService>();
        context.Services.AddTransient<SetupCommandHandler, SetupCommandHandler>();
        context.Services.AddTransient<InitCommandHandler, InitCommandHandler>();
        context.Services.AddTransient<AddCommandHandler, AddCommandHandler>();
        context.Services.AddTransient<UpgradeCommandHandler, UpgradeCommandHandler>();
    }

    public override void ConfigureAtomUICliCommands(AtomUICliCommandContributionContext context)
    {
        context.Add<SetupCommandOptions, SetupCommandHandler>("setup", SetupCommandOptions.Parse, CommandGroup.Write, WriteFormats, isReadOnly: false, requiresWriteConfirmation: true);
        context.Add<InitCommandOptions, InitCommandHandler>("init", InitCommandOptions.Parse, CommandGroup.Write, WriteFormats, isReadOnly: false, requiresProject: true, requiresWriteConfirmation: true);
        context.Add<AddCommandOptions, AddCommandHandler>("add", AddCommandOptions.Parse, CommandGroup.Write, WriteFormats, isReadOnly: false, requiresProject: true, requiresWriteConfirmation: true);
        context.Add<UpgradeCommandOptions, UpgradeCommandHandler>("upgrade", UpgradeCommandOptions.Parse, CommandGroup.Write, WriteFormats, isReadOnly: false, requiresWriteConfirmation: true);
    }
}
