using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;

internal sealed partial class ControlThemeProcessor : ISourceAnalysisProcessor
{
    private static readonly XName NameAttribute = "Name";
    private static readonly XName XNameAttribute = XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");

    public string Id => "control-theme";

    public IReadOnlySet<SourceAnalysisFeature> Provides { get; } =
        new HashSet<SourceAnalysisFeature>
        {
            SourceAnalysisFeature.ControlTheme,
            SourceAnalysisFeature.ThemeVisualTree,
            SourceAnalysisFeature.ThemeSelector,
            SourceAnalysisFeature.TemplateBinding,
            SourceAnalysisFeature.TokenUsage
        };

    public IReadOnlySet<SourceAnalysisFeature> Requires { get; } = new HashSet<SourceAnalysisFeature>();

    public SourceAnalysisPhase Phase => SourceAnalysisPhase.ExtractFacts;

    public ValueTask ExecuteAsync(SourceAnalysisContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        foreach (var path in EnumerateThemeFiles(context.Paths.SourceRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExtractThemeFile(context, path);
        }

        return ValueTask.CompletedTask;
    }

    private static void ExtractThemeFile(SourceAnalysisContext context, string path)
    {
        XDocument document;
        try
        {
            using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
            document = XDocument.Load(reader, LoadOptions.SetLineInfo);
        }
        catch (XmlException exception)
        {
            context.Diagnostics.Add("ATOMUICLI_SRC_AXAML_PARSE_FAILED", "warning", exception.Message, "control-theme");
            return;
        }

        var root = document.Root;
        if (root is null || !root.Name.LocalName.Equals("ControlTheme", StringComparison.Ordinal))
        {
            return;
        }

        var targetType = root.Attribute("TargetType")?.Value;
        var controlName = NormalizeControlName(targetType);
        if (string.IsNullOrWhiteSpace(controlName))
        {
            return;
        }

        var relativePath = context.Paths.GetRelativePath(path);
        var themeName = Path.GetFileNameWithoutExtension(path);
        var rootLine = GetLine(root);
        context.Facts.Add(
            $"control-theme:{controlName}:{themeName}",
            "control-theme",
            new ControlThemeFact(controlName, themeName, new SourceLocation(relativePath, rootLine)),
            new SourceLocation(relativePath, rootLine));

        foreach (var element in root.Descendants())
        {
            ExtractNamedNode(context, controlName, themeName, relativePath, element);
            ExtractSelector(context, controlName, themeName, relativePath, element);
            ExtractTemplateBindings(context, controlName, themeName, relativePath, element);
        }

        ExtractTemplateTrees(context, controlName, themeName, relativePath, root);
    }

    private static void ExtractTemplateTrees(
        SourceAnalysisContext context,
        string controlName,
        string themeName,
        string relativePath,
        XElement root)
    {
        var index = 0;
        foreach (var controlTemplate in root.Descendants().Where(element => element.Name.LocalName.Equals("ControlTemplate", StringComparison.Ordinal)))
        {
            var roots = controlTemplate.Elements()
                .Where(IsVisualTreeElement)
                .Select(CreateTreeNode)
                .ToArray();
            if (roots.Length == 0)
            {
                continue;
            }

            index++;
            var line = GetLine(controlTemplate);
            var source = new SourceLocation(relativePath, line);
            context.Facts.Add(
                $"theme-template-tree:{controlName}:{themeName}:{index:000}",
                "control-theme",
                new ThemeTemplateTreeFact(
                    controlName,
                    themeName,
                    CreateTemplateSelector(controlTemplate),
                    roots,
                    source),
                source);
        }
    }

    private static ThemeTemplateTreeNodeFact CreateTreeNode(XElement element)
    {
        var name = element.Attribute(NameAttribute)?.Value ?? element.Attribute(XNameAttribute)?.Value;
        var children = element.Elements()
            .Where(IsVisualTreeElement)
            .Select(CreateTreeNode)
            .ToArray();
        return new ThemeTemplateTreeNodeFact(element.Name.LocalName, string.IsNullOrWhiteSpace(name) ? null : name, children);
    }

    private static string CreateTemplateSelector(XElement controlTemplate)
    {
        var selectors = controlTemplate
            .Ancestors()
            .Where(ancestor => ancestor.Name.LocalName.Equals("Style", StringComparison.Ordinal))
            .Reverse()
            .Select(style => style.Attribute("Selector")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        return selectors.Length == 0 ? "default" : string.Join(" > ", selectors);
    }

    private static bool IsVisualTreeElement(XElement element)
    {
        var name = element.Name.LocalName;
        if (name.Contains('.', StringComparison.Ordinal))
        {
            return false;
        }

        return name is not "ControlTemplate"
            and not "Style"
            and not "Setter"
            and not "Binding"
            and not "MultiBinding";
    }

    private static void ExtractNamedNode(
        SourceAnalysisContext context,
        string controlName,
        string themeName,
        string relativePath,
        XElement element)
    {
        var name = element.Attribute(NameAttribute)?.Value ?? element.Attribute(XNameAttribute)?.Value;
        if (string.IsNullOrWhiteSpace(name) || !name.StartsWith("PART_", StringComparison.Ordinal))
        {
            return;
        }

        var line = GetLine(element);
        context.Facts.Add(
            $"theme-node:{controlName}:{name}",
            "control-theme",
            new ThemeNodeFact(
                controlName,
                name,
                element.Name.LocalName,
                themeName,
                new SourceLocation(relativePath, line)),
            new SourceLocation(relativePath, line));
    }

    private static void ExtractSelector(
        SourceAnalysisContext context,
        string controlName,
        string themeName,
        string relativePath,
        XElement element)
    {
        if (!element.Name.LocalName.Equals("Style", StringComparison.Ordinal))
        {
            return;
        }

        var selector = element.Attribute("Selector")?.Value;
        if (string.IsNullOrWhiteSpace(selector))
        {
            return;
        }

        var line = GetLine(element);
        var pseudoClasses = PseudoClassRegex()
            .Matches(selector)
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var targetParts = TargetPartRegex()
            .Matches(selector)
            .Select(match => match.Groups["name"].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        context.Facts.Add(
            $"theme-selector:{controlName}:{StableHash(selector)}",
            "control-theme",
            new ThemeSelectorFact(
                controlName,
                selector,
                pseudoClasses,
                targetParts,
                themeName,
                new SourceLocation(relativePath, line)),
            new SourceLocation(relativePath, line));
    }

    private static void ExtractTemplateBindings(
        SourceAnalysisContext context,
        string controlName,
        string themeName,
        string relativePath,
        XElement element)
    {
        var partName = element.Attribute(NameAttribute)?.Value ?? element.Attribute(XNameAttribute)?.Value;
        foreach (var attribute in element.Attributes())
        {
            var match = TemplateBindingRegex().Match(attribute.Value);
            if (!match.Success)
            {
                continue;
            }

            var line = GetLine(element);
            context.Facts.Add(
                $"template-binding:{controlName}:{partName ?? element.Name.LocalName}:{attribute.Name.LocalName}",
                "control-theme",
                new TemplateBindingFact(
                    controlName,
                    partName,
                    attribute.Name.LocalName,
                    match.Groups["name"].Value,
                    themeName,
                    new SourceLocation(relativePath, line)),
                new SourceLocation(relativePath, line));
        }
    }

    private static IEnumerable<string> EnumerateThemeFiles(string sourceRoot)
    {
        if (!Directory.Exists(sourceRoot))
        {
            return [];
        }

        return Directory.EnumerateFiles(sourceRoot, "*Theme.axaml", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static string? NormalizeControlName(string? targetType)
    {
        if (string.IsNullOrWhiteSpace(targetType))
        {
            return null;
        }

        var separatorIndex = targetType.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex >= 0 ? targetType[(separatorIndex + 1)..] : targetType;
    }

    private static int? GetLine(XObject node)
    {
        return node is IXmlLineInfo lineInfo && lineInfo.HasLineInfo() ? lineInfo.LineNumber : null;
    }

    private static string StableHash(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var ch in value)
            {
                hash = (hash * 31) + ch;
            }

            return hash.ToString("x");
        }
    }

    [GeneratedRegex(@":[A-Za-z][A-Za-z0-9_-]*")]
    private static partial Regex PseudoClassRegex();

    [GeneratedRegex(@"#(?<name>PART_[A-Za-z0-9_]+)")]
    private static partial Regex TargetPartRegex();

    [GeneratedRegex(@"\{TemplateBinding\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)")]
    private static partial Regex TemplateBindingRegex();
}
