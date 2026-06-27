using AtomUI.Cli;

namespace AtomUI.Cli.Hosting.Commands;

public sealed class CliCommandParser
{
    public CliCommandParseResult Parse(IReadOnlyList<string> args, CliCommandDescriptorCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(catalog);

        if (args.Count == 1 && args[0] is "--version" or "-v")
        {
            args = ["version"];
        }
        else if (args.Count == 0 || (args.Count == 1 && args[0] is "--help" or "-h"))
        {
            args = ["help"];
        }

        var global = new GlobalCliOptions();
        var index = 0;

        while (index < args.Count)
        {
            var token = args[index];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                break;
            }

            switch (token)
            {
                case "--format":
                    if (!TryReadValue(args, ref index, out var formatValue) || !TryParseFormat(formatValue, out var format))
                    {
                        return CliCommandParseResult.Failure(CreateArgumentError("Invalid --format value."));
                    }
                    global = global with { Format = format };
                    break;
                case "--lang":
                    if (!TryReadValue(args, ref index, out var lang))
                    {
                        return CliCommandParseResult.Failure(CreateArgumentError("Missing --lang value."));
                    }
                    global = global with { Language = lang };
                    break;
                case "--target-version":
                    if (!TryReadValue(args, ref index, out var targetVersion))
                    {
                        return CliCommandParseResult.Failure(CreateArgumentError("Missing --target-version value."));
                    }
                    global = global with { TargetVersion = targetVersion };
                    break;
                case "--product":
                    if (!TryReadValue(args, ref index, out var product))
                    {
                        return CliCommandParseResult.Failure(CreateArgumentError("Missing --product value."));
                    }
                    global = global with { Product = product };
                    break;
                case "--data-root":
                    if (!TryReadValue(args, ref index, out var dataRoot))
                    {
                        return CliCommandParseResult.Failure(CreateArgumentError("Missing --data-root value."));
                    }
                    global = global with { DataRoot = dataRoot };
                    break;
                case "--detail":
                    global = global with { Detail = true };
                    index++;
                    break;
                case "--no-update-check":
                    global = global with { NoUpdateCheck = true };
                    index++;
                    break;
                default:
                    return CliCommandParseResult.Failure(CreateArgumentError($"Unknown global option '{token}'."));
            }
        }

        if (index >= args.Count)
        {
            return CliCommandParseResult.Failure(CreateUnknownCommandError(null));
        }

        var commandName = args[index];
        if (!catalog.TryFind(commandName, out var descriptor) || descriptor is null)
        {
            return CliCommandParseResult.Failure(CreateUnknownCommandError(commandName));
        }

        if (!descriptor.SupportedFormats.Contains(global.Format))
        {
            return CliCommandParseResult.Failure(CreateArgumentError($"Command '{commandName}' does not support '{global.Format}' output."));
        }

        return CliCommandParseResult.Success(
            descriptor,
            global,
            Array.AsReadOnly(args.Skip(index + 1).ToArray()));
    }

    private static bool TryReadValue(IReadOnlyList<string> args, ref int index, out string value)
    {
        if (index + 1 >= args.Count)
        {
            value = string.Empty;
            return false;
        }

        value = args[index + 1];
        index += 2;
        return true;
    }

    private static bool TryParseFormat(string value, out OutputFormat format)
    {
        return value.ToLowerInvariant() switch
        {
            "text" => SetFormat(OutputFormat.Text, out format),
            "json" => SetFormat(OutputFormat.Json, out format),
            "markdown" => SetFormat(OutputFormat.Markdown, out format),
            _ => SetFormat(default, out format, false)
        };
    }

    private static bool SetFormat(OutputFormat value, out OutputFormat format, bool result = true)
    {
        format = value;
        return result;
    }

    private static AtomUICliError CreateUnknownCommandError(string? commandName)
    {
        var details = commandName is null
            ? null
            : new Dictionary<string, string>(StringComparer.Ordinal) { ["command"] = commandName };

        return new AtomUICliError(
            AtomUICliErrorCodes.ArgumentCommandNotFound,
            AtomUICliSeverity.Error,
            commandName is null ? "Command is required." : $"Command '{commandName}' is not registered.",
            "Run `dotnet atomui help` to list available commands.",
            "parse",
            null,
            details);
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
