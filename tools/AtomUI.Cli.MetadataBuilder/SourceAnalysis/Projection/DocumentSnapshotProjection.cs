using System.Text;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Models;
using AtomUI.Cli.MetadataBuilder.SourceAnalysis.Processors;

namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Projection;

internal static class DocumentSnapshotProjection
{
    private const string SchemaVersion = "1.0";
    private const string Language = "zh";
    private const string XamlNamespace = "https://atomui.net";

    public static ExtractedDocumentSnapshot Create(SourceAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var catalog = CatalogSnapshotProjection.Create(context);
        var tokenSnapshot = context.Cache.TryGetValue(TokenProcessor.SnapshotCacheKey, out var tokenValue)
                            && tokenValue is ExtractedTokenSnapshot tokens
            ? tokens
            : null;
        var source = new ExtractedDocumentSource(
            ".workspace/AtomUI",
            context.Identity.SourceRef,
            context.Identity.SourceCommit,
            DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        var snapshotId = $"atomui-docs-source-{context.Identity.SourceCommit}";
        var controls = catalog.Controls
            .OrderBy(control => control.DisplayOrder)
            .Select(control => CreateControl(context, control, tokenSnapshot, source, snapshotId, catalog.TargetVersion))
            .ToArray();

        return new ExtractedDocumentSnapshot(
            SchemaVersion,
            snapshotId,
            catalog.TargetVersion,
            "desktop",
            "AtomUI.Desktop.Controls",
            Language,
            source,
            controls,
            catalog.Documents
                .Where(document => document.Kind.Equals("topic", StringComparison.Ordinal))
                .Select(document => CreateTopic(document, source, snapshotId, catalog.TargetVersion))
                .ToArray());
    }

    private static ExtractedControlDocument CreateControl(
        SourceAnalysisContext context,
        ExtractedCatalogControl control,
        ExtractedTokenSnapshot? tokenSnapshot,
        ExtractedDocumentSource source,
        string snapshotId,
        string targetVersion)
    {
        var type = context.Facts.Get($"control-type:{control.Name}")
            .Select(fact => fact.Value)
            .OfType<ControlTypeFact>()
            .FirstOrDefault();
        var members = context.Facts.GetByPrefix($"control-api:{control.Name}:")
            .Select(fact => fact.Value)
            .OfType<ControlApiMemberFact>()
            .OrderBy(member => member.SourceLocation.Line ?? int.MaxValue)
            .ThenBy(member => member.Name, StringComparer.Ordinal)
            .Select(CreateApiMember)
            .ToArray();
        var events = context.Facts.GetByPrefix($"control-event:{control.Name}:")
            .Select(fact => fact.Value)
            .OfType<ControlApiEventFact>()
            .OrderBy(item => item.SourceLocation.Line ?? int.MaxValue)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .Select(CreateApiEvent)
            .ToArray();
        var examples = context.Facts.GetByPrefix($"gallery-demo:{control.Name}:")
            .Select(fact => fact.Value)
            .OfType<GalleryDemoFact>()
            .OrderBy(example => example.Priority)
            .ThenBy(example => example.SourceKey, StringComparer.Ordinal)
            .Select(CreateExample)
            .ToArray();
        var tokenSet = tokenSnapshot?.ControlTokenSets.FirstOrDefault(item => item.ControlName.Equals(control.Name, StringComparison.Ordinal));
        var tokenDocs = tokenSet?.Tokens
                .OrderBy(token => token.Category, StringComparer.Ordinal)
                .ThenBy(token => token.Name, StringComparer.Ordinal)
                .Select(token => new ExtractedControlTokenDocument(
                    token.Name,
                    "control",
                    token.CustomizationLevel,
                    token.Description,
                    token.Usages.Select(usage => $"{usage.ThemeName}: {usage.TargetProperty}").Distinct(StringComparer.Ordinal).ToArray()))
                .ToArray()
            ?? [];
        var semanticParts = context.Facts.GetByPrefix($"semantic-part:{control.Name}:")
            .Select(fact => fact.Value)
            .OfType<SemanticPartFact>()
            .OrderBy(part => part.Name, StringComparer.Ordinal)
            .Select(part => new ExtractedControlSemanticPartDocument(
                part.Name,
                part.NodeType,
                part.Description,
                part.BoundApis,
                part.TokenUsages,
                "public-stable"))
            .ToArray();
        var usage = CreateUsage(control, examples);
        var apiSurface = new ExtractedControlApiSurfaceDocument(
            members,
            events,
            CreateInheritedContracts(type),
            members.Length == 0
                ? [new ExtractedApiSurfaceDiagnostic("ATOMUICLI_DOC_API_EMPTY", $"No source API facts were extracted for {control.Name}.", "warning", null)]
                : []);
        var logic = CreateLogicStructure(control, type, members, context);
        var theme = CreateTheme(context, control.Name, tokenSet);
        var identity = new ExtractedControlIdentityDocument(
            ToKebabCase(control.Name),
            control.Name,
            control.DisplayName,
            control.CategoryId,
            control.ProductId,
            control.PackageId,
            type?.Namespace ?? control.Namespace,
            XamlNamespace,
            type?.BaseType ?? "Avalonia.Controls.Control",
            "stable",
            control.IsCommercial);
        var sourceFiles = CreateSourceFiles(type, theme, examples);
        var sections = CreateSections(control, usage, apiSurface, logic, theme, examples, tokenDocs, semanticParts, sourceFiles);

        return new ExtractedControlDocument(
            identity.Id,
            identity.Name,
            identity.DisplayName,
            identity.CategoryId,
            identity.ProductId,
            identity.PackageId,
            identity.IsCommercial,
            identity.Status,
            usage.Summary,
            identity,
            usage,
            apiSurface,
            logic,
            theme,
            examples,
            tokenDocs,
            semanticParts,
            sourceFiles,
            sections,
            CreateRelatedCommands(control.Name),
            [],
            source,
            snapshotId,
            SchemaVersion,
            targetVersion,
            Language);
    }

    private static ExtractedControlUsageDocument CreateUsage(
        ExtractedCatalogControl control,
        IReadOnlyList<ExtractedControlExampleDocument> examples)
    {
        var minimal = examples.FirstOrDefault();
        return new ExtractedControlUsageDocument(
            $"{control.DisplayName} 是 AtomUI {control.CategoryName} 分类下的控件。{control.Description}",
            [
                $"需要使用 {control.DisplayName} 承载 {control.CategoryName} 场景时。",
                $"需要遵循 AtomUI token、主题和 Gallery 示例保持一致的控件实现时。"
            ],
            [
                "只需要静态说明文本时，不应使用交互型控件。",
                "业务语义与该控件分类不匹配时，应优先选择更具体的 AtomUI 控件。"
            ],
            [
                minimal?.Snippets.FirstOrDefault() ?? new ExtractedCodeSnippetDocument("xml", $"<atom:{control.Name} />", null, null),
                new ExtractedCodeSnippetDocument("csharp", $"new {control.Name}();", null, null)
            ]);
    }

    private static ExtractedApiMemberDocument CreateApiMember(ControlApiMemberFact fact)
    {
        return new ExtractedApiMemberDocument(
            fact.Name,
            fact.Kind,
            fact.DeclaringType,
            fact.Accessibility,
            fact.Signature,
            fact.Type,
            fact.DefaultValue,
            fact.ContractLevel,
            fact.Description,
            fact.SourceLocation.Path,
            fact.SourceLocation.Line);
    }

    private static ExtractedApiEventDocument CreateApiEvent(ControlApiEventFact fact)
    {
        return new ExtractedApiEventDocument(
            fact.Name,
            fact.Kind,
            fact.DeclaringType,
            fact.Accessibility,
            fact.Signature,
            fact.RoutingStrategy,
            fact.EventArgsType,
            fact.Description,
            fact.SourceLocation.Path,
            fact.SourceLocation.Line);
    }

    private static IReadOnlyList<ExtractedApiInheritanceDocument> CreateInheritedContracts(ControlTypeFact? type)
    {
        if (type is null)
        {
            return [];
        }

        var inherited = new List<ExtractedApiInheritanceDocument>();
        if (!string.IsNullOrWhiteSpace(type.BaseType))
        {
            inherited.Add(new ExtractedApiInheritanceDocument(type.BaseType, "base-type", type.BaseType, "Base type extracted from source class declaration."));
        }

        inherited.AddRange(type.Interfaces.Select(item => new ExtractedApiInheritanceDocument(item, "interface", item, "Interface contract extracted from source class declaration.")));
        return inherited;
    }

    private static ExtractedControlExampleDocument CreateExample(GalleryDemoFact fact)
    {
        return new ExtractedControlExampleDocument(
            fact.SourceKey,
            fact.Title,
            fact.Description,
            fact.Kind,
            fact.Priority,
            fact.BadgeText,
            [new ExtractedCodeSnippetDocument("xml", fact.XamlSnippet, fact.SourceLocation.Path, fact.SourceLocation.Line)],
            fact.SourceLocation.Path,
            fact.SourceLocation.Line);
    }

    private static ExtractedControlLogicStructureDocument CreateLogicStructure(
        ExtractedCatalogControl control,
        ControlTypeFact? type,
        IReadOnlyList<ExtractedApiMemberDocument> members,
        SourceAnalysisContext context)
    {
        var inheritance = new List<ExtractedLogicNodeDocument>
        {
            new(ToKebabCase(control.Name), "control", control.Name, control.Description, [])
        };
        if (!string.IsNullOrWhiteSpace(type?.BaseType))
        {
            inheritance.Add(new ExtractedLogicNodeDocument("base-type", "base-type", type.BaseType!, "Base control type extracted from source.", []));
        }

        if (type is not null)
        {
            inheritance.AddRange(type.Interfaces.Select(item => new ExtractedLogicNodeDocument(ToKebabCase(item), "interface", item, "Implemented interface extracted from source.", [])));
        }

        var apiGroups = members
            .Where(member => member.Kind is "styled-property" or "direct-property")
            .GroupBy(member => member.Kind, StringComparer.Ordinal)
            .Select(group => new ExtractedLogicNodeDocument(
                group.Key,
                "api-group",
                ToTitle(group.Key),
                $"Source-derived {group.Key} contract.",
                group.Select(member => member.Name).Distinct(StringComparer.Ordinal).ToArray()))
            .ToArray();
        var stateFlows = context.Facts.GetByPrefix($"state-flow:{control.Name}:")
            .Select(fact => fact.Value)
            .OfType<StateFlowFact>()
            .OrderBy(flow => flow.PseudoClass, StringComparer.Ordinal)
            .Select(flow => new ExtractedLogicFlowDocument(
                flow.PseudoClass.TrimStart(':'),
                $"{flow.PseudoClass} state",
                [
                    $"Condition: {flow.Condition}",
                    flow.ReferencedApis.Count > 0 ? $"Related API: {string.Join(", ", flow.ReferencedApis)}" : "Related API: source expression only.",
                    $"Source: {flow.SourceLocation.Path}:{flow.SourceLocation.Line}"
                ]))
            .ToArray();

        return new ExtractedControlLogicStructureDocument(
            inheritance,
            apiGroups,
            stateFlows,
            [],
            stateFlows.Select(flow => new ExtractedLogicFlowDocument($"theme-{flow.Id}", $"Theme bridge for {flow.Title}", ["Pseudo class state participates in ControlTheme selectors."])).ToArray());
    }

    private static ExtractedControlThemeDocument CreateTheme(
        SourceAnalysisContext context,
        string controlName,
        ExtractedControlTokenSet? tokenSet)
    {
        var allThemeFacts = context.Facts.GetByPrefix($"control-theme:{controlName}:")
            .Select(fact => fact.Value)
            .OfType<ControlThemeFact>()
            .OrderBy(fact => fact.ThemeName, StringComparer.Ordinal)
            .ToArray();
        var primaryThemeFacts = allThemeFacts
            .Where(fact => fact.ThemeName.Equals($"{controlName}Theme", StringComparison.Ordinal)
                           || fact.SourceLocation.Path.Contains($"/{controlName}s/", StringComparison.Ordinal)
                           || fact.SourceLocation.Path.Contains($"/{controlName}/", StringComparison.Ordinal))
            .ToArray();
        var themeFacts = primaryThemeFacts.Length > 0 ? primaryThemeFacts : allThemeFacts;
        var themeNames = themeFacts.Select(fact => fact.ThemeName).ToHashSet(StringComparer.Ordinal);
        var nodes = context.Facts.GetByPrefix($"theme-node:{controlName}:")
            .Select(fact => fact.Value)
            .OfType<ThemeNodeFact>()
            .Where(node => themeNames.Contains(node.ThemeName))
            .GroupBy(node => (node.ThemeName, node.Name, node.NodeType), ThemeNodeComparer.Instance)
            .Select(group => group.First())
            .OrderBy(node => node.Name, StringComparer.Ordinal)
            .ToArray();
        var selectors = context.Facts.GetByPrefix($"theme-selector:{controlName}:")
            .Select(fact => fact.Value)
            .OfType<ThemeSelectorFact>()
            .Where(selector => themeNames.Contains(selector.ThemeName))
            .OrderBy(selector => selector.Selector, StringComparer.Ordinal)
            .ToArray();
        var bindings = context.Facts.GetByPrefix($"template-binding:{controlName}:")
            .Select(fact => fact.Value)
            .OfType<TemplateBindingFact>()
            .Where(binding => themeNames.Contains(binding.ThemeName))
            .OrderBy(binding => binding.PartName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(binding => binding.Property, StringComparer.Ordinal)
            .ToArray();
        var templates = themeFacts.Length == 0
            ? []
            : themeFacts.Select(theme => new ExtractedThemeTemplateDocument(
                $"{ToKebabCase(controlName)}-{ToKebabCase(theme.ThemeName)}",
                theme.ThemeName,
                theme.SourceLocation.Path,
                theme.SourceLocation.Line,
                nodes
                    .Where(node => node.ThemeName.Equals(theme.ThemeName, StringComparison.Ordinal))
                    .Select(node => new ExtractedThemeNodeDocument(node.NodeType, node.Name, "public-stable", []))
                    .ToArray()))
            .ToArray();
        var selectorGroups = selectors
            .GroupBy(selector => selector.PseudoClasses.Count > 0 ? "state-selector" : "template-selector", StringComparer.Ordinal)
            .Select(group => new ExtractedThemeSelectorGroupDocument(
                group.Key,
                group.Select(selector => selector.Selector).Distinct(StringComparer.Ordinal).ToArray(),
                $"Source-derived {group.Key} selectors."))
            .ToArray();
        var tokenUsages = tokenSet?.ThemeUsages
                .Select(usage => new ExtractedThemeTokenUsageDocument(
                    usage.ResourceExpression,
                    ExtractTokenName(usage.ResourceExpression),
                    usage.Selector,
                    usage.TargetProperty))
                .ToArray()
            ?? [];

        return new ExtractedControlThemeDocument(
            controlName,
            templates,
            selectorGroups,
            bindings.Select(binding => new ExtractedThemeBindingDocument(binding.PartName ?? "(template)", binding.Property, binding.BoundApi)).ToArray(),
            tokenUsages,
            nodes.Select(node => new ExtractedThemeCustomizationBoundaryDocument(node.Name, "public-stable", "Keep this PART name stable when replacing the ControlTheme.")).ToArray());
    }

    private static IReadOnlyList<ExtractedControlSourceFileDocument> CreateSourceFiles(
        ControlTypeFact? type,
        ExtractedControlThemeDocument theme,
        IReadOnlyList<ExtractedControlExampleDocument> examples)
    {
        var files = new List<ExtractedControlSourceFileDocument>();
        if (type is not null)
        {
            files.Add(new ExtractedControlSourceFileDocument("control-source", type.SourceLocation.Path, "Control implementation source file."));
        }

        files.AddRange(theme.Templates.Select(template => new ExtractedControlSourceFileDocument("control-theme", template.SourcePath, "ControlTheme source file.")));
        files.AddRange(examples.Select(example => new ExtractedControlSourceFileDocument("gallery-demo", example.SourcePath, "Gallery example source file.")));
        return files
            .GroupBy(file => (file.Kind, file.Path), SourceFileComparer.Instance)
            .Select(group => group.First())
            .ToArray();
    }

    private static IReadOnlyList<ExtractedDocumentSectionContent> CreateSections(
        ExtractedCatalogControl control,
        ExtractedControlUsageDocument usage,
        ExtractedControlApiSurfaceDocument api,
        ExtractedControlLogicStructureDocument logic,
        ExtractedControlThemeDocument theme,
        IReadOnlyList<ExtractedControlExampleDocument> examples,
        IReadOnlyList<ExtractedControlTokenDocument> tokens,
        IReadOnlyList<ExtractedControlSemanticPartDocument> semanticParts,
        IReadOnlyList<ExtractedControlSourceFileDocument> sourceFiles)
    {
        return
        [
            new ExtractedDocumentSectionContent("overview", "Overview", 100, usage.Summary, ["catalog", "gallery"]),
            new ExtractedDocumentSectionContent("install", "Install", 200, RenderInstall(control), ["catalog"]),
            new ExtractedDocumentSectionContent("usage", "Usage", 300, RenderUsage(usage), ["gallery-demo"]),
            new ExtractedDocumentSectionContent("scenarios", "Scenarios", 400, RenderScenarios(usage), ["catalog"]),
            new ExtractedDocumentSectionContent("examples", "使用示例", 500, RenderExamples(examples), ["gallery-demo"]),
            new ExtractedDocumentSectionContent("api", "API", 600, RenderApi(api), ["control-source"]),
            new ExtractedDocumentSectionContent("events", "事件", 700, RenderEvents(api.Events), ["control-source"]),
            new ExtractedDocumentSectionContent("logic", "逻辑结构", 800, RenderLogic(logic), ["control-source"]),
            new ExtractedDocumentSectionContent("theme", "ControlTheme 结构", 900, RenderTheme(theme), ["control-theme"]),
            new ExtractedDocumentSectionContent("tokens", "Tokens", 1000, RenderTokens(tokens), ["token-source", "control-theme"]),
            new ExtractedDocumentSectionContent("semantic", "Semantic", 1100, RenderSemantic(semanticParts), ["semantic-contract"]),
            new ExtractedDocumentSectionContent("source", "Source", 1200, RenderSource(sourceFiles), ["source-index"])
        ];
    }

    private static string RenderInstall(ExtractedCatalogControl control)
    {
        return $"""
               Package: `{control.PackageId}`

               Namespace: `{control.Namespace}`

               XAML namespace: `{XamlNamespace}`

               Recommended prefix: `atom`
               """;
    }

    private static string RenderUsage(ExtractedControlUsageDocument usage)
    {
        var builder = new StringBuilder();
        foreach (var snippet in usage.MinimalSnippets)
        {
            builder.AppendLine($"```{snippet.Language}");
            builder.AppendLine(snippet.Code);
            builder.AppendLine("```");
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderScenarios(ExtractedControlUsageDocument usage)
    {
        var builder = new StringBuilder();
        builder.AppendLine("When to use:");
        foreach (var item in usage.WhenToUse)
        {
            builder.AppendLine($"- {item}");
        }

        builder.AppendLine();
        builder.AppendLine("When not to use:");
        foreach (var item in usage.WhenNotToUse)
        {
            builder.AppendLine($"- {item}");
        }

        return builder.ToString();
    }

    private static string RenderExamples(IReadOnlyList<ExtractedControlExampleDocument> examples)
    {
        if (examples.Count == 0)
        {
            return "No Gallery examples were extracted for this control.";
        }

        return "Examples are generated from Gallery source. Use `--examples all` to include every example.";
    }

    private static string RenderApi(ExtractedControlApiSurfaceDocument api)
    {
        var properties = api.Members
            .Where(member => member.Kind is "styled-property" or "direct-property")
            .ToArray();
        var publicMethods = api.Members
            .Where(member => member.Kind is "public-method" or "constructor")
            .ToArray();
        var protectedMethods = api.Members
            .Where(member => member.Kind == "protected-method")
            .ToArray();
        var builder = new StringBuilder();
        builder.AppendLine("### 公共属性");
        builder.AppendLine();
        AppendMemberTable(builder, properties);
        builder.AppendLine();
        builder.AppendLine("### 公共方法");
        builder.AppendLine();
        AppendMemberTable(builder, publicMethods);
        builder.AppendLine();
        builder.AppendLine("### Protected 扩展点");
        builder.AppendLine();
        AppendMemberTable(builder, protectedMethods);
        if (api.Events.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("### 事件");
            builder.AppendLine();
            builder.AppendLine(RenderEvents(api.Events));
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendMemberTable(StringBuilder builder, IReadOnlyList<ExtractedApiMemberDocument> members)
    {
        if (members.Count == 0)
        {
            builder.AppendLine("No source facts were extracted.");
            return;
        }

        builder.AppendLine("| Name | Kind | Type | Accessibility | Source |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var member in members)
        {
            builder.AppendLine($"| {member.Name} | {ToApiKind(member.Kind)} | {member.Type ?? ""} | {member.Accessibility} | {member.SourcePath}:{member.SourceLine} |");
        }
    }

    private static string RenderEvents(IReadOnlyList<ExtractedApiEventDocument> events)
    {
        if (events.Count == 0)
        {
            return "No source event contracts were extracted.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("| Name | Kind | Event args | Source |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var item in events)
        {
            builder.AppendLine($"| {item.Name} | {item.Kind} | {item.EventArgsType ?? ""} | {item.SourcePath}:{item.SourceLine} |");
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderLogic(ExtractedControlLogicStructureDocument logic)
    {
        var builder = new StringBuilder();
        foreach (var node in logic.Inheritance)
        {
            builder.AppendLine($"- {node.Kind}: `{node.Label}`");
        }

        foreach (var flow in logic.StateFlows)
        {
            builder.AppendLine();
            builder.AppendLine($"### {flow.Title}");
            foreach (var step in flow.Steps)
            {
                builder.AppendLine($"- {step}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderTheme(ExtractedControlThemeDocument theme)
    {
        if (theme.Templates.Count == 0)
        {
            return "No ControlTheme facts were extracted.";
        }

        var builder = new StringBuilder();
        foreach (var template in theme.Templates)
        {
            builder.AppendLine($"### Template: {template.SourceSelector}");
            builder.AppendLine();
            builder.AppendLine($"Source: `{template.SourcePath}:{template.SourceLine}`");
            builder.AppendLine();
            builder.AppendLine("| PART | Type | Stability |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var node in template.Roots)
            {
                builder.AppendLine($"| {node.Name} | {node.ElementType} | {node.Stability} |");
            }

            builder.AppendLine();
        }

        if (theme.SelectorGroups.Count > 0)
        {
            builder.AppendLine("### Selectors");
            foreach (var group in theme.SelectorGroups)
            {
                builder.AppendLine($"- {group.Kind}: {string.Join(", ", group.Selectors.Take(6))}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderTokens(IReadOnlyList<ExtractedControlTokenDocument> tokens)
    {
        if (tokens.Count == 0)
        {
            return "No control token facts were extracted.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("| Token | Scope | Status | Description |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var token in tokens.Take(80))
        {
            builder.AppendLine($"| {token.Name} | {token.Scope} | {token.Status} | {token.Description} |");
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderSemantic(IReadOnlyList<ExtractedControlSemanticPartDocument> semanticParts)
    {
        if (semanticParts.Count == 0)
        {
            return "No semantic parts were extracted.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("| Part | Node | Responsibility |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var part in semanticParts)
        {
            builder.AppendLine($"| {part.Part} | {part.AtomUINode} | {part.Responsibility} |");
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderSource(IReadOnlyList<ExtractedControlSourceFileDocument> sourceFiles)
    {
        if (sourceFiles.Count == 0)
        {
            return "No source files were indexed.";
        }

        return string.Join(Environment.NewLine, sourceFiles.Select(file => $"- {file.Kind}: `{file.Path}`"));
    }

    private static IReadOnlyList<ExtractedDocumentRelatedItem> CreateRelatedCommands(string controlName)
    {
        return
        [
            new ExtractedDocumentRelatedItem("command", "info", "Control metadata", $"dotnet atomui info {controlName}"),
            new ExtractedDocumentRelatedItem("command", "demo", "Control demos", $"dotnet atomui demo {controlName} --all"),
            new ExtractedDocumentRelatedItem("command", "token", "Control tokens", $"dotnet atomui token {controlName}"),
            new ExtractedDocumentRelatedItem("command", "semantic", "Control semantic parts", $"dotnet atomui semantic {controlName} --include-template")
        ];
    }

    private static ExtractedTopicDocument CreateTopic(
        ExtractedCatalogDocument document,
        ExtractedDocumentSource source,
        string snapshotId,
        string targetVersion)
    {
        return new ExtractedTopicDocument(
            document.TargetId,
            document.Title,
            Language,
            [
                new ExtractedDocumentSectionContent(
                    "overview",
                    "Overview",
                    100,
                    document.Markdown,
                    ["markdown-doc"])
            ],
            [],
            [],
            source,
            snapshotId,
            SchemaVersion,
            targetVersion);
    }

    private static string ExtractTokenName(string expression)
    {
        var lastSpace = expression.LastIndexOf(' ');
        if (lastSpace >= 0 && expression.EndsWith("}", StringComparison.Ordinal))
        {
            return expression[(lastSpace + 1)..^1];
        }

        var dot = expression.LastIndexOf('.');
        return dot >= 0 ? expression[(dot + 1)..] : expression;
    }

    private static string ToApiKind(string kind)
    {
        return kind switch
        {
            "styled-property" => "StyledProperty",
            "direct-property" => "DirectProperty",
            "protected-method" => "ProtectedMethod",
            "public-method" => "PublicMethod",
            "constructor" => "Constructor",
            _ => kind
        };
    }

    private static string ToTitle(string value)
    {
        return value.Replace('-', ' ');
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

    private sealed class SourceFileComparer : IEqualityComparer<(string Kind, string Path)>
    {
        public static readonly SourceFileComparer Instance = new();

        public bool Equals((string Kind, string Path) x, (string Kind, string Path) y)
        {
            return string.Equals(x.Kind, y.Kind, StringComparison.Ordinal)
                   && string.Equals(x.Path, y.Path, StringComparison.Ordinal);
        }

        public int GetHashCode((string Kind, string Path) obj)
        {
            return HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(obj.Kind),
                StringComparer.Ordinal.GetHashCode(obj.Path));
        }
    }

    private sealed class ThemeNodeComparer : IEqualityComparer<(string ThemeName, string Name, string NodeType)>
    {
        public static readonly ThemeNodeComparer Instance = new();

        public bool Equals((string ThemeName, string Name, string NodeType) x, (string ThemeName, string Name, string NodeType) y)
        {
            return string.Equals(x.ThemeName, y.ThemeName, StringComparison.Ordinal)
                   && string.Equals(x.Name, y.Name, StringComparison.Ordinal)
                   && string.Equals(x.NodeType, y.NodeType, StringComparison.Ordinal);
        }

        public int GetHashCode((string ThemeName, string Name, string NodeType) obj)
        {
            return HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(obj.ThemeName),
                StringComparer.Ordinal.GetHashCode(obj.Name),
                StringComparer.Ordinal.GetHashCode(obj.NodeType));
        }
    }
}
