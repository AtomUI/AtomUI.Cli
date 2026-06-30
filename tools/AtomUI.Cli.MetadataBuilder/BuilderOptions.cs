namespace AtomUI.Cli.MetadataBuilder;

internal sealed record BuilderOptions(
    string SourceRoot,
    string OutputPath,
    string TargetVersion,
    string SourceRef,
    string OutputRoot,
    IReadOnlySet<string> RequiredSnapshots)
{
    public string TokenOutputPath => OutputPath;

    public static BuilderOptions Parse(IReadOnlyList<string> args)
    {
        string? sourceRoot = null;
        string? output = null;
        string? outputRoot = null;
        var targetVersion = "6.0";
        var sourceRef = "release/6.0";
        var requiredSnapshots = new SortedSet<string>(StringComparer.Ordinal)
        {
            "catalog",
            "document",
            "semantic",
            "token"
        };

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (arg.Equals("--source-root", StringComparison.Ordinal))
            {
                sourceRoot = ReadValue(args, ref i, arg);
            }
            else if (arg.Equals("--output", StringComparison.Ordinal))
            {
                output = ReadValue(args, ref i, arg);
            }
            else if (arg.Equals("--output-root", StringComparison.Ordinal))
            {
                outputRoot = ReadValue(args, ref i, arg);
            }
            else if (arg.Equals("--target-version", StringComparison.Ordinal))
            {
                targetVersion = ReadValue(args, ref i, arg);
            }
            else if (arg.Equals("--source-ref", StringComparison.Ordinal))
            {
                sourceRef = ReadValue(args, ref i, arg);
            }
            else if (arg.Equals("--snapshots", StringComparison.Ordinal))
            {
                requiredSnapshots = new SortedSet<string>(ReadValue(args, ref i, arg)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(value => value.ToLowerInvariant()), StringComparer.Ordinal);
            }
            else
            {
                throw new InvalidOperationException($"Unknown argument '{arg}'.");
            }
        }

        sourceRoot = ResolveSourceRoot(sourceRoot);
        if (string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(outputRoot))
        {
            throw new InvalidOperationException("Missing required --output or --output-root argument.");
        }

        if (string.IsNullOrWhiteSpace(outputRoot))
        {
            outputRoot = Path.GetDirectoryName(output) ?? ".";
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            output = Path.Combine(outputRoot, "BuiltInTokenSnapshot.g.cs");
        }

        return new BuilderOptions(sourceRoot, output, targetVersion, sourceRef, outputRoot, requiredSnapshots);
    }

    private static string ResolveSourceRoot(string? explicitSourceRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitSourceRoot))
        {
            return explicitSourceRoot;
        }

        var environmentRoot = Environment.GetEnvironmentVariable("ATOMUI_SOURCE_ROOT");
        if (!string.IsNullOrWhiteSpace(environmentRoot))
        {
            return environmentRoot;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../ReferenceProjects/AtomUI"));
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string optionName)
    {
        index++;
        if (index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new InvalidOperationException($"Missing value for {optionName}.");
        }

        return args[index];
    }
}
