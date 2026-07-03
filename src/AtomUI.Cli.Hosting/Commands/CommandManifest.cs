using AtomUI.Cli.Hosting.Mcp;
using AtomUI.Cli.Hosting.Metadata;
using AtomUI.Cli.Hosting.ProjectAnalysis;
using AtomUI.Cli.Hosting.Setup;

namespace AtomUI.Cli.Hosting.Commands;

public sealed record CommandManifest(
    string Name,
    Type OwnerModuleType,
    CommandGroup Group,
    IReadOnlySet<OutputFormat> SupportedFormats,
    bool IsReadOnly = true,
    bool RequiresProject = false,
    bool RequiresWriteConfirmation = false,
    IReadOnlyList<Type>? RequiredModuleTypes = null,
    CommandHelp? HelpMetadata = null)
{
    public IReadOnlyList<Type> RequiredModules { get; } = Array.AsReadOnly((RequiredModuleTypes ?? []).ToArray());

    public CommandHelp Help { get; } = HelpMetadata ?? CommandHelp.CreateDefault(Name);
}

public sealed class CommandHelp
{
    public CommandHelp(
        string summary,
        string usage,
        IEnumerable<CommandArgumentHelp>? arguments = null,
        IEnumerable<CommandOptionHelp>? options = null,
        IEnumerable<string>? examples = null,
        int priority = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        ArgumentException.ThrowIfNullOrWhiteSpace(usage);

        Summary = summary;
        Usage = usage;
        Arguments = Array.AsReadOnly((arguments ?? []).ToArray());
        Options = Array.AsReadOnly((options ?? []).ToArray());
        Examples = Array.AsReadOnly((examples ?? []).ToArray());
        Priority = priority;
    }

    public string Summary { get; }

    public string Usage { get; }

    public IReadOnlyList<CommandArgumentHelp> Arguments { get; }

    public IReadOnlyList<CommandOptionHelp> Options { get; }

    public IReadOnlyList<string> Examples { get; }

    public int Priority { get; }

    public static CommandHelp CreateDefault(string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        return new CommandHelp(
            $"{commandName} command.",
            $"dotnet atomui {commandName} [options]",
            examples: [$"dotnet atomui {commandName}"]);
    }
}

public sealed record CommandArgumentHelp(string Name, string Description, bool IsRequired = true);

public sealed record CommandOptionHelp(string Name, string Description, string? ValueName = null);

public sealed class CommandManifestCatalog
{
    private readonly Dictionary<string, CommandManifest> _manifestsByName;

