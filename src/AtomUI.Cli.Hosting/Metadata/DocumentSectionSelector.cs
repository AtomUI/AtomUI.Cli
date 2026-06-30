namespace AtomUI.Cli.Hosting.Metadata;

public sealed class DocumentSectionSelector
{
    private static readonly string[] SummarySections = ["overview", "install", "usage", "scenarios", "api", "examples", "theme", "demos"];
    private static readonly string[] AgentSections = ["overview", "install", "usage", "api", "events", "logic", "theme", "tokens", "semantic", "examples", "source"];

    public IReadOnlyList<DocumentSectionContent> Select(
        IReadOnlyList<DocumentSectionContent> sections,
        DocumentSection requestedSection,
        DocumentStyle style)
    {
        ArgumentNullException.ThrowIfNull(sections);

        var ordered = sections.OrderBy(section => section.Order).ToArray();
        if (requestedSection != DocumentSection.All)
        {
            var sectionId = DocumentSectionNames.ToId(requestedSection);
            return ordered
                .Where(section => section.Id.Equals(sectionId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        return style switch
        {
            DocumentStyle.Summary => SelectByIds(ordered, SummarySections),
            DocumentStyle.Agent => SelectByIds(ordered, AgentSections),
            _ => ordered
        };
    }

    private static IReadOnlyList<DocumentSectionContent> SelectByIds(
        IReadOnlyList<DocumentSectionContent> sections,
        IReadOnlyList<string> sectionIds)
    {
        var idSet = sectionIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return sections
            .Where(section => idSet.Contains(section.Id))
            .ToArray();
    }
}
