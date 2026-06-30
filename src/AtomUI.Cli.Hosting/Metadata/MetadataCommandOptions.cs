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
    string? PackageId,
    string? Since,
    bool IncludeHidden) : IAtomUICliCommandOptions
{
    public static ListCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        var kindText = reader.GetOption("kind") ?? reader.Positionals.FirstOrDefault() ?? "controls";
        var kind = Enum.TryParse<ListKind>(kindText, ignoreCase: true, out var parsed) ? parsed : ListKind.Controls;
        return new ListCommandOptions(global, kind, reader.GetOption("category"), reader.GetOption("package"), reader.GetOption("since"), reader.HasFlag("include-hidden"));
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

public enum DocumentSection
{
    All,
    Overview,
    Install,
    Usage,
    Scenarios,
    Examples,
    Api,
    Properties,
    Methods,
    Events,
    Logic,
    Theme,
    Tokens,
    Semantic,
    Demos,
    Changelog,
    Source
}

public enum DocumentStyle
{
    Full,
    Summary,
    Agent
}

public enum DocumentExamplesMode
{
    Recommended,
    All,
    Basic,
    State,
    Theme,
    Integration,
    Advanced
}

public sealed record DocCommandOptions(
    GlobalCliOptions Global,
    string? Target,
    string? Topic,
    DocumentSection Section,
    string? InvalidSection,
    DocumentStyle Style,
    string? InvalidStyle,
    DocumentExamplesMode Examples,
    string? InvalidExamples,
    string? ExampleKey,
    bool Strict,
    bool FormatSpecified) : IAtomUICliCommandOptions
{
    public static DocCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        var sectionText = reader.GetOption("section", "all")!;
        var sectionParsed = TryParseSection(sectionText, out var section);
        var styleText = reader.GetOption("style", "full")!;
        var styleParsed = TryParseStyle(styleText, out var style);
        var examplesText = reader.GetOption("examples", "recommended")!;
        var examplesParsed = TryParseExamplesMode(examplesText, out var examples);
        return new DocCommandOptions(
            global,
            reader.Positionals.FirstOrDefault(),
            reader.GetOption("topic"),
            section,
            sectionParsed ? null : sectionText,
            style,
            styleParsed ? null : styleText,
            examples,
            examplesParsed ? null : examplesText,
            reader.GetOption("example"),
            reader.HasFlag("strict"),
            global.FormatSpecified);
    }

    private static bool TryParseSection(string value, out DocumentSection section)
    {
        section = value.ToLowerInvariant() switch
        {
            "all" => DocumentSection.All,
            "overview" => DocumentSection.Overview,
            "install" => DocumentSection.Install,
            "usage" => DocumentSection.Usage,
            "scenarios" => DocumentSection.Scenarios,
            "examples" => DocumentSection.Examples,
            "api" => DocumentSection.Api,
            "properties" => DocumentSection.Properties,
            "methods" => DocumentSection.Methods,
            "events" => DocumentSection.Events,
            "logic" => DocumentSection.Logic,
            "theme" => DocumentSection.Theme,
            "tokens" => DocumentSection.Tokens,
            "semantic" => DocumentSection.Semantic,
            "demos" => DocumentSection.Demos,
            "changelog" => DocumentSection.Changelog,
            "source" => DocumentSection.Source,
            _ => DocumentSection.All
        };

        return section != DocumentSection.All || value.Equals("all", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseStyle(string value, out DocumentStyle style)
    {
        style = value.ToLowerInvariant() switch
        {
            "full" => DocumentStyle.Full,
            "summary" => DocumentStyle.Summary,
            "agent" => DocumentStyle.Agent,
            _ => DocumentStyle.Full
        };

        return style != DocumentStyle.Full || value.Equals("full", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseExamplesMode(string value, out DocumentExamplesMode examples)
    {
        examples = value.ToLowerInvariant() switch
        {
            "recommended" => DocumentExamplesMode.Recommended,
            "all" => DocumentExamplesMode.All,
            "basic" => DocumentExamplesMode.Basic,
            "state" => DocumentExamplesMode.State,
            "theme" => DocumentExamplesMode.Theme,
            "integration" => DocumentExamplesMode.Integration,
            "advanced" => DocumentExamplesMode.Advanced,
            _ => DocumentExamplesMode.Recommended
        };

        return examples != DocumentExamplesMode.Recommended || value.Equals("recommended", StringComparison.OrdinalIgnoreCase);
    }
}

public enum DemoCommandMode
{
    List,
    Detail,
    Code
}

public enum DemoCodeLanguage
{
    All,
    Xaml,
    CSharp
}

public sealed record DemoCommandOptions(
    GlobalCliOptions Global,
    string? Control,
    string? DemoKey,
    string? Scenario,
    string? Match,
    bool ListOnly,
    bool ExpandAll,
    bool CodeOnly,
    DemoCodeLanguage CodeLanguage,
    string? InvalidCodeLanguage,
    bool RemovedLanguageOption,
    bool IncludeSource,
    bool IncludeRelated,
    bool Strict) : IAtomUICliCommandOptions
{
    public DemoCommandMode Mode
    {
        get
        {
            if (CodeOnly)
            {
                return DemoCommandMode.Code;
            }

            return ExpandAll || ListOnly || string.IsNullOrWhiteSpace(DemoKey)
                ? DemoCommandMode.List
                : DemoCommandMode.Detail;
        }
    }

    public static DemoCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        var codeLanguageText = reader.GetOption("code-language", "all")!;
        var codeLanguageParsed = TryParseCodeLanguage(codeLanguageText, out var codeLanguage);
        return new DemoCommandOptions(
            global,
            reader.Positionals.ElementAtOrDefault(0),
            reader.Positionals.ElementAtOrDefault(1),
            reader.GetOption("scenario"),
            reader.GetOption("match"),
            reader.HasFlag("list"),
            reader.HasFlag("all"),
            reader.HasFlag("code-only"),
            codeLanguage,
            codeLanguageParsed ? null : codeLanguageText,
            reader.HasFlag("language"),
            reader.HasFlag("source"),
            reader.GetBool("related", true) && !reader.HasFlag("no-related"),
            reader.HasFlag("strict"));
    }

    private static bool TryParseCodeLanguage(string value, out DemoCodeLanguage language)
    {
        language = value.ToLowerInvariant() switch
        {
            "all" => DemoCodeLanguage.All,
            "xaml" => DemoCodeLanguage.Xaml,
            "csharp" => DemoCodeLanguage.CSharp,
            _ => DemoCodeLanguage.All
        };

        return language != DemoCodeLanguage.All || value.Equals("all", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record TokenCommandOptions(
    GlobalCliOptions Global,
    string? Control,
    string? Token,
    string Scope,
    string Kind,
    string? Category,
    string? Match,
    string? Product,
    string Theme,
    string? Include,
    bool Usage,
    bool Chain,
    bool Source,
    bool CustomizableOnly,
    bool UsedOnly,
    bool Strict) : IAtomUICliCommandOptions
{
    public static TokenCommandOptions Parse(GlobalCliOptions global, IReadOnlyList<string> args)
    {
        var reader = new CommandOptionsReader(args);
        var control = reader.Positionals.ElementAtOrDefault(0);
        var token = reader.Positionals.ElementAtOrDefault(1);
        return new TokenCommandOptions(
            global,
            control,
            token,
            reader.GetOption("scope", control is null ? "shared" : "control")!,
            reader.GetOption("kind", "all")!,
            reader.GetOption("category"),
            reader.GetOption("match"),
            reader.GetOption("product"),
            reader.GetOption("theme", "default")!,
            reader.GetOption("include"),
            reader.HasFlag("usage"),
            reader.HasFlag("chain"),
            reader.HasFlag("source"),
            reader.HasFlag("customizable-only"),
            reader.HasFlag("used-only"),
            reader.HasFlag("strict"));
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
