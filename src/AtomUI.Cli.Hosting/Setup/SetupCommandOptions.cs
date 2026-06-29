using AtomUI.Cli.Hosting.Commands;

namespace AtomUI.Cli.Hosting.Setup;

public sealed record SetupCommandOptions(
    GlobalCliOptions Global,
    string Target,
    string Scope,
    string Workspace,
    bool Write,
    bool Force) : IAtomUICliCommandOptions
{
    public static SetupCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new SetupCommandOptions(
            global,
            reader.GetOption("target") ?? reader.GetOption("client") ?? "all",
            reader.GetOption("scope", "user")!,
            reader.GetOption("workspace", ".")!,
            reader.HasFlag("write"),
            reader.HasFlag("force"));
    }
}

public sealed record InitCommandOptions(
    GlobalCliOptions Global,
    string Path,
    bool Write,
    bool Force) : IAtomUICliCommandOptions
{
    public static InitCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new InitCommandOptions(global, reader.Positionals.FirstOrDefault() ?? ".", reader.HasFlag("write"), reader.HasFlag("force"));
    }
}

public sealed record AddCommandOptions(
    GlobalCliOptions Global,
    string? PackageOrProduct,
    string Path,
    string? Project,
    string? Version,
    bool IncludeRegistration,
    bool Write,
    bool Force) : IAtomUICliCommandOptions
{
    public static AddCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new AddCommandOptions(
            global,
            reader.Positionals.FirstOrDefault(),
            reader.Positionals.ElementAtOrDefault(1) ?? ".",
            reader.GetOption("project"),
            reader.GetOption("version"),
            reader.GetBool("include-registration", true),
            reader.HasFlag("write"),
            reader.HasFlag("force"));
    }
}

public sealed record UpgradeCommandOptions(
    GlobalCliOptions Global,
    string Path,
    string? To,
    bool CliOnly,
    bool Packages,
    bool Write,
    bool Force) : IAtomUICliCommandOptions
{
    public static UpgradeCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new UpgradeCommandOptions(
            global,
            reader.Positionals.FirstOrDefault() ?? ".",
            reader.GetOption("to") ?? global.TargetVersion,
            reader.HasFlag("cli"),
            reader.GetBool("packages", true),
            reader.HasFlag("write"),
            reader.HasFlag("force"));
    }
}
