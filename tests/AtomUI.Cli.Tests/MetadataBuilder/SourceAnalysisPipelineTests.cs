using AtomUI.Cli.MetadataBuilder;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Projection;
using AtomUI.Cli.Hosting.Metadata;
using Xunit;

namespace AtomUI.Cli.Tests.MetadataBuilder;

public sealed class SourceAnalysisPipelineTests
{
    [Fact]
    public void RegistryPlansProcessorsByRequiredFeatures()
    {
        var registry = new SourceAnalysisProcessorRegistry();
        registry.Add(new FakeProcessor(
            "workspace",
            SourceAnalysisPhase.ReadWorkspace,
            FeatureSet(),
            FeatureSet(SourceAnalysisFeature.Workspace)));
        registry.Add(new FakeProcessor(
            "control-source",
            SourceAnalysisPhase.ExtractFacts,
            FeatureSet(SourceAnalysisFeature.Workspace),
            FeatureSet(SourceAnalysisFeature.ControlApi, SourceAnalysisFeature.TemplatePart)));

        var plan = registry.CreatePlan(FeatureSet(SourceAnalysisFeature.TemplatePart));

        Assert.Equal(["workspace", "control-source"], plan.Processors.Select(item => item.Id).ToArray());
    }

    private static IReadOnlySet<SourceAnalysisFeature> FeatureSet(params SourceAnalysisFeature[] features)
    {
        return features.ToHashSet();
    }

    [Fact]
    public void FactStoreKeepsMultipleContributionsForSameKey()
    {
        var store = new SourceFactStore();
        store.Add("semantic-part:Button:PART_LoadingIcon", "control-source", new { Name = "PART_LoadingIcon" });
        store.Add("semantic-part:Button:PART_LoadingIcon", "control-theme", new { NodeType = "LoadingOutlined" });

        var facts = store.Get("semantic-part:Button:PART_LoadingIcon");

        Assert.Equal(2, facts.Count);
        Assert.Contains(facts, fact => fact.ProcessorId == "control-source");
        Assert.Contains(facts, fact => fact.ProcessorId == "control-theme");
    }

    [Fact]
    public void BuilderOptionsParseUnifiedSourceArguments()
    {
        var options = BuilderOptions.Parse([
            "--source-root", ".workspace/AtomUI",
            "--target-version", "6.0",
            "--source-ref", "release/6.0",
            "--output-root", "output/obj/generated"
        ]);

        Assert.Equal(".workspace/AtomUI", options.SourceRoot);
        Assert.Equal("6.0", options.TargetVersion);
        Assert.Equal("release/6.0", options.SourceRef);
        Assert.Equal("output/obj/generated", options.OutputRoot);
    }

