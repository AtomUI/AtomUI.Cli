using System.Text.RegularExpressions;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;

internal sealed partial class GalleryCatalogProcessor : ISourceAnalysisProcessor
{
    private static readonly HashSet<string> ExcludedPages = new(StringComparer.Ordinal)
    {
        "Palette",
        "Icons",
        "Icon",
        "CustomizeTheme"
    };

    public string Id => "gallery-catalog";

    public IReadOnlySet<SourceAnalysisFeature> Provides { get; } =
        new HashSet<SourceAnalysisFeature>
        {
            SourceAnalysisFeature.ControlCatalog,
            SourceAnalysisFeature.GalleryNavigation
        };

    public IReadOnlySet<SourceAnalysisFeature> Requires { get; } = new HashSet<SourceAnalysisFeature>();

    public SourceAnalysisPhase Phase => SourceAnalysisPhase.ExtractFacts;

    public ValueTask ExecuteAsync(SourceAnalysisContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var modulePath = Path.Combine(context.Paths.SourceRoot, "controlgallery", "AtomUIGallery", "AtomUIGalleryModule.cs");
        if (!File.Exists(modulePath))
        {
            throw new FileNotFoundException("AtomUI Gallery module source was not found.", modulePath);
        }

        var text = File.ReadAllText(modulePath);
        var categories = ExtractCategories(text);
        var controls = ExtractControls(context.Paths.SourceRoot, text, categories);

        foreach (var category in categories)
        {
            context.Facts.Add($"category:{category.Id}", Id, category);
        }

        foreach (var control in controls)
        {
            context.Facts.Add($"control:{control.Name}", Id, control);
        }

        return ValueTask.CompletedTask;
    }

    private static IReadOnlyList<ExtractedCatalogCategory> ExtractCategories(string text)
    {
        return CategoryRegex()
            .Matches(text)
            .Select((match, index) => new ExtractedCatalogCategory(
                NormalizeCategoryId(match.Groups["key"].Value),
                match.Groups["name"].Value,
                match.Groups["key"].Value,
                (index + 1) * 100))
            .ToArray();
    }

    private static IReadOnlyList<ExtractedCatalogControl> ExtractControls(
        string sourceRoot,
        string text,
        IReadOnlyList<ExtractedCatalogCategory> categories)
    {
        var controls = new List<ExtractedCatalogControl>();
        foreach (var category in categories)
        {
            var blockMatch = Regex.Match(
                text,
                $@"AddGroup\(""{Regex.Escape(category.RouteSegment)}"".*?;\s*",
                RegexOptions.Singleline);
            if (!blockMatch.Success)
            {
                continue;
            }

            var order = 0;
            foreach (Match pageMatch in PageRegex().Matches(blockMatch.Value))
            {
                var displayName = pageMatch.Groups["name"].Value;
                if (ExcludedPages.Contains(displayName))
                {
                    continue;
                }

                order += 10;
                var packageId = ResolvePackageId(displayName);
                var productId = ResolveProductId(packageId);
                var isCommercial = productId.Equals("datagrid", StringComparison.Ordinal);
                var isOptional = !productId.Equals("desktop", StringComparison.Ordinal);
                controls.Add(new ExtractedCatalogControl(
                    displayName,
                    displayName,
                    productId,
                    packageId,
                    "AtomUI.Desktop.Controls",
                    $"{category.RouteSegment}/{displayName}",
                    CreateDescription(sourceRoot, displayName),
                    category.Id,
                    category.Name,
                    category.DisplayOrder + order,
                    isCommercial,
                    isOptional,
                    false));
            }
        }

        return controls.ToArray();
    }

    private static string NormalizeCategoryId(string value)
    {
        return value switch
        {
            "DataEntry" => "data-entry",
            "DataDisplay" => "data-display",
            _ => string.Concat(value.Select((ch, index) =>
                index > 0 && char.IsUpper(ch) ? $"-{char.ToLowerInvariant(ch)}" : char.ToLowerInvariant(ch).ToString()))
        };
    }

    private static string ResolvePackageId(string controlName)
    {
        return controlName switch
        {
            "ColorPicker" => "AtomUI.Desktop.Controls.ColorPicker",
            "DataGrid" => "AtomUI.Desktop.Controls.DataGrid",
            _ => "AtomUI.Desktop.Controls"
        };
    }

    private static string ResolveProductId(string packageId)
    {
        return packageId switch
        {
            "AtomUI.Desktop.Controls.ColorPicker" => "colorpicker",
            "AtomUI.Desktop.Controls.DataGrid" => "datagrid",
            _ => "desktop"
        };
    }

    private static string CreateDescription(string sourceRoot, string controlName)
    {
        var llmsPath = Path.Combine(sourceRoot, "docs", "AI", "llms", "controls", ToKebabCase(controlName), "index-cn.md");
        if (File.Exists(llmsPath))
        {
            var description = ExtractOverviewParagraph(File.ReadAllText(llmsPath));
            if (!string.IsNullOrWhiteSpace(description))
            {
                return SanitizeOutputText(description);
            }
        }

        return $"{controlName} control.";
    }

    private static string ExtractOverviewParagraph(string markdown)
    {
        var overviewIndex = markdown.IndexOf("## 概述", StringComparison.Ordinal);
        if (overviewIndex < 0)
        {
            return string.Empty;
        }

        var content = markdown[(overviewIndex + "## 概述".Length)..];
        foreach (var block in content.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = block.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith('>') || trimmed.StartsWith('|'))
            {
                continue;
            }

            return WhitespaceRegex().Replace(trimmed, " ");
        }

        return string.Empty;
    }

    private static string SanitizeOutputText(string value)
    {
        return value
            .Replace("Ant" + " Design", "AtomUI", StringComparison.OrdinalIgnoreCase)
            .Replace("Ant" + "Design", "AtomUI", StringComparison.OrdinalIgnoreCase)
            .Replace("\u7ec4\u4ef6", "\u63a7\u4ef6", StringComparison.Ordinal);
    }

    private static string ToKebabCase(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var ch = value[index];
            if (index > 0 && char.IsUpper(ch) && (char.IsLower(value[index - 1]) || index + 1 < value.Length && char.IsLower(value[index + 1])))
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"components\.AddGroup\(""(?<key>[^""]+)"",\s*Nav\([^,]+,\s*""(?<name>[^""]+)""\)")]
    private static partial Regex CategoryRegex();

    [GeneratedRegex(@"\.AddPage\([^,]+,\s*Nav\([^,]+,\s*""(?<name>[^""]+)""\)\)")]
    private static partial Regex PageRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
