using AtomUI.Cli;
using AtomUI.Cli.Hosting;
using AtomUI.Cli.Hosting.Commands;
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
            .AddCommandManifest(CreateFakeManifest())
            .Build();

        var exitCode = await application.RunAsync(["fake", "Button"], TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal("Button", recorder.HandledValue);
        Assert.Equal(1, recorder.Initialized);
        Assert.Equal(1, recorder.Shutdown);
    }

    [Fact]
    public async Task RunAsyncActivatesOnlyCommandOwnerModules()
    {
        var recorder = new ApplicationRecorder();
        var application = AtomUICliApplication
            .CreateBuilder(["fake", "Button"])
            .AddModuleRegistration(CreateRecordingFactoryRegistration(recorder), typeof(RecordingModule))
            .AddModuleRegistration(CreateUnusedFactoryRegistration(recorder), typeof(UnusedModule))
            .AddCommandManifest(CreateFakeManifest())
            .Build();

        var exitCode = await application.RunAsync(["fake", "Button"], TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal("Button", recorder.HandledValue);
        Assert.Equal(1, recorder.RecordingCreated);
        Assert.Equal(1, recorder.Initialized);
        Assert.Equal(1, recorder.Shutdown);
        Assert.Equal(0, recorder.UnusedCreated);
        Assert.Equal(0, recorder.UnusedInitialized);
        Assert.Equal(0, recorder.UnusedShutdown);
    }

    [Fact]
    public async Task RunAsyncMapsPreParseFailureWithoutCreatingModules()
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
        Assert.Equal(0, recorder.Initialized);
        Assert.Equal(0, recorder.Shutdown);
    }

    [Fact]
    public async Task RunAsyncCommandHelpShortcutDoesNotActivateTargetModule()
    {
        var recorder = new ApplicationRecorder();
        var application = AtomUICliApplication
            .CreateBuilder(["fake", "--help"])
            .AddModuleRegistration(CreateRecordingFactoryRegistration(recorder), typeof(RecordingModule))
            .AddCommandManifest(CreateFakeManifest())
            .Build();

        var exitCode = await application.RunAsync(["fake", "--help"], TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Null(recorder.HandledValue);
        Assert.Equal(0, recorder.RecordingCreated);
        Assert.Equal(0, recorder.Initialized);
        Assert.Equal(0, recorder.Shutdown);
    }

    private static ModuleRegistration CreateRegistration(RecordingModule module)
    {
        return ModuleRegistration.For(
            ModuleDescriptor.For<RecordingModule>("Recording"),
            () => module);
    }

    private static ModuleRegistration CreateRecordingFactoryRegistration(ApplicationRecorder recorder)
    {
        return ModuleRegistration.For(
            ModuleDescriptor.For<RecordingModule>("Recording"),
            () => new RecordingModule(recorder, countCreation: true));
    }

    private static ModuleRegistration CreateUnusedFactoryRegistration(ApplicationRecorder recorder)
    {
        return ModuleRegistration.For(
            ModuleDescriptor.For<UnusedModule>("Unused"),
            () => new UnusedModule(recorder, countCreation: true));
    }

    private static CommandManifest CreateFakeManifest()
    {
        return new CommandManifest(
            "fake",
            typeof(RecordingModule),
            CommandGroup.Knowledge,
            new HashSet<OutputFormat> { OutputFormat.Text });
    }

    private sealed class RecordingModule(ApplicationRecorder recorder) : AtomUICliModule
    {
        public RecordingModule()
            : this(new ApplicationRecorder())
        {
        }

        public RecordingModule(ApplicationRecorder recorder, bool countCreation = true)
            : this(recorder)
        {
            if (countCreation)
            {
                recorder.RecordingCreated++;
            }
        }

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

    private sealed class UnusedModule(ApplicationRecorder recorder) : AtomUICliModule
    {
        public UnusedModule()
            : this(new ApplicationRecorder())
        {
        }

        public UnusedModule(ApplicationRecorder recorder, bool countCreation = true)
            : this(recorder)
        {
            if (countCreation)
            {
                recorder.UnusedCreated++;
            }
        }

        public override void Initialize(ModuleInitializationContext context)
        {
            recorder.UnusedInitialized++;
        }

        public override void Shutdown(ModuleShutdownContext context)
        {
            recorder.UnusedShutdown++;
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

        public int RecordingCreated { get; set; }

        public int UnusedCreated { get; set; }

        public int UnusedInitialized { get; set; }

        public int UnusedShutdown { get; set; }
    }
}
