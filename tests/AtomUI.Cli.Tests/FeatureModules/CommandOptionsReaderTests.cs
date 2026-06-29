using AtomUI.Cli.Hosting.Commands;
using Xunit;

namespace AtomUI.Cli.Tests.FeatureModules;

public sealed class CommandOptionsReaderTests
{
    [Fact]
    public void ReaderReadsNamedValueAndKeepsPositionals()
    {
        var reader = new CommandOptionsReader(["Button", "--section", "api", "--strict"]);

        Assert.Equal("Button", reader.Positionals[0]);
        Assert.Equal("api", reader.GetOption("section"));
        Assert.True(reader.HasFlag("strict"));
    }

    [Fact]
    public void ReaderReadsFalseBooleanValue()
    {
        var reader = new CommandOptionsReader(["--include-xaml", "false"]);

        Assert.False(reader.GetBool("include-xaml", defaultValue: true));
    }
}
