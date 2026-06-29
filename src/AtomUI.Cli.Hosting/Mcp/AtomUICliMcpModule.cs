using AtomUI.Cli.Modularity;
using AtomUI.Modularity;

namespace AtomUI.Cli.Hosting.Mcp;

[Module(DisplayName = "AtomUI Cli MCP")]
public sealed partial class AtomUICliMcpModule : AtomUICliModule
{
    private static readonly HashSet<OutputFormat> McpFormats =
    [
        OutputFormat.Text,
        OutputFormat.Json
    ];

    public override void ConfigureServices(ModuleServiceConfigurationContext context)
    {
        context.Services.AddSingleton(McpToolCatalog.CreateDefault());
        context.Services.AddTransient<McpCommandHandler, McpCommandHandler>();
    }

    public override void ConfigureAtomUICliCommands(AtomUICliCommandContributionContext context)
    {
        context.Add<McpCommandOptions, McpCommandHandler>("mcp", McpCommandOptions.Parse, CommandGroup.Integration, McpFormats);
    }
}
