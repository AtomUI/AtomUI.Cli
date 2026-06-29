using AtomUI.Cli.Modularity;
using AtomUI.Modularity;

namespace AtomUI.Cli.Hosting.ProjectAnalysis;

[Module(DisplayName = "AtomUI Cli Project Analysis")]
public sealed partial class AtomUICliProjectAnalysisModule : AtomUICliModule
{
    private static readonly HashSet<OutputFormat> AnalysisFormats =
    [
        OutputFormat.Text,
        OutputFormat.Json,
        OutputFormat.Markdown
    ];

    public override void ConfigureServices(ModuleServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ProjectAnalysisService, ProjectAnalysisService>();
        context.Services.AddTransient<EnvCommandHandler, EnvCommandHandler>();
        context.Services.AddTransient<UsageCommandHandler, UsageCommandHandler>();
        context.Services.AddTransient<DoctorCommandHandler, DoctorCommandHandler>();
        context.Services.AddTransient<LintCommandHandler, LintCommandHandler>();
        context.Services.AddTransient<MigrateCommandHandler, MigrateCommandHandler>();
    }

    public override void ConfigureAtomUICliCommands(AtomUICliCommandContributionContext context)
    {
        context.Add<EnvCommandOptions, EnvCommandHandler>("env", EnvCommandOptions.Parse, CommandGroup.Analysis, AnalysisFormats, requiresProject: true);
        context.Add<DiagnosticCommandOptions, DoctorCommandHandler>("doctor", DiagnosticCommandOptions.Parse, CommandGroup.Analysis, AnalysisFormats, requiresProject: true);
        context.Add<UsageCommandOptions, UsageCommandHandler>("usage", UsageCommandOptions.Parse, CommandGroup.Analysis, AnalysisFormats, requiresProject: true);
        context.Add<DiagnosticCommandOptions, LintCommandHandler>("lint", DiagnosticCommandOptions.Parse, CommandGroup.Analysis, AnalysisFormats, requiresProject: true);
        context.Add<MigrateCommandOptions, MigrateCommandHandler>("migrate", MigrateCommandOptions.Parse, CommandGroup.Analysis, AnalysisFormats, requiresProject: true);
    }
}
