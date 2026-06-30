namespace AtomUI.Cli.Hosting.Metadata;

public sealed class DocumentationQueryService(DocumentSnapshotRegistry registry)
{
    public DocumentQueryResult Query(DocumentQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var snapshots = SelectSnapshots(query.TargetVersion, query.Language);
        if (snapshots.Count == 0)
        {
            return DocumentQueryResult.Failure(
                DocumentQueryStatus.SnapshotUnavailable,
                AtomUICliErrorCodes.DataVersionUnresolved,
                $"Documentation snapshot for version '{query.TargetVersion}' was not found.");
        }

        return query.TargetKind == DocumentTargetKind.Topic
            ? QueryTopic(snapshots, query)
            : QueryControl(snapshots, query);
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

    private static DocumentQueryResult QueryControl(IReadOnlyList<DocumentSnapshot> snapshots, DocumentQuery query)
    {
        var controls = snapshots
            .SelectMany(snapshot => snapshot.Controls)
            .Where(control => MatchesControl(control, query.TargetId, query.Strict))
            .Where(control => string.IsNullOrWhiteSpace(query.ProductId)
                              || control.ProductId.Equals(query.ProductId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (controls.Length == 0)
        {
            var suggestions = snapshots
                .SelectMany(snapshot => snapshot.Controls)
                .Where(control => control.Name.Contains(query.TargetId, StringComparison.OrdinalIgnoreCase)
                                  || query.TargetId.Contains(control.Name, StringComparison.OrdinalIgnoreCase))
                .Select(control => new DocumentSuggestion("control", control.Name, control.DisplayName, control.ProductId))
                .Take(5)
                .ToArray();

            return DocumentQueryResult.Failure(
                DocumentQueryStatus.NotFound,
                AtomUICliErrorCodes.ControlNotFound,
                $"Control '{query.TargetId}' was not found.",
                suggestions: suggestions);
        }

        var distinctProducts = controls.Select(control => control.ProductId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (distinctProducts.Length > 1)
        {
            return DocumentQueryResult.Failure(
                DocumentQueryStatus.AmbiguousTarget,
                AtomUICliErrorCodes.ControlAmbiguous,
                $"Control '{query.TargetId}' matches multiple products. Use --product to choose one.",
                suggestions: controls.Select(control => new DocumentSuggestion("control", control.Name, control.DisplayName, control.ProductId)).ToArray());
        }

        var controlDocument = controls[0];
        var sectionFailure = ValidateSection(controlDocument.Sections, query.Section);
        if (sectionFailure is not null)
        {
            return sectionFailure;
        }

        var exampleFailure = ValidateExample(controlDocument.Examples, query.ExampleKey);
        return exampleFailure ?? DocumentQueryResult.Found(controlDocument);
    }

    private static DocumentQueryResult QueryTopic(IReadOnlyList<DocumentSnapshot> snapshots, DocumentQuery query)
    {
        var topic = snapshots
            .SelectMany(snapshot => snapshot.Topics)
            .FirstOrDefault(item => item.Id.Equals(query.TargetId, StringComparison.OrdinalIgnoreCase));

        if (topic is null)
        {
            return DocumentQueryResult.Failure(
                DocumentQueryStatus.NotFound,
                AtomUICliErrorCodes.DataUnavailable,
                $"Document topic '{query.TargetId}' was not found.",
                suggestions: snapshots
                    .SelectMany(snapshot => snapshot.Topics)
                    .Select(item => new DocumentSuggestion("topic", item.Id, item.Title, null))
                    .Take(5)
                    .ToArray());
        }

        var sectionFailure = ValidateSection(topic.Sections, query.Section);
        return sectionFailure ?? DocumentQueryResult.Found(topic);
    }

    private static bool MatchesControl(ControlDocument control, string targetId, bool strict)
    {
        return strict
            ? control.Name.Equals(targetId, StringComparison.Ordinal)
            : control.Name.Equals(targetId, StringComparison.OrdinalIgnoreCase)
              || control.Id.Equals(targetId, StringComparison.OrdinalIgnoreCase);
    }

    private static DocumentQueryResult? ValidateSection(
        IReadOnlyList<DocumentSectionContent> sections,
        DocumentSection requestedSection)
    {
        if (requestedSection == DocumentSection.All)
        {
            return null;
        }

        var sectionId = DocumentSectionNames.ToId(requestedSection);
        if (sections.Any(section => section.Id.Equals(sectionId, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return DocumentQueryResult.Failure(
            DocumentQueryStatus.SectionNotFound,
            AtomUICliErrorCodes.DataUnavailable,
            $"Documentation section '{sectionId}' was not found.",
            availableSections: sections.Select(section => section.Id).ToArray());
    }

    private static DocumentQueryResult? ValidateExample(
        IReadOnlyList<ControlExampleDocument> examples,
        string? exampleKey)
    {
        if (string.IsNullOrWhiteSpace(exampleKey))
        {
            return null;
        }

        if (examples.Any(example => example.SourceKey.Equals(exampleKey, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return DocumentQueryResult.Failure(
            DocumentQueryStatus.ExampleNotFound,
            AtomUICliErrorCodes.DemoNotFound,
            $"Control example '{exampleKey}' was not found.",
            availableExamples: examples.Select(example => example.SourceKey).ToArray());
    }
}

public sealed record DocumentQuery(
    DocumentTargetKind TargetKind,
    string TargetId,
    string TargetVersion,
    string Language,
    string? ProductId,
    DocumentSection Section,
    DocumentExamplesMode Examples,
    string? ExampleKey,
    bool Strict);

public enum DocumentQueryStatus
{
    Found,
    NotFound,
    AmbiguousTarget,
    SectionNotFound,
    ExampleNotFound,
    SnapshotUnavailable
}

public sealed record DocumentQueryResult(
    DocumentQueryStatus Status,
    string? ErrorCode,
    string? ErrorMessage,
    ControlDocument? Control,
    TopicDocument? Topic,
    IReadOnlyList<string> AvailableSections,
    IReadOnlyList<string> AvailableExamples,
    IReadOnlyList<DocumentSuggestion> Suggestions)
{
    public static DocumentQueryResult Found(ControlDocument control)
    {
        return new DocumentQueryResult(
            DocumentQueryStatus.Found,
            null,
            null,
            control,
            null,
            [],
            [],
            []);
    }

    public static DocumentQueryResult Found(TopicDocument topic)
    {
        return new DocumentQueryResult(
            DocumentQueryStatus.Found,
            null,
            null,
            null,
            topic,
            [],
            [],
            []);
    }

    public static DocumentQueryResult Failure(
        DocumentQueryStatus status,
        string errorCode,
        string errorMessage,
        IReadOnlyList<string>? availableSections = null,
        IReadOnlyList<string>? availableExamples = null,
        IReadOnlyList<DocumentSuggestion>? suggestions = null)
    {
        return new DocumentQueryResult(
            status,
            errorCode,
            errorMessage,
            null,
            null,
            availableSections ?? [],
            availableExamples ?? [],
            suggestions ?? []);
    }
}

public sealed record DocumentSuggestion(
    string Kind,
    string Id,
    string Title,
    string? ProductId);

internal static class DocumentSectionNames
{
    public static string ToId(DocumentSection section)
    {
        return section switch
        {
            DocumentSection.All => "all",
            DocumentSection.Overview => "overview",
            DocumentSection.Install => "install",
            DocumentSection.Usage => "usage",
            DocumentSection.Scenarios => "scenarios",
            DocumentSection.Examples => "examples",
            DocumentSection.Api => "api",
            DocumentSection.Properties => "properties",
            DocumentSection.Methods => "methods",
            DocumentSection.Events => "events",
            DocumentSection.Logic => "logic",
            DocumentSection.Theme => "theme",
            DocumentSection.Tokens => "tokens",
            DocumentSection.Semantic => "semantic",
            DocumentSection.Demos => "demos",
            DocumentSection.Changelog => "changelog",
            DocumentSection.Source => "source",
            _ => "all"
        };
    }
}
