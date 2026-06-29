using AtomUI.Cli;

namespace AtomUI.Cli.Hosting.Commands.BuiltIn;

public sealed record HelpCommandOptions(GlobalCliOptions Global, string? CommandName) : IAtomUICliCommandOptions
{
    public static HelpCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new HelpCommandOptions(global, reader.Positionals.FirstOrDefault());
    }
}
