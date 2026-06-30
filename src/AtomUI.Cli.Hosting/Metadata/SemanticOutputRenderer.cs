using System.Text;

namespace AtomUI.Cli.Hosting.Metadata;

public sealed class SemanticOutputRenderer
{
    public string RenderText(SemanticCommandPayload payload, bool includeDetail)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"AtomUI Semantic: {payload.Control}");
        builder.AppendLine($"Source commit: {payload.SourceCommit}");

        if (payload.Parts.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Template parts:");
            foreach (var part in payload.Parts)
            {
                builder.AppendLine($"  {part.Name} ({part.NodeType})");
                builder.AppendLine($"    {part.Description}");
                if (part.BoundApis.Count > 0)
                {
                    builder.AppendLine($"    APIs: {string.Join(", ", part.BoundApis)}");
                }

                if (part.PseudoClasses.Count > 0)
                {
                    builder.AppendLine($"    States: {string.Join(", ", part.PseudoClasses)}");
                }

                if (includeDetail)
                {
                    builder.AppendLine($"    Source: {FormatSource(part.Source)}");
                }
            }
        }

        if (payload.PseudoClasses.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Pseudo classes:");
            foreach (var pseudoClass in payload.PseudoClasses)
            {
                builder.AppendLine($"  {pseudoClass.Name}");
                builder.AppendLine($"    {pseudoClass.Description}");
                if (pseudoClass.BoundApis.Count > 0)
                {
                    builder.AppendLine($"    APIs: {string.Join(", ", pseudoClass.BoundApis)}");
                }

                if (includeDetail)
                {
                    builder.AppendLine($"    Source: {FormatSource(pseudoClass.Source)}");
                }
            }
        }

        AppendTemplateTrees(builder, payload, includeDetail);

        return builder.ToString().TrimEnd();
    }

    public string RenderMarkdown(SemanticCommandPayload payload, bool includeDetail)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {payload.Control} Semantic Contract");
        builder.AppendLine();
        builder.AppendLine($"Source commit: `{payload.SourceCommit}`");

        if (payload.Parts.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Template Parts");
            builder.AppendLine();
            builder.AppendLine(includeDetail
                ? "| Name | Type | APIs | States | Source |"
                : "| Name | Type | APIs | States |");
            builder.AppendLine(includeDetail
                ? "| --- | --- | --- | --- | --- |"
                : "| --- | --- | --- | --- |");
            foreach (var part in payload.Parts)
            {
                var row = $"| {part.Name} | {part.NodeType} | {string.Join(", ", part.BoundApis)} | {string.Join(", ", part.PseudoClasses)} |";
                builder.AppendLine(includeDetail ? $"{row} {FormatSource(part.Source)} |" : row);
            }
        }

        if (payload.PseudoClasses.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Pseudo Classes");
            builder.AppendLine();
            builder.AppendLine("| Name | APIs | Description |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var pseudoClass in payload.PseudoClasses)
            {
                builder.AppendLine($"| {pseudoClass.Name} | {string.Join(", ", pseudoClass.BoundApis)} | {pseudoClass.Description} |");
            }
        }

        AppendTemplateTreesMarkdown(builder, payload, includeDetail);

        return builder.ToString().TrimEnd();
    }

    private static void AppendTemplateTrees(StringBuilder builder, SemanticCommandPayload payload, bool includeDetail)
    {
        if (payload.Templates.Count == 0)
        {
            return;
        }

        var templates = SelectTemplates(payload.Templates, includeDetail).ToArray();
        builder.AppendLine();
        builder.AppendLine("ControlTheme tree:");
        foreach (var template in templates)
        {
            builder.AppendLine($"  {FormatTemplateHeader(template, includeDetail)}");
            foreach (var root in template.Roots)
            {
                AppendNode(builder, root, 4);
            }
        }

        if (!includeDetail && payload.Templates.Count > templates.Length)
        {
            builder.AppendLine($"  ... {payload.Templates.Count - templates.Length} more template tree(s). Use --include-template to show all.");
        }
    }

    private static void AppendTemplateTreesMarkdown(StringBuilder builder, SemanticCommandPayload payload, bool includeDetail)
    {
        if (payload.Templates.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("## ControlTheme Tree");
        builder.AppendLine();
        builder.AppendLine("```text");
        var templates = SelectTemplates(payload.Templates, includeDetail).ToArray();
        foreach (var template in templates)
        {
            builder.AppendLine(FormatTemplateHeader(template, includeDetail));
            foreach (var root in template.Roots)
            {
                AppendNode(builder, root, 2);
            }
        }

        if (!includeDetail && payload.Templates.Count > templates.Length)
        {
            builder.AppendLine($"... {payload.Templates.Count - templates.Length} more template tree(s). Use --include-template to show all.");
        }

        builder.AppendLine("```");
    }

    private static IEnumerable<SemanticThemeTemplatePayload> SelectTemplates(
        IReadOnlyList<SemanticThemeTemplatePayload> templates,
        bool includeDetail)
    {
        return includeDetail ? templates : templates.Take(1);
    }

    private static void AppendNode(StringBuilder builder, SemanticThemeNodePayload node, int indent)
    {
        builder.AppendLine($"{new string(' ', indent)}{FormatNode(node)}");
        foreach (var child in node.Children)
        {
            AppendNode(builder, child, indent + 2);
        }
    }

    private static string FormatTemplateHeader(SemanticThemeTemplatePayload template, bool includeDetail)
    {
        var selector = string.IsNullOrWhiteSpace(template.Selector) || template.Selector.Equals("default", StringComparison.Ordinal)
            ? string.Empty
            : $" ({template.Selector})";
        var source = includeDetail ? $" [{FormatSource(template.Source)}]" : string.Empty;
        return $"{template.ThemeName}{selector}{source}";
    }

    private static string FormatNode(SemanticThemeNodePayload node)
    {
        return string.IsNullOrWhiteSpace(node.Name)
            ? node.ElementType
            : $"{node.ElementType}#{node.Name}";
    }

    private static string FormatSource(SemanticSourcePayload source)
    {
        return source.Line is null ? source.Path : $"{source.Path}:{source.Line}";
    }
}
