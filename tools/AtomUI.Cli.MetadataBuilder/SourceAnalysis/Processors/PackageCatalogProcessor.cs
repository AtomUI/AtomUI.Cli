using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;

internal sealed partial class PackageCatalogProcessor : ISourceAnalysisProcessor
{
    public string Id => "package-catalog";

    public IReadOnlySet<SourceAnalysisFeature> Provides { get; } =
        new HashSet<SourceAnalysisFeature>
        {
            SourceAnalysisFeature.PackageCatalog,
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
        var buildProperties = ResolveBuildProperties(context.Paths.SourceRoot);
        var centralVersions = ResolveCentralPackageVersions(context.Paths.SourceRoot, buildProperties);
        var packageProjects = EnumerateControlPackageProjects(context.Paths.SourceRoot)
            .Select(path => CreatePackage(context, path, version, controls))
            .Where(package => package is not null)
            .Cast<ExtractedCatalogPackage>()
            .OrderBy(package => package.Id, StringComparer.Ordinal)
            .ToArray();
        var packageDocuments = packageProjects
            .Select(package => CreatePackageDocument(context, package, centralVersions, buildProperties, controls))
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

        foreach (var packageDocument in packageDocuments)
        {
            context.Facts.Add($"package-document:{packageDocument.Id}", Id, packageDocument);
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
            IsCommercial: productId is "datagrid" or "colorpicker");
    }

    private static ExtractedPackageDocument CreatePackageDocument(
        SourceAnalysisContext context,
        ExtractedCatalogPackage package,
        IReadOnlyDictionary<string, string> centralVersions,
        IReadOnlyDictionary<string, string> buildProperties,
        IReadOnlyList<ExtractedCatalogControl> controls)
    {
        var projectPath = Path.Combine(context.Paths.SourceRoot, "src", package.Id, $"{package.Id}.csproj");
        var document = File.Exists(projectPath) ? XDocument.Load(projectPath) : new XDocument(new XElement("Project"));
        var targetFrameworks = ResolveTargetFrameworks(document, buildProperties);
        var moduleDocPath = ResolveModuleDocPath(context.Paths.SourceRoot, package.ProductId);
        var packageControls = controls
            .Where(control => control.PackageId.Equals(package.Id, StringComparison.Ordinal))
            .OrderBy(control => control.DisplayOrder)
            .ThenBy(control => control.Name, StringComparer.Ordinal)
            .Select(control => new ExtractedPackageControl(
                control.Name,
                control.DisplayName,
                control.CategoryId,
                control.CategoryName,
                control.IsCommercial))
            .ToArray();
        return new ExtractedPackageDocument(
            package.Id,
            package.ProductId,
            package.Version,
            package.Description,
            "control",
            IsRequired: !package.IsOptional,
            package.IsOptional,
            package.IsCommercial,
            IsHidden: false,
            targetFrameworks,
            ResolveProperty(document, "RootNamespace", package.Id, buildProperties),
            ResolveProperty(document, "AssemblyName", package.Id, buildProperties),
            ResolveDependencies(document, centralVersions),
            ResolveRegistration(context, package),
            new ExtractedPackageCompatibility(
                targetFrameworks,
                IsAotCompatible: ResolveBoolProperty(document, "IsAotCompatible", defaultValue: true),
                IsTrimCompatible: ResolveBoolProperty(document, "IsTrimmable", defaultValue: true),
                [$"Targets {string.Join(", ", targetFrameworks)}."]),
            packageControls,
            CreateSourceFiles(context, projectPath, moduleDocPath),
            []);
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

    private static IReadOnlyDictionary<string, string> ResolveCentralPackageVersions(
        string sourceRoot,
        IReadOnlyDictionary<string, string> properties)
    {
        var path = Path.Combine(sourceRoot, "Directory.Packages.props");
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var document = XDocument.Load(path);
        return document.Descendants("PackageVersion")
            .Select(element => new
            {
                Include = element.Attribute("Include")?.Value.Trim(),
                Version = ResolveMsBuildProperties(element.Attribute("Version")?.Value.Trim() ?? string.Empty, properties)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Include) && !string.IsNullOrWhiteSpace(item.Version))
            .GroupBy(item => item.Include!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Version!, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, string> ResolveBuildProperties(string sourceRoot)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in new[]
                 {
                     Path.Combine(sourceRoot, "build", "Version.props"),
                     Path.Combine(sourceRoot, "build", "Common.props"),
                     Path.Combine(sourceRoot, "build", "PackageMetaInfo.props"),
                     Path.Combine(sourceRoot, "Directory.Build.props")
                 })
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var document = XDocument.Load(path);
            foreach (var element in document.Descendants().Where(element => !element.HasElements))
            {
                if (!string.IsNullOrWhiteSpace(element.Name.LocalName) && !string.IsNullOrWhiteSpace(element.Value))
                {
                    properties[element.Name.LocalName] = ResolveMsBuildProperties(element.Value.Trim(), properties);
                }
            }
        }

        return properties;
    }

