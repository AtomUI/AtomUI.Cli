using AtomUI.Cli;

namespace AtomUI.Cli.Hosting.Commands.BuiltIn;

public sealed class HelpCommandHandler(CommandManifestCatalog commandManifests) : IAtomUICliCommandHandler<BuiltInCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(
        BuiltInCommandOptions options,
        CliInvocationContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var commandLines = commandManifests.Manifests
            .OrderBy(manifest => manifest.Name, StringComparer.OrdinalIgnoreCase)
            .Select(manifest => $"  {manifest.Name}");
        var text = $"dotnet atomui <command> [options]{Environment.NewLine}{string.Join(Environment.NewLine, commandLines)}";

        return ValueTask.FromResult(AtomUICliResult.Success(text));
    }
}
