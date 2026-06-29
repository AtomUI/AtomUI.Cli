using System.Xml.Linq;
using AtomUI.Cli.Hosting.Metadata;

namespace AtomUI.Cli.Hosting.ProjectAnalysis;

public sealed class ProjectAnalysisService(MetadataQueryService metadata)
{
    private static readonly string[] ExcludedSegments = ["/bin/", "/obj/", "/output/", "/.git/", "/.vs/", "/.idea/"];

    public ValueTask<ProjectAnalysisResult> AnalyzeAsync(ProjectAnalysisRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var inputPath = string.IsNullOrWhiteSpace(request.Path) ? "." : request.Path;
        var fullPath = Path.GetFullPath(inputPath);
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            return ValueTask.FromResult(ProjectAnalysisResult.Failure(CreateError(
                AtomUICliErrorCodes.ProjectNotFound,
                $"Project input '{inputPath}' was not found.",
                "resolve")));
        }

        try
        {
            var projectPaths = ResolveProjectPaths(fullPath);
            if (projectPaths.Count == 0)
            {
                return ValueTask.FromResult(ProjectAnalysisResult.Failure(CreateError(
                    AtomUICliErrorCodes.ProjectNotFound,
                    $"No project files were found under '{inputPath}'.",
                    "resolve")));
            }

            var projects = new List<ProjectDescriptor>();
            var packages = new List<PackageReferenceDescriptor>();
            var usages = new List<ControlUsageDescriptor>();
            var diagnostics = new List<ProjectDiagnosticDescriptor>();

            foreach (var projectPath in projectPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var project = ReadProject(projectPath);
                projects.Add(project);
                packages.AddRange(ReadPackages(projectPath));

                var projectDirectory = Path.GetDirectoryName(projectPath)!;
                if (request.IncludeXaml)
                {
                    usages.AddRange(ScanXaml(projectDirectory, cancellationToken));
                }

                if (request.IncludeCSharp)
                {
                    usages.AddRange(ScanCSharp(projectDirectory, cancellationToken));
                }
            }

            if (packages.Count == 0)
            {
                diagnostics.Add(new ProjectDiagnosticDescriptor(
                    AtomUICliErrorCodes.ProjectLintFinding,
                    AtomUICliSeverity.Warning,
                    "No AtomUI package references were found.",
                    null,
                    null));
            }

            var context = new ProjectAnalysisContext(
                inputPath,
                Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath)!,
                Array.AsReadOnly(projects.ToArray()),
                Array.AsReadOnly(packages.ToArray()),
                Array.AsReadOnly(usages.ToArray()),
                Array.AsReadOnly(diagnostics.ToArray()));

            return ValueTask.FromResult(ProjectAnalysisResult.Success(context));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return ValueTask.FromResult(ProjectAnalysisResult.Failure(CreateError(
                AtomUICliErrorCodes.ProjectFileReadFailed,
                ex.Message,
                "read")));
        }
    }

    private static IReadOnlyList<string> ResolveProjectPaths(string fullPath)
    {
        if (File.Exists(fullPath) && fullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return [fullPath];
        }

        if (File.Exists(fullPath) && (fullPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) || fullPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)))
        {
            var root = Path.GetDirectoryName(fullPath)!;
            return Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
                .Where(IsNotExcluded)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if (!Directory.Exists(fullPath))
        {
            return [];
        }

        var direct = Directory.EnumerateFiles(fullPath, "*.csproj", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (direct.Length > 0)
        {
            return direct;
        }

        return Directory.EnumerateFiles(fullPath, "*.csproj", SearchOption.AllDirectories)
            .Where(IsNotExcluded)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ProjectDescriptor ReadProject(string projectPath)
    {
        var document = XDocument.Load(projectPath, LoadOptions.None);
        var frameworks = document.Descendants()
            .Where(element => element.Name.LocalName is "TargetFramework" or "TargetFrameworks")
            .SelectMany(element => (element.Value ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .DefaultIfEmpty("net10.0")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ProjectDescriptor(projectPath, Path.GetFileNameWithoutExtension(projectPath), Array.AsReadOnly(frameworks));
    }

    private static IReadOnlyList<PackageReferenceDescriptor> ReadPackages(string projectPath)
    {
        var document = XDocument.Load(projectPath, LoadOptions.None);
        return document.Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => new PackageReferenceDescriptor(
                projectPath,
                element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value ?? string.Empty,
                element.Attribute("Version")?.Value))
            .Where(reference => reference.PackageId.StartsWith("AtomUI", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private IReadOnlyList<ControlUsageDescriptor> ScanXaml(string root, CancellationToken cancellationToken)
    {
        var controls = metadata.Catalog.Controls;
        var usages = new List<ControlUsageDescriptor>();
        foreach (var file in EnumerateSourceFiles(root, [".axaml", ".xaml"]))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (var control in controls)
                {
                    var marker = $"<{control.Name}";
                    var column = lines[i].IndexOf(marker, StringComparison.Ordinal);
                    if (column >= 0)
                    {
                        usages.Add(new ControlUsageDescriptor(control.Name, "xaml", file, i + 1, column + 1));
                    }
                }
            }
        }

        return usages;
    }

    private IReadOnlyList<ControlUsageDescriptor> ScanCSharp(string root, CancellationToken cancellationToken)
    {
        var controls = metadata.Catalog.Controls;
        var usages = new List<ControlUsageDescriptor>();
        foreach (var file in EnumerateSourceFiles(root, [".cs"]))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (var control in controls)
                {
                    var marker = $"new {control.Name}";
                    var column = lines[i].IndexOf(marker, StringComparison.Ordinal);
                    if (column >= 0)
                    {
                        usages.Add(new ControlUsageDescriptor(control.Name, "csharp", file, i + 1, column + 1));
                    }
                }
            }
        }

        return usages;
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root, IReadOnlyList<string> extensions)
    {
        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(IsNotExcluded)
            .Where(file => extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsNotExcluded(string path)
    {
        var normalized = path.Replace(Path.DirectorySeparatorChar, '/');
        return !ExcludedSegments.Any(segment => normalized.Contains(segment, StringComparison.OrdinalIgnoreCase));
    }

    private static AtomUICliError CreateError(string code, string message, string stage)
    {
        return new AtomUICliError(
            code,
            AtomUICliSeverity.Error,
            message,
            null,
            stage,
            null,
            null);
    }
}