    private static string ResolveMsBuildProperties(string value, IReadOnlyDictionary<string, string> properties)
    {
        return MsBuildPropertyRegex().Replace(value, match =>
        {
            var propertyName = match.Groups["name"].Value;
            return properties.TryGetValue(propertyName, out var propertyValue) ? propertyValue : match.Value;
        });
    }

    private static IReadOnlyList<string> ResolveTargetFrameworks(
        XDocument document,
        IReadOnlyDictionary<string, string> properties)
    {
        var frameworks = ResolveProperty(document, "TargetFrameworks", string.Empty, properties);
        if (string.IsNullOrWhiteSpace(frameworks))
        {
            frameworks = ResolveProperty(document, "TargetFramework", "net10.0", properties);
        }

        return frameworks
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<ExtractedPackageDependency> ResolveDependencies(
        XDocument document,
        IReadOnlyDictionary<string, string> centralVersions)
    {
        var projectReferences = document.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFileNameWithoutExtension(value!))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(id => new ExtractedPackageDependency(id, string.Empty, "project", id.StartsWith("AtomUI.", StringComparison.Ordinal)))
            .ToArray();
        var packageReferences = document.Descendants("PackageReference")
            .Select(element =>
            {
                var id = element.Attribute("Include")?.Value.Trim() ?? string.Empty;
                var version = element.Attribute("Version")?.Value.Trim()
                              ?? (centralVersions.TryGetValue(id, out var centralVersion) ? centralVersion : string.Empty);
                return new ExtractedPackageDependency(id, version, "package", id.StartsWith("AtomUI.", StringComparison.Ordinal));
            })
            .Where(dependency => !string.IsNullOrWhiteSpace(dependency.Id))
            .ToArray();

        return projectReferences
            .Concat(packageReferences)
            .DistinctBy(dependency => dependency.Id, StringComparer.OrdinalIgnoreCase)
            .OrderBy(dependency => dependency.IsAtomUI ? 0 : 1)
            .ThenBy(dependency => dependency.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<ExtractedPackageRegistration> ResolveRegistration(
        SourceAnalysisContext context,
        ExtractedCatalogPackage package)
    {
        var sourcePath = FindRegistrationSource(context.Paths.SourceRoot, package.Id);
        var names = package.ProductId switch
        {
            "desktop" => new[] { "UseAtomUI", "UseDesktopControls" },
            "datagrid" => new[] { "UseAtomUI", "UseDataGrid" },
            "colorpicker" => new[] { "UseAtomUI", "UseColorPicker" },
            _ => new[] { "UseAtomUI" }
        };
        return names
            .Select(name => new ExtractedPackageRegistration(
                name,
                "extension-method",
                $"Register {package.Id} services and resources.",
                sourcePath is null ? null : context.Paths.GetRelativePath(sourcePath)))
            .ToArray();
    }

    private static string? FindRegistrationSource(string sourceRoot, string packageId)
    {
        var projectDirectory = Path.Combine(sourceRoot, "src", packageId);
        if (!Directory.Exists(projectDirectory))
        {
            return null;
        }

        return Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).Contains("Service", StringComparison.OrdinalIgnoreCase)
                           || Path.GetFileName(path).Contains("Module", StringComparison.OrdinalIgnoreCase)
                           || Path.GetFileName(path).Contains("Extensions", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static IReadOnlyList<ExtractedPackageSourceFile> CreateSourceFiles(
        SourceAnalysisContext context,
        string projectPath,
        string moduleDocPath)
    {
        var files = new List<ExtractedPackageSourceFile>();
        if (File.Exists(projectPath))
        {
            files.Add(new ExtractedPackageSourceFile("project", context.Paths.GetRelativePath(projectPath)));
        }

        if (File.Exists(moduleDocPath))
        {
            files.Add(new ExtractedPackageSourceFile("module-doc", context.Paths.GetRelativePath(moduleDocPath)));
        }

        return files;
    }

    private static string ResolveProperty(
        XDocument document,
        string name,
        string fallback,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        var value = document.Descendants(name).FirstOrDefault()?.Value.Trim() ?? fallback;
        return properties is null ? value : ResolveMsBuildProperties(value, properties);
    }

    private static bool ResolveBoolProperty(XDocument document, string name, bool defaultValue)
    {
        var value = ResolveProperty(document, name, string.Empty);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolvePackageDescription(string sourceRoot, string productId, string packageId)
    {
        var modulePath = ResolveModuleDocPath(sourceRoot, productId);
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

    private static string ResolveModuleDocPath(string sourceRoot, string productId)
    {
        return Path.Combine(sourceRoot, "docs", "modules", ResolveModuleDirectory(productId), "overview.md");
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

    [GeneratedRegex(@"\$\((?<name>[^)]+)\)")]
    private static partial Regex MsBuildPropertyRegex();
}
