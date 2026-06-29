using AtomUI.Cli.Hosting.Commands;

namespace AtomUI.Cli.Hosting.Metadata;

public enum ListKind
{
    Controls,
    Products,
    Packages,
    Categories,
    All
}

public sealed record ListCommandOptions(
    GlobalCliOptions Global,
    ListKind Kind,
    string? Category,
    string? Since,
    bool IncludeHidden) : IAtomUICliCommandOptions
{
    public static ListCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        var kindText = reader.GetOption("kind") ?? reader.Positionals.FirstOrDefault() ?? "controls";
        var kind = Enum.TryParse<ListKind>(kindText, ignoreCase: true, out var parsed) ? parsed : ListKind.Controls;
        return new ListCommandOptions(global, kind, reader.GetOption("category"), reader.GetOption("since"), reader.HasFlag("include-hidden"));
    }
}

public sealed record InfoCommandOptions(GlobalCliOptions Global, string? Control, string? Include, bool Strict) : IAtomUICliCommandOptions
{
    public static InfoCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new InfoCommandOptions(global, reader.Positionals.FirstOrDefault(), reader.GetOption("include"), reader.HasFlag("strict"));
    }
}

public sealed record DocCommandOptions(GlobalCliOptions Global, string? Target, string? Topic, string? Section, bool Strict, string Style) : IAtomUICliCommandOptions
{
    public static DocCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new DocCommandOptions(global, reader.Positionals.FirstOrDefault(), reader.GetOption("topic"), reader.GetOption("section"), reader.HasFlag("strict"), reader.GetOption("style", "full")!);
    }
}

public sealed record DemoCommandOptions(GlobalCliOptions Global, string? Control, string? DemoName, bool ListOnly, bool CodeOnly, string Language) : IAtomUICliCommandOptions
{
    public static DemoCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new DemoCommandOptions(global, reader.Positionals.ElementAtOrDefault(0), reader.Positionals.ElementAtOrDefault(1), reader.HasFlag("list"), reader.HasFlag("code-only"), reader.GetOption("language", "all")!);
    }
}

public sealed record TokenCommandOptions(GlobalCliOptions Global, string? Control, string Scope, string? Name, string? Match, bool IncludeInherited) : IAtomUICliCommandOptions
{
    public static TokenCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        var control = reader.Positionals.FirstOrDefault();
        return new TokenCommandOptions(global, control, reader.GetOption("scope", control is null ? "global" : "control")!, reader.GetOption("name"), reader.GetOption("match"), reader.GetBool("include-inherited", true));
    }
}

public sealed record SemanticCommandOptions(GlobalCliOptions Global, string? Control, string? Part, bool IncludeTemplate, bool Strict) : IAtomUICliCommandOptions
{
    public static SemanticCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new SemanticCommandOptions(global, reader.Positionals.FirstOrDefault(), reader.GetOption("part"), reader.HasFlag("include-template"), reader.HasFlag("strict"));
    }
}

public sealed record DesignCommandOptions(GlobalCliOptions Global, string Section, string Audience) : IAtomUICliCommandOptions
{
    public static DesignCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new DesignCommandOptions(global, reader.GetOption("section", "all")!, reader.GetOption("audience", "developer")!);
    }
}

public sealed record PackageCommandOptions(GlobalCliOptions Global, string? PackageOrProduct, bool Strict, bool IncludeCompatibility) : IAtomUICliCommandOptions
{
    public static PackageCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new PackageCommandOptions(global, reader.Positionals.FirstOrDefault(), reader.HasFlag("strict"), reader.GetBool("include-compatibility", true));
    }
}

public sealed record ChangelogCommandOptions(GlobalCliOptions Global, string? Range, string? Control, string? PackageId, string Severity) : IAtomUICliCommandOptions
{
    public static ChangelogCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        return new ChangelogCommandOptions(global, reader.Positionals.ElementAtOrDefault(0), reader.GetOption("control") ?? reader.Positionals.ElementAtOrDefault(1), reader.GetOption("package"), reader.GetOption("severity", "all")!);
    }
}
