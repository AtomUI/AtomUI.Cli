using System.Text;
using System.Text.RegularExpressions;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;

internal sealed partial class MarkdownDocProcessor : ISourceAnalysisProcessor
{
    public string Id => "markdown-doc";

    public IReadOnlySet<SourceAnalysisFeature> Provides { get; } =
        new HashSet<SourceAnalysisFeature>
        {
            SourceAnalysisFeature.MarkdownDoc
        };

    public IReadOnlySet<SourceAnalysisFeature> Requires { get; } = new HashSet<SourceAnalysisFeature>();

    public SourceAnalysisPhase Phase => SourceAnalysisPhase.ExtractFacts;

    public ValueTask ExecuteAsync(SourceAnalysisContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var document = CreateDesignTopic(context);
        context.Facts.Add("document:topic:design-language", Id, document);
        return ValueTask.CompletedTask;
    }

    private static ExtractedCatalogDocument CreateDesignTopic(SourceAnalysisContext context)
    {
        var sourceFiles = ResolveDesignSourceFiles(context.Paths.SourceRoot)
            .Where(File.Exists)
            .ToArray();
        var builder = new StringBuilder();
        foreach (var path in sourceFiles)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine("---");
                builder.AppendLine();
            }

            builder.AppendLine(ReadMarkdownSource(context, path));
        }

        var markdown = builder.Length == 0
            ? "# AtomUI 文档总览\n\n当前源码未提供可生成的设计文档。"
            : builder.ToString().Trim();
        return new ExtractedCatalogDocument(
            "topic",
            "design-language",
            ExtractTitle(markdown),
            SanitizeText(markdown));
    }

    private static IEnumerable<string> ResolveDesignSourceFiles(string sourceRoot)
    {
        yield return Path.Combine(sourceRoot, "docs", "overview.md");
        yield return Path.Combine(sourceRoot, "docs", "controls", "overview.md");
        yield return Path.Combine(sourceRoot, "docs", "engineering", "control-development-guidelines.md");
        yield return Path.Combine(sourceRoot, "docs", "engineering", "control-documentation-guidelines.md");
        yield return Path.Combine(sourceRoot, "docs", "engineering", "control-token-guidelines.md");
        yield return Path.Combine(sourceRoot, "docs", "gallery", "gallery-showcase-design-pattern.md");
    }

    private static string ReadMarkdownSource(SourceAnalysisContext context, string path)
    {
        var relativePath = context.Paths.GetRelativePath(path);
        var markdown = File.ReadAllText(path).Trim();
        return $"<!-- Source: {relativePath} -->{Environment.NewLine}{markdown}";
    }

    private static string ExtractTitle(string markdown)
    {
        var match = HeadingRegex().Match(markdown);
        return match.Success ? match.Groups["title"].Value.Trim() : "AtomUI 文档总览";
    }

    private static string SanitizeText(string value)
    {
        return value
            .Replace("Ant" + " Design", "AtomUI", StringComparison.OrdinalIgnoreCase)
            .Replace("Ant" + "Design", "AtomUI", StringComparison.OrdinalIgnoreCase)
            .Replace("\u7ec4\u4ef6", "\u63a7\u4ef6", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^#\s+(?<title>.+)$", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();
}
