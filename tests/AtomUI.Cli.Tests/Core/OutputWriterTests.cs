using AtomUI.Cli;
using AtomUI.Cli.Hosting.Output;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class OutputWriterTests
{
    [Fact]
    public async Task OutputWriterWritesSuccessTextToStdout()
    {
        await using var stdout = new StringWriter();
        var writer = new ConsoleOutputWriter(stdout);

        await writer.WriteLineAsync("ok", TestContext.Current.CancellationToken);

        Assert.Equal($"ok{Environment.NewLine}", stdout.ToString());
    }

    [Fact]
    public async Task ErrorWriterWritesJsonErrorEnvelopeToStderr()
    {
        await using var stderr = new StringWriter();
        var writer = new ConsoleErrorWriter(stderr, new JsonOutputSerializer());
        var error = new AtomUICliError(
            AtomUICliErrorCodes.ControlNotFound,
            AtomUICliSeverity.Error,
            "Control 'Foo' was not found.",
            "Run `dotnet atomui list controls`.",
            "execute",
            null,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["control"] = "Foo"
            });

        await writer.WriteErrorAsync("info", error, OutputFormat.Json, TestContext.Current.CancellationToken);

        var json = stderr.ToString();
        Assert.Contains("\"schemaVersion\":\"1.0\"", json, StringComparison.Ordinal);
        Assert.Contains("\"success\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"command\":\"info\"", json, StringComparison.Ordinal);
        Assert.Contains("\"code\":\"ATOMUICLI_CTRL001\"", json, StringComparison.Ordinal);
        Assert.EndsWith(Environment.NewLine, json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ErrorWriterWritesReadableTextToStderr()
    {
        await using var stderr = new StringWriter();
        var writer = new ConsoleErrorWriter(stderr, new JsonOutputSerializer());
        var error = new AtomUICliError(
            AtomUICliErrorCodes.ArgumentInvalidValue,
            AtomUICliSeverity.Error,
            "Invalid format.",
            null,
            "parse",
            null,
            null);

        await writer.WriteErrorAsync("list", error, OutputFormat.Text, TestContext.Current.CancellationToken);

        Assert.Equal($"ATOMUICLI_ARG002: Invalid format.{Environment.NewLine}", stderr.ToString());
    }
}
