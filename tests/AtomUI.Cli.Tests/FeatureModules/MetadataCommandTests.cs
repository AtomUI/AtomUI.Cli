using System.Text.Json;
using AtomUI.Cli;
using AtomUI.Cli.Entry;
using AtomUI.Cli.Hosting.Commands;
using AtomUI.Cli.Hosting.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AtomUI.Cli.Tests.FeatureModules;

public sealed class MetadataCommandTests
{
    [Theory]
    [InlineData("list", "controls")]
    [InlineData("info", "Button")]
    [InlineData("doc", "Button")]
    [InlineData("demo", "Button", "--list")]
    [InlineData("token")]
    [InlineData("semantic", "Button")]
    [InlineData("design.md")]
    [InlineData("package", "AtomUI.Desktop.Controls")]
    [InlineData("changelog")]
    public async Task MetadataCommandReturnsSuccess(params string[] args)
    {
        var exitCode = await Program.Main(args);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ListDefaultOutputsControlsGroupedByGalleryCategory()
    {
        var result = await DispatchListAsync(["list"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.Contains("AtomUI controls (71)", output, StringComparison.Ordinal);
        Assert.Contains("General (4)", output, StringComparison.Ordinal);
        Assert.Contains("Data Entry (18)", output, StringComparison.Ordinal);
        Assert.Contains("Data Display (22)", output, StringComparison.Ordinal);
        Assert.Contains("Other (2)", output, StringComparison.Ordinal);
        Assert.Contains("Button", output, StringComparison.Ordinal);
        Assert.Contains("ColorPicker", output, StringComparison.Ordinal);
        Assert.Contains("DataGrid", output, StringComparison.Ordinal);
        Assert.Contains("Tour", output, StringComparison.Ordinal);
        Assert.Contains("Watermark", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Palette", output, StringComparison.Ordinal);
        Assert.DoesNotContain("CustomizeTheme", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListCategoriesUsesGalleryOrder()
    {
        var result = await DispatchListAsync(["list", "categories"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.Equal(
            string.Join(Environment.NewLine, "General", "Layout", "Navigation", "Data Entry", "Data Display", "Feedback", "Other"),
            output);
    }

    [Theory]
    [InlineData("data-display", 22, "DataGrid", "Card", "TreeView", "Tour")]
    [InlineData("data-entry", 18, "AutoComplete", "ColorPicker", "TreeSelect", "Upload")]
    public async Task ListControlsFiltersByNormalizedCategory(string category, int expectedCount, params string[] expectedControls)
    {
        var result = await DispatchListAsync(["list", "controls", "--category", category]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.Contains($"({expectedCount})", output, StringComparison.Ordinal);
        foreach (var expectedControl in expectedControls)
        {
            Assert.Contains(expectedControl, output, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task ListJsonReturnsStructuredControlPayload()
    {
        var result = await DispatchListAsync(["--format", "json", "list"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        using var document = JsonDocument.Parse(output);
        var payload = document.RootElement.GetProperty("payload");

        Assert.Equal("controls", payload.GetProperty("kind").GetString());
        Assert.Equal(71, payload.GetProperty("total").GetInt32());
        var categories = payload.GetProperty("categories");
        Assert.Equal(7, categories.GetArrayLength());
        Assert.Equal("general", categories[0].GetProperty("id").GetString());
        Assert.Equal("General", categories[0].GetProperty("name").GetString());
        Assert.Equal(4, categories[0].GetProperty("count").GetInt32());
        Assert.Equal("button", categories[0].GetProperty("controls")[0].GetProperty("id").GetString());
        Assert.Equal("Button", categories[0].GetProperty("controls")[0].GetProperty("name").GetString());
        Assert.DoesNotContain("\"payload\":\"", output, StringComparison.Ordinal);
    }

    private static async Task<ListDispatchResult> DispatchListAsync(string[] args)
    {
        var output = new RecordingOutputWriter();
        var services = new ServiceCollection()
            .AddSingleton(MetadataCatalog.CreateDefault())
            .AddSingleton<MetadataQueryService>()
            .AddTransient<ListCommandHandler>()
            .BuildServiceProvider();
        var descriptor = CliCommandDescriptor.Create<ListCommandOptions, ListCommandHandler>(
            "list",
            ListCommandOptions.Parse,
            CommandGroup.Knowledge,
            new HashSet<OutputFormat> { OutputFormat.Text, OutputFormat.Json, OutputFormat.Markdown });
        var dispatcher = new CliCommandDispatcher(
            new CliCommandParser(),
            services.GetRequiredService<IServiceScopeFactory>(),
            output);

        var result = await dispatcher.DispatchAsync(
            args,
            CliCommandDescriptorCatalog.Create([descriptor]),
            TestContext.Current.CancellationToken);

        return new ListDispatchResult(result, output);
    }

    private sealed record ListDispatchResult(AtomUICliResult Result, RecordingOutputWriter Output);

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
