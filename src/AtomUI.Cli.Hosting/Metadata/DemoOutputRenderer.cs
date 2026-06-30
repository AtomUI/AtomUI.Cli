using System.Text;

namespace AtomUI.Cli.Hosting.Metadata;

public sealed class DemoOutputRenderer
{
    public string RenderText(DemoCommandPayload payload, bool includeDetail, bool expandAll = false)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return payload.Mode == DemoCommandMode.List
            ? expandAll ? RenderExpandedListText(payload, includeDetail) : RenderListText(payload, includeDetail)
            : RenderDetailText(payload, includeDetail);
    }

    public string RenderMarkdown(DemoCommandPayload payload, bool includeDetail, bool expandAll = false)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return payload.Mode == DemoCommandMode.List
            ? expandAll ? RenderExpandedListMarkdown(payload, includeDetail) : RenderListMarkdown(payload, includeDetail)
            : RenderDetailMarkdown(payload, includeDetail);
    }

    public string RenderCodeOnly(DemoCommandPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var selectedDemo = payload.SelectedDemo ?? throw new InvalidOperationException("Selected demo is required.");
        return string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            selectedDemo.Snippets.Select(snippet => snippet.Code));
    }

    private static string RenderListText(DemoCommandPayload payload, bool includeDetail)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{payload.Control.Name} demos ({payload.Demos.Count})");

        foreach (var group in payload.Demos.GroupBy(demo => demo.Scenario, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine();
            builder.AppendLine(FormatScenario(group.Key));
            foreach (var demo in group.OrderBy(demo => demo.Priority).ThenBy(demo => demo.SourceKey, StringComparer.Ordinal))
            {
                builder.AppendLine($"  {demo.SourceKey.PadRight(24)} {demo.Title}");
                if (includeDetail)
                {
                    builder.AppendLine($"    languages: {string.Join(", ", demo.Snippets.Select(snippet => snippet.Language).Distinct(StringComparer.OrdinalIgnoreCase))}");
                    builder.AppendLine($"    source: {demo.SourcePath}");
                }
            }
        }

        AppendWarningsText(builder, payload);

        if (payload.Demos.Count > 0)
        {
            var suggestedDemo = SelectSuggestedDemo(payload.Demos);

            builder.AppendLine();
            builder.AppendLine("Run:");
            builder.AppendLine($"  dotnet atomui demo {payload.Control.Name} {suggestedDemo.SourceKey}");
            if (payload.AvailableScenarios.Any(scenario => scenario.Equals("theme", StringComparison.OrdinalIgnoreCase)))
            {
                builder.AppendLine($"  dotnet atomui demo {payload.Control.Name} --scenario theme");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderExpandedListText(DemoCommandPayload payload, bool includeDetail)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{payload.Control.Name} demos ({payload.Demos.Count})");

        foreach (var group in payload.Demos.GroupBy(demo => demo.Scenario, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine();
            builder.AppendLine($"## {FormatScenario(group.Key)}");

            foreach (var demo in group.OrderBy(demo => demo.Priority).ThenBy(demo => demo.SourceKey, StringComparer.Ordinal))
            {
                builder.AppendLine();
                AppendExpandedDemoText(builder, demo, includeDetail);
            }
        }

        AppendWarningsText(builder, payload);
        AppendSnapshotText(builder, payload, includeDetail);
        return builder.ToString().TrimEnd();
    }

    private static string RenderDetailText(DemoCommandPayload payload, bool includeDetail)
    {
        var demo = payload.SelectedDemo ?? throw new InvalidOperationException("Selected demo is required.");
        var builder = new StringBuilder();
        builder.AppendLine($"{payload.Control.Name} demo: {demo.Title}");
        builder.AppendLine($"SourceKey: {demo.SourceKey}");
        builder.AppendLine($"Scenario: {demo.Scenario}");
        if (includeDetail)
        {
            builder.AppendLine($"Source: {demo.SourcePath}{FormatSourceLine(demo.SourceLine)}");
            builder.AppendLine($"Snapshot: {payload.Source.SnapshotId}");
            builder.AppendLine($"Source ref: {payload.Source.SourceRootConvention} @ {payload.Source.SourceRef}");
            builder.AppendLine($"Source commit: {payload.Source.SourceCommit}");
            builder.AppendLine($"Generated: {payload.Source.GeneratedAt}");
        }

        builder.AppendLine();
        builder.AppendLine(demo.Description);

        foreach (var snippet in demo.Snippets)
        {
            builder.AppendLine();
            builder.AppendLine($"{FormatSnippetLanguage(snippet.Language)}:");
            AppendIndentedCode(builder, snippet.Code);
        }

        if (demo.RelatedCommands.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Related:");
            foreach (var related in demo.RelatedCommands)
            {
                builder.AppendLine($"  {related.Command}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderListMarkdown(DemoCommandPayload payload, bool includeDetail)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {payload.Control.Name} Demos");
        builder.AppendLine();

        foreach (var group in payload.Demos.GroupBy(demo => demo.Scenario, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"## {FormatScenario(group.Key)}");
            builder.AppendLine();
            builder.AppendLine(includeDetail
                ? "| SourceKey | Title | Description | Languages | Source |"
                : "| SourceKey | Title | Description |");
            builder.AppendLine(includeDetail
                ? "| --- | --- | --- | --- | --- |"
                : "| --- | --- | --- |");
            foreach (var demo in group.OrderBy(demo => demo.Priority).ThenBy(demo => demo.SourceKey, StringComparer.Ordinal))
            {
                builder.AppendLine(includeDetail
                    ? $"| `{demo.SourceKey}` | {demo.Title} | {demo.Description} | {string.Join(", ", demo.Snippets.Select(snippet => snippet.Language))} | `{demo.SourcePath}` |"
                    : $"| `{demo.SourceKey}` | {demo.Title} | {demo.Description} |");
            }

            builder.AppendLine();
        }

        AppendWarningsMarkdown(builder, payload);
        return builder.ToString().TrimEnd();
    }

    private static string RenderExpandedListMarkdown(DemoCommandPayload payload, bool includeDetail)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {payload.Control.Name} Demos");
        builder.AppendLine();

        foreach (var group in payload.Demos.GroupBy(demo => demo.Scenario, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"## {FormatScenario(group.Key)}");
            builder.AppendLine();

            foreach (var demo in group.OrderBy(demo => demo.Priority).ThenBy(demo => demo.SourceKey, StringComparer.Ordinal))
            {
                AppendExpandedDemoMarkdown(builder, demo, includeDetail);
                builder.AppendLine();
            }
        }

        AppendWarningsMarkdown(builder, payload);
        AppendSnapshotMarkdown(builder, payload, includeDetail);
        return builder.ToString().TrimEnd();
    }

    private static string RenderDetailMarkdown(DemoCommandPayload payload, bool includeDetail)
    {
        var demo = payload.SelectedDemo ?? throw new InvalidOperationException("Selected demo is required.");
        var builder = new StringBuilder();
        builder.AppendLine($"# {payload.Control.Name} Demo: {demo.Title}");
        builder.AppendLine();
        builder.AppendLine($"SourceKey: `{demo.SourceKey}`");
        builder.AppendLine($"Scenario: `{demo.Scenario}`");
        if (includeDetail)
        {
            builder.AppendLine($"Source: `{demo.SourcePath}{FormatSourceLine(demo.SourceLine)}`");
            builder.AppendLine($"Snapshot: `{payload.Source.SnapshotId}`");
            builder.AppendLine($"Source ref: `{payload.Source.SourceRootConvention} @ {payload.Source.SourceRef}`");
            builder.AppendLine($"Source commit: `{payload.Source.SourceCommit}`");
            builder.AppendLine($"Generated: `{payload.Source.GeneratedAt}`");
        }

        builder.AppendLine();
        builder.AppendLine(demo.Description);

        foreach (var snippet in demo.Snippets)
        {
            builder.AppendLine();
            builder.AppendLine($"```{snippet.Language}");
            builder.AppendLine(snippet.Code);
            builder.AppendLine("```");
        }

        if (demo.RelatedCommands.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Related");
            builder.AppendLine();
            builder.AppendLine("```bash");
            foreach (var related in demo.RelatedCommands)
            {
                builder.AppendLine(related.Command);
            }

            builder.AppendLine("```");
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendWarningsText(StringBuilder builder, DemoCommandPayload payload)
    {
        if (payload.Warnings.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Warnings:");
        foreach (var warning in payload.Warnings)
        {
            builder.AppendLine($"  {warning.Code}: {warning.Message}");
        }
    }

    private static void AppendWarningsMarkdown(StringBuilder builder, DemoCommandPayload payload)
    {
        if (payload.Warnings.Count == 0)
        {
            return;
        }

        builder.AppendLine("## Warnings");
        builder.AppendLine();
        foreach (var warning in payload.Warnings)
        {
            builder.AppendLine($"- `{warning.Code}`: {warning.Message}");
        }

        builder.AppendLine();
    }

    private static void AppendExpandedDemoText(
        StringBuilder builder,
        DemoItemPayload demo,
        bool includeDetail)
    {
        builder.AppendLine($"{demo.SourceKey} - {demo.Title}");
        if (includeDetail)
        {
            builder.AppendLine($"Source: {demo.SourcePath}{FormatSourceLine(demo.SourceLine)}");
        }

        builder.AppendLine(demo.Description);

        foreach (var snippet in demo.Snippets)
        {
            builder.AppendLine();
            builder.AppendLine($"{FormatSnippetLanguage(snippet.Language)}:");
            AppendIndentedCode(builder, snippet.Code);
        }

        if (demo.RelatedCommands.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Related:");
            foreach (var related in demo.RelatedCommands)
            {
                builder.AppendLine($"  {related.Command}");
            }
        }
    }

    private static void AppendExpandedDemoMarkdown(
        StringBuilder builder,
        DemoItemPayload demo,
        bool includeDetail)
    {
        builder.AppendLine($"### {demo.Title}");
        builder.AppendLine();
        builder.AppendLine($"SourceKey: `{demo.SourceKey}`");
        builder.AppendLine($"Scenario: `{demo.Scenario}`");
        if (includeDetail)
        {
            builder.AppendLine($"Source: `{demo.SourcePath}{FormatSourceLine(demo.SourceLine)}`");
        }

        builder.AppendLine();
        builder.AppendLine(demo.Description);

        foreach (var snippet in demo.Snippets)
        {
            builder.AppendLine();
            builder.AppendLine($"```{snippet.Language}");
            builder.AppendLine(snippet.Code);
            builder.AppendLine("```");
        }

        if (demo.RelatedCommands.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Related:");
            foreach (var related in demo.RelatedCommands)
            {
                builder.AppendLine($"- `{related.Command}`");
            }
        }
    }

    private static void AppendSnapshotText(
        StringBuilder builder,
        DemoCommandPayload payload,
        bool includeDetail)
    {
        if (!includeDetail)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Snapshot:");
        builder.AppendLine($"  id: {payload.Source.SnapshotId}");
        builder.AppendLine($"  source ref: {payload.Source.SourceRootConvention} @ {payload.Source.SourceRef}");
        builder.AppendLine($"  source commit: {payload.Source.SourceCommit}");
        builder.AppendLine($"  generated: {payload.Source.GeneratedAt}");
    }

    private static void AppendSnapshotMarkdown(
        StringBuilder builder,
        DemoCommandPayload payload,
        bool includeDetail)
    {
        if (!includeDetail)
        {
            return;
        }

        builder.AppendLine("## Snapshot");
        builder.AppendLine();
        builder.AppendLine($"- Snapshot: `{payload.Source.SnapshotId}`");
        builder.AppendLine($"- Source ref: `{payload.Source.SourceRootConvention} @ {payload.Source.SourceRef}`");
        builder.AppendLine($"- Source commit: `{payload.Source.SourceCommit}`");
        builder.AppendLine($"- Generated: `{payload.Source.GeneratedAt}`");
        builder.AppendLine();
    }

    private static void AppendIndentedCode(StringBuilder builder, string code)
    {
        foreach (var line in code.Split(Environment.NewLine))
        {
            builder.AppendLine($"  {line}");
        }
    }

    private static string FormatScenario(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Other"
            : char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static string FormatSnippetLanguage(string value)
    {
        return value.Equals("xml", StringComparison.OrdinalIgnoreCase)
            ? "XAML"
            : value.ToUpperInvariant();
    }

    private static DemoItemPayload SelectSuggestedDemo(IReadOnlyList<DemoItemPayload> demos)
    {
        return demos.FirstOrDefault(demo => demo.Scenario.Equals("state", StringComparison.OrdinalIgnoreCase))
               ?? demos.FirstOrDefault(demo => demo.Scenario.Equals("basic", StringComparison.OrdinalIgnoreCase))
               ?? demos[0];
    }

    private static string FormatSourceLine(int? sourceLine)
    {
        return sourceLine is null ? string.Empty : $":{sourceLine.Value}";
    }
}
