namespace AtomUI.Cli.Hosting.ProjectAnalysis;

public sealed class EnvCommandHandler(ProjectAnalysisService analysis) : IAtomUICliCommandHandler<EnvCommandOptions>
{
    public async ValueTask<AtomUICliResult> ExecuteAsync(EnvCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        var result = await analysis.AnalyzeAsync(new ProjectAnalysisRequest(options.Path, IncludeXaml: false, IncludeCSharp: false), cancellationToken);
        if (!result.IsSuccess)
        {
            return AtomUICliResult.Failure(result.Error!);
        }

        var projectContext = result.Context!;
        return AtomUICliResult.Success(
            $"AtomUI project environment{Environment.NewLine}Root: {projectContext.RootPath}{Environment.NewLine}Projects: {projectContext.Projects.Count}{Environment.NewLine}Packages: {projectContext.Packages.Count}");
    }
}

public sealed class UsageCommandHandler(ProjectAnalysisService analysis) : IAtomUICliCommandHandler<UsageCommandOptions>
{
    public async ValueTask<AtomUICliResult> ExecuteAsync(UsageCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        if (!options.IncludeCSharp && !options.IncludeXaml)
        {
            return AtomUICliResult.Failure(new AtomUICliError(
                AtomUICliErrorCodes.ArgumentInvalidValue,
                AtomUICliSeverity.Error,
                "At least one source scanner must be enabled.",
                null,
                "parse",
                null,
                null));
        }

        var result = await analysis.AnalyzeAsync(new ProjectAnalysisRequest(options.Path, options.IncludeXaml, options.IncludeCSharp), cancellationToken);
        if (!result.IsSuccess)
        {
            return AtomUICliResult.Failure(result.Error!);
        }

        var usages = result.Context!.Usages
            .Where(usage => string.IsNullOrWhiteSpace(options.Control) || usage.Control.Equals(options.Control, StringComparison.OrdinalIgnoreCase))
            .GroupBy(usage => usage.Control, StringComparer.OrdinalIgnoreCase)
            .Select(group => $"{group.Key}: {group.Count()}")
            .Order(StringComparer.OrdinalIgnoreCase);

        return AtomUICliResult.Success(string.Join(Environment.NewLine, usages.DefaultIfEmpty("No AtomUI control usage found.")));
    }
}

public sealed class DoctorCommandHandler(ProjectAnalysisService analysis) : IAtomUICliCommandHandler<DiagnosticCommandOptions>
{
    public async ValueTask<AtomUICliResult> ExecuteAsync(DiagnosticCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        var result = await analysis.AnalyzeAsync(new ProjectAnalysisRequest(options.Path, IncludeXaml: true, IncludeCSharp: true), cancellationToken);
        if (!result.IsSuccess)
        {
            return AtomUICliResult.Failure(result.Error!);
        }

        var diagnostics = result.Context!.Diagnostics;
        var text = diagnostics.Count == 0
            ? "No AtomUI project issues found."
            : string.Join(Environment.NewLine, diagnostics.Select(item => $"{item.Code} [{item.Severity}] {item.Message}"));

        return AtomUICliResult.FromDiagnostics(
            diagnostics.Select(item => new AtomUICliDiagnostic(item.Code, item.Severity, "project", item.Message, item.FilePath, item.Line, null)),
            options.FailOnWarning,
            text);
    }
}

public sealed class LintCommandHandler(ProjectAnalysisService analysis) : IAtomUICliCommandHandler<DiagnosticCommandOptions>
{
    public async ValueTask<AtomUICliResult> ExecuteAsync(DiagnosticCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        var result = await analysis.AnalyzeAsync(new ProjectAnalysisRequest(options.Path, IncludeXaml: true, IncludeCSharp: true), cancellationToken);
        if (!result.IsSuccess)
        {
            return AtomUICliResult.Failure(result.Error!);
        }

        return AtomUICliResult.Success(result.Context!.Diagnostics.Count == 0
            ? "No AtomUI lint findings."
            : string.Join(Environment.NewLine, result.Context.Diagnostics.Select(item => $"{item.Code}: {item.Message}")));
    }
}

public sealed class MigrateCommandHandler(ProjectAnalysisService analysis) : IAtomUICliCommandHandler<MigrateCommandOptions>
{
    public async ValueTask<AtomUICliResult> ExecuteAsync(MigrateCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        var result = await analysis.AnalyzeAsync(new ProjectAnalysisRequest(options.Path, IncludeXaml: true, IncludeCSharp: true), cancellationToken);
        if (!result.IsSuccess)
        {
            return AtomUICliResult.Failure(result.Error!);
        }

        return AtomUICliResult.Success(
            $"Migration analysis complete for {result.Context!.Projects.Count} project(s). Target: {options.To ?? "metadata default"}.");
    }
}
