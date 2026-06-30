using System.Text.RegularExpressions;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;

internal sealed partial class ChangelogProcessor : ISourceAnalysisProcessor
{
    public string Id => "changelog";

    public IReadOnlySet<SourceAnalysisFeature> Provides { get; } =
        new HashSet<SourceAnalysisFeature>
        {
            SourceAnalysisFeature.Changelog
        };

    public IReadOnlySet<SourceAnalysisFeature> Requires { get; } =
        new HashSet<SourceAnalysisFeature>
        {
            SourceAnalysisFeature.ControlCatalog
        };

    public SourceAnalysisPhase Phase => SourceAnalysisPhase.ExtractFacts;

    public ValueTask ExecuteAsync(SourceAnalysisContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var changelogPath = Path.Combine(context.Paths.SourceRoot, "CHANGELOG.md");
        if (!File.Exists(changelogPath))
        {
            context.Diagnostics.Add("ATOMUICLI_SRC_CHANGELOG_MISSING", "warning", "CHANGELOG.md was not found in AtomUI source root.", Id);
            return ValueTask.CompletedTask;
        }

        var controlNames = context.Facts.GetByPrefix("control:")
            .Select(fact => fact.Value)
            .OfType<ExtractedCatalogControl>()
            .Select(control => control.Name)
            .OrderByDescending(name => name.Length)
            .ThenBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var entries = ParseChangelog(File.ReadAllText(changelogPath), controlNames);
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            context.Facts.Add($"changelog:{entry.Version}:{index:0000}:{entry.TargetKind}:{entry.TargetId}", Id, entry);
        }

        return ValueTask.CompletedTask;
    }

    private static IReadOnlyList<ExtractedCatalogChangelogEntry> ParseChangelog(
        string text,
        IReadOnlyList<string> controlNames)
    {
        var entries = new List<ExtractedCatalogChangelogEntry>();
        foreach (Match versionMatch in VersionBlockRegex().Matches(text))
        {
            var version = versionMatch.Groups["version"].Value.Trim();
            var body = versionMatch.Groups["body"].Value;
            var currentGroup = "info";
            var lines = body.Split('\n');
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var topLevel = TopLevelBulletRegex().Match(line);
                if (topLevel.Success)
                {
                    var value = NormalizeText(topLevel.Groups["text"].Value);
                    if (HasNestedBullet(lines, index))
                    {
                        currentGroup = NormalizeSeverity(value);
                        continue;
                    }

                    AddEntries(entries, version, NormalizeSeverity(value), value, controlNames);
                    continue;
                }

                var nested = NestedBulletRegex().Match(line);
                if (nested.Success)
                {
                    var value = NormalizeText(nested.Groups["text"].Value);
                    AddEntries(entries, version, currentGroup, value, controlNames);
                }
            }
        }

        return entries;
    }

    private static void AddEntries(
        List<ExtractedCatalogChangelogEntry> entries,
        string version,
        string severity,
        string message,
        IReadOnlyList<string> controlNames)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        entries.Add(new ExtractedCatalogChangelogEntry(version, "release", "AtomUI", severity, message));
        foreach (var controlName in FindMentionedControls(message, controlNames))
        {
            entries.Add(new ExtractedCatalogChangelogEntry(version, "control", controlName, severity, message));
        }
    }

    private static bool HasNestedBullet(IReadOnlyList<string> lines, int index)
    {
        for (var next = index + 1; next < lines.Count; next++)
        {
            var line = lines[next];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            return NestedBulletRegex().IsMatch(line);
        }

        return false;
    }

    private static IEnumerable<string> FindMentionedControls(string message, IReadOnlyList<string> controlNames)
    {
        foreach (var controlName in controlNames)
        {
            if (Regex.IsMatch(message, $@"\b{Regex.Escape(controlName)}\b", RegexOptions.CultureInvariant))
            {
                yield return controlName;
            }
        }
    }

    private static string NormalizeSeverity(string value)
    {
        var normalized = value.Trim().Trim(':').ToLowerInvariant();
        if (normalized.Contains("breaking", StringComparison.Ordinal))
        {
            return "breaking";
        }

        if (normalized.Contains("compatibility", StringComparison.Ordinal))
        {
            return "compatibility";
        }

        if (normalized.Contains("added", StringComparison.Ordinal) || normalized.Contains("new", StringComparison.Ordinal))
        {
            return "added";
        }

        if (normalized.Contains("changed", StringComparison.Ordinal) || normalized.Contains("improve", StringComparison.Ordinal))
        {
            return "changed";
        }

        if (normalized.Contains("fixed", StringComparison.Ordinal) || normalized.Contains("fix", StringComparison.Ordinal))
        {
            return "fixed";
        }

        if (normalized.Contains("documentation", StringComparison.Ordinal))
        {
            return "docs";
        }

        if (normalized.Contains("build", StringComparison.Ordinal) || normalized.Contains("release", StringComparison.Ordinal))
        {
            return "build";
        }

        return "info";
    }

    private static string NormalizeText(string value)
    {
        return value
            .Trim()
            .Replace("Ant" + " Design", "AtomUI", StringComparison.OrdinalIgnoreCase)
            .Replace("Ant" + "Design", "AtomUI", StringComparison.OrdinalIgnoreCase)
            .Replace("\u7ec4\u4ef6", "\u63a7\u4ef6", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^##\s+(?<version>[^\r\n]+)\s*(?:\r?\n`[^\r\n]+`)?(?<body>.*?)(?=^##\s+|\z)", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex VersionBlockRegex();

    [GeneratedRegex(@"^-\s+(?<text>.+)$")]
    private static partial Regex TopLevelBulletRegex();

    [GeneratedRegex(@"^\s{2,}-\s+(?<text>.+)$")]
    private static partial Regex NestedBulletRegex();
}
