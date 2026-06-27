using AtomUI.Cli;
using AtomUI.Cli.Hosting.Commands;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class CommandCatalogTests
{
    [Fact]
    public void CreateRejectsDuplicateCommandNames()
    {
        var first = CreateDescriptor("info");
        var second = CreateDescriptor("info");

        var ex = Assert.Throws<ArgumentException>(() => CliCommandDescriptorCatalog.Create([first, second]));

        Assert.Contains("info", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TryFindUsesOrdinalIgnoreCaseCommandNameLookup()
    {
        var descriptor = CreateDescriptor("info");
        var catalog = CliCommandDescriptorCatalog.Create([descriptor]);

        Assert.True(catalog.TryFind("INFO", out var actual));
        Assert.Same(descriptor, actual);
    }

    private static CliCommandDescriptor CreateDescriptor(string name)
    {
        return CliCommandDescriptor.Create<NoopCommandOptions, NoopCommandHandler>(
            name,
            (global, _) => new NoopCommandOptions(global),
            CommandGroup.Knowledge,
            new HashSet<OutputFormat> { OutputFormat.Text });
    }

    private sealed record NoopCommandOptions(GlobalCliOptions Global) : IAtomUICliCommandOptions;

    private sealed class NoopCommandHandler : IAtomUICliCommandHandler<NoopCommandOptions>
    {
        public ValueTask<AtomUICliResult> ExecuteAsync(
            NoopCommandOptions options,
            CliInvocationContext context,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(AtomUICliResult.Success());
        }
    }
}
