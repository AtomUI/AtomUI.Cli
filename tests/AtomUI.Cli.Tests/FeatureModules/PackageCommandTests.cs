using System.Text.Json;
using AtomUI.Cli;
using AtomUI.Cli.Hosting.Commands;
using AtomUI.Cli.Hosting.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AtomUI.Cli.Tests.FeatureModules;

public sealed class PackageCommandTests
{
    [Fact]
    public async Task PackageListJsonReturnsStructuredPayload()
    {
        var result = await DispatchPackageAsync(["--format", "json", "package"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);
        using var document = JsonDocument.Parse(output);
        var payload = document.RootElement.GetProperty("payload");

        Assert.Equal(JsonValueKind.Object, payload.ValueKind);
        Assert.Equal("package", payload.GetProperty("command").GetString());
        Assert.True(payload.GetProperty("packages").GetArrayLength() >= 3);
        Assert.DoesNotContain("\"payload\":\"", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PackageProductTextOutputsProductPackages()
    {
        var result = await DispatchPackageAsync(["package", "desktop"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);

        Assert.Contains("Product: desktop", output, StringComparison.Ordinal);
        Assert.Contains("AtomUI.Desktop.Controls", output, StringComparison.Ordinal);
        Assert.Contains("Required packages:", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PackageInvalidIncludeReturnsArgumentError()
    {
        var result = await DispatchPackageAsync(["package", "--include", "bad"]);

        Assert.False(result.Result.IsSuccess);
        Assert.Equal(AtomUICliErrorCodes.ArgumentInvalidValue, result.Result.Error?.Code);
        Assert.Empty(result.Output.Lines);
    }

    [Fact]
    public async Task PackageDetailTextExpandsSelectedIncludes()
    {
        var result = await DispatchPackageAsync(["package", "AtomUI.Desktop.Controls", "--include", "dependencies,registration,controls"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);

        Assert.Contains("Dependencies:", output, StringComparison.Ordinal);
        Assert.Contains("AtomUI.Controls", output, StringComparison.Ordinal);
        Assert.Contains("Avalonia.X11 12.0.5", output, StringComparison.Ordinal);
        Assert.Contains("Registration:", output, StringComparison.Ordinal);
        Assert.Contains("UseAtomUI", output, StringComparison.Ordinal);
        Assert.Contains("Controls:", output, StringComparison.Ordinal);
        Assert.Contains("Button", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PackageMarkdownOutputsPackageDocument()
    {
        var result = await DispatchPackageAsync(["--format", "markdown", "package", "AtomUI.Desktop.Controls", "--include", "dependencies,registration,controls"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);

        Assert.StartsWith("# AtomUI Package: AtomUI.Desktop.Controls", output, StringComparison.Ordinal);
        Assert.Contains("## Dependencies", output, StringComparison.Ordinal);
        Assert.Contains("## Registration", output, StringComparison.Ordinal);
        Assert.Contains("## Controls", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PackageCommercialFilterListsCommercialPackages()
    {
        var result = await DispatchPackageAsync(["package", "--commercial"]);

        Assert.True(result.Result.IsSuccess);
        var output = Assert.Single(result.Output.Lines);

        Assert.Contains("AtomUI.Desktop.Controls.DataGrid", output, StringComparison.Ordinal);
        Assert.Contains("AtomUI.Desktop.Controls.ColorPicker", output, StringComparison.Ordinal);
        Assert.DoesNotContain("AtomUI.Desktop.Controls              ", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PackageMissingTargetVersionReturnsDataError()
    {
        var result = await DispatchPackageAsync(["--target-version", "9.0", "package"]);

        Assert.False(result.Result.IsSuccess);
        Assert.Equal(AtomUICliErrorCodes.DataVersionUnresolved, result.Result.Error?.Code);
        Assert.Empty(result.Output.Lines);
    }

    [Fact]
    public void PackageSnapshotRegistryUsesBuildTimeFactory()
    {
        var snapshot = Assert.Single(PackageSnapshotRegistry.CreateDefault().Snapshots);

        Assert.Equal("6.0", snapshot.TargetVersion);
        Assert.StartsWith("atomui-package-source-", snapshot.SnapshotId, StringComparison.Ordinal);
        Assert.Contains(snapshot.Packages, package => package.Id == "AtomUI.Desktop.Controls");
        Assert.Contains("net10.0", snapshot.Packages.Single(package => package.Id == "AtomUI.Desktop.Controls").TargetFrameworks);
        Assert.Contains(
            snapshot.Packages.Single(package => package.Id == "AtomUI.Desktop.Controls").Dependencies,
            dependency => dependency.Id == "Avalonia.X11" && dependency.Version == "12.0.5");
    }

    private static async Task<PackageDispatchResult> DispatchPackageAsync(string[] args)
    {
        var output = new RecordingOutputWriter();
        var services = new ServiceCollection()
            .AddSingleton(PackageSnapshotRegistry.CreateDefault())
            .AddSingleton<PackageQueryService>()
            .AddSingleton<PackageOutputRenderer>()
            .AddTransient<PackageCommandHandler>()
            .BuildServiceProvider();
        var descriptor = CliCommandDescriptor.Create<PackageCommandOptions, PackageCommandHandler>(
            "package",
            PackageCommandOptions.Parse,
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

        return new PackageDispatchResult(result, output);
    }

    private sealed record PackageDispatchResult(AtomUICliResult Result, RecordingOutputWriter Output);

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
