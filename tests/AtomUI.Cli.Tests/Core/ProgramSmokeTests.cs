using AtomUI.Cli.Entry;
using System.Diagnostics;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class ProgramSmokeTests
{
    [Fact]
    public async Task MainReturnsZeroForVersion()
    {
        var exitCode = await Program.Main(["--version"]);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task MainReturnsArgumentExitCodeForUnknownCommand()
    {
        var exitCode = await Program.Main(["missing"]);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task PublishedDllEntrypointReturnsForVersion()
    {
        var cliAssembly = Path.Combine(AppContext.BaseDirectory, "AtomUI.Cli.dll");
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { cliAssembly, "--version" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });

        Assert.NotNull(process);

        var exited = await WaitForExitAsync(process, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
        }

        Assert.True(exited);
        var stdout = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, process.ExitCode);
        Assert.Equal($"1.0.0-alpha.1{Environment.NewLine}", stdout);
        Assert.True(string.IsNullOrWhiteSpace(stderr));
    }

    [Fact]
    public async Task PublishedDllEntrypointHelpShowsUsefulSections()
    {
        var result = await RunCliProcessAsync(["help"], TestContext.Current.CancellationToken);

        Assert.True(result.Exited);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("AtomUI Cli", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Version:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Copyright (c) 2018-2026 Qinware Technologies Co., Ltd. All rights reserved.", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Usage:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Common commands:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Command groups:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Global options:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("More:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("info <control>", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Show AtomUI control information", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublishedDllEntrypointHelpCommandShowsCommandDetails()
    {
        var result = await RunCliProcessAsync(["help", "info"], TestContext.Current.CancellationToken);

        Assert.True(result.Exited);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("info", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Show AtomUI control information", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Usage:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("dotnet atomui info <control> [options]", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Arguments:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Options:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Examples:", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublishedDllEntrypointCommandHelpShortcutShowsCommandDetails()
    {
        var result = await RunCliProcessAsync(["info", "--help"], TestContext.Current.CancellationToken);

        Assert.True(result.Exited);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("dotnet atomui info <control> [options]", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Examples:", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublishedDllEntrypointHelpJsonReturnsStructuredPayload()
    {
        var result = await RunCliProcessAsync(["help", "--format", "json"], TestContext.Current.CancellationToken);

        Assert.True(result.Exited);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("\"payload\":{", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("\"version\":\"1.0.0-alpha.1\"", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("\"copyright\":\"Copyright (c) 2018-2026 Qinware Technologies Co., Ltd. All rights reserved.\"", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("\"commonCommands\"", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("\"groups\"", result.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("\"payload\":\"", result.Stdout, StringComparison.Ordinal);
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var waitTask = process.WaitForExitAsync(cancellationToken);
        var completed = await Task.WhenAny(
            waitTask,
            Task.Delay(timeout, cancellationToken));

        return completed == waitTask;
    }

    private static async Task<CliProcessResult> RunCliProcessAsync(string[] args, CancellationToken cancellationToken)
    {
        var cliAssembly = Path.Combine(AppContext.BaseDirectory, "AtomUI.Cli.dll");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add(cliAssembly);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);

        var exited = await WaitForExitAsync(process, TimeSpan.FromSeconds(5), cancellationToken);
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
        }

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        return new CliProcessResult(exited, exited ? process.ExitCode : -1, stdout, stderr);
    }

    private sealed record CliProcessResult(bool Exited, int ExitCode, string Stdout, string Stderr);
}