    [Fact]
    public void RuntimeMetadataQueryServiceDoesNotContainControlSpecificInfoFactories()
    {
        var repoRoot = ResolveRepoRoot();
        var path = Path.Combine(repoRoot, "src", "AtomUI.Cli.Hosting", "Metadata", "MetadataQueryServices.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("CreateButtonInfoPayload", text, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateDataGridInfoPayload", text, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateFallbackInfoPayload", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeDocumentSnapshotRegistryDoesNotContainGeneratedDocumentFacts()
    {
        var repoRoot = ResolveRepoRoot();
        var path = Path.Combine(repoRoot, "src", "AtomUI.Cli.Hosting", "Metadata", "DocumentSnapshotRegistry.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("CreateBuildTimeSnapshots", text, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateButton(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateDataGrid(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateButtonExamples", text, StringComparison.Ordinal);
        Assert.DoesNotContain("builtin-doc-snapshot", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeMetadataFallbackDoesNotContainRealSourceFacts()
    {
        var method = typeof(MetadataCatalog).GetMethod(
            "CreateFallback",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var fallback = Assert.IsType<MetadataCatalog>(method.Invoke(null, null));

        Assert.Equal("runtime-empty-fallback", fallback.SourceCommit);
        Assert.Empty(fallback.Products);
        Assert.Empty(fallback.Packages);
        Assert.Empty(fallback.Categories);
        Assert.Empty(fallback.Controls);
        Assert.Empty(fallback.Tokens);
        Assert.Empty(fallback.Demos);
        Assert.Empty(fallback.Documents);
        Assert.Empty(fallback.SemanticParts);
        Assert.Empty(fallback.Changelog);
    }

    [Fact]
    public void RuntimeMetadataSnapshotDoesNotContainStaticControlCatalogFactories()
    {
        var repoRoot = ResolveRepoRoot();
        var path = Path.Combine(repoRoot, "src", "AtomUI.Cli.Hosting", "Metadata", "MetadataSnapshot.cs");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("CreateCategories", text, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateControls", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Basic Button", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Initial Button metadata", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Use DataGrid for tabular data", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GalleryShowCaseProcessorExtractsButtonExamplesFromSource()
    {
        var sourceRoot = MetadataBuilderTestPaths.ResolveAtomUISourceRoot();
        var context = MetadataBuilderTestPaths.CreateContext(sourceRoot);

        await new GalleryShowCaseProcessor().ExecuteAsync(context, TestContext.Current.CancellationToken);

        var examples = context.Facts.GetByPrefix("gallery-demo:Button:")
            .Select(fact => fact.Value)
            .OfType<GalleryDemoFact>()
            .ToArray();
        var loading = Assert.Single(examples, example => example.SourceKey == "button-loading");
        Assert.Equal("Button", loading.ControlName);
        Assert.Equal("state", loading.Kind);
        Assert.Contains("IsLoading=\"True\"", loading.XamlSnippet, StringComparison.Ordinal);
        Assert.Contains("controlgallery/AtomUIGallery/ShowCases/General/Button/Views/ButtonShowCase.axaml", loading.SourceLocation.Path, StringComparison.Ordinal);
        Assert.Contains(examples, example => example.SourceKey == "button-color-variant");
    }

    [Fact]
    public async Task CatalogProjectionUsesSourceProcessorsForPackagesDocumentsAndChangelog()
    {
        var sourceRoot = MetadataBuilderTestPaths.ResolveAtomUISourceRoot();
        var context = MetadataBuilderTestPaths.CreateContext(sourceRoot);
        var registry = new SourceAnalysisProcessorRegistry();
        registry.Add(new GalleryCatalogProcessor());
        registry.Add(new PackageCatalogProcessor());
        registry.Add(new MarkdownDocProcessor());
        registry.Add(new ChangelogProcessor());

        var plan = registry.CreatePlan(FeatureSet(
            SourceAnalysisFeature.ControlCatalog,
            SourceAnalysisFeature.ProductCatalog,
            SourceAnalysisFeature.MarkdownDoc,
            SourceAnalysisFeature.Changelog));

        await new SourceAnalysisPipeline().ExecuteAsync(plan, context, TestContext.Current.CancellationToken);

        var catalog = CatalogSnapshotProjection.Create(context);
        Assert.Contains(catalog.Controls, control => control.Name == "Button");

        var desktopPackage = Assert.Single(catalog.Packages, package => package.Id == "AtomUI.Desktop.Controls");
        Assert.Equal("desktop", desktopPackage.ProductId);
        Assert.Equal("6.0.7", desktopPackage.Version);
        Assert.Contains("Button", desktopPackage.Controls);

        var design = Assert.Single(catalog.Documents, document => document.Kind == "topic" && document.TargetId == "design-language");
        Assert.Contains("# AtomUI 文档总览", design.Markdown, StringComparison.Ordinal);
        Assert.Contains("AtomUI 控件 Token 设计规范", design.Markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("Prefer clear hierarchy and tokenized styling", design.Markdown, StringComparison.Ordinal);

        Assert.Contains(catalog.Changelog, entry =>
            entry.Version == "6.0.6"
            && entry.TargetKind == "release"
            && entry.TargetId == "AtomUI"
            && entry.Message.Contains("customizable size support", StringComparison.Ordinal));
        Assert.DoesNotContain(catalog.Changelog, entry => entry.Message.Contains("Initial Button metadata", StringComparison.Ordinal));
    }

    private static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "AtomUICli.slnx");
            if (File.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Cannot locate AtomUICli repository root from test output.");
    }

    private sealed class FakeProcessor(
        string id,
        SourceAnalysisPhase phase,
        IReadOnlySet<SourceAnalysisFeature> requires,
        IReadOnlySet<SourceAnalysisFeature> provides) : ISourceAnalysisProcessor
    {
        public string Id => id;
        public SourceAnalysisPhase Phase => phase;
        public IReadOnlySet<SourceAnalysisFeature> Requires => requires;
        public IReadOnlySet<SourceAnalysisFeature> Provides => provides;

        public ValueTask ExecuteAsync(SourceAnalysisContext context, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
