using AtomUI.Cli;
using System.Text;

namespace AtomUI.Cli.Hosting.Commands.BuiltIn;

public sealed class HelpCommandHandler(CommandManifestCatalog commandManifests) : IAtomUICliCommandHandler<HelpCommandOptions>
{
    private static readonly string[] CommonCommandNames = ["list", "info", "doc", "demo", "doctor", "setup"];

    private static readonly CommandOptionHelp[] GlobalOptions =
    [
        new CommandOptionHelp("--format", "Output format.", "text|json|markdown"),
        new CommandOptionHelp("--lang", "Output language.", "zh|en"),
        new CommandOptionHelp("--target-version", "AtomUI target version.", "version"),
        new CommandOptionHelp("--data-root", "Additional metadata data root.", "path"),
        new CommandOptionHelp("--detail", "Show more fields."),
        new CommandOptionHelp("--no-update-check", "Skip update check.")
    ];

    public ValueTask<AtomUICliResult> ExecuteAsync(
        HelpCommandOptions options,
        CliInvocationContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(options.CommandName))
        {
            return ValueTask.FromResult(CreateCommandHelpResult(options));
        }

        return ValueTask.FromResult(AtomUICliResult.Success(
            options.Global.Format == OutputFormat.Json
                ? CreateHomeJsonPayload()
                : RenderHome(options.Global.Format)));
    }

    private AtomUICliResult CreateCommandHelpResult(HelpCommandOptions options)
    {
        if (!commandManifests.TryFind(options.CommandName!, out var manifest) || manifest is null)
        {
            var suggestions = FindSuggestions(options.CommandName!);
            return AtomUICliResult.Failure(new AtomUICliError(
                AtomUICliErrorCodes.ArgumentCommandNotFound,
                AtomUICliSeverity.Error,
                $"Command '{options.CommandName}' is not registered.",
                CreateSuggestionText(suggestions),
                "parse",
                null,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["command"] = options.CommandName!,
                    ["suggestions"] = string.Join(",", suggestions)
                }));
        }

        return AtomUICliResult.Success(
            options.Global.Format == OutputFormat.Json
                ? CreateCommandJsonPayload(manifest)
                : RenderCommand(manifest, options.Global.Format));
    }

    private string RenderHome(OutputFormat format)
    {
        if (format == OutputFormat.Markdown)
        {
            return RenderHomeMarkdown();
        }

        var builder = new StringBuilder();
        builder.AppendLine("AtomUI Cli");
        builder.AppendLine($"Version: {CliProductInfo.Current.Version}");
        builder.AppendLine(CliProductInfo.Current.Copyright);
        builder.AppendLine();
        builder.AppendLine("Usage:");
        builder.AppendLine("  dotnet atomui <command> [options]");
        builder.AppendLine();
        builder.AppendLine("Common commands:");
        foreach (var manifest in GetCommonCommands())
        {
            AppendCommandSummary(builder, manifest);
        }

        builder.AppendLine();
        builder.AppendLine("Command groups:");
        foreach (var group in GetGroups())
        {
            builder.AppendLine($"  {GetGroupLabel(group.Key)}:");
            foreach (var manifest in group)
            {
                AppendCommandSummary(builder, manifest, indent: 4);
            }
        }

        builder.AppendLine();
        builder.AppendLine("Global options:");
        foreach (var option in GlobalOptions)
        {
            AppendOption(builder, option);
        }

        builder.AppendLine();
        builder.AppendLine("More:");
        builder.AppendLine("  dotnet atomui help <command>");
        builder.AppendLine("  dotnet atomui <command> --help");
        return builder.ToString().TrimEnd();
    }

    private string RenderHomeMarkdown()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# AtomUI Cli");
        builder.AppendLine();
        builder.AppendLine($"Version: {CliProductInfo.Current.Version}");
        builder.AppendLine();
        builder.AppendLine(CliProductInfo.Current.Copyright);
        builder.AppendLine();
        builder.AppendLine("## Usage");
        builder.AppendLine();
        builder.AppendLine("```bash");
        builder.AppendLine("dotnet atomui <command> [options]");
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## Common commands");
        foreach (var manifest in GetCommonCommands())
        {
            builder.AppendLine($"- `{GetDisplayUsage(manifest)}` - {manifest.Help.Summary}");
        }

        builder.AppendLine();
        builder.AppendLine("## Command groups");
        foreach (var group in GetGroups())
        {
            builder.AppendLine();
            builder.AppendLine($"### {GetGroupLabel(group.Key)}");
            foreach (var manifest in group)
            {
                builder.AppendLine($"- `{GetDisplayUsage(manifest)}` - {manifest.Help.Summary}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Global options");
        foreach (var option in GlobalOptions)
        {
            builder.AppendLine($"- `{FormatOptionName(option)}` - {option.Description}");
        }

        builder.AppendLine();
        builder.AppendLine("## More");
        builder.AppendLine();
        builder.AppendLine("```bash");
        builder.AppendLine("dotnet atomui help <command>");
        builder.AppendLine("dotnet atomui <command> --help");
        builder.AppendLine("```");
        return builder.ToString().TrimEnd();
    }

    private string RenderCommand(CommandManifest manifest, OutputFormat format)
    {
        if (format == OutputFormat.Markdown)
        {
            return RenderCommandMarkdown(manifest);
        }

        var builder = new StringBuilder();
        builder.AppendLine(manifest.Name);
        builder.AppendLine();
        builder.AppendLine(manifest.Help.Summary);
        builder.AppendLine();
        builder.AppendLine("Usage:");
        builder.AppendLine($"  {manifest.Help.Usage}");
        AppendArguments(builder, manifest);
        AppendOptions(builder, manifest);
        AppendExamples(builder, manifest);
        builder.AppendLine();
        builder.AppendLine("Supported formats:");
        builder.AppendLine($"  {string.Join(", ", manifest.SupportedFormats.Select(FormatOutputFormat))}");
        builder.AppendLine();
        builder.AppendLine("Requires project:");
        builder.AppendLine($"  {FormatBoolean(manifest.RequiresProject)}");
        builder.AppendLine();
        builder.AppendLine("Write confirmation:");
        builder.AppendLine($"  {FormatBoolean(manifest.RequiresWriteConfirmation)}");
        return builder.ToString().TrimEnd();
    }

    private static string RenderCommandMarkdown(CommandManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {manifest.Name}");
        builder.AppendLine();
        builder.AppendLine(manifest.Help.Summary);
        builder.AppendLine();
        builder.AppendLine("## Usage");
        builder.AppendLine();
        builder.AppendLine("```bash");
        builder.AppendLine(manifest.Help.Usage);
        builder.AppendLine("```");
        if (manifest.Help.Arguments.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Arguments");
            foreach (var argument in manifest.Help.Arguments)
            {
                builder.AppendLine($"- `{argument.Name}` - {argument.Description}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Options");
        foreach (var option in GetCommandOptions(manifest))
        {
            builder.AppendLine($"- `{FormatOptionName(option)}` - {option.Description}");
        }

        builder.AppendLine();
        builder.AppendLine("## Examples");
        builder.AppendLine();
        builder.AppendLine("```bash");
        foreach (var example in manifest.Help.Examples)
        {
            builder.AppendLine(example);
        }
        builder.AppendLine("```");
        return builder.ToString().TrimEnd();
    }

    private static void AppendArguments(StringBuilder builder, CommandManifest manifest)
    {
        if (manifest.Help.Arguments.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Arguments:");
        foreach (var argument in manifest.Help.Arguments)
        {
            builder.AppendLine($"  {argument.Name.PadRight(18)} {argument.Description}");
        }
    }

    private static void AppendOptions(StringBuilder builder, CommandManifest manifest)
    {
        builder.AppendLine();
        builder.AppendLine("Options:");
        foreach (var option in GetCommandOptions(manifest))
        {
            AppendOption(builder, option);
        }
    }

    private static void AppendExamples(StringBuilder builder, CommandManifest manifest)
    {
        if (manifest.Help.Examples.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Examples:");
        foreach (var example in manifest.Help.Examples)
        {
            builder.AppendLine($"  {example}");
        }
    }

    private object CreateHomeJsonPayload()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["version"] = CliProductInfo.Current.Version,
            ["copyright"] = CliProductInfo.Current.Copyright,
            ["usage"] = "dotnet atomui <command> [options]",
            ["commonCommands"] = GetCommonCommands().Select(ToCommandSummaryJson).ToArray(),
            ["groups"] = GetGroups()
                .Select(group => new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["name"] = GetGroupLabel(group.Key),
                    ["commands"] = group.Select(ToCommandSummaryJson).ToArray()
                })
                .ToArray(),
            ["globalOptions"] = GlobalOptions.Select(ToOptionJson).ToArray(),
            ["more"] = new[] { "dotnet atomui help <command>", "dotnet atomui <command> --help" }
        };
    }

    private static object CreateCommandJsonPayload(CommandManifest manifest)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = manifest.Name,
            ["summary"] = manifest.Help.Summary,
            ["usage"] = manifest.Help.Usage,
            ["arguments"] = manifest.Help.Arguments.Select(ToArgumentJson).ToArray(),
            ["options"] = GetCommandOptions(manifest).Select(ToOptionJson).ToArray(),
            ["examples"] = manifest.Help.Examples.ToArray(),
            ["supportedFormats"] = manifest.SupportedFormats.Select(FormatOutputFormat).ToArray(),
            ["group"] = manifest.Group.ToString(),
            ["ownerModule"] = manifest.OwnerModuleType.Name,
            ["requiresProject"] = manifest.RequiresProject,
            ["requiresWriteConfirmation"] = manifest.RequiresWriteConfirmation
        };
    }

    private IEnumerable<CommandManifest> GetCommonCommands()
    {
        foreach (var name in CommonCommandNames)
        {
            if (commandManifests.TryFind(name, out var manifest) && manifest is not null)
            {
                yield return manifest;
            }
        }
    }

    private IEnumerable<IGrouping<CommandGroup, CommandManifest>> GetGroups()
    {
        return commandManifests.Manifests
            .OrderBy(manifest => GetGroupOrder(manifest.Group))
            .ThenBy(manifest => manifest.Help.Priority)
            .ThenBy(manifest => manifest.Name, StringComparer.OrdinalIgnoreCase)
            .GroupBy(manifest => manifest.Group)
            .OrderBy(group => GetGroupOrder(group.Key));
    }

    private IReadOnlyList<string> FindSuggestions(string commandName)
    {
        return commandManifests.Manifests
            .Select(manifest => new
            {
                manifest.Name,
                Distance = GetEditDistance(commandName, manifest.Name)
            })
            .Where(candidate => candidate.Distance <= 3
                                || candidate.Name.StartsWith(commandName, StringComparison.OrdinalIgnoreCase)
                                || candidate.Name.Contains(commandName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(candidate => candidate.Name)
            .ToArray();
    }

    private static string? CreateSuggestionText(IReadOnlyList<string> suggestions)
    {
        return suggestions.Count == 0
            ? "Run `dotnet atomui help` to list available commands."
            : $"Run `dotnet atomui help {suggestions[0]}`.";
    }

    private static void AppendCommandSummary(StringBuilder builder, CommandManifest manifest, int indent = 2)
    {
        builder.Append(' ', indent);
        builder.Append(GetDisplayUsage(manifest).PadRight(34));
        builder.Append(' ');
        builder.AppendLine(manifest.Help.Summary);
    }

    private static void AppendOption(StringBuilder builder, CommandOptionHelp option)
    {
        builder.Append("  ");
        builder.Append(FormatOptionName(option).PadRight(32));
        builder.Append(' ');
        builder.AppendLine(option.Description);
    }

    private static IReadOnlyList<CommandOptionHelp> GetCommandOptions(CommandManifest manifest)
    {
        return manifest.Help.Options
            .Concat([new CommandOptionHelp("--format", $"Output format: {string.Join(", ", manifest.SupportedFormats.Select(FormatOutputFormat))}.", "format")])
            .ToArray();
    }

    private static string GetDisplayUsage(CommandManifest manifest)
    {
        const string prefix = "dotnet atomui ";
        var usage = manifest.Help.Usage.StartsWith(prefix, StringComparison.Ordinal)
            ? manifest.Help.Usage[prefix.Length..]
            : manifest.Name;
        usage = usage.Replace(" [options]", string.Empty, StringComparison.Ordinal);
        return usage.Length > 32 ? manifest.Name : usage;
    }

    private static string FormatOptionName(CommandOptionHelp option)
    {
        return string.IsNullOrWhiteSpace(option.ValueName)
            ? option.Name
            : $"{option.Name} <{option.ValueName}>";
    }

    private static IReadOnlyDictionary<string, object?> ToCommandSummaryJson(CommandManifest manifest)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = manifest.Name,
            ["usage"] = GetDisplayUsage(manifest),
            ["summary"] = manifest.Help.Summary,
            ["group"] = manifest.Group.ToString()
        };
    }

    private static IReadOnlyDictionary<string, object?> ToArgumentJson(CommandArgumentHelp argument)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = argument.Name,
            ["description"] = argument.Description,
            ["isRequired"] = argument.IsRequired
        };
    }

    private static IReadOnlyDictionary<string, object?> ToOptionJson(CommandOptionHelp option)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = option.Name,
            ["valueName"] = option.ValueName,
            ["description"] = option.Description
        };
    }

    private static string GetGroupLabel(CommandGroup group)
    {
        return group switch
        {
            CommandGroup.Knowledge => "Knowledge",
            CommandGroup.Analysis => "Project analysis",
            CommandGroup.Write => "Setup",
            CommandGroup.Integration => "Integration",
            _ => group.ToString()
        };
    }

    private static int GetGroupOrder(CommandGroup group)
    {
        return group switch
        {
            CommandGroup.Knowledge => 0,
            CommandGroup.Analysis => 1,
            CommandGroup.Write => 2,
            CommandGroup.Integration => 3,
            _ => 100
        };
    }

    private static string FormatOutputFormat(OutputFormat format)
    {
        return format.ToString().ToLowerInvariant();
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "yes" : "no";
    }

    private static int GetEditDistance(string left, string right)
    {
        var distances = new int[left.Length + 1, right.Length + 1];
        for (var i = 0; i <= left.Length; i++)
        {
            distances[i, 0] = i;
        }

        for (var j = 0; j <= right.Length; j++)
        {
            distances[0, j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = char.ToLowerInvariant(left[i - 1]) == char.ToLowerInvariant(right[j - 1]) ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[left.Length, right.Length];
    }
}
