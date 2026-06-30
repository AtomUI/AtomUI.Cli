using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AtomUI.Cli.MetadataBuilder;

internal sealed partial class TokenSourceExtractor
{
    private static readonly string[] IgnoredPathSegments =
    [
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}GeneratedFiles{Path.DirectorySeparatorChar}"
    ];

    public ExtractedTokenSnapshot Extract(string sourceRoot, string targetVersion)
    {
        var normalizedRoot = Path.GetFullPath(sourceRoot);
        var sourceCommit = ResolveSourceCommit(normalizedRoot);
        var enumIndex = LoadTokenKindIndex(normalizedRoot);
        var registeredControls = LoadRegisteredControlIndex(normalizedRoot);
        var usageIndex = LoadUsageIndex(normalizedRoot);
        var diagnostics = new List<ExtractedDiagnostic>();

        var sharedTokens = ExtractSharedTokens(normalizedRoot)
            .OrderBy(token => KindOrder(token.Kind))
            .ThenBy(token => token.Name, StringComparer.Ordinal)
            .ToArray();

        var controlTokenSets = EnumerateSourceFiles(normalizedRoot, "*Token.cs")
            .Where(IsControlTokenFile)
            .Select(path => ExtractControlTokenSet(
                normalizedRoot,
                path,
                enumIndex,
                registeredControls,
                usageIndex,
                diagnostics))
            .Where(tokenSet => tokenSet is not null)
            .Cast<ExtractedControlTokenSet>()
            .OrderBy(tokenSet => tokenSet.ControlName, StringComparer.Ordinal)
            .ToArray();

        return new ExtractedTokenSnapshot(
            "1.0",
            $"atomui-token-source-{sourceCommit}",
            targetVersion,
            sourceCommit,
            CreateProducts(),
            sharedTokens,
            controlTokenSets,
            diagnostics);
    }

    private static IReadOnlyList<ExtractedProduct> CreateProducts()
    {
        return
        [
            new ExtractedProduct("desktop", "AtomUI Desktop", "AtomUI.Desktop.Controls", false),
            new ExtractedProduct("datagrid", "AtomUI DataGrid", "AtomUI.Desktop.Controls.DataGrid", true),
            new ExtractedProduct("colorpicker", "AtomUI ColorPicker", "AtomUI.Desktop.Controls.ColorPicker", false),
            new ExtractedProduct("controls", "AtomUI Controls", "AtomUI.Controls", false)
        ];
    }

    private static IReadOnlyList<ExtractedSharedToken> ExtractSharedTokens(string sourceRoot)
    {
        var tokenRoot = Path.Combine(sourceRoot, "src", "AtomUI.Core", "Theme", "TokenSystem", "TokenDefinitions");
        if (!Directory.Exists(tokenRoot))
        {
            return [];
        }

        var tokens = new List<ExtractedSharedToken>();
        foreach (var path in Directory.EnumerateFiles(tokenRoot, "DesignToken.*.cs", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var lines = File.ReadAllLines(path);
            for (var index = 0; index < lines.Length; index++)
            {
                var match = PropertyRegex().Match(lines[index]);
                if (!match.Success)
                {
                    continue;
                }

                var name = match.Groups["name"].Value;
                var type = NormalizeType(match.Groups["type"].Value);
                var defaultValue = NormalizeDefaultValue(match.Groups["default"].Value);
                var kind = ResolveSharedKind(path, lines, index);
                tokens.Add(new ExtractedSharedToken(
                    name,
                    kind,
                    ResolveCategory(name, type),
                    type,
                    defaultValue,
                    FindSummary(lines, index),
                    RelativePath(sourceRoot, path),
                    index + 1));
            }
        }

        return tokens
            .GroupBy(token => token.Name, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }

    private static ExtractedControlTokenSet? ExtractControlTokenSet(
        string sourceRoot,
        string path,
        IReadOnlyDictionary<string, TokenKindInfo> enumIndex,
        IReadOnlyDictionary<string, IReadOnlyList<string>> registeredControlIndex,
        IReadOnlyDictionary<string, IReadOnlyList<ExtractedTokenUsage>> usageIndex,
        List<ExtractedDiagnostic> diagnostics)
    {
        var text = File.ReadAllText(path);
        var classMatch = TokenClassRegex().Match(text);
        if (!classMatch.Success)
        {
            return null;
        }

        var lines = File.ReadAllLines(path);
        var tokenClassName = classMatch.Groups["class"].Value;
        var namespaceName = NamespaceRegex().Match(text).Groups["namespace"].Value;
        var tokenId = ResolveTokenId(text, tokenClassName);
        var controlName = tokenId;
        var packageId = ResolvePackageId(path);
        var productId = ResolveProductId(packageId);
        var isCommercial = packageId.Contains(".DataGrid", StringComparison.Ordinal);
        enumIndex.TryGetValue(tokenId, out var tokenKindInfo);

        var properties = ExtractTokenProperties(sourceRoot, path, lines, tokenId, tokenClassName, tokenKindInfo, usageIndex);
        if (properties.Count == 0)
        {
            diagnostics.Add(new ExtractedDiagnostic("ATOMUICLI_TOKEN_NO_PROPERTIES", "warning", $"No token properties were extracted from {RelativePath(sourceRoot, path)}."));
            return null;
        }

        if (tokenKindInfo is not null)
        {
            var propertyNames = properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var enumName in tokenKindInfo.Names.Where(name => !propertyNames.Contains(name)))
            {
                diagnostics.Add(new ExtractedDiagnostic(
                    "ATOMUICLI_TOKEN_KIND_MISSING_SOURCE",
                    "warning",
                    $"{tokenId}TokenKind.{enumName} exists in generated source but was not found as a token property."));
            }
        }

        var registeredControls = registeredControlIndex.TryGetValue(tokenClassName, out var controls) && controls.Count > 0
            ? controls
            : [controlName];
        var themeUsages = properties.SelectMany(property => property.Usages).ToArray();
        var sharedDependencies = properties
            .SelectMany(property => property.Dependencies)
            .Where(dependency => dependency.StartsWith("Shared.", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(dependency => dependency, StringComparer.Ordinal)
            .ToArray();

        return new ExtractedControlTokenSet(
            controlName,
            tokenId,
            string.IsNullOrWhiteSpace(namespaceName) ? tokenClassName : $"{namespaceName}.{tokenClassName}",
            $"{tokenClassName}.ScopeProvider",
            packageId,
            productId,
            isCommercial,
            registeredControls,
            properties,
            sharedDependencies,
            themeUsages);
    }

    private static IReadOnlyList<ExtractedControlToken> ExtractTokenProperties(
        string sourceRoot,
        string path,
        string[] lines,
        string tokenId,
        string tokenClassName,
        TokenKindInfo? tokenKindInfo,
        IReadOnlyDictionary<string, IReadOnlyList<ExtractedTokenUsage>> usageIndex)
    {
        var propertyRows = new List<(string Name, string Type, string? DefaultValue, string Description, int Line)>();
        for (var index = 0; index < lines.Length; index++)
        {
            var match = PropertyRegex().Match(lines[index]);
            if (!match.Success)
            {
                continue;
            }

            propertyRows.Add((
                match.Groups["name"].Value,
                NormalizeType(match.Groups["type"].Value),
                NormalizeDefaultValue(match.Groups["default"].Value),
                FindSummary(lines, index),
                index + 1));
        }

        var tokenNames = propertyRows.Select(row => row.Name).ToArray();
        var dependencies = ExtractControlDependencies(File.ReadAllText(path), tokenId, tokenNames);
        var order = tokenKindInfo?.Names
            .Select((name, index) => (name, index))
            .ToDictionary(item => item.name, item => item.index, StringComparer.Ordinal)
            ?? new Dictionary<string, int>(StringComparer.Ordinal);

        return propertyRows
            .OrderBy(row => order.TryGetValue(row.Name, out var index) ? index : int.MaxValue)
            .ThenBy(row => row.Line)
            .Select(row =>
            {
                var tokenKey = $"{tokenId}.{row.Name}";
                var usages = usageIndex.TryGetValue(tokenKey, out var tokenUsages) ? tokenUsages : [];
                var generatedPath = tokenKindInfo is null ? null : tokenKindInfo.GeneratedPath;
                return new ExtractedControlToken(
                    tokenId,
                    row.Name,
                    ResolveCategory(row.Name, row.Type),
                    row.Type,
                    row.DefaultValue,
                    row.Description,
                    tokenClassName,
                    RelativePath(sourceRoot, path),
                    row.Line,
                    generatedPath,
                    dependencies.TryGetValue(row.Name, out var tokenDependencies) ? tokenDependencies : [],
                    usages,
                    IsInternalToken(path, row.Line) ? "internal" : "public-stable");
            })
            .ToArray();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> ExtractControlDependencies(
        string text,
        string tokenId,
        IReadOnlyList<string> tokenNames)
    {
        var body = ExtractMethodBody(text, "CalculateTokenValues");
        if (string.IsNullOrWhiteSpace(body))
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        }

        var tokenNameSet = tokenNames.ToHashSet(StringComparer.Ordinal);
        var localDependencies = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var tokenDependencies = tokenNames.ToDictionary(name => name, _ => (IReadOnlyList<string>)[], StringComparer.Ordinal);

        foreach (var statement in SplitStatements(body))
        {
            var localMatch = LocalAssignmentRegex().Match(statement);
            if (localMatch.Success)
            {
                var localName = localMatch.Groups["name"].Value;
                var expression = localMatch.Groups["expression"].Value;
                localDependencies[localName] = ExtractDependencies(expression, tokenId, tokenNameSet, localDependencies, null);
            }

            foreach (var tokenName in tokenNames)
            {
                var assignmentMatch = Regex.Match(
                    statement,
                    $@"(?:^|[{{\s;]){Regex.Escape(tokenName)}\s*=\s*(?<expression>.+)$",
                    RegexOptions.Singleline);
                if (!assignmentMatch.Success)
                {
                    continue;
                }

                var dependencies = ExtractDependencies(
                    statement,
                    tokenId,
                    tokenNameSet,
                    localDependencies,
                    tokenName);
                tokenDependencies[tokenName] = tokenDependencies[tokenName]
                    .Concat(dependencies)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
            }
        }

        return tokenDependencies;
    }

    private static IReadOnlyList<string> ExtractDependencies(
        string expression,
        string tokenId,
        IReadOnlySet<string> tokenNames,
        IReadOnlyDictionary<string, IReadOnlyList<string>> localDependencies,
        string? selfTokenName)
    {
        var dependencies = new List<string>();

        foreach (Match match in SharedTokenRegex().Matches(expression))
        {
            dependencies.Add($"Shared.{match.Groups["name"].Value}");
        }

        foreach (var (localName, localDeps) in localDependencies)
        {
            if (Regex.IsMatch(expression, $@"\b{Regex.Escape(localName)}\b"))
            {
                dependencies.AddRange(localDeps);
            }
        }

        foreach (var tokenName in tokenNames.OrderBy(name => expression.IndexOf(name, StringComparison.Ordinal)))
        {
            if (tokenName.Equals(selfTokenName, StringComparison.Ordinal))
            {
                continue;
            }

            if (Regex.IsMatch(expression, $@"(?<![A-Za-z0-9_\.]){Regex.Escape(tokenName)}\b"))
            {
                dependencies.Add($"{tokenId}.{tokenName}");
            }
        }

        return dependencies
            .Where(dependency => !string.IsNullOrWhiteSpace(dependency))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> LoadRegisteredControlIndex(string sourceRoot)
    {
        var result = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var path in EnumerateSourceFiles(sourceRoot, "*.cs"))
        {
            var text = File.ReadAllText(path);
            foreach (Match match in RegisterTokenScopeRegex().Matches(text))
            {
                var tokenClassName = match.Groups["tokenClass"].Value;
                var controlName = ClassNameRegex().Match(text).Groups["class"].Value;
                if (string.IsNullOrWhiteSpace(controlName))
                {
                    controlName = Path.GetFileNameWithoutExtension(path);
                }

                if (!result.TryGetValue(tokenClassName, out var controls))
                {
                    controls = new SortedSet<string>(StringComparer.Ordinal);
                    result[tokenClassName] = controls;
                }

                controls.Add(controlName);
            }
        }

        return result.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<ExtractedTokenUsage>> LoadUsageIndex(string sourceRoot)
    {
        var result = new Dictionary<string, List<ExtractedTokenUsage>>(StringComparer.Ordinal);
        foreach (var path in EnumerateSourceFiles(sourceRoot, "*.axaml"))
        {
            var lines = File.ReadAllLines(path);
            var currentSelector = Path.GetFileNameWithoutExtension(path);
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                var selectorMatch = SelectorRegex().Match(line);
                if (selectorMatch.Success)
                {
                    currentSelector = selectorMatch.Groups["selector"].Value;
                }

                foreach (Match match in TokenResourceRegex().Matches(line))
                {
                    var tokenId = match.Groups["token"].Value;
                    var tokenName = match.Groups["name"].Value;
                    var key = $"{tokenId}.{tokenName}";
                    if (!result.TryGetValue(key, out var usages))
                    {
                        usages = [];
                        result[key] = usages;
                    }

                    usages.Add(new ExtractedTokenUsage(
                        Path.GetFileNameWithoutExtension(path),
                        currentSelector,
                        ResolveTargetProperty(line, match.Index),
                        ResolveTargetNode(line),
                        match.Value,
                        RelativePath(sourceRoot, path),
                        index + 1));
                }
            }
        }

        return result.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<ExtractedTokenUsage>)pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, TokenKindInfo> LoadTokenKindIndex(string sourceRoot)
    {
        var result = new Dictionary<string, TokenKindInfo>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(sourceRoot, "TokenResourceConst.g.cs", SearchOption.AllDirectories)
                     .Where(path => path.Contains($"{Path.DirectorySeparatorChar}GeneratedFiles{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            var text = File.ReadAllText(path);
            foreach (Match match in TokenKindEnumRegex().Matches(text))
            {
                var enumName = match.Groups["enum"].Value;
                if (!enumName.EndsWith("TokenKind", StringComparison.Ordinal) || enumName.Equals("SharedTokenKind", StringComparison.Ordinal))
                {
                    continue;
                }

                var tokenId = enumName[..^"TokenKind".Length];
                var body = match.Groups["body"].Value;
                var names = body
                    .Split([',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(name => IdentifierRegex().IsMatch(name))
                    .ToArray();

                result[tokenId] = new TokenKindInfo(RelativePath(sourceRoot, path), names);
            }
        }

        return result;
    }

    private static IEnumerable<string> EnumerateSourceFiles(string sourceRoot, string pattern)
    {
        return Directory.EnumerateFiles(sourceRoot, pattern, SearchOption.AllDirectories)
            .Where(path => !IgnoredPathSegments.Any(segment => path.Contains(segment, StringComparison.Ordinal)))
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static bool IsControlTokenFile(string path)
    {
        var text = File.ReadAllText(path);
        return text.Contains("AbstractControlDesignToken", StringComparison.Ordinal)
               && text.Contains("ControlDesignToken", StringComparison.Ordinal);
    }

    private static string ResolveTokenId(string text, string tokenClassName)
    {
        var idMatch = TokenIdRegex().Match(text);
        return idMatch.Success ? idMatch.Groups["id"].Value : tokenClassName[..^"Token".Length];
    }

    private static string ResolvePackageId(string path)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(path) ?? ".");
        while (directory is not null)
        {
            var project = directory.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (project is not null)
            {
                return Path.GetFileNameWithoutExtension(project.Name);
            }

            directory = directory.Parent;
        }

        return "AtomUI.Desktop.Controls";
    }

    private static string ResolveProductId(string packageId)
    {
        return packageId switch
        {
            "AtomUI.Desktop.Controls.DataGrid" => "datagrid",
            "AtomUI.Desktop.Controls.ColorPicker" => "colorpicker",
            "AtomUI.Controls" => "controls",
            _ => "desktop"
        };
    }

    private static string ResolveSourceCommit(string sourceRoot)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", "rev-parse --short HEAD")
            {
                WorkingDirectory = sourceRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (process is null)
            {
                return "unknown";
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private static string ResolveSharedKind(string path, string[] lines, int propertyLine)
    {
        for (var index = propertyLine - 1; index >= 0 && index >= propertyLine - 8; index--)
        {
            var match = DesignTokenKindRegex().Match(lines[index]);
            if (match.Success)
            {
                return match.Groups["kind"].Value.ToLowerInvariant();
            }
        }

        var fileName = Path.GetFileName(path);
        if (fileName.Contains(".Seed.", StringComparison.Ordinal))
        {
            return "seed";
        }

        if (fileName.Contains("Map", StringComparison.Ordinal))
        {
            return "map";
        }

        return "alias";
    }

    private static string ResolveCategory(string name, string type)
    {
        if (name.Contains("Color", StringComparison.Ordinal)
            || name.Contains("Bg", StringComparison.Ordinal)
            || name.Contains("Brush", StringComparison.Ordinal)
            || name.Contains("Foreground", StringComparison.Ordinal)
            || name.Contains("Background", StringComparison.Ordinal)
            || type.Contains("Color", StringComparison.Ordinal)
            || type.Contains("Brush", StringComparison.Ordinal))
        {
            return "color";
        }

        if (name.Contains("Shadow", StringComparison.Ordinal) || type.Contains("Shadow", StringComparison.Ordinal))
        {
            return "shadow";
        }

        if (name.Contains("Font", StringComparison.Ordinal)
            || name.Contains("LineHeight", StringComparison.Ordinal)
            || name.Contains("TextHeight", StringComparison.Ordinal)
            || type.Contains("Font", StringComparison.Ordinal))
        {
            return "font";
        }

        if (name.Contains("Motion", StringComparison.Ordinal)
            || name.Contains("Duration", StringComparison.Ordinal)
            || name.Contains("Easing", StringComparison.Ordinal)
            || type.Contains("TimeSpan", StringComparison.Ordinal))
        {
            return "motion";
        }

        if (name.Contains("Radius", StringComparison.Ordinal) || type.Contains("CornerRadius", StringComparison.Ordinal))
        {
            return "radius";
        }

        if (name.Contains("Margin", StringComparison.Ordinal)
            || name.Contains("Spacing", StringComparison.Ordinal)
            || name.Contains("Space", StringComparison.Ordinal)
            || name.Contains("Offset", StringComparison.Ordinal))
        {
            return "spacing";
        }

        if (name.Contains("Padding", StringComparison.Ordinal)
            || name.Contains("Gutter", StringComparison.Ordinal)
            || name.Contains("Height", StringComparison.Ordinal)
            || name.Contains("Width", StringComparison.Ordinal)
            || name.Contains("Size", StringComparison.Ordinal)
            || type.Contains("Thickness", StringComparison.Ordinal))
        {
            return name.Contains("Size", StringComparison.Ordinal) ? "size" : "layout";
        }

        return "other";
    }

    private static string ResolveTargetProperty(string line, int matchIndex)
    {
        var setterMatch = SetterPropertyRegex().Match(line);
        if (setterMatch.Success)
        {
            return setterMatch.Groups["property"].Value;
        }

        var before = line[..matchIndex];
        var attributeMatch = AttributeBeforeResourceRegex().Match(before);
        return attributeMatch.Success ? attributeMatch.Groups["property"].Value : "Value";
    }

    private static string? ResolveTargetNode(string line)
    {
        var match = PartNameRegex().Match(line);
        return match.Success ? match.Groups["part"].Value : null;
    }

    private static string FindSummary(string[] lines, int propertyLine)
    {
        var summaryLines = new Stack<string>();
        for (var index = propertyLine - 1; index >= 0; index--)
        {
            var line = lines[index].Trim();
            if (line.Length == 0 || line.StartsWith("[", StringComparison.Ordinal))
            {
                continue;
            }

            if (!line.StartsWith("///", StringComparison.Ordinal))
            {
                break;
            }

            var text = line[3..].Trim();
            if (text is "<summary>" or "</summary>")
            {
                continue;
            }

            summaryLines.Push(text);
        }

        return string.Join(" ", summaryLines).Trim();
    }

    private static bool IsInternalToken(string path, int propertyLine)
    {
        var lines = File.ReadAllLines(path);
        for (var index = propertyLine - 1; index >= 0; index--)
        {
            var line = lines[index];
            if (line.Contains("#endregion", StringComparison.Ordinal))
            {
                return false;
            }

            if (line.Contains("#region", StringComparison.Ordinal))
            {
                return line.Contains("内部", StringComparison.OrdinalIgnoreCase)
                       || line.Contains("internal", StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    private static string NormalizeType(string value)
    {
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

    private static string? NormalizeDefaultValue(string value)
    {
        value = value.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Equals("double.NaN", StringComparison.Ordinal))
        {
            return null;
        }

        return value;
    }

    private static string ExtractMethodBody(string text, string methodName)
    {
        var methodIndex = text.IndexOf(methodName, StringComparison.Ordinal);
        if (methodIndex < 0)
        {
            return string.Empty;
        }

        var openBrace = text.IndexOf('{', methodIndex);
        if (openBrace < 0)
        {
            return string.Empty;
        }

        var depth = 0;
        for (var index = openBrace; index < text.Length; index++)
        {
            if (text[index] == '{')
            {
                depth++;
            }
            else if (text[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return text[(openBrace + 1)..index];
                }
            }
        }

        return string.Empty;
    }

    private static IReadOnlyList<string> SplitStatements(string body)
    {
        var statements = new List<string>();
        var start = 0;
        for (var index = 0; index < body.Length; index++)
        {
            if (body[index] != ';')
            {
                continue;
            }

            statements.Add(body[start..index].Trim());
            start = index + 1;
        }

        return statements;
    }

    private static string RelativePath(string root, string path)
    {
        return Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static int KindOrder(string kind)
    {
        return kind switch
        {
            "seed" => 0,
            "map" => 1,
            "alias" => 2,
            _ => 3
        };
    }

    [GeneratedRegex(@"namespace\s+(?<namespace>[A-Za-z0-9_.]+)\s*;")]
    private static partial Regex NamespaceRegex();

    [GeneratedRegex(@"(?:internal|public)\s+(?:sealed\s+|partial\s+)*class\s+(?<class>\w+Token)\s*:\s*AbstractControlDesignToken")]
    private static partial Regex TokenClassRegex();

    [GeneratedRegex(@"class\s+(?<class>[A-Za-z_][A-Za-z0-9_]*)")]
    private static partial Regex ClassNameRegex();

    [GeneratedRegex(@"^\s*public\s+(?<type>[A-Za-z0-9_<>,\.\?\[\]\s]+?)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get;\s*set;\s*\}(?:\s*=\s*(?<default>[^;]+);)?")]
    private static partial Regex PropertyRegex();

    [GeneratedRegex(@"public\s+const\s+string\s+ID\s*=\s*""(?<id>[^""]+)""")]
    private static partial Regex TokenIdRegex();

    [GeneratedRegex(@"public\s+enum\s+(?<enum>\w+TokenKind)\s*\{(?<body>.*?)\}", RegexOptions.Singleline)]
    private static partial Regex TokenKindEnumRegex();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex(@"RegisterTokenResourceScope\((?<tokenClass>\w+Token)\.ScopeProvider\)")]
    private static partial Regex RegisterTokenScopeRegex();

    [GeneratedRegex(@"\{atom:(?<token>[A-Za-z_][A-Za-z0-9_]*)TokenResource\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\}")]
    private static partial Regex TokenResourceRegex();

    [GeneratedRegex(@"Selector\s*=\s*""(?<selector>[^""]+)""")]
    private static partial Regex SelectorRegex();

    [GeneratedRegex(@"Property\s*=\s*""(?<property>[^""]+)""")]
    private static partial Regex SetterPropertyRegex();

    [GeneratedRegex(@"(?<property>[A-Za-z_:][A-Za-z0-9_:\.]+)\s*=\s*[""'][^""']*$")]
    private static partial Regex AttributeBeforeResourceRegex();

    [GeneratedRegex(@"#(?<part>PART_[A-Za-z0-9_]+)")]
    private static partial Regex PartNameRegex();

    [GeneratedRegex(@"DesignTokenKind\(DesignTokenKind\.(?<kind>Seed|Map|Alias)\)")]
    private static partial Regex DesignTokenKindRegex();

    [GeneratedRegex(@"SharedToken\.(?<name>[A-Za-z_][A-Za-z0-9_]*)")]
    private static partial Regex SharedTokenRegex();

    [GeneratedRegex(@"(?:^|\s)(?:var|[A-Za-z_][A-Za-z0-9_<>,\.\?]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<expression>.+)$", RegexOptions.Singleline)]
    private static partial Regex LocalAssignmentRegex();

    private sealed record TokenKindInfo(string GeneratedPath, IReadOnlyList<string> Names);
}
