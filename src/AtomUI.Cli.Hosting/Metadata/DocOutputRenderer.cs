using System.Text;

namespace AtomUI.Cli.Hosting.Metadata;

public sealed class DocOutputRenderer
{
    public string RenderMarkdown(DocCommandPayload payload, bool includeDetail)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var builder = new StringBuilder();
        builder.AppendLine($"# {payload.Title}");
        builder.AppendLine();

        foreach (var section in payload.Sections)
        {
            builder.AppendLine($"## {section.Title}");
            builder.AppendLine();
            builder.AppendLine(section.Markdown.Trim());
            builder.AppendLine();
        }

        if (payload.Warnings.Count > 0)
        {
            builder.AppendLine("## Warnings");
            builder.AppendLine();
            foreach (var warning in payload.Warnings)
            {
                builder.AppendLine($"- {warning.Code}: {warning.Message}");
            }

            builder.AppendLine();
        }

        if (payload.Related.Count > 0)
        {
            builder.AppendLine("## Related");
            builder.AppendLine();
            foreach (var related in payload.Related)
            {
                builder.AppendLine($"- `{related.Command}`");
            }

            builder.AppendLine();
        }

        if (includeDetail)
        {
            builder.AppendLine("## Source");
            builder.AppendLine();
            builder.AppendLine($"- Snapshot: `{payload.Source.SnapshotId}`");
            builder.AppendLine($"- Source ref: `{payload.Source.SourceRef}`");
            builder.AppendLine($"- Source commit: `{payload.Source.SourceCommit}`");
            builder.AppendLine($"- Generated at: `{payload.Source.GeneratedAt}`");
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    public string RenderText(DocCommandPayload payload, bool includeDetail)
    {
        var markdown = RenderMarkdown(payload, includeDetail);
        var builder = new StringBuilder();
        foreach (var line in markdown.Split(Environment.NewLine))
        {
            if (IsMarkdownTableDivider(line))
            {
                continue;
            }

            if (IsMarkdownTableRow(line))
            {
                builder.AppendLine(RenderTableRow(line));
                continue;
            }

            builder.AppendLine(line
                .Replace("# ", string.Empty, StringComparison.Ordinal)
                .Replace("## ", string.Empty, StringComparison.Ordinal)
                .Replace("`", string.Empty, StringComparison.Ordinal));
        }

        return builder.ToString().TrimEnd();
    }

    private static bool IsMarkdownTableDivider(string line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith("| ---", StringComparison.Ordinal);
    }

    private static bool IsMarkdownTableRow(string line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith("|", StringComparison.Ordinal) && trimmed.EndsWith("|", StringComparison.Ordinal);
    }

    private static string RenderTableRow(string line)
    {
        var cells = line.Trim()
            .Trim('|')
            .Split('|', StringSplitOptions.TrimEntries);
        return string.Join("  ", cells);
    }
}
