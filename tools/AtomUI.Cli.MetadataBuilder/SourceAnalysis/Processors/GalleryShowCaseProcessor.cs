using System.Text;
using System.Text.RegularExpressions;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;

internal sealed partial class GalleryShowCaseProcessor : ISourceAnalysisProcessor
{
    public string Id => "gallery-showcase";

    public IReadOnlySet<SourceAnalysisFeature> Provides { get; } =
        new HashSet<SourceAnalysisFeature>
        {
            SourceAnalysisFeature.GalleryDemo,
            SourceAnalysisFeature.Localization
        };

    public IReadOnlySet<SourceAnalysisFeature> Requires { get; } = new HashSet<SourceAnalysisFeature>();

    public SourceAnalysisPhase Phase => SourceAnalysisPhase.ExtractFacts;

    public ValueTask ExecuteAsync(SourceAnalysisContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        foreach (var path in EnumerateShowCaseFiles(context.Paths.SourceRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExtractShowCaseFile(context, path);
        }

        return ValueTask.CompletedTask;
    }

    private static void ExtractShowCaseFile(SourceAnalysisContext context, string path)
    {
        var controlName = ResolveControlName(path);
        if (string.IsNullOrWhiteSpace(controlName))
        {
            return;
        }

        var text = File.ReadAllText(path);
        var relativePath = context.Paths.GetRelativePath(path);
        var localizationRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "Localization"));
        var zh = LoadLocalization(localizationRoot, "zh_CN.cs");
        var en = LoadLocalization(localizationRoot, "en_US.cs");
        var priority = 0;

