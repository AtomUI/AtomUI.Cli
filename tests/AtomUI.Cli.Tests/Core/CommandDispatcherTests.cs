using AtomUI.Cli;
using AtomUI.Cli.Hosting.Commands;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class CommandDispatcherTests
{
    [Fact]
    public async Task DispatchCreatesScopeAndInvokesHandlerWithParsedOptions()
    {
        var recorder = new HandlerRecorder();
        var services = new ServiceCollection()
            .AddSingleton(recorder)
            .AddTransient<FakeCommandHandler>()
            .BuildServiceProvider();

        var descriptor = CliCommandDescriptor.Create<FakeCommandOptions, FakeCommandHandler>(
            "fake",
            (global, args) => new FakeCommandOptions(global, args.SingleOrDefault() ?? string.Empty),
            CommandGroup.Knowledge,
            new HashSet<OutputFormat> { OutputFormat.Text });
        var catalog = CliCommandDescriptorCatalog.Create([descriptor]);
        var dispatcher = new CliCommandDispatcher(new CliCommandParser(), services.GetRequiredService<IServiceScopeFactory>());

        var result = await dispatcher.DispatchAsync(["fake", "Button"], catalog, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Button", recorder.Value);
        Assert.Equal("fake", recorder.CommandName);
    }

    [Fact]
    public async Task DispatchReturnsParserFailureWithoutCreatingHandler()
    {
        var recorder = new HandlerRecorder();
        var services = new ServiceCollection()
            .AddSingleton(recorder)
            .AddTransient<FakeCommandHandler>()
            .BuildServiceProvider();
        var dispatcher = new CliCommandDispatcher(new CliCommandParser(), services.GetRequiredService<IServiceScopeFactory>());

        var result = await dispatcher.DispatchAsync(["missing"], CliCommandDescriptorCatalog.Empty, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(AtomUICliErrorCodes.ArgumentCommandNotFound, result.Error?.Code);
        Assert.Equal(0, recorder.CallCount);
    }

    private sealed record FakeCommandOptions(GlobalCliOptions Global, string Value) : IAtomUICliCommandOptions;

    private sealed class FakeCommandHandler(HandlerRecorder recorder) : IAtomUICliCommandHandler<FakeCommandOptions>
    {
        public ValueTask<AtomUICliResult> ExecuteAsync(
            FakeCommandOptions options,
            CliInvocationContext context,
            CancellationToken cancellationToken)
        {
            recorder.CallCount++;
            recorder.Value = options.Value;
            recorder.CommandName = context.CommandName;
            return ValueTask.FromResult(AtomUICliResult.Success());
        }
    }

    private sealed class HandlerRecorder
    {
        public int CallCount { get; set; }

        public string? Value { get; set; }

        public string? CommandName { get; set; }
    }
}
