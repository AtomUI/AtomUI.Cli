namespace AtomUI.Cli.Hosting.Mcp;

public sealed class McpCommandHandler(McpToolCatalog catalog) : IAtomUICliCommandHandler<McpCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(McpCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(AtomUICliResult.Success(catalog.RenderText()));
    }
}
