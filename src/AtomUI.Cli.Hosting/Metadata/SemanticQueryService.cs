namespace AtomUI.Cli.Hosting.Metadata;

public sealed class SemanticQueryService(SemanticSnapshotRegistry registry)
{
    public SemanticCommandPayload? Query(SemanticCommandOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Control))
        {
            return null;
        }

        var control = registry.Snapshot.Controls.FirstOrDefault(item =>
            item.Name.Equals(options.Control, StringComparison.OrdinalIgnoreCase));
        if (control is null)
        {
            return null;
        }

        var parts = control.Parts
            .Where(part => string.IsNullOrWhiteSpace(options.Part)
                           || part.Name.Equals(options.Part, StringComparison.OrdinalIgnoreCase))
            .OrderBy(part => part.Name, StringComparer.Ordinal)
            .Select(CreatePartPayload)
            .ToArray();
        var pseudoClasses = control.PseudoClasses
            .OrderBy(pseudoClass => pseudoClass.Name, StringComparer.Ordinal)
            .Select(CreatePseudoClassPayload)
            .ToArray();
        var templates = control.Templates
            .OrderBy(template => template.ThemeName, StringComparer.Ordinal)
            .ThenBy(template => template.Source.Line ?? int.MaxValue)
            .ThenBy(template => template.Selector, StringComparer.Ordinal)
            .Select(CreateTemplatePayload)
            .ToArray();
        var diagnostics = registry.Snapshot.Diagnostics
            .Select(diagnostic => new SemanticDiagnosticPayload(diagnostic.Code, diagnostic.Severity, diagnostic.Message))
            .ToArray();

        return new SemanticCommandPayload(
            registry.Snapshot.SchemaVersion,
            "semantic",
            registry.Snapshot.TargetVersion,
            registry.Snapshot.SnapshotId,
            registry.Snapshot.SourceCommit,
            control.Name,
            options.Part,
            parts,
            pseudoClasses,
            templates,
            diagnostics);
    }

    private static SemanticPartPayload CreatePartPayload(SemanticPartDocument part)
    {
        return new SemanticPartPayload(
            part.Name,
            part.NodeType,
            part.Description,
            part.BoundApis,
            part.PseudoClasses,
            part.TokenUsages,
            new SemanticSourcePayload(part.Source.Path, part.Source.Line));
    }

    private static SemanticPseudoClassPayload CreatePseudoClassPayload(SemanticPseudoClassDocument pseudoClass)
    {
        return new SemanticPseudoClassPayload(
            pseudoClass.Name,
            pseudoClass.Description,
            pseudoClass.BoundApis,
            pseudoClass.Selectors,
            new SemanticSourcePayload(pseudoClass.Source.Path, pseudoClass.Source.Line));
    }

    private static SemanticThemeTemplatePayload CreateTemplatePayload(SemanticThemeTemplateDocument template)
    {
        return new SemanticThemeTemplatePayload(
            template.ThemeName,
            template.Selector,
            new SemanticSourcePayload(template.Source.Path, template.Source.Line),
            template.Roots.Select(CreateNodePayload).ToArray());
    }

    private static SemanticThemeNodePayload CreateNodePayload(SemanticThemeNodeDocument node)
    {
        return new SemanticThemeNodePayload(
            node.ElementType,
            node.Name,
            node.Children.Select(CreateNodePayload).ToArray());
    }
}