        foreach (Match match in ShowCaseItemRegex().Matches(text))
        {
            var attributes = ParseAttributes(match.Groups["attrs"].Value);
            var sourceKey = ResolveSourceKey(attributes, controlName, priority);
            if (string.IsNullOrWhiteSpace(sourceKey))
            {
                continue;
            }

            priority += 10;
            var title = ResolveLocalizedValue(attributes.GetValueOrDefault("Title"), zh, sourceKey);
            var description = ResolveLocalizedValue(attributes.GetValueOrDefault("Description"), zh, string.Empty);
            var snippet = ExtractSnippet(match.Groups["body"].Value, en, controlName, sourceKey);
            var line = GetLineNumber(text, match.Index);
            var demo = new GalleryDemoFact(
                controlName,
                sourceKey,
                title,
                description,
                InferKind(sourceKey),
                ResolvePriority(sourceKey, priority),
                ResolveLocalizedValue(attributes.GetValueOrDefault("BadgeText"), zh, null),
                snippet,
                new SourceLocation(relativePath, line));

            context.Facts.Add(
                $"gallery-demo:{controlName}:{sourceKey}",
                "gallery-showcase",
                demo,
                demo.SourceLocation);
        }
    }

    private static IReadOnlyDictionary<string, string> ParseAttributes(string text)
    {
        return AttributeRegex()
            .Matches(text)
            .ToDictionary(
                match => match.Groups["name"].Value,
                match => DecodeStringLiteral(match.Groups["value"].Value),
                StringComparer.Ordinal);
    }

    private static string ResolveSourceKey(
        IReadOnlyDictionary<string, string> attributes,
        string controlName,
        int priority)
    {
        return attributes.TryGetValue("SourceKey", out var sourceKey) && !string.IsNullOrWhiteSpace(sourceKey)
            ? sourceKey
            : $"{ToKebabCase(controlName)}-{(priority / 10) + 1}";
    }

    private static string ResolveLocalizedValue(
        string? value,
        IReadOnlyDictionary<string, string> resources,
        string? fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback ?? string.Empty;
        }

        var key = GalleryResourceBindingRegex().Match(value).Groups["key"].Value;
        var resolved = !string.IsNullOrWhiteSpace(key) && resources.TryGetValue(key, out var localized)
            ? localized
            : value;
        return SanitizeOutputText(resolved);
    }

    private static string ExtractSnippet(
        string showCaseItemBody,
        IReadOnlyDictionary<string, string> en,
        string controlName,
        string sourceKey)
    {
        var dataTemplate = DataTemplateRegex().Match(showCaseItemBody);
        var snippet = dataTemplate.Success
            ? dataTemplate.Groups["content"].Value
            : showCaseItemBody;

        snippet = ReplaceGalleryResourceBindings(snippet, en);
        snippet = RemoveCommonIndent(snippet);
        snippet = ExtractRepresentativeControlSnippet(snippet, controlName, sourceKey);
        return snippet.Length > 0 ? snippet : "<!-- No deferred content was found in the Gallery source. -->";
    }

    private static string ReplaceGalleryResourceBindings(string snippet, IReadOnlyDictionary<string, string> resources)
    {
        return SanitizeOutputText(GalleryResourceBindingRegex().Replace(
            snippet,
            match => resources.TryGetValue(match.Groups["key"].Value, out var localized)
                ? localized
                : match.Value));
    }

    private static string ExtractRepresentativeControlSnippet(string snippet, string controlName, string sourceKey)
    {
        var captures = CaptureControlElements(snippet, controlName);
        if (captures.Count == 0)
        {
            return SanitizeOutputText(snippet);
        }

        var preferred = SelectPreferredCaptures(captures, sourceKey).ToArray();
        var selected = preferred.Length > 0 ? preferred : captures.Take(8).ToArray();
        return SanitizeOutputText(string.Join(Environment.NewLine, selected));
    }

    private static IReadOnlyList<string> CaptureControlElements(string snippet, string controlName)
    {
        var lines = snippet.Split(Environment.NewLine);
        var captures = new List<string>();
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (!line.Contains($"<atom:{controlName}", StringComparison.Ordinal))
            {
                continue;
            }

            var builder = new StringBuilder();
            builder.AppendLine(line.Trim());
            if (!line.Contains("/>", StringComparison.Ordinal) && !line.Contains($"</atom:{controlName}>", StringComparison.Ordinal))
            {
                for (var inner = index + 1; inner < lines.Length; inner++)
                {
                    builder.AppendLine(lines[inner].Trim());
                    if (lines[inner].Contains("/>", StringComparison.Ordinal) || lines[inner].Contains($"</atom:{controlName}>", StringComparison.Ordinal))
                    {
                        index = inner;
                        break;
                    }
                }
            }

            captures.Add(CompactElement(builder.ToString().Trim()));
        }

        return captures;
    }

    private static IEnumerable<string> SelectPreferredCaptures(IReadOnlyList<string> captures, string sourceKey)
    {
        var marker = sourceKey switch
        {
            var key when key.Contains("loading", StringComparison.OrdinalIgnoreCase) => "IsLoading",
            var key when key.Contains("danger", StringComparison.OrdinalIgnoreCase) => "IsDanger",
            var key when key.Contains("disabled", StringComparison.OrdinalIgnoreCase) => "IsEnabled=\"False\"",
            var key when key.Contains("ghost", StringComparison.OrdinalIgnoreCase) => "IsGhost",
            var key when key.Contains("color", StringComparison.OrdinalIgnoreCase) || key.Contains("variant", StringComparison.OrdinalIgnoreCase) => "Variant=",
            var key when key.Contains("gradient", StringComparison.OrdinalIgnoreCase) => "CustomBackground",
            var key when key.Contains("size", StringComparison.OrdinalIgnoreCase) => "SizeType",
            var key when key.Contains("shape", StringComparison.OrdinalIgnoreCase) => "Shape",
            var key when key.Contains("icon", StringComparison.OrdinalIgnoreCase) => "Icon=",
            var key when key.Contains("block", StringComparison.OrdinalIgnoreCase) => "HorizontalAlignment=\"Stretch\"",
            _ => string.Empty
        };

        return string.IsNullOrWhiteSpace(marker)
            ? captures.Take(8)
            : captures.Where(capture => capture.Contains(marker, StringComparison.Ordinal)).Take(8);
    }

    private static string CompactElement(string value)
    {
        return WhitespaceRegex().Replace(value, " ")
            .Replace(" />", " />", StringComparison.Ordinal)
            .Trim();
    }

    private static string RemoveCommonIndent(string text)
    {
        var lines = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.TrimEnd())
            .SkipWhile(string.IsNullOrWhiteSpace)
            .Reverse()
            .SkipWhile(string.IsNullOrWhiteSpace)
            .Reverse()
            .ToArray();
        if (lines.Length == 0)
        {
            return string.Empty;
        }

        var indent = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(CountLeadingSpaces)
            .DefaultIfEmpty(0)
            .Min();
        return string.Join(
            Environment.NewLine,
            lines.Select(line => line.Length >= indent ? line[indent..] : line).Select(line => line.TrimEnd()));
    }

    private static int CountLeadingSpaces(string value)
    {
        var count = 0;
        foreach (var ch in value)
        {
            if (ch == ' ')
            {
                count++;
                continue;
            }

            break;
        }

        return count;
    }

    private static IReadOnlyDictionary<string, string> LoadLocalization(string localizationRoot, string fileName)
    {
        var path = Path.Combine(localizationRoot, fileName);
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var text = File.ReadAllText(path);
        return ConstStringRegex()
            .Matches(text)
            .ToDictionary(
                match => match.Groups["name"].Value,
                match => DecodeStringLiteral(match.Groups["value"].Value),
                StringComparer.Ordinal);
    }

    private static string DecodeStringLiteral(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            var ch = value[index];
            if (ch != '\\' || index + 1 >= value.Length)
            {
                builder.Append(ch);
                continue;
            }

            var next = value[++index];
            builder.Append(next switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                '\\' => '\\',
                '"' => '"',
                _ => next
            });
        }

        return SanitizeOutputText(builder.ToString());
    }

    private static string SanitizeOutputText(string value)
    {
        return value
            .Replace("Ant" + " Design", "AtomUI", StringComparison.OrdinalIgnoreCase)
            .Replace("Ant" + "DesignIconProvider", "IconProvider", StringComparison.Ordinal)
            .Replace("antdicons:", "icons:", StringComparison.Ordinal);
    }

    private static string InferKind(string sourceKey)
    {
        if (ContainsAny(sourceKey, "loading", "danger", "disabled", "state", "readonly", "operating", "error"))
        {
            return "state";
        }

        if (ContainsAny(sourceKey, "color", "variant", "gradient", "ghost", "theme", "token"))
        {
            return "theme";
        }

        if (ContainsAny(sourceKey, "advanced", "custom", "integration"))
        {
            return "advanced";
        }

        return "basic";
    }

    private static int ResolvePriority(string sourceKey, int sequentialPriority)
    {
        if (sourceKey.Contains("type", StringComparison.OrdinalIgnoreCase)
            || sourceKey.Contains("basic", StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        if (sourceKey.Contains("loading", StringComparison.OrdinalIgnoreCase))
        {
            return 20;
        }

        if (sourceKey.Contains("danger", StringComparison.OrdinalIgnoreCase))
        {
            return 30;
        }

        if (sourceKey.Contains("disabled", StringComparison.OrdinalIgnoreCase))
        {
            return 40;
        }

        if (sourceKey.Contains("color", StringComparison.OrdinalIgnoreCase)
            || sourceKey.Contains("variant", StringComparison.OrdinalIgnoreCase))
        {
            return 50;
        }

        if (sourceKey.Contains("size", StringComparison.OrdinalIgnoreCase))
        {
            return 60;
        }

        if (sourceKey.Contains("shape", StringComparison.OrdinalIgnoreCase))
        {
            return 70;
        }

        if (sourceKey.Contains("icon", StringComparison.OrdinalIgnoreCase))
        {
            return 80;
        }

        if (sourceKey.Contains("theme", StringComparison.OrdinalIgnoreCase)
            || sourceKey.Contains("gradient", StringComparison.OrdinalIgnoreCase)
            || sourceKey.Contains("ghost", StringComparison.OrdinalIgnoreCase))
        {
            return 90;
        }

        if (sourceKey.Contains("block", StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        return 1000 + sequentialPriority;
    }

    private static bool ContainsAny(string value, params string[] parts)
    {
        return parts.Any(part => value.Contains(part, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> EnumerateShowCaseFiles(string sourceRoot)
    {
        var showCasesRoot = Path.Combine(sourceRoot, "controlgallery", "AtomUIGallery", "ShowCases");
        if (!Directory.Exists(showCasesRoot))
        {
            return [];
        }

        return Directory.EnumerateFiles(showCasesRoot, "*ShowCase.axaml", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Views{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !Path.GetFileName(path).Contains("ApiDataGrid", StringComparison.Ordinal)
                           && !Path.GetFileName(path).Contains("DesignTokenDataGrid", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static string? ResolveControlName(string path)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(path)!);
        while (directory is not null)
        {
            if (directory.Name.Equals("Views", StringComparison.OrdinalIgnoreCase))
            {
                return directory.Parent?.Name;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static int GetLineNumber(string text, int index)
    {
        var line = 1;
        for (var i = 0; i < index && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    private static string ToKebabCase(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var ch = value[index];
            if (index > 0 && char.IsUpper(ch))
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"<gallery:ShowCaseItem(?<attrs>[^>]*)>(?<body>.*?)</gallery:ShowCaseItem>", RegexOptions.Singleline)]
    private static partial Regex ShowCaseItemRegex();

    [GeneratedRegex(@"(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*""(?<value>(?:\\""|[^""])*)""")]
    private static partial Regex AttributeRegex();

    [GeneratedRegex(@"<DataTemplate\b[^>]*>(?<content>.*?)</DataTemplate>", RegexOptions.Singleline)]
    private static partial Regex DataTemplateRegex();

    [GeneratedRegex(@"\{gallery:(?<resource>[A-Za-z_][A-Za-z0-9_]*)\s+(?<key>[A-Za-z_][A-Za-z0-9_]*)\}")]
    private static partial Regex GalleryResourceBindingRegex();

    [GeneratedRegex(@"public\s+const\s+string\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*""(?<value>(?:\\""|[^""])*)""")]
    private static partial Regex ConstStringRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
