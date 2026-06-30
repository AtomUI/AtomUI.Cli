using System.Text.RegularExpressions;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;

internal sealed partial class ControlSourceProcessor : ISourceAnalysisProcessor
{
    public string Id => "control-source";

    public IReadOnlySet<SourceAnalysisFeature> Provides { get; } =
        new HashSet<SourceAnalysisFeature>
        {
            SourceAnalysisFeature.ControlApi,
            SourceAnalysisFeature.AvaloniaProperty,
            SourceAnalysisFeature.EventContract,
            SourceAnalysisFeature.TemplatePart,
            SourceAnalysisFeature.PseudoClass,
            SourceAnalysisFeature.LifecycleContract
        };

    public IReadOnlySet<SourceAnalysisFeature> Requires { get; } = new HashSet<SourceAnalysisFeature>();

    public SourceAnalysisPhase Phase => SourceAnalysisPhase.ExtractFacts;

    public ValueTask ExecuteAsync(SourceAnalysisContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var constantIndex = LoadPseudoClassConstants(context.Paths.SourceRoot);
        foreach (var path in EnumerateSourceFiles(context.Paths.SourceRoot, "*.cs"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = File.ReadAllText(path);
            var controlName = ResolveClassName(text, path);
            if (string.IsNullOrWhiteSpace(controlName))
            {
                continue;
            }

            var relativePath = context.Paths.GetRelativePath(path);
            var aliases = LoadTypeAliases(text);
            ExtractControlType(context, controlName, text, relativePath, aliases);
            ExtractApiMembers(context, controlName, text, relativePath);
            ExtractApiEvents(context, controlName, text, relativePath);
            ExtractPseudoClassAttributes(context, controlName, text, relativePath, constantIndex);
            ExtractPseudoClassStateFlows(context, controlName, text, relativePath, constantIndex);
            ExtractNameScopeLookups(context, controlName, text, relativePath);
        }

        return ValueTask.CompletedTask;
    }

    private static IReadOnlyDictionary<string, string> LoadPseudoClassConstants(string sourceRoot)
    {
        var constants = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in EnumerateSourceFiles(sourceRoot, "*.cs"))
        {
            var text = File.ReadAllText(path);
            var className = ResolveClassName(text, path);
            if (string.IsNullOrWhiteSpace(className))
            {
                continue;
            }

            foreach (Match match in ConstStringRegex().Matches(text))
            {
                var name = match.Groups["name"].Value;
                var value = match.Groups["value"].Value;
                if (!value.StartsWith(':'))
                {
                    continue;
                }

                constants[$"{className}.{name}"] = value;
                constants[name] = value;
            }
        }

        return constants;
    }

    private static IReadOnlyDictionary<string, string> LoadTypeAliases(string text)
    {
        return UsingAliasRegex()
            .Matches(text)
            .ToDictionary(
                match => match.Groups["alias"].Value,
                match => match.Groups["type"].Value.Trim(),
                StringComparer.Ordinal);
    }

