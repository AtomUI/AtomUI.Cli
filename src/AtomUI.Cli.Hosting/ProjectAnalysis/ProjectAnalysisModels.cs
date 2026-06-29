namespace AtomUI.Cli.Hosting.ProjectAnalysis;

public sealed record ProjectAnalysisRequest(
    string Path,
    bool IncludeXaml,
    bool IncludeCSharp);

public sealed record ProjectAnalysisResult(
    bool IsSuccess,
    ProjectAnalysisContext? Context,
    AtomUICliError? Error)
{
    public static ProjectAnalysisResult Success(ProjectAnalysisContext context)
    {
        return new ProjectAnalysisResult(true, context, null);
    }

    public static ProjectAnalysisResult Failure(AtomUICliError error)
    {
        return new ProjectAnalysisResult(false, null, error);
    }
}

public sealed record ProjectAnalysisContext(
    string InputPath,
    string RootPath,
    IReadOnlyList<ProjectDescriptor> Projects,
    IReadOnlyList<PackageReferenceDescriptor> Packages,
    IReadOnlyList<ControlUsageDescriptor> Usages,
    IReadOnlyList<ProjectDiagnosticDescriptor> Diagnostics);

public sealed record ProjectDescriptor(
    string Path,
    string Name,
    IReadOnlyList<string> TargetFrameworks);

public sealed record PackageReferenceDescriptor(
    string ProjectPath,
    string PackageId,
    string? Version);

public sealed record ControlUsageDescriptor(
    string Control,
    string SourceKind,
    string FilePath,
    int Line,
    int Column);

public sealed record ProjectDiagnosticDescriptor(
    string Code,
    AtomUICliSeverity Severity,
    string Message,
    string? FilePath,
    int? Line);
