using System.Text;
using System.Text.RegularExpressions;

namespace AtomUI.Cli.Hosting.Metadata;

public sealed partial class DesignDocumentQueryService(DocumentSnapshotRegistry registry)
{
    private const string SchemaVersion = "1.0";
    private const string CommandName = "design.md";
    private const string TopicId = "design-language";

    private static readonly HashSet<string> SupportedSections = new(StringComparer.OrdinalIgnoreCase)
    {
        "all",
        "overview",
        "principles",
        "layout",
        "tokens",
        "accessibility",
        "patterns",
        "controls"
    };

    private static readonly HashSet<string> SupportedAudiences = new(StringComparer.OrdinalIgnoreCase)
    {
        "developer",
        "agent"
    };

    public DesignDocumentQueryResult Query(DesignCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var section = Normalize(options.Section, "all");
        if (!SupportedSections.Contains(section))
        {
            return DesignDocumentQueryResult.Failure(
                AtomUICliErrorCodes.ArgumentInvalidValue,
                $"Unknown design section '{options.Section}'. Supported sections: {string.Join(", ", SupportedSections.Order(StringComparer.OrdinalIgnoreCase))}.");
        }

        var audience = Normalize(options.Audience, "developer");
        if (!SupportedAudiences.Contains(audience))
        {
            return DesignDocumentQueryResult.Failure(
                AtomUICliErrorCodes.ArgumentInvalidValue,
                $"Unknown design audience '{options.Audience}'. Supported audiences: developer, agent.");
        }

        var snapshots = SelectSnapshots(options.Global.TargetVersion, options.Global.Language);
        if (snapshots.Count == 0)
        {
            var requestedVersion = string.IsNullOrWhiteSpace(options.Global.TargetVersion)
                ? "latest"
                : options.Global.TargetVersion;
            return DesignDocumentQueryResult.Failure(
                AtomUICliErrorCodes.DataVersionUnresolved,
                $"Design document snapshot for version '{requestedVersion}' was not found.");
        }

        var topic = snapshots
            .SelectMany(snapshot => snapshot.Topics)
            .FirstOrDefault(item => item.Id.Equals(TopicId, StringComparison.OrdinalIgnoreCase));
        if (topic is null)
        {
            return DesignDocumentQueryResult.Failure(
                AtomUICliErrorCodes.DataUnavailable,
                "AtomUI design.md is not available in the current metadata snapshot.");
        }

        var fullMarkdown = string.Join(
            Environment.NewLine + Environment.NewLine,
            topic.Sections
                .OrderBy(sectionContent => sectionContent.Order)
                .Select(sectionContent => sectionContent.Markdown));
        var doc = ApplyAudience(SelectSection(fullMarkdown, section), audience);
        var warnings = topic.Warnings
            .Select(warning => new DesignDocumentWarningPayload(warning.Code, warning.Message, warning.Section))
            .ToArray();
        var payload = new DesignCommandPayload(
            SchemaVersion,
            CommandName,
            topic.TargetVersion,
            section,
            audience,
            doc,
            new DesignDocumentSourcePayload(
                topic.SnapshotSchemaVersion,
                topic.SnapshotId,
                topic.Source.SourceRootConvention,
                topic.Source.SourceRef,
                topic.Source.SourceCommit,
                topic.Source.GeneratedAt),
            warnings);

        return DesignDocumentQueryResult.Found(payload);
    }

