using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Projection;

internal static class SemanticSnapshotProjection
{
    public static ExtractedSemanticSnapshot Create(SourceAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var parts = context.Facts.GetByPrefix("semantic-part:")
            .Select(fact => fact.Value)
            .OfType<SemanticPartFact>()
            .GroupBy(part => part.ControlName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(part => part.Name, StringComparer.Ordinal)
                    .Select(part => new ExtractedSemanticPart(
                        part.Name,
                        part.NodeType,
                        part.Description,
                        part.BoundApis,
                        part.PseudoClasses,
                        part.TokenUsages,
                        part.SourceLocation.Path,
                        part.SourceLocation.Line))
                    .ToArray(),
                StringComparer.Ordinal);
        var pseudoClasses = context.Facts.GetByPrefix("semantic-pseudo-class:")
            .Select(fact => fact.Value)
            .OfType<SemanticPseudoClassFact>()
            .GroupBy(part => part.ControlName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(pseudoClass => pseudoClass.Name, StringComparer.Ordinal)
                    .Select(pseudoClass => new ExtractedSemanticPseudoClass(
                        pseudoClass.Name,
                        pseudoClass.Description,
                        pseudoClass.BoundApis,
                        pseudoClass.Selectors,
                        pseudoClass.SourceLocation.Path,
                        pseudoClass.SourceLocation.Line))
                    .ToArray(),
                StringComparer.Ordinal);
        var templates = context.Facts.GetByPrefix("theme-template-tree:")
            .Select(fact => fact.Value)
            .OfType<ThemeTemplateTreeFact>()
            .GroupBy(template => template.ControlName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(template => template.ThemeName, StringComparer.Ordinal)
                    .ThenBy(template => template.SourceLocation.Line ?? int.MaxValue)
                    .ThenBy(template => template.Selector, StringComparer.Ordinal)
                    .Select(CreateTemplate)
                    .ToArray(),
                StringComparer.Ordinal);

        var controls = parts.Keys
            .Concat(pseudoClasses.Keys)
            .Concat(templates.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => new ExtractedSemanticControl(
                name,
                parts.GetValueOrDefault(name, []),
                pseudoClasses.GetValueOrDefault(name, []),
                templates.GetValueOrDefault(name, [])))
            .ToArray();
        var diagnostics = context.Diagnostics.Diagnostics
            .Where(diagnostic => !diagnostic.ProcessorId.Equals("token", StringComparison.Ordinal))
            .Select(diagnostic => new ExtractedDiagnostic(diagnostic.Code, diagnostic.Severity, diagnostic.Message))
            .ToArray();

        return new ExtractedSemanticSnapshot(
            context.Identity.SnapshotSchemaVersion,
            $"atomui-semantic-source-{context.Identity.SourceCommit}",
            context.Identity.TargetVersion,
            context.Identity.SourceCommit,
            controls,
            diagnostics);
    }

    private static ExtractedSemanticThemeTemplate CreateTemplate(ThemeTemplateTreeFact fact)
    {
        return new ExtractedSemanticThemeTemplate(
            fact.ThemeName,
            fact.Selector,
            fact.SourceLocation.Path,
            fact.SourceLocation.Line,
            fact.Roots.Select(CreateNode).ToArray());
    }

    private static ExtractedSemanticThemeNode CreateNode(ThemeTemplateTreeNodeFact fact)
    {
        return new ExtractedSemanticThemeNode(
            fact.ElementType,
            fact.Name,
            fact.Children.Select(CreateNode).ToArray());
    }
}