    private static void ExtractControlType(
        SourceAnalysisContext context,
        string controlName,
        string text,
        string relativePath,
        IReadOnlyDictionary<string, string> aliases)
    {
        var classMatch = ClassDeclarationRegex().Match(text);
        if (!classMatch.Success || !classMatch.Groups["name"].Value.Equals(controlName, StringComparison.Ordinal))
        {
            return;
        }

        var baseTypes = classMatch.Groups["bases"].Success
            ? classMatch.Groups["bases"].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => ResolveTypeAlias(value.Trim(), aliases))
                .ToArray()
            : [];
        var baseType = baseTypes.FirstOrDefault(value => !value.StartsWith('I'));
        var interfaces = baseTypes
            .Where(value => value.StartsWith('I'))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var namespaceName = NamespaceRegex().Match(text).Groups["name"].Value;
        var line = GetLineNumber(text, classMatch.Index);
        context.Facts.Add(
            $"control-type:{controlName}",
            "control-source",
            new ControlTypeFact(
                controlName,
                string.IsNullOrWhiteSpace(namespaceName) ? "AtomUI.Desktop.Controls" : namespaceName,
                baseType,
                interfaces,
                new SourceLocation(relativePath, line)),
            new SourceLocation(relativePath, line));
    }

    private static string ResolveTypeAlias(string value, IReadOnlyDictionary<string, string> aliases)
    {
        var normalized = NormalizeWhitespace(value);
        return aliases.TryGetValue(normalized, out var resolved) ? resolved : normalized;
    }

    private static void ExtractApiMembers(
        SourceAnalysisContext context,
        string controlName,
        string text,
        string relativePath)
    {
        var emitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in AvaloniaPropertyRegex().Matches(text))
        {
            var propertyName = match.Groups["name"].Value;
            var line = GetLineNumber(text, match.Index);
            var kind = match.Groups["kind"].Value.Equals("DirectProperty", StringComparison.Ordinal)
                ? "direct-property"
                : "styled-property";
            AddMember(
                context,
                emitted,
                controlName,
                propertyName,
                kind,
                "public",
                $"{match.Groups["access"].Value} static readonly {match.Groups["kind"].Value}<{match.Groups["type"].Value}> {propertyName}Property",
                match.Groups["type"].Value.Trim(),
                null,
                "public-api",
                $"{propertyName} is a source-defined {match.Groups["kind"].Value}.",
                relativePath,
                line);
        }

        foreach (Match match in ConstructorRegex().Matches(text))
        {
            var line = GetLineNumber(text, match.Index);
            AddMember(
                context,
                emitted,
                controlName,
                ".ctor",
                "constructor",
                match.Groups["access"].Value,
                NormalizeWhitespace(match.Value.Split('{')[0]),
                null,
                null,
                "public-api",
                $"{controlName} constructor extracted from source.",
                relativePath,
                line);
        }

        foreach (Match match in MethodRegex().Matches(text))
        {
            var access = match.Groups["access"].Value;
            var name = match.Groups["name"].Value;
            if (name.Equals(controlName, StringComparison.Ordinal))
            {
                continue;
            }

            var modifiers = NormalizeWhitespace(match.Groups["modifiers"].Value);
            var kind = access.Equals("protected", StringComparison.Ordinal)
                ? "protected-method"
                : "public-method";
            var line = GetLineNumber(text, match.Index);
            AddMember(
                context,
                emitted,
                controlName,
                name,
                kind,
                string.IsNullOrWhiteSpace(modifiers) ? access : $"{access} {modifiers}",
                NormalizeWhitespace(match.Value.Split('{')[0]),
                match.Groups["type"].Value.Trim(),
                null,
                access.Equals("protected", StringComparison.Ordinal) ? "extension-point" : "public-api",
                $"{name} is a {kind.Replace('-', ' ')} extracted from source.",
                relativePath,
                line);
        }
    }

    private static void AddMember(
        SourceAnalysisContext context,
        HashSet<string> emitted,
        string controlName,
        string name,
        string kind,
        string accessibility,
        string signature,
        string? type,
        string? defaultValue,
        string contractLevel,
        string description,
        string relativePath,
        int line)
    {
        var key = $"{name}:{kind}:{line}";
        if (!emitted.Add(key))
        {
            return;
        }

        context.Facts.Add(
            $"control-api:{controlName}:{name}:{kind}:{line}",
            "control-source",
            new ControlApiMemberFact(
                controlName,
                name,
                kind,
                controlName,
                accessibility,
                signature,
                type,
                defaultValue,
                contractLevel,
                description,
                new SourceLocation(relativePath, line)),
            new SourceLocation(relativePath, line));
    }

    private static void ExtractApiEvents(
        SourceAnalysisContext context,
        string controlName,
        string text,
        string relativePath)
    {
        foreach (Match match in EventRegex().Matches(text))
        {
            var line = GetLineNumber(text, match.Index);
            var explicitOwner = match.Groups["owner"].Success ? match.Groups["owner"].Value : null;
            var name = explicitOwner is null
                ? match.Groups["name"].Value
                : $"{explicitOwner}.{match.Groups["name"].Value}";
            context.Facts.Add(
                $"control-event:{controlName}:{name}",
                "control-source",
                new ControlApiEventFact(
                    controlName,
                    name,
                    explicitOwner is null ? "clr-event" : "interface-event",
                    explicitOwner ?? controlName,
                    match.Groups["access"].Success ? match.Groups["access"].Value : "explicit",
                    NormalizeWhitespace(match.Value.Split('{')[0]),
                    null,
                    match.Groups["type"].Value.Trim(),
                    $"{name} event extracted from source.",
                    new SourceLocation(relativePath, line)),
                new SourceLocation(relativePath, line));
        }
    }

    private static void ExtractPseudoClassAttributes(
        SourceAnalysisContext context,
        string controlName,
        string text,
        string relativePath,
        IReadOnlyDictionary<string, string> constantIndex)
    {
        foreach (Match match in PseudoClassesAttributeRegex().Matches(text))
        {
            foreach (var rawValue in SplitArguments(match.Groups["args"].Value))
            {
                var pseudoClass = ResolvePseudoClass(rawValue, constantIndex);
                if (pseudoClass is null)
                {
                    continue;
                }

                var line = GetLineNumber(text, match.Index);
                context.Facts.Add(
                    $"pseudo-class:{controlName}:{pseudoClass}",
                    "control-source",
                    new PseudoClassFact(
                        controlName,
                        pseudoClass,
                        rawValue,
                        new SourceLocation(relativePath, line)),
                    new SourceLocation(relativePath, line));
            }
        }
    }

    private static void ExtractPseudoClassStateFlows(
        SourceAnalysisContext context,
        string controlName,
        string text,
        string relativePath,
        IReadOnlyDictionary<string, string> constantIndex)
    {
        foreach (Match match in PseudoClassesSetRegex().Matches(text))
        {
            var pseudoClass = ResolvePseudoClass(match.Groups["pseudo"].Value, constantIndex);
            if (pseudoClass is null)
            {
                continue;
            }

            var condition = NormalizeWhitespace(match.Groups["condition"].Value);
            var line = GetLineNumber(text, match.Index);
            context.Facts.Add(
                $"state-flow:{controlName}:{pseudoClass}",
                "control-source",
                new StateFlowFact(
                    controlName,
                    pseudoClass,
                    condition,
                    ExtractReferencedApis(condition),
                    new SourceLocation(relativePath, line)),
                new SourceLocation(relativePath, line));
        }
    }

    private static void ExtractNameScopeLookups(
        SourceAnalysisContext context,
        string controlName,
        string text,
        string relativePath)
    {
        foreach (Match match in NameScopeFindRegex().Matches(text))
        {
            var partName = match.Groups["name"].Value;
            var requestedType = match.Groups["type"].Success
                ? match.Groups["type"].Value.Trim()
                : null;
            var line = GetLineNumber(text, match.Index);
            context.Facts.Add(
                $"name-scope-lookup:{controlName}:{partName}",
                "control-source",
                new NameScopeLookupFact(
                    controlName,
                    partName,
                    requestedType,
                    new SourceLocation(relativePath, line)),
                new SourceLocation(relativePath, line));
        }
    }

    private static IReadOnlyList<string> ExtractReferencedApis(string condition)
    {
        return IdentifierRegex()
            .Matches(condition)
            .Select(match => match.Value)
            .Where(value => value.Length > 1 && char.IsUpper(value[0]))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string? ResolvePseudoClass(string expression, IReadOnlyDictionary<string, string> constantIndex)
    {
        var value = expression.Trim();
        if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2)
        {
            return value[1..^1];
        }

        return constantIndex.TryGetValue(value, out var pseudoClass) ? pseudoClass : null;
    }

    private static IReadOnlyList<string> SplitArguments(string text)
    {
        return text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => NormalizeWhitespace(value))
            .Where(value => value.Length > 0)
            .ToArray();
    }

    private static string ResolveClassName(string text, string path)
    {
        var match = ClassRegex().Match(text);
        return match.Success ? match.Groups["name"].Value : Path.GetFileNameWithoutExtension(path);
    }

    private static IEnumerable<string> EnumerateSourceFiles(string sourceRoot, string pattern)
    {
        if (!Directory.Exists(sourceRoot))
        {
            return [];
        }

        return Directory.EnumerateFiles(sourceRoot, pattern, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.Contains($"{Path.DirectorySeparatorChar}GeneratedFiles{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal);
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

    private static string NormalizeWhitespace(string text)
    {
        return WhitespaceRegex().Replace(text.Trim(), " ");
    }

    [GeneratedRegex(@"(?:public|internal|private|protected)?\s*(?:static\s+)?(?:partial\s+)?class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)")]
    private static partial Regex ClassRegex();

    [GeneratedRegex(@"namespace\s+(?<name>[A-Za-z_][A-Za-z0-9_.]*)\s*;")]
    private static partial Regex NamespaceRegex();

    [GeneratedRegex(@"using\s+(?<alias>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<type>[^;]+);")]
    private static partial Regex UsingAliasRegex();

    [GeneratedRegex(@"class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?::\s*(?<bases>[^{]+))?\{", RegexOptions.Singleline)]
    private static partial Regex ClassDeclarationRegex();

    [GeneratedRegex(@"(?<access>public)\s+static\s+readonly\s+(?<kind>StyledProperty|DirectProperty)<(?<type>[^>]+)>\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)Property\s*=", RegexOptions.Singleline)]
    private static partial Regex AvaloniaPropertyRegex();

    [GeneratedRegex(@"(?<access>public)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\([^)]*\)\s*\{", RegexOptions.Singleline)]
    private static partial Regex ConstructorRegex();

    [GeneratedRegex(@"(?<access>public|protected)\s+(?<modifiers>(?:override|virtual|static|async|sealed|new)\s+)*(?<type>[A-Za-z_][A-Za-z0-9_<>.,?\s]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\((?<params>[^)]*)\)\s*(?:=>|[{;])", RegexOptions.Singleline)]
    private static partial Regex MethodRegex();

    [GeneratedRegex(@"(?:(?<access>public|protected|private|internal)\s+)?event\s+(?<type>[A-Za-z_][A-Za-z0-9_<>.?]*)\s+(?:(?<owner>[A-Za-z_][A-Za-z0-9_]*)\.)?(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Singleline)]
    private static partial Regex EventRegex();

    [GeneratedRegex(@"const\s+string\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*""(?<value>[^""]+)""")]
    private static partial Regex ConstStringRegex();

    [GeneratedRegex(@"\[PseudoClasses\((?<args>.*?)\)\]", RegexOptions.Singleline)]
    private static partial Regex PseudoClassesAttributeRegex();

    [GeneratedRegex(@"PseudoClasses\.Set\(\s*(?<pseudo>[^,]+)\s*,\s*(?<condition>[^;]+?)\s*\);", RegexOptions.Singleline)]
    private static partial Regex PseudoClassesSetRegex();

    [GeneratedRegex(@"NameScope\.Find(?:<(?<type>[^>]+)>)?\(\s*""(?<name>PART_[^""]+)""\s*\)")]
    private static partial Regex NameScopeFindRegex();

    [GeneratedRegex(@"\b[A-Za-z_][A-Za-z0-9_]*\b")]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
