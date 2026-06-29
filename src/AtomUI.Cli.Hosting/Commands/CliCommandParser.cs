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
            if (token is "--help" or "-h")
            {
                break;
            }

            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                break;
            }

            var consumed = TryConsumeGlobalOption(args, ref index, ref global, errorOnUnknown: true, out var error);
            if (error is not null)
            {
                return CliCommandParseResult.Failure(error);
            }

            if (!consumed)
            {
                return CliCommandParseResult.Failure(CreateArgumentError($"Unknown global option '{token}'."));
            }
        }

        if (index >= args.Count)
        {
            return CliCommandParseResult.Failure(CreateUnknownCommandError(null));
        }

        var commandName = args[index] is "--help" or "-h" ? "help" : args[index];
        var commandArguments = new List<string>();
        var hasCommandHelpFlag = false;
        var commandArgIndex = index + 1;
        while (commandArgIndex < args.Count)
        {
            var token = args[commandArgIndex];
            if (token is "--help" or "-h")
            {
                hasCommandHelpFlag = true;
                commandArgIndex++;
                continue;
            }

            if (TryConsumeGlobalOption(args, ref commandArgIndex, ref global, errorOnUnknown: false, out var error))
            {
                if (error is not null)
                {
                    return CliCommandParseResult.Failure(error);
                }

                continue;
            }

            commandArguments.Add(token);
            commandArgIndex++;
        }

        if (hasCommandHelpFlag && !commandName.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            commandArguments.Clear();
            commandArguments.Add(commandName);
            commandName = "help";
        }

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
            Array.AsReadOnly(commandArguments.ToArray()));
    }

    private static bool TryConsumeGlobalOption(
        IReadOnlyList<string> args,
        ref int index,
        ref GlobalCliOptions global,
        bool errorOnUnknown,
        out AtomUICliError? error)
    {
        error = null;
        var token = args[index];
        switch (token)
        {
            case "--format":
                if (!TryReadValue(args, ref index, out var formatValue) || !TryParseFormat(formatValue, out var format))
                {
                    error = CreateArgumentError("Invalid --format value.");
                }
                else
                {
                    global = global with { Format = format };
                }
                return true;
            case "--markdown":
                global = global with { Format = OutputFormat.Markdown };
                index++;
                return true;
            case "--lang":
                if (!TryReadValue(args, ref index, out var lang))
                {
                    error = CreateArgumentError("Missing --lang value.");
                }
                else
                {
                    global = global with { Language = lang };
                }
                return true;
            case "--target-version":
                if (!TryReadValue(args, ref index, out var targetVersion))
                {
                    error = CreateArgumentError("Missing --target-version value.");
                }
                else
                {
                    global = global with { TargetVersion = targetVersion };
                }
                return true;
            case "--product":
                if (!TryReadValue(args, ref index, out var product))
                {
                    error = CreateArgumentError("Missing --product value.");
                }
                else
                {
                    global = global with { Product = product };
                }
                return true;
            case "--data-root":
                if (!TryReadValue(args, ref index, out var dataRoot))
                {
                    error = CreateArgumentError("Missing --data-root value.");
                }
                else
                {
                    global = global with { DataRoot = dataRoot };
                }
                return true;
            case "--detail":
                global = global with { Detail = true };
                index++;
                return true;
            case "--no-update-check":
                global = global with { NoUpdateCheck = true };
                index++;
                return true;
            default:
                if (errorOnUnknown && token.StartsWith("--", StringComparison.Ordinal))
                {
                    error = CreateArgumentError($"Unknown global option '{token}'.");
                    return true;
                }

                return false;
        }
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
