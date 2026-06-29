namespace AtomUI.Cli.Hosting.Commands;

public sealed class CommandPreParser
{
    public CommandPreParseResult Parse(IReadOnlyList<string> args, CommandManifestCatalog manifestCatalog)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(manifestCatalog);

        var commandName = GetCommandName(args);
        if (commandName is null)
        {
            commandName = "help";
        }

        if (!manifestCatalog.TryFind(commandName, out var manifest) || manifest is null)
        {
            return CommandPreParseResult.Failure(CreateUnknownCommandError(commandName));
        }

        var formatResult = TryReadFormat(args, out var format);
        if (!formatResult)
        {
            return CommandPreParseResult.Failure(CreateArgumentError("Invalid --format value."));
        }

        if (format is not null && !manifest.SupportedFormats.Contains(format.Value))
        {
            return CommandPreParseResult.Failure(CreateArgumentError($"Command '{commandName}' does not support '{format}' output."));
        }

        return CommandPreParseResult.Success(manifest);
    }

    private static string? GetCommandName(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return "help";
        }

        if (args.Count == 1 && args[0] is "--version" or "-v")
        {
            return "version";
        }

        if (args.Count == 1 && args[0] is "--help" or "-h")
        {
            return "help";
        }

        var index = 0;
        while (index < args.Count)
        {
            var token = args[index];
            if (token is "--help" or "-h")
            {
                return "help";
            }

            if (token is "--version" or "-v")
            {
                return "version";
            }

            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                return HasHelpFlag(args, index + 1) ? "help" : token;
            }

            index += IsBooleanGlobalOption(token) ? 1 : 2;
        }

        return null;
    }

    private static bool HasHelpFlag(IReadOnlyList<string> args, int startIndex)
    {
        for (var index = startIndex; index < args.Count; index++)
        {
            if (args[index] is "--help" or "-h")
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadFormat(IReadOnlyList<string> args, out OutputFormat? format)
    {
        format = null;
        for (var index = 0; index < args.Count; index++)
        {
            if (!args[index].Equals("--format", StringComparison.Ordinal))
            {
                continue;
            }

            if (index + 1 >= args.Count)
            {
                return false;
            }

            return TryParseFormat(args[index + 1], out format);
        }

        return true;
    }

    private static bool TryParseFormat(string value, out OutputFormat? format)
    {
        format = value.ToLowerInvariant() switch
        {
            "text" => OutputFormat.Text,
            "json" => OutputFormat.Json,
            "markdown" => OutputFormat.Markdown,
            _ => null
        };

        return format is not null;
    }

    private static bool IsBooleanGlobalOption(string token)
    {
        return token is "--detail" or "--no-update-check";
    }

    private static AtomUICliError CreateUnknownCommandError(string commandName)
    {
        return new AtomUICliError(
            AtomUICliErrorCodes.ArgumentCommandNotFound,
            AtomUICliSeverity.Error,
            $"Command '{commandName}' is not registered.",
            "Run `dotnet atomui help` to list available commands.",
            "parse",
            null,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["command"] = commandName
            });
    }

    private static AtomUICliError CreateArgumentError(string message)
    {
        return new AtomUICliError(
            AtomUICliErrorCodes.ArgumentInvalidValue,
            AtomUICliSeverity.Error,
            message,
            null,
            "parse",
            null,
            null);
    }
}

public sealed record CommandPreParseResult(
    bool IsSuccess,
    CommandManifest? Manifest,
    AtomUICliError? Error)
{
    public static CommandPreParseResult Success(CommandManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return new CommandPreParseResult(true, manifest, null);
    }

    public static CommandPreParseResult Failure(AtomUICliError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new CommandPreParseResult(false, null, error);
    }
}