    private CommandManifestCatalog(IReadOnlyList<CommandManifest> manifests)
    {
        Manifests = manifests;
        _manifestsByName = manifests.ToDictionary(
            manifest => manifest.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<CommandManifest> Manifests { get; }

    public static CommandManifestCatalog Create(IEnumerable<CommandManifest> manifests)
    {
        ArgumentNullException.ThrowIfNull(manifests);

        var manifestArray = manifests.ToArray();
        foreach (var manifest in manifestArray)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(manifest.Name);
            ArgumentNullException.ThrowIfNull(manifest.OwnerModuleType);
            ArgumentNullException.ThrowIfNull(manifest.SupportedFormats);
        }

        var duplicate = manifestArray
            .GroupBy(manifest => manifest.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new ArgumentException($"Command '{duplicate.Key}' is registered more than once.", nameof(manifests));
        }

        return new CommandManifestCatalog(Array.AsReadOnly(manifestArray));
    }

    public static CommandManifestCatalog CreateBuiltIn()
    {
        return Create(
        [
            Integration(
                "version",
                typeof(AtomUICliCoreModule),
                TextJson,
                Help("Print the AtomUI Cli version.", "dotnet atomui version", ["dotnet atomui --version", "dotnet atomui version"], 900)),
            Integration(
                "help",
                typeof(AtomUICliCoreModule),
                TextJsonMarkdown,
                Help(
                    "Show help for AtomUI Cli or a specific command.",
                    "dotnet atomui help [command]",
                    ["dotnet atomui help", "dotnet atomui help info"],
                    0,
                    arguments: [new CommandArgumentHelp("command", "Optional command name.", IsRequired: false)])),
            Knowledge(
                "list",
                "List available AtomUI controls, packages, products, or categories.",
                "dotnet atomui list [controls|packages|products|categories] [options]",
                ["dotnet atomui list", "dotnet atomui list packages --format json"],
                10),
            Knowledge(
                "info",
                "Show AtomUI control information.",
                "dotnet atomui info <control> [options]",
                ["dotnet atomui info Button", "dotnet atomui info DataGrid --format json"],
                20,
                arguments: [new CommandArgumentHelp("control", "Control name, for example Button or DataGrid.")],
                options:
                [
                    new CommandOptionHelp("--include", "Include information sections.", "section"),
                    new CommandOptionHelp("--strict", "Disable fuzzy matching.")
                ]),
            Knowledge(
                "doc",
                "Generate usage documentation for a control.",
                "dotnet atomui doc <control> [options]",
                ["dotnet atomui doc Button", "dotnet atomui doc DataGrid --format markdown"],
                30,
                arguments: [new CommandArgumentHelp("control", "Control name.")],
                options:
                [
                    new CommandOptionHelp("--topic", "Limit documentation to a topic.", "topic"),
                    new CommandOptionHelp("--section", "Limit output to a documentation section.", "section")
                ]),
            Knowledge(
                "demo",
                "Browse AtomUI control demos and print demo code.",
                "dotnet atomui demo <control> [demo-key] [options]",
                [
                    "dotnet atomui demo Button",
                    "dotnet atomui demo Button --all",
                    "dotnet atomui demo Button --scenario state",
                    "dotnet atomui demo Button button-loading",
                    "dotnet atomui demo Button button-loading --code-only --code-language xaml",
                    "dotnet atomui demo Button --format json"
                ],
                40,
                arguments:
                [
                    new CommandArgumentHelp("control", "Control name."),
                    new CommandArgumentHelp("demo-key", "Stable demo SourceKey, such as button-loading.", IsRequired: false)
                ],
                options:
                [
                    new CommandOptionHelp("--list", "List matching demos."),
                    new CommandOptionHelp("--all", "Expand all matching demos with code."),
                    new CommandOptionHelp("--scenario", "Filter demos by scenario.", "name"),
                    new CommandOptionHelp("--match", "Search SourceKey, title, or description.", "text"),
                    new CommandOptionHelp("--code-only", "Print code only. Requires demo-key."),
                    new CommandOptionHelp("--code-language", "Code language.", "xaml|csharp|all"),
                    new CommandOptionHelp("--source", "Include source path and snapshot identity."),
                    new CommandOptionHelp("--related", "Include related commands.", "true|false"),
                    new CommandOptionHelp("--strict", "Disable fuzzy suggestions.")
                ]),
            Knowledge(
                "token",
                "Inspect shared and control design tokens, resource keys, dependency chains, and theme usage.",
                "dotnet atomui token [control] [token] [options]",
                [
                    "dotnet atomui token",
                    "dotnet atomui token Button",
                    "dotnet atomui token Button Padding --chain --usage",
                    "dotnet atomui token --kind seed",
                    "dotnet atomui token Button --format markdown"
                ],
                100,
                arguments:
                [
                    new CommandArgumentHelp("control", "Optional control name.", IsRequired: false),
                    new CommandArgumentHelp("token", "Optional token name.", IsRequired: false)
                ],
                options:
                [
                    new CommandOptionHelp("--scope", "Token scope.", "shared|control|all"),
                    new CommandOptionHelp("--kind", "Token kind.", "seed|map|alias|control|resource|all"),
                    new CommandOptionHelp("--category", "Token category.", "color|size|font|motion|radius|shadow|spacing|state|layout|other"),
                    new CommandOptionHelp("--match", "Search token name, description, resource key, or type.", "text"),
                    new CommandOptionHelp("--theme", "Theme algorithm.", "default|dark|compact|all"),
                    new CommandOptionHelp("--include", "Expand token information.", "usage,chain,source,examples,diagnostics"),
                    new CommandOptionHelp("--usage", "Include ControlTheme usage points."),
                    new CommandOptionHelp("--chain", "Include token dependency chain."),
                    new CommandOptionHelp("--source", "Include source and generated resource information."),
                    new CommandOptionHelp("--customizable-only", "Only show public stable customization tokens."),
                    new CommandOptionHelp("--used-only", "Only show tokens consumed by ControlTheme."),
                    new CommandOptionHelp("--strict", "Fail when snapshot diagnostics contain errors.")
                ]),
            Knowledge(
                "semantic",
                "Show semantic DOM information for a control.",
                "dotnet atomui semantic <control> [options]",
                ["dotnet atomui semantic Button", "dotnet atomui semantic DataGrid --include-template"],
                110,
                arguments: [new CommandArgumentHelp("control", "Control name.")],
                options:
                [
                    new CommandOptionHelp("--part", "Semantic part name.", "part"),
                    new CommandOptionHelp("--include-template", "Include template information.")
                ]),
            Knowledge(
                "design.md",
                "Generate design guidance for AtomUI implementation.",
                "dotnet atomui design.md [options]",
                ["dotnet atomui design.md", "dotnet atomui design.md --section tokens", "dotnet atomui design.md --format json"],
                120,
                options:
                [
                    new CommandOptionHelp("--section", "Design guidance section.", "section"),
                    new CommandOptionHelp("--audience", "Target audience.", "developer|agent")
                ]),
            Knowledge(
                "package",
                "Show package or product information.",
                "dotnet atomui package [package-or-product] [options]",
                ["dotnet atomui package", "dotnet atomui package desktop", "dotnet atomui package AtomUI.Desktop.Controls --include dependencies,registration,controls"],
                130,
                arguments: [new CommandArgumentHelp("package-or-product", "Optional package or product id.", IsRequired: false)],
                options:
                [
                    new CommandOptionHelp("--kind", "Limit target resolution.", "package|product|all"),
                    new CommandOptionHelp("--include", "Expand package information.", "dependencies,controls,registration,compatibility,conflicts,replacements,source,diagnostics,all"),
                    new CommandOptionHelp("--tree", "Include package dependency tree."),
                    new CommandOptionHelp("--commercial", "Only list commercial packages."),
                    new CommandOptionHelp("--include-hidden", "Include hidden packages."),
                    new CommandOptionHelp("--strict", "Disable fuzzy matching."),
                    new CommandOptionHelp("--product", "Filter by product.", "product")
                ]),
            Knowledge(
                "changelog",
                "Show AtomUI release notes and control changes.",
                "dotnet atomui changelog [range] [options]",
                ["dotnet atomui changelog", "dotnet atomui changelog 1.0.0..1.1.0 --control Button"],
                140,
                arguments: [new CommandArgumentHelp("range", "Optional version range.", IsRequired: false)],
                options:
                [
                    new CommandOptionHelp("--control", "Filter by control.", "control"),
                    new CommandOptionHelp("--package", "Filter by package.", "package")
                ]),
            Analysis(
                "env",
                "Print AtomUI project environment information.",
                "dotnet atomui env [path]",
                ["dotnet atomui env", "dotnet atomui env ./src/App"],
                200,
                arguments: [new CommandArgumentHelp("path", "Project or solution path.", IsRequired: false)]),
            Analysis(
                "doctor",
                "Diagnose an AtomUI project.",
                "dotnet atomui doctor [path] [options]",
                ["dotnet atomui doctor", "dotnet atomui doctor ./src/App --format json"],
                50,
                arguments: [new CommandArgumentHelp("path", "Project or solution path.", IsRequired: false)],
                options:
                [
                    new CommandOptionHelp("--rule", "Limit diagnosis to one rule.", "rule"),
                    new CommandOptionHelp("--fail-on-warning", "Return diagnostic failure for warnings.")
                ]),
            Analysis(
                "usage",
                "Analyze AtomUI control usage in a project.",
                "dotnet atomui usage [path] [options]",
                ["dotnet atomui usage", "dotnet atomui usage ./src/App --control Button"],
                210,
                arguments: [new CommandArgumentHelp("path", "Project or solution path.", IsRequired: false)],
                options:
                [
                    new CommandOptionHelp("--control", "Filter by control.", "control"),
                    new CommandOptionHelp("--group-by", "Group usage results.", "control|file|package")
                ]),
            Analysis(
                "lint",
                "Check AtomUI project conventions.",
                "dotnet atomui lint [path] [options]",
                ["dotnet atomui lint", "dotnet atomui lint ./src/App --fail-on-warning"],
                220,
                arguments: [new CommandArgumentHelp("path", "Project or solution path.", IsRequired: false)],
                options:
                [
                    new CommandOptionHelp("--rule", "Limit lint to one rule.", "rule"),
                    new CommandOptionHelp("--fail-on-warning", "Return diagnostic failure for warnings.")
                ]),
            Analysis(
                "migrate",
                "Generate migration guidance for an AtomUI project.",
                "dotnet atomui migrate [path] [options]",
                ["dotnet atomui migrate", "dotnet atomui migrate ./src/App --to 2.0.0"],
                230,
                arguments: [new CommandArgumentHelp("path", "Project or solution path.", IsRequired: false)],
                options:
                [
                    new CommandOptionHelp("--from", "Source AtomUI version.", "version"),
                    new CommandOptionHelp("--to", "Target AtomUI version.", "version")
                ]),
            Integration(
                "mcp",
                typeof(AtomUICliMcpModule),
                TextJson,
                Help(
                    "Start the AtomUI MCP server.",
                    "dotnet atomui mcp [options]",
                    ["dotnet atomui mcp", "dotnet atomui mcp --target-version 1.0.0"],
                    300,
                    options:
                    [
                        new CommandOptionHelp("--transport", "MCP transport.", "stdio"),
                        new CommandOptionHelp("--tool-prefix", "Tool name prefix.", "name")
                    ])),
            Write(
                "setup",
                Help(
                    "Configure local AtomUI Cli integration.",
                    "dotnet atomui setup [options]",
                    ["dotnet atomui setup", "dotnet atomui setup --target codex --write"],
                    60,
                    options:
                    [
                        new CommandOptionHelp("--target", "Integration target.", "codex|cursor|vscode|all"),
                        new CommandOptionHelp("--write", "Apply the generated write plan.")
                    ]),
                requiredModuleTypes: []),
            Write(
                "init",
                Help(
                    "Initialize AtomUI project configuration.",
                    "dotnet atomui init [path] [options]",
                    ["dotnet atomui init", "dotnet atomui init ./src/App --write"],
                    400,
                    arguments: [new CommandArgumentHelp("path", "Project path.", IsRequired: false)],
                    options:
                    [
                        new CommandOptionHelp("--write", "Apply the generated write plan."),
                        new CommandOptionHelp("--force", "Overwrite conflicting configuration.")
                    ]),
                requiresProject: true,
                requiredModuleTypes: []),
            Write(
                "add",
                Help(
                    "Add an AtomUI package or product to a project.",
                    "dotnet atomui add <package-or-product> [path] [options]",
                    ["dotnet atomui add datagrid", "dotnet atomui add AtomUI.Desktop.Controls ./src/App --write"],
                    410,
                    arguments:
                    [
                        new CommandArgumentHelp("package-or-product", "Package or product id."),
                        new CommandArgumentHelp("path", "Project path.", IsRequired: false)
                    ],
                    options:
                    [
                        new CommandOptionHelp("--project", "Project file path.", "path"),
                        new CommandOptionHelp("--write", "Apply the generated write plan.")
                    ]),
                requiresProject: true,
                requiredModuleTypes: [typeof(AtomUICliMetadataModule)]),
            Write(
                "upgrade",
                Help(
                    "Upgrade AtomUI Cli or project packages.",
                    "dotnet atomui upgrade [path] [options]",
                    ["dotnet atomui upgrade", "dotnet atomui upgrade ./src/App --to 2.0.0 --write"],
                    420,
                    arguments: [new CommandArgumentHelp("path", "Project path.", IsRequired: false)],
                    options:
                    [
                        new CommandOptionHelp("--to", "Target version.", "version"),
                        new CommandOptionHelp("--write", "Apply the generated write plan.")
                    ]),
                requiredModuleTypes: [typeof(AtomUICliMetadataModule)])
        ]);
    }

    public bool TryFind(string name, out CommandManifest? manifest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _manifestsByName.TryGetValue(name, out manifest);
    }

    private static readonly IReadOnlySet<OutputFormat> TextJson =
        new HashSet<OutputFormat> { OutputFormat.Text, OutputFormat.Json };

    private static readonly IReadOnlySet<OutputFormat> TextJsonMarkdown =
        new HashSet<OutputFormat> { OutputFormat.Text, OutputFormat.Json, OutputFormat.Markdown };

    private static CommandManifest Knowledge(
        string name,
        string summary,
        string usage,
        IReadOnlyList<string> examples,
        int priority,
        IReadOnlyList<CommandArgumentHelp>? arguments = null,
        IReadOnlyList<CommandOptionHelp>? options = null)
    {
        return new CommandManifest(
            name,
            typeof(AtomUICliMetadataModule),
            CommandGroup.Knowledge,
            TextJsonMarkdown,
            HelpMetadata: Help(summary, usage, examples, priority, arguments, options));
    }

    private static CommandManifest Analysis(
        string name,
        string summary,
        string usage,
        IReadOnlyList<string> examples,
        int priority,
        IReadOnlyList<CommandArgumentHelp>? arguments = null,
        IReadOnlyList<CommandOptionHelp>? options = null)
    {
        return new CommandManifest(
            name,
            typeof(AtomUICliProjectAnalysisModule),
            CommandGroup.Analysis,
            TextJsonMarkdown,
            RequiresProject: true,
            RequiredModuleTypes: [typeof(AtomUICliMetadataModule)],
            HelpMetadata: Help(summary, usage, examples, priority, arguments, options));
    }

    private static CommandManifest Integration(
        string name,
        Type ownerModuleType,
        IReadOnlySet<OutputFormat> supportedFormats,
        CommandHelp help)
    {
        return new CommandManifest(
            name,
            ownerModuleType,
            CommandGroup.Integration,
            supportedFormats,
            HelpMetadata: help);
    }

    private static CommandManifest Write(
        string name,
        CommandHelp help,
        bool requiresProject = false,
        IReadOnlyList<Type>? requiredModuleTypes = null)
    {
        return new CommandManifest(
            name,
            typeof(AtomUICliSetupModule),
            CommandGroup.Write,
            TextJsonMarkdown,
            IsReadOnly: false,
            RequiresProject: requiresProject,
            RequiresWriteConfirmation: true,
            RequiredModuleTypes: requiredModuleTypes,
            HelpMetadata: help);
    }

    private static CommandHelp Help(
        string summary,
        string usage,
        IReadOnlyList<string> examples,
        int priority,
        IReadOnlyList<CommandArgumentHelp>? arguments = null,
        IReadOnlyList<CommandOptionHelp>? options = null)
    {
        return new CommandHelp(summary, usage, arguments, options, examples, priority);
    }
}
