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

    [Fact]
    public async Task DemoDefaultListsSnapshotExamplesByScenario()
    {
        var result = await DispatchDemoAsync(["demo", "Button"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("Button demos (12)", output, StringComparison.Ordinal);
        Assert.Contains("Basic", output, StringComparison.Ordinal);
        Assert.Contains("State", output, StringComparison.Ordinal);
        Assert.Contains("Theme", output, StringComparison.Ordinal);
        Assert.Contains("button-loading", output, StringComparison.Ordinal);
        Assert.Contains("button-color-variant", output, StringComparison.Ordinal);
        Assert.Contains("dotnet atomui demo Button button-loading", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Basic Button", output, StringComparison.Ordinal);
        Assert.DoesNotContain("<Button Content=\"Save\" />", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DemoAllExpandsEveryMatchingExampleWithCode()
    {
        var result = await DispatchDemoAsync(["demo", "Button", "--all", "--related", "false"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("Button demos (12)", output, StringComparison.Ordinal);
        Assert.Contains("## Basic", output, StringComparison.Ordinal);
        Assert.Contains("button-type - 按钮类型", output, StringComparison.Ordinal);
        Assert.Contains("展示 Default、Primary、Dashed、Text 和 Link 的视觉优先级。", output, StringComparison.Ordinal);
        Assert.Contains("<atom:Button ButtonType=\"Primary\" Content=\"Primary\" />", output, StringComparison.Ordinal);
        Assert.Contains("button-color-variant - 颜色变体", output, StringComparison.Ordinal);
        Assert.Contains("<atom:Button Color=\"Success\" Variant=\"Outlined\" Content=\"Success\" />", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Run:", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Related:", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DemoAllMarkdownExpandsFilteredExamplesWithFencedCode()
    {
        var result = await DispatchDemoAsync(["--format", "markdown", "demo", "Button", "--all", "--scenario", "state", "--related", "false"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("# Button Demos", output, StringComparison.Ordinal);
        Assert.Contains("## State", output, StringComparison.Ordinal);
        Assert.Contains("### 加载状态", output, StringComparison.Ordinal);
        Assert.Contains("SourceKey: `button-loading`", output, StringComparison.Ordinal);
        Assert.Contains("```xaml", output, StringComparison.Ordinal);
        Assert.Contains("<atom:Button ButtonType=\"Primary\" IsLoading=\"True\" Content=\"Saving\" />", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## Basic", output, StringComparison.Ordinal);
        Assert.DoesNotContain("| SourceKey | Title | Description |", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DemoScenarioFiltersList()
    {
        var result = await DispatchDemoAsync(["demo", "Button", "--scenario", "state"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.Contains("Button demos (3)", output, StringComparison.Ordinal);
        Assert.Contains("button-loading", output, StringComparison.Ordinal);
        Assert.Contains("button-danger", output, StringComparison.Ordinal);
        Assert.Contains("button-disabled", output, StringComparison.Ordinal);
        Assert.DoesNotContain("button-type", output, StringComparison.Ordinal);
        Assert.DoesNotContain("button-color-variant", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DemoDetailOutputsSelectedSourceKey()
    {
        var result = await DispatchDemoAsync(["demo", "Button", "button-loading"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("Button demo: 加载状态", output, StringComparison.Ordinal);
        Assert.Contains("SourceKey: button-loading", output, StringComparison.Ordinal);
        Assert.Contains("Scenario: state", output, StringComparison.Ordinal);
        Assert.Contains("<atom:Button ButtonType=\"Primary\" IsLoading=\"True\" Content=\"Saving\" />", output, StringComparison.Ordinal);
        Assert.Contains("dotnet atomui doc Button --example button-loading", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Button demos (", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DemoDetailSourceOptionOutputsSourceIdentity()
    {
        var result = await DispatchDemoAsync(["demo", "Button", "button-loading", "--source"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.Contains("Source: controlgallery/AtomUIGallery/ShowCases/General/Button/Views/ButtonShowCase.axaml", output, StringComparison.Ordinal);
        Assert.Contains("Snapshot: atomui-docs-v6-builtin", output, StringComparison.Ordinal);
        Assert.Contains("Source ref: ../ReferenceProjects/AtomUI @ release/6.0", output, StringComparison.Ordinal);
        Assert.Contains("Source commit: builtin-doc-snapshot", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DemoDetailRelatedFalseOmitsRelatedCommands()
    {
        var result = await DispatchDemoAsync(["demo", "Button", "button-loading", "--related", "false"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.DoesNotContain("Related:", output, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet atomui doc Button --example button-loading", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DemoCodeOnlyOutputsOnlyRequestedXaml()
    {
        var result = await DispatchDemoAsync(["demo", "Button", "button-loading", "--code-only", "--code-language", "xaml"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.Equal("<atom:Button ButtonType=\"Primary\" IsLoading=\"True\" Content=\"Saving\" />", output);
    }

    [Fact]
    public async Task DemoJsonReturnsStructuredPayload()
    {
        var result = await DispatchDemoAsync(["--format", "json", "demo", "Button"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        using var document = JsonDocument.Parse(output);
        var payload = document.RootElement.GetProperty("payload");

        Assert.Equal(JsonValueKind.Object, payload.ValueKind);
        Assert.Equal("demo", payload.GetProperty("command").GetString());
        Assert.Equal("List", payload.GetProperty("mode").GetString());
        Assert.Equal("Button", payload.GetProperty("control").GetProperty("name").GetString());
        Assert.Equal("atomui-docs-v6-builtin", payload.GetProperty("source").GetProperty("snapshotId").GetString());
        Assert.Contains(
            payload.GetProperty("availableScenarios").EnumerateArray(),
            item => item.GetString() == "state");
        Assert.Contains(
            payload.GetProperty("demos").EnumerateArray(),
            item => item.GetProperty("sourceKey").GetString() == "button-loading");
        Assert.DoesNotContain("\"payload\":\"", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DemoRejectsCodeOnlyWithoutDemoKey()
    {
        var result = await DispatchDemoAsync(["demo", "Button", "--code-only"]);

        Assert.False(result.Result.IsSuccess);
        Assert.NotNull(result.Result.Error);
        Assert.Equal(AtomUICliErrorCodes.ArgumentInvalidValue, result.Result.Error.Code);
        Assert.Empty(result.Output.Lines);
    }

    [Fact]
    public async Task DemoRejectsRemovedLanguageOption()
    {
        var result = await DispatchDemoAsync(["demo", "Button", "button-loading", "--language", "xaml"]);

        Assert.False(result.Result.IsSuccess);
        Assert.NotNull(result.Result.Error);
        Assert.Equal(AtomUICliErrorCodes.ArgumentInvalidValue, result.Result.Error.Code);
        Assert.Empty(result.Output.Lines);
    }

    [Fact]
    public async Task DocDefaultOutputsMarkdownDocument()
    {
        var result = await DispatchDocAsync(["doc", "Button"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("# Button", output, StringComparison.Ordinal);
        Assert.Contains("## Overview", output, StringComparison.Ordinal);
        Assert.Contains("## Install", output, StringComparison.Ordinal);
        Assert.Contains("## Usage", output, StringComparison.Ordinal);
        Assert.Contains("<atom:Button ButtonType=\"Primary\" Content=\"Save\" />", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocDefaultOutputsFullControlDocumentationContract()
    {
        var result = await DispatchDocAsync(["doc", "Button"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);

        Assert.Contains("## 使用示例", output, StringComparison.Ordinal);
        Assert.Contains("SourceKey: `button-loading`", output, StringComparison.Ordinal);
        Assert.Contains("SourceKey: `button-color-variant`", output, StringComparison.Ordinal);
        Assert.Contains("## API", output, StringComparison.Ordinal);
        Assert.Contains("### 公共属性", output, StringComparison.Ordinal);
        Assert.Contains("StyledProperty", output, StringComparison.Ordinal);
        Assert.Contains("### 事件", output, StringComparison.Ordinal);
        Assert.Contains("IFormItemAware.ValueChanged", output, StringComparison.Ordinal);
        Assert.Contains("### Protected 扩展点", output, StringComparison.Ordinal);
        Assert.Contains("OnApplyTemplate", output, StringComparison.Ordinal);
        Assert.Contains("MeasureOverride", output, StringComparison.Ordinal);
        Assert.Contains("## 逻辑结构", output, StringComparison.Ordinal);
        Assert.Contains("ResolveEffectiveColorAndVariant", output, StringComparison.Ordinal);
        Assert.Contains("## ControlTheme 结构", output, StringComparison.Ordinal);
        Assert.Contains("PART_WaveSpirit", output, StringComparison.Ordinal);
        Assert.Contains("Template variant: Default / Link / Primary / Text", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocJsonReturnsStructuredPayload()
    {
        var result = await DispatchDocAsync(["--format", "json", "doc", "Button"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        using var document = JsonDocument.Parse(output);
        var payload = document.RootElement.GetProperty("payload");

        Assert.Equal(JsonValueKind.Object, payload.ValueKind);
        Assert.Equal("doc", payload.GetProperty("command").GetString());
        Assert.Equal("Button", payload.GetProperty("targetId").GetString());
        Assert.Equal("control", payload.GetProperty("targetKind").GetString());
        Assert.Equal("AtomUI.Desktop.Controls", payload.GetProperty("packageId").GetString());
        Assert.True(payload.GetProperty("sections").GetArrayLength() > 0);
        Assert.Contains(
            payload.GetProperty("sections").EnumerateArray(),
            section => section.GetProperty("id").GetString() == "usage");
        var control = payload.GetProperty("control");
        Assert.Equal("Button", control.GetProperty("identity").GetProperty("name").GetString());
        Assert.True(control.GetProperty("apiSurface").GetProperty("members").GetArrayLength() >= 12);
        Assert.Contains(
            control.GetProperty("apiSurface").GetProperty("members").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "OnApplyTemplate"
                    && item.GetProperty("kind").GetString() == "protected-method");
        Assert.Contains(
            control.GetProperty("apiSurface").GetProperty("events").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "IFormItemAware.ValueChanged"
                    && item.GetProperty("kind").GetString() == "interface-event");
        Assert.True(control.GetProperty("examples").GetArrayLength() >= 10);
        Assert.Contains(
            control.GetProperty("examples").EnumerateArray(),
            item => item.GetProperty("sourceKey").GetString() == "button-color-variant");
        Assert.Contains(
            control.GetProperty("theme").GetProperty("templates").EnumerateArray(),
            item => item.GetProperty("id").GetString() == "button-default-template");
        Assert.DoesNotContain("\"payload\":\"", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocThemeSectionFiltersMarkdownOutput()
    {
        var result = await DispatchDocAsync(["doc", "Button", "--section", "theme"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("# Button", output, StringComparison.Ordinal);
        Assert.Contains("## ControlTheme 结构", output, StringComparison.Ordinal);
        Assert.Contains("PART_ContentPresenter", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## API", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## 使用示例", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocExampleOutputsOnlySelectedExampleSection()
    {
        var result = await DispatchDocAsync(["doc", "Button", "--example", "button-loading"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("# Button", output, StringComparison.Ordinal);
        Assert.Contains("## 使用示例", output, StringComparison.Ordinal);
        Assert.Contains("SourceKey: `button-loading`", output, StringComparison.Ordinal);
        Assert.Contains("IsLoading=\"True\"", output, StringComparison.Ordinal);
        Assert.DoesNotContain("SourceKey: `button-color-variant`", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## Overview", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## API", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## ControlTheme 结构", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocSectionFiltersMarkdownOutput()
    {
        var result = await DispatchDocAsync(["doc", "Button", "--section", "api"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("# Button", output, StringComparison.Ordinal);
        Assert.Contains("## API", output, StringComparison.Ordinal);
        Assert.Contains("ButtonType", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## Usage", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## Tokens", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocSummaryStyleOmitsLongTailSections()
    {
        var result = await DispatchDocAsync(["doc", "Button", "--style", "summary"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.Contains("## Overview", output, StringComparison.Ordinal);
        Assert.Contains("## Usage", output, StringComparison.Ordinal);
        Assert.Contains("## API", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## Semantic", output, StringComparison.Ordinal);
        Assert.DoesNotContain("## Changelog", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocTopicOutputsTopicDocument()
    {
        var result = await DispatchDocAsync(["doc", "--topic", "design-language"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("# AtomUI Design Language", output, StringComparison.Ordinal);
        Assert.Contains("## Overview", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TokenButtonOutputsControlTokenCatalog()
    {
        var result = await DispatchTokenAsync(["token", "Button"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.Contains("AtomUI Token: Button", output, StringComparison.Ordinal);
        Assert.Contains("Token type: AtomUI.Desktop.Controls.ButtonToken", output, StringComparison.Ordinal);
        Assert.Contains("Resource: ButtonTokenResource / ButtonTokenKind", output, StringComparison.Ordinal);
        var registeredControls = output
            .Split(Environment.NewLine)
            .Single(line => line.TrimStart().StartsWith("Registered controls:", StringComparison.Ordinal));
        Assert.Contains("Button", registeredControls, StringComparison.Ordinal);
        Assert.Contains("IconButton", registeredControls, StringComparison.Ordinal);
        Assert.Contains("HyperLinkButton", registeredControls, StringComparison.Ordinal);
        Assert.Contains("SplitButton", registeredControls, StringComparison.Ordinal);
        Assert.Contains("ToggleIconButton", registeredControls, StringComparison.Ordinal);
        Assert.Contains("Layout", output, StringComparison.Ordinal);
        Assert.Contains("Padding", output, StringComparison.Ordinal);
        Assert.Contains("Description: 按钮内间距", output, StringComparison.Ordinal);
        Assert.Contains("Source: derived from Shared.PaddingContentHorizontal, Shared.LineWidth, Shared.ControlHeight, Button.ContentLineHeight", output, StringComparison.Ordinal);
        Assert.Contains("XAML: {atom:ButtonTokenResource Padding}", output, StringComparison.Ordinal);
        Assert.Contains("{atom:ButtonTokenResource Padding}", output, StringComparison.Ordinal);
        Assert.Contains("Color", output, StringComparison.Ordinal);
        Assert.Contains("DefaultHoverColor", output, StringComparison.Ordinal);
        Assert.Contains("PaddingSM", output, StringComparison.Ordinal);
        Assert.Contains("ContentFontSizeLG", output, StringComparison.Ordinal);
        Assert.Contains("IconOnyPaddingSM", output, StringComparison.Ordinal);
        Assert.Contains("GutterToFlyout", output, StringComparison.Ordinal);
        Assert.Contains("Source: from shared ColorText", output, StringComparison.Ordinal);
        Assert.DoesNotContain("from shared   Button.", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TokenDetailCanExpandChainAndUsage()
    {
        var result = await DispatchTokenAsync(["token", "Button", "Padding", "--chain", "--usage"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.Contains("Button.Padding", output, StringComparison.Ordinal);
        Assert.Contains("Description: 按钮内间距", output, StringComparison.Ordinal);
        Assert.Contains("Type: Thickness", output, StringComparison.Ordinal);
        Assert.Contains("Resource key: ButtonTokenKind.Padding", output, StringComparison.Ordinal);
        Assert.Contains("Customization: public-stable", output, StringComparison.Ordinal);
        Assert.True(
            output.IndexOf("Description: 按钮内间距", StringComparison.Ordinal) <
            output.IndexOf("Type: Thickness", StringComparison.Ordinal));
        Assert.Contains("Depends on:", output, StringComparison.Ordinal);
        Assert.Contains("Shared.PaddingContentHorizontal", output, StringComparison.Ordinal);
        Assert.Contains("Shared.LineWidth", output, StringComparison.Ordinal);
        Assert.Contains("Used by:", output, StringComparison.Ordinal);
        Assert.Contains("ButtonTheme", output, StringComparison.Ordinal);
        Assert.Contains("Setter: Padding", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TokenMarkdownIncludesDescriptionsInTokenTable()
    {
        var result = await DispatchTokenAsync(["--format", "markdown", "token", "Button"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        Assert.StartsWith("# Button Tokens", output, StringComparison.Ordinal);
        Assert.Contains("| Token | Type | Description | Source | XAML |", output, StringComparison.Ordinal);
        Assert.Contains("| Padding | Thickness | 按钮内间距 | derived from Shared.PaddingContentHorizontal, Shared.LineWidth, Shared.ControlHeight, Button.ContentLineHeight | `{atom:ButtonTokenResource Padding}` |", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TokenJsonReturnsStructuredPayload()
    {
        var result = await DispatchTokenAsync(["--format", "json", "token", "Button", "Padding", "--chain", "--usage"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        using var document = JsonDocument.Parse(output);
        var payload = document.RootElement.GetProperty("payload");

        Assert.Equal("1.0", payload.GetProperty("schemaVersion").GetString());
        Assert.Equal("token", payload.GetProperty("command").GetString());
        Assert.Equal("Button", payload.GetProperty("control").GetProperty("name").GetString());
        Assert.Equal("ButtonTokenResource", payload.GetProperty("control").GetProperty("resourceKind").GetString());
        var token = Assert.Single(payload.GetProperty("tokens").EnumerateArray());
        Assert.Equal("Padding", token.GetProperty("name").GetString());
        Assert.Equal("control", token.GetProperty("scope").GetString());
        Assert.Equal("layout", token.GetProperty("category").GetString());
        Assert.Equal("ButtonTokenKind.Padding", token.GetProperty("resource").GetProperty("resourceKey").GetString());
        Assert.Equal("{atom:ButtonTokenResource Padding}", token.GetProperty("resource").GetProperty("markupExtension").GetString());
        Assert.Contains(
            payload.GetProperty("graph").EnumerateArray(),
            item => item.GetProperty("from").GetString() == "Shared.PaddingContentHorizontal"
                    && item.GetProperty("to").GetString() == "Button.Padding");
        Assert.Contains(
            payload.GetProperty("usages").EnumerateArray(),
            item => item.GetProperty("targetProperty").GetString() == "Padding");
        Assert.DoesNotContain("\"payload\":\"", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TokenJsonIncludesAllButtonTokenKindEntries()
    {
        var result = await DispatchTokenAsync(["--format", "json", "token", "Button"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        using var document = JsonDocument.Parse(output);
        var tokens = document.RootElement.GetProperty("payload").GetProperty("tokens").EnumerateArray().ToArray();
        var tokenNames = tokens
            .Select(token => token.GetProperty("name").GetString())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(53, tokens.Length);
        Assert.Contains("BorderColorDisabled", tokenNames);
        Assert.Contains("CirclePadding", tokenNames);
        Assert.Contains("ContentFontSizeLG", tokenNames);
        Assert.Contains("ContentLineHeightSM", tokenNames);
        Assert.Contains("DangerShadow", tokenNames);
        Assert.Contains("DefaultBorderColorDisabled", tokenNames);
        Assert.Contains("ExtraContentMarginLG", tokenNames);
        Assert.Contains("IconOnyPadding", tokenNames);
        Assert.Contains("OnlyIconSizeSM", tokenNames);
        Assert.Contains("TextTextActiveColor", tokenNames);
    }

    [Fact]
    public void TokenSnapshotComesFromBuildTimeSourceExtraction()
    {
        var snapshot = TokenSnapshotRegistry.CreateDefault().Snapshot;

        Assert.NotEqual("snapshot", snapshot.SourceCommit);
        Assert.StartsWith("atomui-token-source-", snapshot.SnapshotId, StringComparison.Ordinal);
        Assert.True(snapshot.SharedTokens.Count > 50);
        Assert.True(snapshot.ControlTokenSets.Count > 20);
        Assert.Contains(snapshot.ControlTokenSets, control => control.ControlName == "Button" && control.Tokens.Count == 53);
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

    private static async Task<ListDispatchResult> DispatchDocAsync(string[] args)
    {
        var output = new RecordingOutputWriter();
        var services = new ServiceCollection()
            .AddSingleton(MetadataCatalog.CreateDefault())
            .AddSingleton<MetadataQueryService>()
            .AddSingleton(DocumentSnapshotRegistry.CreateDefault())
            .AddSingleton<DocumentationQueryService>()
            .AddSingleton<DocumentSectionSelector>()
            .AddSingleton<DocOutputRenderer>()
            .AddTransient<DocCommandHandler>()
            .BuildServiceProvider();
        var descriptor = CliCommandDescriptor.Create<DocCommandOptions, DocCommandHandler>(
            "doc",
            DocCommandOptions.Parse,
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

    private static async Task<ListDispatchResult> DispatchDemoAsync(string[] args)
    {
        var output = new RecordingOutputWriter();
        var services = new ServiceCollection()
            .AddSingleton(MetadataCatalog.CreateDefault())
            .AddSingleton<MetadataQueryService>()
            .AddSingleton(DocumentSnapshotRegistry.CreateDefault())
            .AddSingleton<DemoQueryService>()
            .AddSingleton<DemoOutputRenderer>()
            .AddTransient<DemoCommandHandler>()
            .BuildServiceProvider();
        var descriptor = CliCommandDescriptor.Create<DemoCommandOptions, DemoCommandHandler>(
            "demo",
            DemoCommandOptions.Parse,
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

    private static async Task<ListDispatchResult> DispatchTokenAsync(string[] args)
    {
        var output = new RecordingOutputWriter();
        var services = new ServiceCollection()
            .AddSingleton(MetadataCatalog.CreateDefault())
            .AddSingleton<MetadataQueryService>()
            .AddSingleton(TokenSnapshotRegistry.CreateDefault())
            .AddSingleton<TokenQueryService>()
            .AddSingleton<TokenOutputRenderer>()
            .AddTransient<TokenCommandHandler>()
            .BuildServiceProvider();
        var descriptor = CliCommandDescriptor.Create<TokenCommandOptions, TokenCommandHandler>(
            "token",
            TokenCommandOptions.Parse,
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
