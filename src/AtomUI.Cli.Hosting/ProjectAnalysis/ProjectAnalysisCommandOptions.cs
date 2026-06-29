using AtomUI.Cli.Hosting.Commands;

namespace AtomUI.Cli.Hosting.ProjectAnalysis;

public sealed record EnvCommandOptions(GlobalCliOptions Global, string Path) : IAtomUICliCommandOptions
{
    public static EnvCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new EnvCommandOptions(global, reader.Positionals.FirstOrDefault() ?? ".");
    }
}

public sealed record UsageCommandOptions(
    GlobalCliOptions Global,
    string Path,
    string? Control,
    string GroupBy,
    bool IncludeLocations,
    bool IncludeUnusedPackages,
    bool IncludeCSharp,
    bool IncludeXaml) : IAtomUICliCommandOptions
{
    public static UsageCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new UsageCommandOptions(
            global,
            reader.Positionals.FirstOrDefault() ?? ".",
            reader.GetOption("control"),
            reader.GetOption("group-by", "control")!,
            reader.HasFlag("include-locations"),
            reader.HasFlag("include-unused-packages"),
            reader.GetBool("include-csharp", true),
            reader.GetBool("include-xaml", true));
    }
}

public sealed record DiagnosticCommandOptions(
    GlobalCliOptions Global,
    string Path,
    string? Rule,
    bool FailOnWarning) : IAtomUICliCommandOptions
{
    public static DiagnosticCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new DiagnosticCommandOptions(
            global,
            reader.Positionals.FirstOrDefault() ?? ".",
            reader.GetOption("rule"),
            reader.HasFlag("fail-on-warning"));
    }
}

public sealed record MigrateCommandOptions(
    GlobalCliOptions Global,
    string Path,
    string? From,
    string? To,
    bool IncludeWritePlan) : IAtomUICliCommandOptions
{
    public static MigrateCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new MigrateCommandOptions(
            global,
            reader.Positionals.FirstOrDefault() ?? ".",
            reader.GetOption("from"),
            reader.GetOption("to") ?? global.TargetVersion,
            reader.HasFlag("include-write-plan"));
    }
}
