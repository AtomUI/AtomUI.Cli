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
    IReadOnlyList<Type>? RequiredModuleTypes = null)
{
    public IReadOnlyList<Type> RequiredModules { get; } = Array.AsReadOnly((RequiredModuleTypes ?? []).ToArray());
}

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
            Integration("version", typeof(AtomUICliCoreModule), TextJson),
            Integration("help", typeof(AtomUICliCoreModule), TextJsonMarkdown),
            Knowledge("list"),
            Knowledge("info"),
            Knowledge("doc"),
            Knowledge("demo"),
            Knowledge("token"),
            Knowledge("semantic"),
            Knowledge("design.md"),
            Knowledge("package"),
            Knowledge("changelog"),
            Analysis("env"),
            Analysis("doctor"),
            Analysis("usage"),
            Analysis("lint"),
            Analysis("migrate"),
            Integration("mcp", typeof(AtomUICliMcpModule), TextJson),
            Write("setup", requiredModuleTypes: []),
            Write("init", requiresProject: true, requiredModuleTypes: []),
            Write("add", requiresProject: true, requiredModuleTypes: [typeof(AtomUICliMetadataModule)]),
            Write("upgrade", requiredModuleTypes: [typeof(AtomUICliMetadataModule)])
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

    private static CommandManifest Knowledge(string name)
    {
        return new CommandManifest(
            name,
            typeof(AtomUICliMetadataModule),
            CommandGroup.Knowledge,
            TextJsonMarkdown);
    }

    private static CommandManifest Analysis(string name)
    {
        return new CommandManifest(
            name,
            typeof(AtomUICliProjectAnalysisModule),
            CommandGroup.Analysis,
            TextJsonMarkdown,
            RequiresProject: true,
            RequiredModuleTypes: [typeof(AtomUICliMetadataModule)]);
    }

    private static CommandManifest Integration(string name, Type ownerModuleType, IReadOnlySet<OutputFormat> supportedFormats)
    {
        return new CommandManifest(
            name,
            ownerModuleType,
            CommandGroup.Integration,
            supportedFormats);
    }

    private static CommandManifest Write(string name, bool requiresProject = false, IReadOnlyList<Type>? requiredModuleTypes = null)
    {
        return new CommandManifest(
            name,
            typeof(AtomUICliSetupModule),
            CommandGroup.Write,
            TextJsonMarkdown,
            IsReadOnly: false,
            RequiresProject: requiresProject,
            RequiresWriteConfirmation: true,
            RequiredModuleTypes: requiredModuleTypes);
    }
}
