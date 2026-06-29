namespace AtomUI.Cli.Hosting.Mcp;

public sealed record McpCommandOptions(GlobalCliOptions Global) : IAtomUICliCommandOptions
{
    public static McpCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        return new McpCommandOptions(global);
    }
}
