using AtomUI.Cli;
using AtomUI.Cli.Hosting.Commands;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class CommandParserTests
{
    [Fact]
    public void ParseBindsGlobalOptionsBeforeCommand()
    {
        var descriptor = CreateDescriptor("list");
        var catalog = CliCommandDescriptorCatalog.Create([descriptor]);
        var parser = new CliCommandParser();

        var result = parser.Parse(
            ["--format", "json", "--lang", "en", "--detail", "list", "controls"],
            catalog);

        Assert.True(result.IsSuccess);
        Assert.Same(descriptor, result.Descriptor);
        Assert.Equal(OutputFormat.Json, result.GlobalOptions.Format);
        Assert.Equal("en", result.GlobalOptions.Language);
        Assert.True(result.GlobalOptions.Detail);
        Assert.Equal(["controls"], result.CommandArguments);
    }

    [Fact]
    public void ParseReturnsArg003ForUnknownCommand()
    {
        var parser = new CliCommandParser();
        var catalog = CliCommandDescriptorCatalog.Empty;

        var result = parser.Parse(["missing"], catalog);

        Assert.False(result.IsSuccess);
        Assert.Equal(AtomUICliErrorCodes.ArgumentCommandNotFound, result.Error?.Code);
    }

    [Fact]
    public void ParseReturnsArg002ForInvalidFormat()
    {
        var parser = new CliCommandParser();
        var catalog = CliCommandDescriptorCatalog.Create([CreateDescriptor("list")]);

        var result = parser.Parse(["--format", "xml", "list"], catalog);

        Assert.False(result.IsSuccess);
        Assert.Equal(AtomUICliErrorCodes.ArgumentInvalidValue, result.Error?.Code);
    }

    [Fact]
    public void ParseMapsCommandHelpShortcutToHelpDescriptor()
    {
        var helpDescriptor = CreateDescriptor("help", OutputFormat.Markdown);
        var infoDescriptor = CreateDescriptor("info");
        var catalog = CliCommandDescriptorCatalog.Create([helpDescriptor, infoDescriptor]);
        var parser = new CliCommandParser();

        var result = parser.Parse(["info", "--help"], catalog);

        Assert.True(result.IsSuccess);
        Assert.Same(helpDescriptor, result.Descriptor);
        Assert.Equal(["info"], result.CommandArguments);
    }

    [Fact]
    public void ParseBindsGlobalFormatAfterHelpCommandArgument()
    {
        var helpDescriptor = CreateDescriptor("help", OutputFormat.Markdown);
        var catalog = CliCommandDescriptorCatalog.Create([helpDescriptor]);
        var parser = new CliCommandParser();

        var result = parser.Parse(["help", "info", "--format", "markdown"], catalog);

        Assert.True(result.IsSuccess);
        Assert.Same(helpDescriptor, result.Descriptor);
        Assert.Equal(OutputFormat.Markdown, result.GlobalOptions.Format);
        Assert.Equal(["info"], result.CommandArguments);
    }

    private static CliCommandDescriptor CreateDescriptor(string name, params OutputFormat[] additionalFormats)
    {
        return CliCommandDescriptor.Create<NoopCommandOptions, NoopCommandHandler>(
            name,
            (global, _) => new NoopCommandOptions(global),
            CommandGroup.Knowledge,
            new HashSet<OutputFormat>([OutputFormat.Text, OutputFormat.Json, .. additionalFormats]));
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
