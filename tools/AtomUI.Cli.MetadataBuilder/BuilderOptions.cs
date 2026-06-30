namespace AtomUI.Cli.MetadataBuilder;

internal sealed record BuilderOptions(
    string SourceRoot,
    string OutputPath,
    string TargetVersion)
{
    public static BuilderOptions Parse(IReadOnlyList<string> args)
    {
        string? sourceRoot = null;
        string? output = null;
        var targetVersion = "6.0";

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
            else if (arg.Equals("--target-version", StringComparison.Ordinal))
            {
                targetVersion = ReadValue(args, ref i, arg);
            }
            else
            {
                throw new InvalidOperationException($"Unknown argument '{arg}'.");
            }
        }

        sourceRoot = ResolveSourceRoot(sourceRoot);
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException("Missing required --output argument.");
        }

        sourceRoot = Path.GetFullPath(sourceRoot);
        output = Path.GetFullPath(output);

        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"AtomUI source root '{sourceRoot}' does not exist. Configure --source-root, AtomUIDocSourceRoot, or ATOMUI_SOURCE_ROOT.");
        }

        return new BuilderOptions(sourceRoot, output, targetVersion);
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