    private IReadOnlyList<DocumentSnapshot> SelectSnapshots(string? targetVersion, string language)
    {
        var normalizedVersion = string.IsNullOrWhiteSpace(targetVersion) ? null : targetVersion;
        var versionMatches = registry.Snapshots
            .Where(snapshot => normalizedVersion is null
                               || snapshot.TargetVersion.Equals(normalizedVersion, StringComparison.OrdinalIgnoreCase)
                               || snapshot.TargetVersion.StartsWith(normalizedVersion, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(snapshot => snapshot.TargetVersion, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (versionMatches.Length == 0)
        {
            return [];
        }

        var languageMatches = versionMatches
            .Where(snapshot => snapshot.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return languageMatches.Length > 0 ? languageMatches : versionMatches;
    }

    private static string SelectSection(string markdown, string section)
    {
        if (section.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return markdown;
        }

        var sourcePaths = section.ToLowerInvariant() switch
        {
            "overview" => ["docs/overview.md"],
            "controls" => ["docs/controls/overview.md", "docs/engineering/control-documentation-guidelines.md"],
            "principles" => ["docs/engineering/control-development-guidelines.md"],
            "tokens" => ["docs/engineering/control-token-guidelines.md"],
            "patterns" or "layout" => ["docs/gallery/gallery-showcase-design-pattern.md"],
            "accessibility" => Array.Empty<string>(),
            _ => Array.Empty<string>()
        };
        var selected = ExtractSourceBlocks(markdown, sourcePaths).ToArray();
        if (selected.Length > 0)
        {
            return string.Join(Environment.NewLine + Environment.NewLine + "---" + Environment.NewLine + Environment.NewLine, selected);
        }

        return ExtractHeadingSection(markdown, section) ?? markdown;
    }

    private static IEnumerable<string> ExtractSourceBlocks(string markdown, IReadOnlyList<string> sourcePaths)
    {
        if (sourcePaths.Count == 0)
        {
            yield break;
        }

        var sourcePathSet = sourcePaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matches = SourceMarkerRegex().Matches(markdown);
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var sourcePath = match.Groups["path"].Value;
            if (!sourcePathSet.Contains(sourcePath))
            {
                continue;
            }

            var start = match.Index;
            var end = index + 1 < matches.Count ? matches[index + 1].Index : markdown.Length;
            yield return markdown[start..end].Trim().TrimEnd('-').Trim();
        }
    }

    private static string ApplyAudience(string doc, string audience)
    {
        if (!audience.Equals("agent", StringComparison.OrdinalIgnoreCase))
        {
            return doc;
        }

        var builder = new StringBuilder();
        builder.AppendLine("<!-- Audience: agent -->");
        builder.AppendLine();
        builder.AppendLine(doc.Trim());
        return builder.ToString().Trim();
    }

    private static string? ExtractHeadingSection(string markdown, string section)
    {
        var aliases = GetHeadingAliases(section)
            .Select(NormalizeHeadingTitle)
            .ToHashSet(StringComparer.Ordinal);
        if (aliases.Count == 0)
        {
            return null;
        }

        var matches = HeadingRegex().Matches(markdown);
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var title = NormalizeHeadingTitle(match.Groups["title"].Value);
            if (!aliases.Contains(title))
            {
                continue;
            }

            var level = match.Groups["marker"].Value.Length;
            var end = markdown.Length;
            for (var nextIndex = index + 1; nextIndex < matches.Count; nextIndex++)
            {
                var next = matches[nextIndex];
                if (next.Groups["marker"].Value.Length <= level)
                {
                    end = next.Index;
                    break;
                }
            }

            return markdown[match.Index..end].Trim();
        }

        return null;
    }

    private static IReadOnlyList<string> GetHeadingAliases(string section)
    {
        return section.ToLowerInvariant() switch
        {
            "overview" => ["overview", "introduction", "design overview", "总览", "概览", "简介"],
            "principles" => ["principles", "design principles", "development principles", "control principles", "设计原则", "研发原则", "控件研发"],
            "layout" => ["layout", "spacing", "layout and spacing", "布局", "间距", "布局与间距"],
            "tokens" => ["tokens", "token", "design tokens", "design token", "token design", "token 设计", "设计 token", "设计令牌"],
            "accessibility" => ["accessibility", "a11y", "accessible design", "可访问性", "无障碍"],
            "patterns" => ["patterns", "design patterns", "gallery patterns", "模式", "设计模式", "gallery 模式"],
            "controls" => ["controls", "control selection", "control guidelines", "控件", "控件选型", "控件规范"],
            _ => []
        };
    }

    private static string NormalizeHeadingTitle(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
    }

    [GeneratedRegex(@"<!--\s*Source:\s*(?<path>[^>]+?)\s*-->", RegexOptions.Multiline)]
    private static partial Regex SourceMarkerRegex();

    [GeneratedRegex(@"^(?<marker>#{2,6})\s+(?<title>.+?)\s*#*\s*$", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();
}

public sealed record DesignDocumentQueryResult(
    bool IsFound,
    DesignCommandPayload? Payload,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static DesignDocumentQueryResult Found(DesignCommandPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return new DesignDocumentQueryResult(true, payload, null, null);
    }

    public static DesignDocumentQueryResult Failure(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new DesignDocumentQueryResult(false, null, code, message);
    }
}
