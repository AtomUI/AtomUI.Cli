using AtomUI.Cli.MetadataBuilder.SourceAnalysis;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Projection;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Writers;

namespace AtomUI.Cli.MetadataBuilder;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = BuilderOptions.Parse(args);
            var sourceRoot = Path.GetFullPath(options.SourceRoot);
            if (!Directory.Exists(sourceRoot))
            {
                throw new DirectoryNotFoundException($"AtomUI source root '{options.SourceRoot}' does not exist. Configure --source-root, AtomUIDocSourceRoot, or ATOMUI_SOURCE_ROOT.");
            }

            var context = new SourceAnalysisContext(
                new SourceIdentity("default-reference-project", options.SourceRef, ResolveSourceCommit(sourceRoot), options.TargetVersion, "1.0"),
                new SourcePathIndex(sourceRoot));
            var registry = new SourceAnalysisProcessorRegistry();
            registry.Add(new GalleryCatalogProcessor());
            registry.Add(new PackageCatalogProcessor());
            registry.Add(new MarkdownDocProcessor());
            registry.Add(new ChangelogProcessor());
            registry.Add(new GalleryShowCaseProcessor());
            registry.Add(new ControlSourceProcessor());
            registry.Add(new ControlThemeProcessor());
            registry.Add(new SemanticContractProcessor());
            registry.Add(new TokenProcessor());

            var plan = registry.CreatePlan(MapRequiredFeatures(options.RequiredSnapshots));
            new SourceAnalysisPipeline()
                .ExecuteAsync(plan, context, CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            if (options.RequiredSnapshots.Contains("token"))
            {
                var snapshot = TokenSnapshotProjection.Create(context);
                TokenSnapshotCodeWriter.Write(snapshot, Path.GetFullPath(options.TokenOutputPath));
                Console.WriteLine($"Generated AtomUI token snapshot: {snapshot.ControlTokenSets.Count} control token sets, {snapshot.SharedTokens.Count} shared tokens.");
            }

            if (options.RequiredSnapshots.Contains("semantic"))
            {
                var snapshot = SemanticSnapshotProjection.Create(context);
                var outputPath = Path.GetFullPath(Path.Combine(options.OutputRoot, "BuiltInSemanticSnapshot.g.cs"));
                SemanticSnapshotCodeWriter.Write(snapshot, outputPath);
                Console.WriteLine($"Generated AtomUI semantic snapshot: {snapshot.Controls.Count} controls.");
            }

            if (options.RequiredSnapshots.Contains("catalog"))
            {
                var snapshot = CatalogSnapshotProjection.Create(context);
                var outputPath = Path.GetFullPath(Path.Combine(options.OutputRoot, "BuiltInMetadataCatalog.g.cs"));
                CatalogSnapshotCodeWriter.Write(snapshot, outputPath);
                Console.WriteLine($"Generated AtomUI catalog snapshot: {snapshot.Controls.Count} controls.");
            }

            if (options.RequiredSnapshots.Contains("document"))
            {
                var snapshot = DocumentSnapshotProjection.Create(context);
                var outputPath = Path.GetFullPath(Path.Combine(options.OutputRoot, "BuiltInDocumentSnapshot.g.cs"));
                DocumentSnapshotCodeWriter.Write(snapshot, outputPath);
                Console.WriteLine($"Generated AtomUI document snapshot: {snapshot.Controls.Count} controls.");
            }

            if (options.RequiredSnapshots.Contains("package"))
            {
                var snapshot = PackageSnapshotProjection.Create(context);
                var outputPath = Path.GetFullPath(Path.Combine(options.OutputRoot, "BuiltInPackageSnapshot.g.cs"));
                PackageSnapshotCodeWriter.Write(snapshot, outputPath);
                Console.WriteLine($"Generated AtomUI package snapshot: {snapshot.Packages.Count} packages.");
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"AtomUI source analysis snapshot generation failed: {exception.Message}");
            return 1;
        }
    }

    private static IReadOnlySet<SourceAnalysisFeature> MapRequiredFeatures(IReadOnlySet<string> requiredSnapshots)
    {
        var features = new HashSet<SourceAnalysisFeature>();
        foreach (var snapshot in requiredSnapshots)
        {
            if (snapshot.Equals("token", StringComparison.OrdinalIgnoreCase))
            {
                features.Add(SourceAnalysisFeature.TokenDefinition);
                features.Add(SourceAnalysisFeature.TokenGraph);
                features.Add(SourceAnalysisFeature.TokenUsage);
            }
            else if (snapshot.Equals("semantic", StringComparison.OrdinalIgnoreCase))
            {
                features.Add(SourceAnalysisFeature.SemanticContract);
            }
            else if (snapshot.Equals("catalog", StringComparison.OrdinalIgnoreCase))
            {
                features.Add(SourceAnalysisFeature.ControlCatalog);
                features.Add(SourceAnalysisFeature.PackageCatalog);
                features.Add(SourceAnalysisFeature.ProductCatalog);
                features.Add(SourceAnalysisFeature.GalleryNavigation);
                features.Add(SourceAnalysisFeature.MarkdownDoc);
                features.Add(SourceAnalysisFeature.Changelog);
            }
            else if (snapshot.Equals("package", StringComparison.OrdinalIgnoreCase))
            {
                features.Add(SourceAnalysisFeature.ControlCatalog);
                features.Add(SourceAnalysisFeature.PackageCatalog);
                features.Add(SourceAnalysisFeature.ProductCatalog);
                features.Add(SourceAnalysisFeature.CommercialVisibility);
            }
            else if (snapshot.Equals("document", StringComparison.OrdinalIgnoreCase))
            {
                features.Add(SourceAnalysisFeature.ControlCatalog);
                features.Add(SourceAnalysisFeature.GalleryNavigation);
                features.Add(SourceAnalysisFeature.GalleryDemo);
                features.Add(SourceAnalysisFeature.ControlApi);
                features.Add(SourceAnalysisFeature.ControlTheme);
                features.Add(SourceAnalysisFeature.SemanticContract);
                features.Add(SourceAnalysisFeature.TokenDefinition);
                features.Add(SourceAnalysisFeature.TokenGraph);
                features.Add(SourceAnalysisFeature.TokenUsage);
                features.Add(SourceAnalysisFeature.MarkdownDoc);
            }
            else
            {
                throw new InvalidOperationException($"Unknown snapshot '{snapshot}'.");
            }
        }

        return features;
    }

    private static string ResolveSourceCommit(string sourceRoot)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo("git", "rev-parse --short=12 HEAD")
            {
                WorkingDirectory = sourceRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process is null)
            {
                return "unknown";
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(3000);
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}
