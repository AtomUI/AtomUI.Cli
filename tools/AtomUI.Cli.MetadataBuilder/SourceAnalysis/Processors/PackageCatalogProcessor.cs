using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;

internal sealed partial class PackageCatalogProcessor : ISourceAnalysisProcessor
{
    public string Id => "package-catalog";

    public IReadOnlySet<SourceAnalysisFeature> Provides { get; } =
        new HashSet<SourceAnalysisFeature>
        {
            SourceAnalysisFeature.ProductCatalog,
            SourceAnalysisFeature.CommercialVisibility
        };

    public IReadOnlySet<SourceAnalysisFeature> Requires { get; } =
        new HashSet<SourceAnalysisFeature>
        {
            SourceAnalysisFeature.ControlCatalog
        };

    public SourceAnalysisPhase Phase => SourceAnalysisPhase.ExtractFacts;

    public ValueTask ExecuteAsync(SourceAnalysisContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var controls = context.Facts.GetByPrefix("control:")
            .Select(fact => fact.Value)
            .OfType<ExtractedCatalogControl>()
            .ToArray();
        var version = ResolveAtomUIVersion(context.Paths.SourceRoot);
        var packageProjects = EnumerateControlPackageProjects(context.Paths.SourceRoot)
            .Select(path => CreatePackage(context, path, version, controls))
            .Where(package => package is not null)
            .Cast<ExtractedCatalogPackage>()
            .OrderBy(package => package.Id, StringComparer.Ordinal)
            .ToArray();

        foreach (var product in packageProjects
                     .Select(package => CreateProduct(context.Paths.SourceRoot, package))
                     .DistinctBy(product => product.Id)
                     .OrderBy(product => product.Id, StringComparer.Ordinal))
        {
            context.Facts.Add($"product:{product.Id}", Id, product);
        }

        foreach (var package in packageProjects)
        {
            context.Facts.Add($"package:{package.Id}", Id, package);
        }

        return ValueTask.CompletedTask;
    }

    private static IEnumerable<string> EnumerateControlPackageProjects(string sourceRoot)
    {
        var srcRoot = Path.Combine(sourceRoot, "src");
        if (!Directory.Exists(srcRoot))
        {
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(srcRoot, "AtomUI.Desktop.Controls*.csproj", SearchOption.AllDirectories))
        {
            yield return path;
        }
    }

    private static ExtractedCatalogPackage? CreatePackage(
        SourceAnalysisContext context,
        string projectPath,
        string version,
        IReadOnlyList<ExtractedCatalogControl> controls)
    {
        var packageId = ResolvePackageId(projectPath);
        var packageControls = controls
            .Where(control => control.PackageId.Equals(packageId, StringComparison.Ordinal))
            .Select(control => control.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (packageControls.Length == 0)
        {
            return null;
        }

        var productId = ResolveProductId(packageId);
        return new ExtractedCatalogPackage(
            packageId,
            productId,
            version,
            ResolvePackageDescription(context.Paths.SourceRoot, productId, packageId),
            packageControls,
            IsOptional: !productId.Equals("desktop", StringComparison.Ordinal),
            IsCommercial: productId.Equals("datagrid", StringComparison.Ordinal));
    }

    private static ExtractedCatalogProduct CreateProduct(string sourceRoot, ExtractedCatalogPackage package)
    {
        return new ExtractedCatalogProduct(
            package.ProductId,
            ResolveProductName(package.Id),
            ResolvePackageDescription(sourceRoot, package.ProductId, package.Id));
    }

    private static string ResolvePackageId(string projectPath)
    {
        return Path.GetFileNameWithoutExtension(projectPath);
    }

    private static string ResolveProductId(string packageId)
    {
        var suffix = packageId.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? packageId;
        return suffix switch
        {
            "Controls" => "desktop",
            _ => suffix.ToLowerInvariant()
        };
    }

    private static string ResolveProductName(string packageId)
    {
        var suffix = packageId.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? packageId;
        return suffix.Equals("Controls", StringComparison.Ordinal)
            ? "AtomUI Desktop"
            : $"AtomUI {suffix}";
    }

    private static string ResolveAtomUIVersion(string sourceRoot)
    {
        var versionPath = Path.Combine(sourceRoot, "build", "Version.props");
        if (!File.Exists(versionPath))
        {
            return "unknown";
        }

        var document = XDocument.Load(versionPath);
        return document.Descendants("AtomUIVersion").FirstOrDefault()?.Value.Trim() ?? "unknown";
    }

    private static string ResolvePackageDescription(string sourceRoot, string productId, string packageId)
    {
        var modulePath = Path.Combine(sourceRoot, "docs", "modules", ResolveModuleDirectory(productId), "overview.md");
        if (File.Exists(modulePath))
        {
            var text = File.ReadAllText(modulePath);
            var paragraph = ExtractFirstParagraph(text);
            if (!string.IsNullOrWhiteSpace(paragraph))
            {
                return SanitizeText(paragraph);
            }
        }

        return $"{packageId} controls package.";
    }

    private static string ResolveModuleDirectory(string productId)
    {
        return productId switch
        {
            "desktop" => "desktop-controls",
            _ => $"desktop-controls-{ToKebabCase(productId)}"
        };
    }

    private static string ExtractFirstParagraph(string markdown)
    {
        foreach (var block in markdown.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = block.Trim();
            if (trimmed.Length == 0
                || trimmed.StartsWith('#')
                || trimmed.StartsWith('|')
                || trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                continue;
            }

            return WhitespaceRegex().Replace(trimmed, " ");
        }

        return string.Empty;
    }

    private static string SanitizeText(string value)
    {
        return value
            .Replace("Ant" + " Design", "AtomUI", StringComparison.OrdinalIgnoreCase)
            .Replace("Ant" + "Design", "AtomUI", StringComparison.OrdinalIgnoreCase)
            .Replace("\u7ec4\u4ef6", "\u63a7\u4ef6", StringComparison.Ordinal);
    }

    private static string ToKebabCase(string value)
    {
        return KebabBoundaryRegex().Replace(value, "-$1").Trim('-').ToLowerInvariant();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("([A-Z])")]
    private static partial Regex KebabBoundaryRegex();
}
