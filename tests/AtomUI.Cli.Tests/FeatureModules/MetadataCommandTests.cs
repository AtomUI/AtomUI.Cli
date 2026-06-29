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

    [Fact]
    public async Task ListMarkdownShortcutOutputsMarkdown()
    {
        var result = await DispatchListAsync(["list", "--markdown"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("# AtomUI Controls", output, StringComparison.Ordinal);
        Assert.Contains("## General", output, StringComparison.Ordinal);
        Assert.Contains("| Name | Package | Product |", output, StringComparison.Ordinal);
        Assert.DoesNotContain("AtomUI controls (71)", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InfoDefaultOutputsControlContractAsPlainText()
    {
        var result = await DispatchInfoAsync(["info", "Button"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.Contains("Button - General", output, StringComparison.Ordinal);
        Assert.Contains("Package: AtomUI.Desktop.Controls", output, StringComparison.Ordinal);
        Assert.Contains("Namespace: AtomUI.Desktop.Controls", output, StringComparison.Ordinal);
        Assert.Contains("Base type: Avalonia.Controls.Button", output, StringComparison.Ordinal);
        Assert.Contains("Usage:", output, StringComparison.Ordinal);
        Assert.Contains("<atom:Button ButtonType=\"Primary\" Content=\"Save\" />", output, StringComparison.Ordinal);
        Assert.Contains("Template parts:", output, StringComparison.Ordinal);
        Assert.Contains("PART_LoadingIcon", output, StringComparison.Ordinal);
        Assert.Contains("States:", output, StringComparison.Ordinal);
        Assert.Contains(":loading", output, StringComparison.Ordinal);
        Assert.Contains("Tokens:", output, StringComparison.Ordinal);
        Assert.Contains("ButtonToken", output, StringComparison.Ordinal);
        Assert.Contains("Related:", output, StringComparison.Ordinal);
        Assert.DoesNotContain("# Button", output, StringComparison.Ordinal);
        Assert.DoesNotContain("| Field |", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InfoMarkdownOutputsMarkdownOnlyWhenRequested()
    {
        var result = await DispatchInfoAsync(["--format", "markdown", "info", "Button"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("# Button", output, StringComparison.Ordinal);
        Assert.Contains("| Field | Value |", output, StringComparison.Ordinal);
        Assert.Contains("```xml", output, StringComparison.Ordinal);
        Assert.Contains("<atom:Button ButtonType=\"Primary\" Content=\"Save\" />", output, StringComparison.Ordinal);
        Assert.Contains("## API", output, StringComparison.Ordinal);
        Assert.Contains("| Property | Type | Default | Kind | Description |", output, StringComparison.Ordinal);
        Assert.Contains("## Template Parts", output, StringComparison.Ordinal);
        Assert.Contains("## Related Commands", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InfoJsonReturnsStructuredPayload()
    {
        var result = await DispatchInfoAsync(["--format", "json", "info", "Button"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        using var document = JsonDocument.Parse(output);
        var payload = document.RootElement.GetProperty("payload");

        Assert.Equal("1.0", payload.GetProperty("schemaVersion").GetString());
        Assert.Equal("info", payload.GetProperty("command").GetString());
        Assert.Equal("Button", payload.GetProperty("control").GetProperty("name").GetString());
        Assert.Equal("AtomUI.Desktop.Controls", payload.GetProperty("control").GetProperty("packageId").GetString());
        Assert.Equal("Avalonia.Controls.Button", payload.GetProperty("type").GetProperty("baseType").GetString());
        Assert.Equal("<atom:Button ButtonType=\"Primary\" Content=\"Save\" />", payload.GetProperty("usage").GetProperty("xamlSnippet").GetString());
        Assert.Contains(
            payload.GetProperty("api").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "ButtonType"
                    && item.GetProperty("propertyKind").GetString() == "styled");
        Assert.Contains(
            payload.GetProperty("template").GetProperty("parts").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "PART_LoadingIcon");
        Assert.Contains(
            payload.GetProperty("states").EnumerateArray(),
            item => item.GetProperty("name").GetString() == ":loading");
        Assert.Contains(
            payload.GetProperty("tokens").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "ButtonToken"
                    && item.GetProperty("scope").GetString() == "control");
        Assert.DoesNotContain("\"payload\":\"", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InfoDataGridIncludesCommercialContractInformation()
    {
        var result = await DispatchInfoAsync(["--format", "json", "info", "DataGrid", "--product", "datagrid"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        using var document = JsonDocument.Parse(output);
        var payload = document.RootElement.GetProperty("payload");

        Assert.Equal("DataGrid", payload.GetProperty("control").GetProperty("name").GetString());
        Assert.Equal("datagrid", payload.GetProperty("control").GetProperty("productId").GetString());
        Assert.True(payload.GetProperty("control").GetProperty("isCommercial").GetBoolean());
        Assert.Equal("AtomUI.Desktop.Controls.DataGrid", payload.GetProperty("control").GetProperty("packageId").GetString());
        Assert.Contains(
            payload.GetProperty("api").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "ItemsSource");
        Assert.Contains(
            payload.GetProperty("tokens").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "HeaderBg"
                    && item.GetProperty("scope").GetString() == "control");
        Assert.Contains(
            payload.GetProperty("diagnostics").EnumerateArray(),
            item => item.GetProperty("code").GetString() == "ATOMUICLI_INFO_COMMERCIAL");
    }

    [Fact]
    public async Task InfoIncludeFiltersPlainTextSections()
    {
        var result = await DispatchInfoAsync(["info", "Button", "--include", "tokens"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.Contains("Button - General", output, StringComparison.Ordinal);
        Assert.Contains("Tokens:", output, StringComparison.Ordinal);
        Assert.Contains("ButtonToken", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Usage:", output, StringComparison.Ordinal);
        Assert.DoesNotContain("API:", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Template parts:", output, StringComparison.Ordinal);
        Assert.DoesNotContain("States:", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Demos:", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Related:", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InfoIncludeFiltersMarkdownSections()
    {
        var result = await DispatchInfoAsync(["--format", "markdown", "info", "Button", "--include", "usage,api"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("# Button", output, StringComparison.Ordinal);
        Assert.Contains("## Usage", output, StringComparison.Ordinal);
        Assert.Contains("## API", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## Template Parts", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## States", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## Tokens", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## Demos", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## Related Commands", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InfoRejectsUnknownIncludeSection()
    {
        var result = await DispatchInfoAsync(["info", "Button", "--include", "tokens,unknown"]);

        Assert.False(result.Result.IsSuccess);
        Assert.NotNull(result.Result.Error);
        Assert.Equal(AtomUICliErrorCodes.ArgumentInvalidValue, result.Result.Error.Code);
        Assert.Empty(result.Output.Lines);
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

    private static async Task<ListDispatchResult> DispatchInfoAsync(string[] args)
    {
        var output = new RecordingOutputWriter();
        var services = new ServiceCollection()
            .AddSingleton(MetadataCatalog.CreateDefault())
            .AddSingleton<MetadataQueryService>()
            .AddTransient<InfoCommandHandler>()
            .BuildServiceProvider();
        var descriptor = CliCommandDescriptor.Create<InfoCommandOptions, InfoCommandHandler>(
            "info",
            InfoCommandOptions.Parse,
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
