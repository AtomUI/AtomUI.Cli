namespace AtomUI.Cli.Hosting.Metadata;

public sealed class DemoQueryService(DocumentSnapshotRegistry registry)
{
    public DemoQueryResult Query(DemoQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var snapshots = SelectSnapshots(query.TargetVersion, query.Language);
        if (snapshots.Count == 0)
        {
            return DemoQueryResult.Failure(
                DemoQueryStatus.SnapshotUnavailable,
                AtomUICliErrorCodes.DataVersionUnresolved,
                $"Documentation snapshot for version '{query.TargetVersion}' was not found.");
        }

        var controls = snapshots
            .SelectMany(snapshot => snapshot.Controls)
            .Where(control => MatchesControl(control, query.ControlName, query.Strict))
            .Where(control => string.IsNullOrWhiteSpace(query.ProductId)
                              || control.ProductId.Equals(query.ProductId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (controls.Length == 0)
        {
            var suggestions = snapshots
                .SelectMany(snapshot => snapshot.Controls)
                .Where(control => control.Name.Contains(query.ControlName, StringComparison.OrdinalIgnoreCase)
                                  || query.ControlName.Contains(control.Name, StringComparison.OrdinalIgnoreCase))
                .SelectMany(control => control.Examples.Take(3))
                .Select(CreateSuggestion)
                .Take(5)
                .ToArray();

            return DemoQueryResult.Failure(
                DemoQueryStatus.ControlNotFound,
                AtomUICliErrorCodes.ControlNotFound,
                $"Control '{query.ControlName}' was not found.",
                suggestions);
        }

        var controlDocument = controls[0];
        var availableScenarios = controlDocument.Examples
            .Select(example => example.Kind)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (query.Mode is DemoCommandMode.Detail or DemoCommandMode.Code)
        {
            return QueryDetail(controlDocument, availableScenarios, query);
        }

        return QueryList(controlDocument, availableScenarios, query);
    }

    private IReadOnlyList<DocumentSnapshot> SelectSnapshots(string targetVersion, string language)
    {
        var normalizedVersion = string.IsNullOrWhiteSpace(targetVersion) ? null : targetVersion;
        var versionMatches = registry.Snapshots
            .Where(snapshot => normalizedVersion is null
                               || snapshot.TargetVersion.Equals(normalizedVersion, StringComparison.OrdinalIgnoreCase)
                               || snapshot.TargetVersion.StartsWith(normalizedVersion, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (versionMatches.Length == 0)
        {
            return Array.Empty<DocumentSnapshot>();
        }

        var languageMatches = versionMatches
            .Where(snapshot => snapshot.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return languageMatches.Length > 0 ? languageMatches : versionMatches;
    }

    private static DemoQueryResult QueryDetail(
        ControlDocument control,
        IReadOnlyList<string> availableScenarios,
        DemoQuery query)
    {
        var demo = control.Examples.FirstOrDefault(example => MatchesDemoKey(example, query.DemoKey!, query.Strict));
        if (demo is null)
        {
            var suggestions = control.Examples
                .Where(example => example.SourceKey.Contains(query.DemoKey!, StringComparison.OrdinalIgnoreCase)
                                  || query.DemoKey!.Contains(example.SourceKey, StringComparison.OrdinalIgnoreCase)
                                  || example.Title.Contains(query.DemoKey!, StringComparison.OrdinalIgnoreCase))
                .DefaultIfEmpty(control.Examples.FirstOrDefault())
                .Where(example => example is not null)
                .Select(example => CreateSuggestion(example!))
                .DistinctBy(suggestion => suggestion.SourceKey)
                .Take(5)
                .ToArray();

            return DemoQueryResult.Failure(
                DemoQueryStatus.DemoNotFound,
                AtomUICliErrorCodes.DemoNotFound,
                $"Demo '{query.DemoKey}' was not found.",
                suggestions,
                availableScenarios);
        }

        return DemoQueryResult.FoundDetail(control, demo, availableScenarios);
    }

    private static DemoQueryResult QueryList(
        ControlDocument control,
        IReadOnlyList<string> availableScenarios,
        DemoQuery query)
    {
        IEnumerable<ControlExampleDocument> examples = control.Examples;

        if (!string.IsNullOrWhiteSpace(query.DemoKey))
        {
            examples = examples.Where(example => example.SourceKey.Contains(query.DemoKey, StringComparison.OrdinalIgnoreCase)
                                                 || example.Title.Contains(query.DemoKey, StringComparison.OrdinalIgnoreCase)
                                                 || example.Description.Contains(query.DemoKey, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Scenario))
        {
            examples = examples.Where(example => query.Strict
                ? example.Kind.Equals(query.Scenario, StringComparison.Ordinal)
                : example.Kind.Equals(query.Scenario, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Match))
        {
            examples = examples.Where(example =>
                example.SourceKey.Contains(query.Match, StringComparison.OrdinalIgnoreCase)
                || example.Title.Contains(query.Match, StringComparison.OrdinalIgnoreCase)
                || example.Description.Contains(query.Match, StringComparison.OrdinalIgnoreCase)
                || example.Kind.Contains(query.Match, StringComparison.OrdinalIgnoreCase));
        }

        var orderedExamples = examples
            .OrderBy(example => example.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(example => example.Priority)
            .ThenBy(example => example.SourceKey, StringComparer.Ordinal)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(query.Scenario) && orderedExamples.Length == 0 && query.Strict)
        {
            return DemoQueryResult.Failure(
                DemoQueryStatus.ScenarioNotFound,
                AtomUICliErrorCodes.DemoNotFound,
                $"Scenario '{query.Scenario}' was not found.",
                control.Examples.Select(CreateSuggestion).Take(5).ToArray(),
                availableScenarios);
        }

        var warnings = !string.IsNullOrWhiteSpace(query.Scenario) && orderedExamples.Length == 0
            ? [new DocumentWarning("ATOMUICLI_DEMO_EMPTY", $"No demos matched scenario '{query.Scenario}'.", "scenario")]
            : Array.Empty<DocumentWarning>();

        return DemoQueryResult.FoundList(control, orderedExamples, availableScenarios, warnings);
    }

    private static bool MatchesControl(ControlDocument control, string targetId, bool strict)
    {
        return strict
            ? control.Name.Equals(targetId, StringComparison.Ordinal)
            : control.Name.Equals(targetId, StringComparison.OrdinalIgnoreCase)
              || control.Id.Equals(targetId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesDemoKey(ControlExampleDocument example, string demoKey, bool strict)
    {
        return strict
            ? example.SourceKey.Equals(demoKey, StringComparison.Ordinal)
            : example.SourceKey.Equals(demoKey, StringComparison.OrdinalIgnoreCase);
    }

    private static DemoSuggestion CreateSuggestion(ControlExampleDocument example)
    {
        return new DemoSuggestion(example.SourceKey, example.Title, example.Kind);
    }
}

public sealed record DemoQuery(
    string ControlName,
    string? DemoKey,
    string? ProductId,
    string? Scenario,
    string? Match,
    DemoCommandMode Mode,
    DemoCodeLanguage CodeLanguage,
    bool Strict,
    string TargetVersion,
    string Language);

public enum DemoQueryStatus
{
    ListFound,
    DemoFound,
    ControlNotFound,
    DemoNotFound,
    ScenarioNotFound,
    SnapshotUnavailable,
    DataUnavailable
}

public sealed record DemoQueryResult(
    DemoQueryStatus Status,
    ControlDocument? Control,
    IReadOnlyList<ControlExampleDocument> Demos,
    ControlExampleDocument? SelectedDemo,
    IReadOnlyList<string> AvailableScenarios,
    IReadOnlyList<DemoSuggestion> Suggestions,
    IReadOnlyList<DocumentWarning> Warnings,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static DemoQueryResult FoundList(
        ControlDocument control,
        IReadOnlyList<ControlExampleDocument> demos,
        IReadOnlyList<string> availableScenarios,
        IReadOnlyList<DocumentWarning> warnings)
    {
        return new DemoQueryResult(
            DemoQueryStatus.ListFound,
            control,
            demos,
            null,
            availableScenarios,
            [],
            warnings,
            null,
            null);
    }

    public static DemoQueryResult FoundDetail(
        ControlDocument control,
        ControlExampleDocument selectedDemo,
        IReadOnlyList<string> availableScenarios)
    {
        return new DemoQueryResult(
            DemoQueryStatus.DemoFound,
            control,
            [],
            selectedDemo,
            availableScenarios,
            [],
            [],
            null,
            null);
    }

    public static DemoQueryResult Failure(
        DemoQueryStatus status,
        string errorCode,
        string errorMessage,
        IReadOnlyList<DemoSuggestion>? suggestions = null,
        IReadOnlyList<string>? availableScenarios = null)
    {
        return new DemoQueryResult(
            status,
            null,
            [],
            null,
            availableScenarios ?? [],
            suggestions ?? [],
            [],
            errorCode,
            errorMessage);
    }
}

public sealed record DemoSuggestion(string SourceKey, string Title, string Scenario);
