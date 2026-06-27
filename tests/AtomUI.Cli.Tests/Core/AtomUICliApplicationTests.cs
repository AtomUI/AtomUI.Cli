using AtomUI.Cli;
using AtomUI.Cli.Hosting;
using AtomUI.Cli.Modularity;
using AtomUI.Modularity;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class AtomUICliApplicationTests
{
    [Fact]
    public async Task RunAsyncInitializesHostDispatchesCommandAndShutsDownModules()
    {
        var recorder = new ApplicationRecorder();
        var module = new RecordingModule(recorder);
        var application = AtomUICliApplication
            .CreateBuilder(["fake", "Button"])
            .AddModuleRegistration(CreateRegistration(module), typeof(RecordingModule))
            .Build();

        var exitCode = await application.RunAsync(["fake", "Button"], TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal("Button", recorder.HandledValue);
        Assert.Equal(1, recorder.Initialized);
        Assert.Equal(1, recorder.Shutdown);
    }

    [Fact]
    public async Task RunAsyncMapsParserFailureAndStillShutsDownModules()
    {
        var recorder = new ApplicationRecorder();
        var module = new RecordingModule(recorder);
        var application = AtomUICliApplication
            .CreateBuilder(["missing"])
            .AddModuleRegistration(CreateRegistration(module), typeof(RecordingModule))
            .Build();

        var exitCode = await application.RunAsync(["missing"], TestContext.Current.CancellationToken);

        Assert.Equal(2, exitCode);
        Assert.Null(recorder.HandledValue);
        Assert.Equal(1, recorder.Shutdown);
    }

    private static ModuleRegistration CreateRegistration(RecordingModule module)
    {
        return ModuleRegistration.For(
            ModuleDescriptor.For<RecordingModule>("Recording"),
            () => module);
    }

    private sealed class RecordingModule(ApplicationRecorder recorder) : AtomUICliModule
    {
        public override void ConfigureServices(ModuleServiceConfigurationContext context)
        {
            context.Services.AddSingleton(recorder);
            context.Services.AddTransient<FakeCommandHandler, FakeCommandHandler>();
        }

        public override ValueTask ConfigureAtomUICliCommandsAsync(
            AtomUICliCommandContributionContext context,
            CancellationToken cancellationToken = default)
        {
            context.Add<FakeCommandOptions, FakeCommandHandler>(
                "fake",
                (global, args) => new FakeCommandOptions(global, args.SingleOrDefault() ?? string.Empty),
                CommandGroup.Knowledge,
                new HashSet<OutputFormat> { OutputFormat.Text });

            return ValueTask.CompletedTask;
        }

        public override void Initialize(ModuleInitializationContext context)
        {
            recorder.Initialized++;
        }

        public override void Shutdown(ModuleShutdownContext context)
        {
            recorder.Shutdown++;
        }
    }

    private sealed record FakeCommandOptions(GlobalCliOptions Global, string Value) : IAtomUICliCommandOptions;

    private sealed class FakeCommandHandler(ApplicationRecorder recorder) : IAtomUICliCommandHandler<FakeCommandOptions>
    {
        public ValueTask<AtomUICliResult> ExecuteAsync(
            FakeCommandOptions options,
            CliInvocationContext context,
            CancellationToken cancellationToken)
        {
            recorder.HandledValue = options.Value;
            return ValueTask.FromResult(AtomUICliResult.Success());
        }
    }

    private sealed class ApplicationRecorder
    {
        public string? HandledValue { get; set; }

        public int Initialized { get; set; }

        public int Shutdown { get; set; }
    }
}
