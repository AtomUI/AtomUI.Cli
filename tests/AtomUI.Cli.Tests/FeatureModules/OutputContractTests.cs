using System.Text.Json;
using AtomUI.Cli;
using AtomUI.Cli.Hosting.Commands;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AtomUI.Cli.Tests.FeatureModules;

public sealed class OutputContractTests
{
    [Fact]
    public async Task JsonFormatSerializesSuccessEnvelopeForObjectPayload()
    {
        var output = new RecordingOutputWriter();
        var services = new ServiceCollection()
            .AddTransient<ObjectCommandHandler>()
            .BuildServiceProvider();
        var descriptor = CliCommandDescriptor.Create<ObjectCommandOptions, ObjectCommandHandler>(
            "object",
            static (global, _) => new ObjectCommandOptions(global),
            CommandGroup.Knowledge,
            new HashSet<OutputFormat> { OutputFormat.Text, OutputFormat.Json });
        var dispatcher = new CliCommandDispatcher(
            new CliCommandParser(),
            services.GetRequiredService<IServiceScopeFactory>(),
            output);

        var result = await dispatcher.DispatchAsync(
            ["--format", "json", "object"],
            CliCommandDescriptorCatalog.Create([descriptor]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var line = Assert.Single(output.Lines);
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;
        Assert.Equal("1.0", root.GetProperty("schemaVersion").GetString());
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("object", root.GetProperty("command").GetString());
        Assert.Equal("AtomUI.Controls", root.GetProperty("payload").GetProperty("package").GetString());
        Assert.Equal(2, root.GetProperty("payload").GetProperty("count").GetInt32());
    }

    private sealed record ObjectCommandOptions(GlobalCliOptions Global) : IAtomUICliCommandOptions;

    private sealed record ObjectPayload(string Package, int Count) : IAtomUICliJsonPayload
    {
        public IReadOnlyDictionary<string, object?> ToJsonPayload()
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["package"] = Package,
                ["count"] = Count
            };
        }
    }

    private sealed class ObjectCommandHandler : IAtomUICliCommandHandler<ObjectCommandOptions>
    {
        public ValueTask<AtomUICliResult> ExecuteAsync(
            ObjectCommandOptions options,
            CliInvocationContext context,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(AtomUICliResult.Success(new ObjectPayload("AtomUI.Controls", 2)));
        }
    }

    private sealed class RecordingOutputWriter : IOutputWriter
    {
        public List<string> Lines { get; } = [];

        public ValueTask WriteLineAsync(string text, CancellationToken cancellationToken = default)
        {
            Lines.Add(text);
            return ValueTask.CompletedTask;
        }
    }
}
