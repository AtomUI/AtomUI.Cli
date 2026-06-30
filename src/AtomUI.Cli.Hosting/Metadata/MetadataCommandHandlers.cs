using System.Text;

namespace AtomUI.Cli.Hosting.Metadata;

public sealed class ListCommandHandler(MetadataQueryService metadata) : IAtomUICliCommandHandler<ListCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(ListCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        var validationFailure = ValidateFilters(options);
        if (validationFailure is not null)
        {
            return ValueTask.FromResult(validationFailure);
        }

        var payload = metadata.CreateListPayload(options);
        object resultPayload = options.Global.Format switch
        {
            OutputFormat.Json => payload,
            OutputFormat.Markdown => ListOutputRenderer.RenderMarkdown(payload, options.Global.Detail),
            _ => ListOutputRenderer.RenderText(payload, options.Global.Detail)
        };

        return ValueTask.FromResult(AtomUICliResult.Success(resultPayload));
    }

    private AtomUICliResult? ValidateFilters(ListCommandOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Global.Product) && metadata.FindProduct(options.Global.Product) is null)
        {
            return MetadataErrors.NotFound(AtomUICliErrorCodes.PackageNotFound, $"Product '{options.Global.Product}' was not found.");
        }

        if (!string.IsNullOrWhiteSpace(options.PackageId) && metadata.FindPackage(options.PackageId) is null)
        {
            return MetadataErrors.NotFound(AtomUICliErrorCodes.PackageNotFound, $"Package '{options.PackageId}' was not found.");
        }

        return null;
    }
}

internal static class ListOutputRenderer
{
    public static string RenderText(ListCommandPayload payload, bool includeDetail)
    {
        return payload.Kind switch
        {
            ListKind.Products => RenderProducts(payload.Products),
            ListKind.Packages => RenderPackages(payload.Packages),
            ListKind.Categories => RenderCategories(payload.Categories),
            ListKind.All => RenderAll(payload, includeDetail),
            _ => RenderControls(payload, includeDetail)
        };
    }

    public static string RenderMarkdown(ListCommandPayload payload, bool includeDetail)
    {
        var builder = new StringBuilder();
        builder.AppendLine(payload.Kind == ListKind.All ? "# AtomUI Metadata" : "# AtomUI Controls");
        builder.AppendLine();

        if (payload.Products.Count > 0)
        {
            builder.AppendLine("## Products");
            builder.AppendLine();
            builder.AppendLine("| Id | Name | Description |");
            builder.AppendLine("| --- | --- | --- |");
            foreach (var product in payload.Products)
            {
                builder.AppendLine($"| {product.Id} | {product.Name} | {product.Description} |");
            }

            builder.AppendLine();
        }

        if (payload.Packages.Count > 0)
        {
            builder.AppendLine("## Packages");
            builder.AppendLine();
            builder.AppendLine("| Id | Product | Version | Controls |");
            builder.AppendLine("| --- | --- | --- | --- |");
            foreach (var package in payload.Packages)
            {
                builder.AppendLine($"| {package.Id} | {package.ProductId} | {package.Version} | {package.ControlCount} |");
            }

            builder.AppendLine();
        }

        foreach (var category in payload.Categories)
        {
            builder.AppendLine($"## {category.Name}");
            builder.AppendLine();
            builder.Append(includeDetail
                ? "| Name | Package | Product | Route | Commercial | Optional Package |"
                : "| Name | Package | Product |");
            builder.AppendLine();
            builder.Append(includeDetail
                ? "| --- | --- | --- | --- | --- | --- |"
                : "| --- | --- | --- |");
            builder.AppendLine();

            foreach (var control in category.Controls)
            {
                builder.Append(includeDetail
                    ? $"| {control.Name} | {control.PackageId} | {control.ProductId} | {control.GalleryRoute} | {FormatBool(control.IsCommercial)} | {FormatBool(control.IsOptionalPackage)} |"
                    : $"| {control.Name} | {control.PackageId} | {control.ProductId} |");
                builder.AppendLine();
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderControls(ListCommandPayload payload, bool includeDetail)
    {
        if (payload.Total == 0)
        {
            return string.IsNullOrWhiteSpace(payload.CategoryFilter)
                ? "No controls matched the current filters."
                : $"No controls matched category '{payload.CategoryFilter}'.";
        }

        var builder = new StringBuilder();
        var title = string.IsNullOrWhiteSpace(payload.CategoryFilter)
            ? $"AtomUI controls ({payload.Total})"
            : $"{payload.Categories.FirstOrDefault()?.Name ?? payload.CategoryFilter} controls ({payload.Total})";

        builder.AppendLine(title);
        builder.AppendLine();

        foreach (var category in payload.Categories)
        {
            builder.AppendLine($"{category.Name} ({category.Controls.Count})");
            foreach (var control in category.Controls)
            {
                builder.AppendLine($"  {control.Name.PadRight(18)} {control.PackageId}");
                if (includeDetail)
                {
                    builder.AppendLine($"    product: {control.ProductId}");
                    builder.AppendLine($"    route: {control.GalleryRoute}");
                    builder.AppendLine($"    commercial: {FormatBool(control.IsCommercial)}");
                    builder.AppendLine($"    optional package: {FormatBool(control.IsOptionalPackage)}");
                }
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderCategories(IReadOnlyList<ListControlCategoryPayload> categories)
    {
        return categories.Count == 0
            ? "No categories matched the current filters."
            : string.Join(Environment.NewLine, categories.Select(category => category.Name));
    }

    private static string RenderProducts(IReadOnlyList<ListProductPayload> products)
    {
        return products.Count == 0
            ? "No products matched the current filters."
            : string.Join(Environment.NewLine, products.Select(product => $"{product.Id}: {product.Name}"));
    }

    private static string RenderPackages(IReadOnlyList<ListPackagePayload> packages)
    {
        return packages.Count == 0
            ? "No packages matched the current filters."
            : string.Join(Environment.NewLine, packages.Select(package => $"{package.Id} {package.Version} ({FormatControlCount(package.ControlCount)})"));
    }

    private static string RenderAll(ListCommandPayload payload, bool includeDetail)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"AtomUI metadata {payload.TargetVersion}");
        builder.AppendLine();
        builder.AppendLine("Products:");
        builder.AppendLine(RenderProducts(payload.Products));
        builder.AppendLine();
        builder.AppendLine("Packages:");
        builder.AppendLine(RenderPackages(payload.Packages));
        builder.AppendLine();
        builder.AppendLine("Controls:");
        builder.AppendLine(RenderControls(payload, includeDetail));
        return builder.ToString().TrimEnd();
    }

    private static string FormatBool(bool value)
    {
        return value ? "yes" : "no";
    }

    private static string FormatControlCount(int count)
    {
        return count == 1 ? "1 control" : $"{count} controls";
    }
}

public sealed class InfoCommandHandler(MetadataQueryService metadata) : IAtomUICliCommandHandler<InfoCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(InfoCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Control))
        {
            return ValueTask.FromResult(MetadataErrors.MissingRequired("Control name is required."));
        }

        if (!InfoSectionSelection.TryParse(options.Include, out var sections, out var invalidSection))
        {
            return ValueTask.FromResult(MetadataErrors.InvalidValue(
                $"Unknown info include section '{invalidSection}'. Supported sections: {InfoSectionSelection.SupportedSectionList}."));
        }

        var control = metadata.FindControl(options.Control, options.Global.Product);
        if (control is null)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.ControlNotFound, $"Control '{options.Control}' was not found."));
        }

        var payload = metadata.CreateInfoPayload(control, options);
        object resultPayload = options.Global.Format switch
        {
            OutputFormat.Json => payload,
            OutputFormat.Markdown => InfoOutputRenderer.RenderMarkdown(payload, options.Global.Detail, sections),
            _ => InfoOutputRenderer.RenderText(payload, options.Global.Detail, sections)
        };

        return ValueTask.FromResult(AtomUICliResult.Success(resultPayload));
    }
}

internal readonly record struct InfoSectionSelection(
    bool Usage,
    bool Api,
    bool Template,
    bool States,
    bool Tokens,
    bool Demos,
    bool Diagnostics,
    bool Related)
{
    public const string SupportedSectionList = "identity, usage, api, events, methods, template, states, tokens, demos, diagnostics, related, all";

    private static readonly InfoSectionSelection All = new(
        Usage: true,
        Api: true,
        Template: true,
        States: true,
        Tokens: true,
        Demos: true,
        Diagnostics: true,
        Related: true);

    public static bool TryParse(string? value, out InfoSectionSelection selection, out string? invalidSection)
    {
        invalidSection = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            selection = All;
            return true;
        }

        var requested = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
        if (requested.Length == 0)
        {
            selection = All;
            return true;
        }

        if (requested.Any(item => item.Equals("all", StringComparison.OrdinalIgnoreCase)))
        {
            selection = All;
            return true;
        }

        var usage = false;
        var api = false;
        var template = false;
        var states = false;
        var tokens = false;
        var demos = false;
        var diagnostics = false;
        var related = false;

        foreach (var section in requested)
        {
            switch (section.ToLowerInvariant())
            {
                case "identity":
                    break;
                case "usage":
                    usage = true;
                    break;
                case "api":
                case "events":
                case "methods":
                    api = true;
                    break;
                case "template":
                    template = true;
                    break;
                case "states":
                    states = true;
                    break;
                case "tokens":
                    tokens = true;
                    break;
                case "demos":
                    demos = true;
                    break;
                case "diagnostics":
                    diagnostics = true;
                    break;
                case "related":
                    related = true;
                    break;
                default:
                    selection = default;
                    invalidSection = section;
                    return false;
            }
        }

        selection = new InfoSectionSelection(usage, api, template, states, tokens, demos, diagnostics, related);
        return true;
    }
}

internal static class InfoOutputRenderer
{
    public static string RenderText(InfoCommandPayload payload, bool includeDetail, InfoSectionSelection sections)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{payload.Control.Name} - {payload.Control.CategoryName}");
        builder.AppendLine($"Package: {payload.Control.PackageId}");
        builder.AppendLine($"Namespace: {payload.Type.Namespace}");
        builder.AppendLine($"Base type: {payload.Type.BaseType}");
        builder.AppendLine($"Product: {payload.Control.ProductId}");
        builder.AppendLine($"Gallery: {payload.Control.GalleryRoute}");
        builder.AppendLine($"Status: {payload.Control.Status}");
        builder.AppendLine();
        builder.AppendLine(payload.Description.Subtitle);

        if (sections.Usage && !string.IsNullOrWhiteSpace(payload.Usage.XamlSnippet))
        {
            builder.AppendLine();
            builder.AppendLine("Usage:");
            builder.AppendLine($"  {payload.Usage.XamlSnippet}");
        }

        if (sections.Api)
        {
            AppendTextApi(builder, payload, includeDetail);
        }

        if (sections.Template)
        {
            AppendTextTemplate(builder, payload, includeDetail);
        }

        if (sections.States)
        {
            AppendTextStates(builder, payload, includeDetail);
        }

        if (sections.Tokens)
        {
            AppendTextTokens(builder, payload, includeDetail);
        }

        if (sections.Demos)
        {
            AppendTextDemos(builder, payload, includeDetail);
        }

        if (sections.Diagnostics)
        {
            AppendTextDiagnostics(builder, payload);
        }

        if (sections.Related)
        {
            AppendTextRelatedCommands(builder, payload);
        }

        return builder.ToString().TrimEnd();
    }

    public static string RenderMarkdown(InfoCommandPayload payload, bool includeDetail, InfoSectionSelection sections)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {payload.Control.Name}");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine($"| Category | {payload.Control.CategoryName} |");
        builder.AppendLine($"| Package | {payload.Control.PackageId} |");
        builder.AppendLine($"| Namespace | {payload.Type.Namespace} |");
        builder.AppendLine($"| Base type | {payload.Type.BaseType} |");
        builder.AppendLine($"| Product | {payload.Control.ProductId} |");
        builder.AppendLine($"| Gallery | {payload.Control.GalleryRoute} |");
        builder.AppendLine($"| Status | {payload.Control.Status} |");
        builder.AppendLine();
        builder.AppendLine(payload.Description.Subtitle);

        if (sections.Usage && !string.IsNullOrWhiteSpace(payload.Usage.XamlSnippet))
        {
            builder.AppendLine();
            builder.AppendLine("## Usage");
            builder.AppendLine();
            builder.AppendLine("```xml");
            builder.AppendLine(payload.Usage.XamlSnippet);
            builder.AppendLine("```");
        }

        if (sections.Api)
        {
            AppendMarkdownApi(builder, payload, includeDetail);
        }

        if (sections.Template)
        {
            AppendMarkdownTemplate(builder, payload, includeDetail);
        }

        if (sections.States)
        {
            AppendMarkdownStates(builder, payload, includeDetail);
        }

        if (sections.Tokens)
        {
            AppendMarkdownTokens(builder, payload, includeDetail);
        }

        if (sections.Demos)
        {
            AppendMarkdownDemos(builder, payload, includeDetail);
        }

        if (sections.Diagnostics)
        {
            AppendMarkdownDiagnostics(builder, payload);
        }

        if (sections.Related)
        {
            AppendMarkdownRelatedCommands(builder, payload);
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendTextApi(StringBuilder builder, InfoCommandPayload payload, bool includeDetail)
    {
        var api = SelectApi(payload, includeDetail);
        if (api.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("API:");
        foreach (var item in api)
        {
            builder.AppendLine(
                $"  {item.Name.PadRight(16)} {item.Type.PadRight(22)} {item.DefaultValue.PadRight(8)} {ToDisplayKind(item.PropertyKind).PadRight(7)} {item.Description}");
        }
    }

    private static void AppendTextTemplate(StringBuilder builder, InfoCommandPayload payload, bool includeDetail)
    {
        var parts = SelectTemplateParts(payload, includeDetail);
        if (parts.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Template parts:");
        foreach (var part in parts)
        {
            builder.AppendLine($"  {part.Name.PadRight(22)} {part.Type}");
        }
    }

    private static void AppendTextStates(StringBuilder builder, InfoCommandPayload payload, bool includeDetail)
    {
        var states = SelectStates(payload, includeDetail);
        if (states.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("States:");
        builder.AppendLine($"  {string.Join(", ", states.Select(state => state.Name))}");
    }

    private static void AppendTextTokens(StringBuilder builder, InfoCommandPayload payload, bool includeDetail)
    {
        var tokens = SelectTokens(payload, includeDetail);
        if (tokens.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Tokens:");
        foreach (var token in tokens)
        {
            builder.AppendLine($"  {token.Name.PadRight(14)} {ToDisplayKind(token.Scope).PadRight(7)} {ToDisplayKind(token.Status)}");
        }
    }

    private static void AppendTextDemos(StringBuilder builder, InfoCommandPayload payload, bool includeDetail)
    {
        var demos = SelectDemos(payload, includeDetail);
        if (demos.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Demos:");
        foreach (var demo in demos)
        {
            builder.AppendLine($"  {demo.Name.PadRight(12)} {demo.Title}");
        }
    }

    private static void AppendTextDiagnostics(StringBuilder builder, InfoCommandPayload payload)
    {
        if (payload.Diagnostics.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Diagnostics:");
        foreach (var diagnostic in payload.Diagnostics)
        {
            builder.AppendLine($"  {diagnostic.Code}: {diagnostic.Message}");
        }
    }

    private static void AppendTextRelatedCommands(StringBuilder builder, InfoCommandPayload payload)
    {
        if (payload.RelatedCommands.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Related:");
        foreach (var command in payload.RelatedCommands)
        {
            builder.AppendLine($"  {command.Command}");
        }
    }

    private static void AppendMarkdownApi(StringBuilder builder, InfoCommandPayload payload, bool includeDetail)
    {
        var api = SelectApi(payload, includeDetail);
        if (api.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("## API");
        builder.AppendLine();
        builder.AppendLine("| Property | Type | Default | Kind | Description |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var item in api)
        {
            builder.AppendLine($"| {item.Name} | {item.Type} | {item.DefaultValue} | {ToDisplayKind(item.PropertyKind)} | {item.Description} |");
        }
    }

    private static void AppendMarkdownTemplate(StringBuilder builder, InfoCommandPayload payload, bool includeDetail)
    {
        var parts = SelectTemplateParts(payload, includeDetail);
        if (parts.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("## Template Parts");
        builder.AppendLine();
        builder.AppendLine("| Name | Type | Required | Description |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var part in parts)
        {
            builder.AppendLine($"| {part.Name} | {part.Type} | {FormatBool(part.IsRequired)} | {part.Description} |");
        }
    }

    private static void AppendMarkdownStates(StringBuilder builder, InfoCommandPayload payload, bool includeDetail)
    {
        var states = SelectStates(payload, includeDetail);
        if (states.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("## States");
        builder.AppendLine();
        builder.AppendLine("| Name | Kind | Description |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var state in states)
        {
            builder.AppendLine($"| {state.Name} | {state.Kind} | {state.Description} |");
        }
    }

    private static void AppendMarkdownTokens(StringBuilder builder, InfoCommandPayload payload, bool includeDetail)
    {
        var tokens = SelectTokens(payload, includeDetail);
        if (tokens.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("## Tokens");
        builder.AppendLine();
        builder.AppendLine("| Name | Scope | Status | Description |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var token in tokens)
        {
            builder.AppendLine($"| {token.Name} | {token.Scope} | {token.Status} | {token.Description} |");
        }
    }

    private static void AppendMarkdownDemos(StringBuilder builder, InfoCommandPayload payload, bool includeDetail)
    {
        var demos = SelectDemos(payload, includeDetail);
        if (demos.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("## Demos");
        builder.AppendLine();
        builder.AppendLine("| Name | Title | Description |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var demo in demos)
        {
            builder.AppendLine($"| {demo.Name} | {demo.Title} | {demo.Description} |");
        }
    }

    private static void AppendMarkdownDiagnostics(StringBuilder builder, InfoCommandPayload payload)
    {
        if (payload.Diagnostics.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("## Diagnostics");
        builder.AppendLine();
        builder.AppendLine("| Code | Severity | Message |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var diagnostic in payload.Diagnostics)
        {
            builder.AppendLine($"| {diagnostic.Code} | {diagnostic.Severity} | {diagnostic.Message} |");
        }
    }

    private static void AppendMarkdownRelatedCommands(StringBuilder builder, InfoCommandPayload payload)
    {
        if (payload.RelatedCommands.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("## Related Commands");
        builder.AppendLine();
        builder.AppendLine("```bash");
        foreach (var command in payload.RelatedCommands)
        {
            builder.AppendLine(command.Command);
        }

        builder.AppendLine("```");
    }

    private static IReadOnlyList<InfoApiMemberPayload> SelectApi(InfoCommandPayload payload, bool includeDetail)
    {
        return includeDetail
            ? payload.Api
            : payload.Api.Where(item => item.IsCurated).Take(8).ToArray();
    }

    private static IReadOnlyList<InfoTemplatePartPayload> SelectTemplateParts(InfoCommandPayload payload, bool includeDetail)
    {
        return includeDetail
            ? payload.Template.Parts
            : payload.Template.Parts.Take(5).ToArray();
    }

    private static IReadOnlyList<InfoControlStatePayload> SelectStates(InfoCommandPayload payload, bool includeDetail)
    {
        return includeDetail
            ? payload.States
            : payload.States.Take(8).ToArray();
    }

    private static IReadOnlyList<InfoTokenSummaryPayload> SelectTokens(InfoCommandPayload payload, bool includeDetail)
    {
        return includeDetail
            ? payload.Tokens
            : payload.Tokens.Take(5).ToArray();
    }

    private static IReadOnlyList<InfoDemoSummaryPayload> SelectDemos(InfoCommandPayload payload, bool includeDetail)
    {
        return includeDetail
            ? payload.Demos
            : payload.Demos.Take(5).ToArray();
    }

    private static string ToDisplayKind(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? value
            : char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static string FormatBool(bool value)
    {
        return value ? "yes" : "no";
    }
}

public sealed class DocCommandHandler(
    MetadataQueryService metadata,
    DocumentationQueryService documents,
    DocumentSectionSelector sectionSelector,
    DocOutputRenderer renderer) : IAtomUICliCommandHandler<DocCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(DocCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(options.InvalidSection))
        {
            return ValueTask.FromResult(MetadataErrors.InvalidValue(
                $"Unknown doc section '{options.InvalidSection}'. Supported sections: all, overview, install, usage, scenarios, examples, api, properties, methods, events, logic, theme, tokens, semantic, demos, changelog, source."));
        }

        if (!string.IsNullOrWhiteSpace(options.InvalidStyle))
        {
            return ValueTask.FromResult(MetadataErrors.InvalidValue(
                $"Unknown doc style '{options.InvalidStyle}'. Supported styles: full, summary, agent."));
        }

        if (!string.IsNullOrWhiteSpace(options.InvalidExamples))
        {
            return ValueTask.FromResult(MetadataErrors.InvalidValue(
                $"Unknown doc examples mode '{options.InvalidExamples}'. Supported modes: recommended, all, basic, state, theme, integration, advanced."));
        }

        if (!string.IsNullOrWhiteSpace(options.Target) && !string.IsNullOrWhiteSpace(options.Topic))
        {
            return ValueTask.FromResult(MetadataErrors.InvalidValue("Document target and --topic cannot be used together."));
        }

        if (string.IsNullOrWhiteSpace(options.Target) && string.IsNullOrWhiteSpace(options.Topic))
        {
            return ValueTask.FromResult(MetadataErrors.MissingRequired("Document target or --topic is required."));
        }

        if (!string.IsNullOrWhiteSpace(options.Global.Product) && metadata.FindProduct(options.Global.Product) is null)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.PackageNotFound, $"Product '{options.Global.Product}' was not found."));
        }

        var targetKind = string.IsNullOrWhiteSpace(options.Topic) ? DocumentTargetKind.Control : DocumentTargetKind.Topic;
        var target = options.Topic ?? options.Target!;
        var query = new DocumentQuery(
            targetKind,
            target,
            options.Global.TargetVersion ?? "6.0",
            options.Global.Language,
            options.Global.Product,
            options.Section,
            options.Examples,
            options.ExampleKey,
            options.Strict);
        var queryResult = documents.Query(query);
        if (queryResult.Status != DocumentQueryStatus.Found)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(
                queryResult.ErrorCode ?? AtomUICliErrorCodes.DataUnavailable,
                queryResult.ErrorMessage ?? $"Document '{target}' was not found."));
        }

        var payload = BuildPayload(options, queryResult);
        var effectiveFormat = options.FormatSpecified ? options.Global.Format : OutputFormat.Markdown;
        object resultPayload = effectiveFormat switch
        {
            OutputFormat.Json => payload,
            OutputFormat.Text => renderer.RenderText(payload, options.Global.Detail),
            _ => renderer.RenderMarkdown(payload, options.Global.Detail)
        };

        return ValueTask.FromResult(AtomUICliResult.Success(resultPayload));
    }

    private DocCommandPayload BuildPayload(DocCommandOptions options, DocumentQueryResult queryResult)
    {
        if (queryResult.Control is not null)
        {
            var control = queryResult.Control;
            var effectiveSection = string.IsNullOrWhiteSpace(options.ExampleKey)
                ? options.Section
                : DocumentSection.Examples;
            var sections = sectionSelector.Select(control.Sections, effectiveSection, options.Style);
            return new DocCommandPayload(
                "1.0",
                "doc",
                control.TargetVersion,
                "control",
                control.Name,
                control.DisplayName,
                control.Language,
                control.ProductId,
                control.PackageId,
                control.IsCommercial,
                options.Section,
                options.Style,
                options.Examples,
                options.ExampleKey,
                CreateSource(control.Source, control.SnapshotSchemaVersion, control.SnapshotId),
                CreateControl(control, options),
                sections.Select(section => CreateSection(section, SelectExamples(control.Examples, options))).ToArray(),
                control.Related.Select(CreateRelated).ToArray(),
                control.Warnings.Select(CreateWarning).ToArray(),
                queryResult.Suggestions.Select(CreateSuggestion).ToArray());
        }

        var topic = queryResult.Topic!;
        var topicSections = sectionSelector.Select(topic.Sections, options.Section, options.Style);
        return new DocCommandPayload(
            "1.0",
            "doc",
            topic.TargetVersion,
            "topic",
            topic.Id,
            topic.Title,
            topic.Language,
            null,
            null,
            false,
            options.Section,
            options.Style,
            options.Examples,
            options.ExampleKey,
            CreateSource(topic.Source, topic.SnapshotSchemaVersion, topic.SnapshotId),
            null,
            topicSections.Select(section => CreateSection(section, null)).ToArray(),
            topic.Related.Select(CreateRelated).ToArray(),
            topic.Warnings.Select(CreateWarning).ToArray(),
            queryResult.Suggestions.Select(CreateSuggestion).ToArray());
    }

    private static DocSourceIdentityPayload CreateSource(
        DocumentSourceIdentity source,
        string schemaVersion,
        string snapshotId)
    {
        return new DocSourceIdentityPayload(
            schemaVersion,
            snapshotId,
            source.SourceRef,
            source.SourceCommit,
            source.GeneratedAt);
    }

    private static DocSectionPayload CreateSection(
        DocumentSectionContent section,
        IReadOnlyList<ControlExampleDocument>? selectedExamples)
    {
        return new DocSectionPayload(
            section.Id,
            section.Title,
            section.Order,
            section.Id.Equals("examples", StringComparison.OrdinalIgnoreCase) && selectedExamples is not null
                ? RenderExamplesMarkdown(selectedExamples)
                : section.Markdown,
            section.SourceKinds);
    }

    private static DocControlDocumentPayload CreateControl(ControlDocument control, DocCommandOptions options)
    {
        var examples = SelectExamples(control.Examples, options);
        return new DocControlDocumentPayload(
            CreateIdentity(control.Identity),
            CreateUsage(control.Usage),
            CreateApiSurface(control.ApiSurface),
            CreateLogicStructure(control.LogicStructure),
            CreateTheme(control.Theme),
            examples.Select(CreateExample).ToArray(),
            control.Tokens.Select(CreateToken).ToArray(),
            control.SemanticParts.Select(CreateSemanticPart).ToArray(),
            control.SourceFiles.Select(CreateSourceFile).ToArray());
    }

    private static IReadOnlyList<ControlExampleDocument> SelectExamples(
        IReadOnlyList<ControlExampleDocument> examples,
        DocCommandOptions options)
    {
        IEnumerable<ControlExampleDocument> filtered = examples;
        if (!string.IsNullOrWhiteSpace(options.ExampleKey))
        {
            filtered = filtered.Where(example => example.SourceKey.Equals(options.ExampleKey, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            filtered = options.Examples switch
            {
                DocumentExamplesMode.All => filtered,
                DocumentExamplesMode.Basic => filtered.Where(example => example.Kind.Equals("basic", StringComparison.OrdinalIgnoreCase)),
                DocumentExamplesMode.State => filtered.Where(example => example.Kind.Equals("state", StringComparison.OrdinalIgnoreCase)),
                DocumentExamplesMode.Theme => filtered.Where(example => example.Kind.Equals("theme", StringComparison.OrdinalIgnoreCase)),
                DocumentExamplesMode.Integration => filtered.Where(example => example.Kind.Equals("integration", StringComparison.OrdinalIgnoreCase)),
                DocumentExamplesMode.Advanced => filtered.Where(example => example.Kind.Equals("advanced", StringComparison.OrdinalIgnoreCase)),
                _ => filtered.Where(example => example.Priority <= 100)
            };
        }

        return filtered
            .OrderBy(example => example.Priority)
            .ThenBy(example => example.SourceKey, StringComparer.Ordinal)
            .ToArray();
    }

    private static string RenderExamplesMarkdown(IReadOnlyList<ControlExampleDocument> examples)
    {
        if (examples.Count == 0)
        {
            return "No examples matched the current filters.";
        }

        var builder = new StringBuilder();
        foreach (var example in examples)
        {
            builder.AppendLine($"### {example.Title}");
            builder.AppendLine();
            builder.AppendLine($"SourceKey: `{example.SourceKey}`");
            builder.AppendLine();
            builder.AppendLine(example.Description);
            builder.AppendLine();
            foreach (var snippet in example.Snippets)
            {
                builder.AppendLine($"```{snippet.Language}");
                builder.AppendLine(snippet.Code);
                builder.AppendLine("```");
                builder.AppendLine();
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static DocControlIdentityPayload CreateIdentity(ControlDocumentIdentity identity)
    {
        return new DocControlIdentityPayload(
            identity.Id,
            identity.Name,
            identity.DisplayName,
            identity.CategoryId,
            identity.ProductId,
            identity.PackageId,
            identity.Namespace,
            identity.XamlNamespace,
            identity.BaseType,
            identity.Status,
            identity.IsCommercial);
    }

    private static DocUsagePayload CreateUsage(ControlUsageDocument usage)
    {
        return new DocUsagePayload(
            usage.Summary,
            usage.WhenToUse,
            usage.WhenNotToUse,
            usage.MinimalSnippets.Select(CreateSnippet).ToArray());
    }

    private static DocApiSurfacePayload CreateApiSurface(ControlApiSurfaceDocument apiSurface)
    {
        return new DocApiSurfacePayload(
            apiSurface.Members.Select(CreateApiMember).ToArray(),
            apiSurface.Events.Select(CreateApiEvent).ToArray(),
            apiSurface.InheritedContracts.Select(CreateApiInheritance).ToArray(),
            apiSurface.Diagnostics.Select(CreateApiDiagnostic).ToArray());
    }

    private static DocApiMemberPayload CreateApiMember(ApiMemberDocument member)
    {
        return new DocApiMemberPayload(
            member.Name,
            member.Kind,
            member.DeclaringType,
            member.Accessibility,
            member.Signature,
            member.Type,
            member.DefaultValue,
            member.ContractLevel,
            member.Description,
            member.SourcePath,
            member.SourceLine);
    }

    private static DocApiEventPayload CreateApiEvent(ApiEventDocument apiEvent)
    {
        return new DocApiEventPayload(
            apiEvent.Name,
            apiEvent.Kind,
            apiEvent.DeclaringType,
            apiEvent.Accessibility,
            apiEvent.Signature,
            apiEvent.RoutingStrategy,
            apiEvent.EventArgsType,
            apiEvent.Description,
            apiEvent.SourcePath,
            apiEvent.SourceLine);
    }

    private static DocApiInheritancePayload CreateApiInheritance(ApiInheritanceDocument inheritance)
    {
        return new DocApiInheritancePayload(
            inheritance.MemberName,
            inheritance.Kind,
            inheritance.DeclaringType,
            inheritance.Reason);
    }

    private static DocApiDiagnosticPayload CreateApiDiagnostic(ApiSurfaceDiagnostic diagnostic)
    {
        return new DocApiDiagnosticPayload(
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Severity,
            diagnostic.MemberName);
    }

    private static DocLogicStructurePayload CreateLogicStructure(ControlLogicStructureDocument logic)
    {
        return new DocLogicStructurePayload(
            logic.Inheritance.Select(CreateLogicNode).ToArray(),
            logic.PublicApiGroups.Select(CreateLogicNode).ToArray(),
            logic.StateFlows.Select(CreateLogicFlow).ToArray(),
            logic.RuntimeCollaborators.Select(CreateLogicNode).ToArray(),
            logic.ThemeBridge.Select(CreateLogicFlow).ToArray());
    }

    private static DocLogicNodePayload CreateLogicNode(LogicNodeDocument node)
    {
        return new DocLogicNodePayload(
            node.Id,
            node.Kind,
            node.Label,
            node.Description,
            node.RelatedApis);
    }

    private static DocLogicFlowPayload CreateLogicFlow(LogicFlowDocument flow)
    {
        return new DocLogicFlowPayload(flow.Id, flow.Title, flow.Steps);
    }

    private static DocControlThemePayload CreateTheme(ControlThemeDocument theme)
    {
        return new DocControlThemePayload(
            theme.TargetType,
            theme.Templates.Select(CreateThemeTemplate).ToArray(),
            theme.SelectorGroups.Select(CreateThemeSelectorGroup).ToArray(),
            theme.TemplateBindings.Select(CreateThemeBinding).ToArray(),
            theme.TokenUsages.Select(CreateThemeTokenUsage).ToArray(),
            theme.CustomizationBoundaries.Select(CreateThemeCustomizationBoundary).ToArray());
    }

    private static DocThemeTemplatePayload CreateThemeTemplate(ThemeTemplateDocument template)
    {
        return new DocThemeTemplatePayload(
            template.Id,
            template.SourceSelector,
            template.SourcePath,
            template.SourceLine,
            template.Roots.Select(CreateThemeNode).ToArray());
    }

    private static DocThemeNodePayload CreateThemeNode(ThemeNodeDocument node)
    {
        return new DocThemeNodePayload(
            node.ElementType,
            node.Name,
            node.Stability,
            node.Children.Select(CreateThemeNode).ToArray());
    }

    private static DocThemeSelectorGroupPayload CreateThemeSelectorGroup(ThemeSelectorGroupDocument group)
    {
        return new DocThemeSelectorGroupPayload(group.Kind, group.Selectors, group.Description);
    }

    private static DocThemeBindingPayload CreateThemeBinding(ThemeBindingDocument binding)
    {
        return new DocThemeBindingPayload(binding.TargetNode, binding.TargetProperty, binding.SourceProperty);
    }

    private static DocThemeTokenUsagePayload CreateThemeTokenUsage(ThemeTokenUsageDocument usage)
    {
        return new DocThemeTokenUsagePayload(usage.ResourceKind, usage.TokenName, usage.TargetSelector, usage.TargetProperty);
    }

    private static DocThemeCustomizationBoundaryPayload CreateThemeCustomizationBoundary(ThemeCustomizationBoundaryDocument boundary)
    {
        return new DocThemeCustomizationBoundaryPayload(boundary.Target, boundary.Stability, boundary.Guidance);
    }

    private static DocExamplePayload CreateExample(ControlExampleDocument example)
    {
        return new DocExamplePayload(
            example.SourceKey,
            example.Title,
            example.Description,
            example.Kind,
            example.Priority,
            example.BadgeText,
            example.Snippets.Select(CreateSnippet).ToArray(),
            example.SourcePath,
            example.SourceLine);
    }

    private static DocCodeSnippetPayload CreateSnippet(CodeSnippetDocument snippet)
    {
        return new DocCodeSnippetPayload(snippet.Language, snippet.Code, snippet.SourcePath, snippet.SourceLine);
    }

    private static DocTokenPayload CreateToken(ControlTokenDocument token)
    {
        return new DocTokenPayload(token.Name, token.Scope, token.Status, token.Description, token.ThemeUsage);
    }

    private static DocSemanticPartPayload CreateSemanticPart(ControlSemanticPartDocument semanticPart)
    {
        return new DocSemanticPartPayload(
            semanticPart.Part,
            semanticPart.AtomUINode,
            semanticPart.Responsibility,
            semanticPart.RelatedApis,
            semanticPart.RelatedTokens,
            semanticPart.Stability);
    }

    private static DocSourceFilePayload CreateSourceFile(ControlSourceFileDocument sourceFile)
    {
        return new DocSourceFilePayload(sourceFile.Kind, sourceFile.Path, sourceFile.Description);
    }

    private static DocRelatedItemPayload CreateRelated(DocumentRelatedItem item)
    {
        return new DocRelatedItemPayload(item.Kind, item.Id, item.Title, item.Command);
    }

    private static DocWarningPayload CreateWarning(DocumentWarning warning)
    {
        return new DocWarningPayload(warning.Code, warning.Message, warning.Section);
    }

    private static DocSuggestionPayload CreateSuggestion(DocumentSuggestion suggestion)
    {
        return new DocSuggestionPayload(suggestion.Kind, suggestion.Id, suggestion.Title, suggestion.ProductId);
    }
}

public sealed class DemoCommandHandler(
    MetadataQueryService metadata,
    DemoQueryService demos,
    DemoOutputRenderer renderer) : IAtomUICliCommandHandler<DemoCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(DemoCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Control))
        {
            return ValueTask.FromResult(MetadataErrors.MissingRequired("Control name is required."));
        }

        if (options.RemovedLanguageOption)
        {
            return ValueTask.FromResult(MetadataErrors.InvalidValue("Option '--language' was removed. Use --code-language."));
        }

        if (!string.IsNullOrWhiteSpace(options.InvalidCodeLanguage))
        {
            return ValueTask.FromResult(MetadataErrors.InvalidValue(
                $"Unknown demo code language '{options.InvalidCodeLanguage}'. Supported languages: xaml, csharp, all."));
        }

        if (options.CodeOnly && string.IsNullOrWhiteSpace(options.DemoKey))
        {
            return ValueTask.FromResult(MetadataErrors.InvalidValue("--code-only requires a demo-key."));
        }

        if (options.CodeOnly && options.Global.Format == OutputFormat.Json)
        {
            return ValueTask.FromResult(MetadataErrors.InvalidValue("--code-only cannot be combined with --format json."));
        }

        if (!string.IsNullOrWhiteSpace(options.Global.Product) && metadata.FindProduct(options.Global.Product) is null)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.PackageNotFound, $"Product '{options.Global.Product}' was not found."));
        }

        var query = new DemoQuery(
            options.Control,
            options.DemoKey,
            options.Global.Product,
            options.Scenario,
            options.Match,
            options.Mode,
            options.CodeLanguage,
            options.Strict,
            options.Global.TargetVersion ?? "6.0",
            options.Global.Language);
        var queryResult = demos.Query(query);
        if (queryResult.Status is not DemoQueryStatus.ListFound and not DemoQueryStatus.DemoFound)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(
                queryResult.ErrorCode ?? AtomUICliErrorCodes.DataUnavailable,
                queryResult.ErrorMessage ?? $"Demo query for '{options.Control}' failed."));
        }

        var payload = BuildPayload(options, queryResult);
        if (payload.SelectedDemo is not null && payload.SelectedDemo.Snippets.Count == 0)
        {
            return ValueTask.FromResult(MetadataErrors.InvalidValue(
                $"Demo '{payload.SelectedDemo.SourceKey}' does not contain code language '{options.CodeLanguage.ToString().ToLowerInvariant()}'."));
        }

        var includeSource = options.Global.Detail
                            || (options.IncludeSource && (options.Mode is not DemoCommandMode.List || options.ExpandAll));
        object resultPayload = options.Mode switch
        {
            DemoCommandMode.Code => renderer.RenderCodeOnly(payload),
            _ => options.Global.Format switch
            {
                OutputFormat.Json => payload,
                OutputFormat.Markdown => renderer.RenderMarkdown(payload, includeSource, options.ExpandAll),
                _ => renderer.RenderText(payload, includeSource, options.ExpandAll)
            }
        };

        return ValueTask.FromResult(AtomUICliResult.Success(resultPayload));
    }

    private static DemoCommandPayload BuildPayload(DemoCommandOptions options, DemoQueryResult queryResult)
    {
        var control = queryResult.Control ?? throw new InvalidOperationException("Control document is required.");
        var selectedDemo = queryResult.SelectedDemo is null
            ? null
            : CreateDemoItem(control, queryResult.SelectedDemo, options);

        return new DemoCommandPayload(
            "1.0",
            "demo",
            control.TargetVersion,
            options.Mode,
            options.CodeLanguage,
            CreateControl(control),
            CreateSource(control),
            options.DemoKey,
            options.Scenario,
            options.Match,
            queryResult.AvailableScenarios,
            queryResult.Demos.Select(demo => CreateDemoItem(control, demo, options)).ToArray(),
            selectedDemo,
            queryResult.Suggestions.Select(CreateSuggestion).ToArray(),
            queryResult.Warnings.Select(CreateWarning).ToArray());
    }

    private static DemoControlPayload CreateControl(ControlDocument control)
    {
        return new DemoControlPayload(
            control.Id,
            control.Name,
            control.DisplayName,
            control.CategoryId,
            control.ProductId,
            control.PackageId,
            control.IsCommercial);
    }

    private static DemoSourceIdentityPayload CreateSource(ControlDocument control)
    {
        return new DemoSourceIdentityPayload(
            control.SnapshotSchemaVersion,
            control.SnapshotId,
            control.Source.SourceRootConvention,
            control.Source.SourceRef,
            control.Source.SourceCommit,
            control.Source.GeneratedAt);
    }

    private static DemoItemPayload CreateDemoItem(
        ControlDocument control,
        ControlExampleDocument example,
        DemoCommandOptions options)
    {
        return new DemoItemPayload(
            example.SourceKey,
            example.Title,
            example.Description,
            example.Kind,
            example.Priority,
            example.BadgeText,
            SelectSnippets(example.Snippets, options.CodeLanguage).Select(CreateSnippet).ToArray(),
            example.SourcePath,
            example.SourceLine,
            options.IncludeRelated ? CreateRelatedCommands(control, example) : []);
    }

    private static IReadOnlyList<CodeSnippetDocument> SelectSnippets(
        IReadOnlyList<CodeSnippetDocument> snippets,
        DemoCodeLanguage codeLanguage)
    {
        return codeLanguage switch
        {
            DemoCodeLanguage.Xaml => snippets.Where(snippet => IsXamlSnippet(snippet.Language)).ToArray(),
            DemoCodeLanguage.CSharp => snippets.Where(snippet => snippet.Language.Equals("csharp", StringComparison.OrdinalIgnoreCase)).ToArray(),
            _ => snippets.ToArray()
        };
    }

    private static bool IsXamlSnippet(string language)
    {
        return language.Equals("xaml", StringComparison.OrdinalIgnoreCase)
               || language.Equals("xml", StringComparison.OrdinalIgnoreCase);
    }

    private static DemoCodeSnippetPayload CreateSnippet(CodeSnippetDocument snippet)
    {
        return new DemoCodeSnippetPayload(
            IsXamlSnippet(snippet.Language) ? "xaml" : snippet.Language.ToLowerInvariant(),
            snippet.Code,
            snippet.SourcePath,
            snippet.SourceLine);
    }

    private static IReadOnlyList<DemoRelatedCommandPayload> CreateRelatedCommands(
        ControlDocument control,
        ControlExampleDocument example)
    {
        return
        [
            new DemoRelatedCommandPayload(
                $"dotnet atomui doc {control.Name} --example {example.SourceKey}",
                "Open this example inside the full control documentation."),
            new DemoRelatedCommandPayload(
                $"dotnet atomui info {control.Name}",
                "Show the control metadata and API summary.")
        ];
    }

    private static DemoSuggestionPayload CreateSuggestion(DemoSuggestion suggestion)
    {
        return new DemoSuggestionPayload(suggestion.SourceKey, suggestion.Title, suggestion.Scenario);
    }

    private static DemoWarningPayload CreateWarning(DocumentWarning warning)
    {
        return new DemoWarningPayload(warning.Code, warning.Message);
    }
}

public sealed class TokenCommandHandler(
    TokenQueryService tokens,
    TokenOutputRenderer renderer) : IAtomUICliCommandHandler<TokenCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(TokenCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        var query = new TokenQueryRequest(
            options.Control,
            options.Token,
            options.Scope,
            options.Kind,
            options.Category,
            options.Match,
            options.Product ?? options.Global.Product,
            options.Theme,
            options.Include,
            options.Usage,
            options.Chain,
            options.Source,
            options.CustomizableOnly,
            options.UsedOnly,
            options.Strict,
            options.Global.TargetVersion);
        var queryResult = tokens.Query(query);
        if (queryResult.Status != TokenQueryStatus.Found)
        {
            var message = queryResult.ErrorMessage ?? "Token query failed.";
            if (queryResult.Suggestions.Count > 0)
            {
                message = $"{message} Suggestions: {string.Join(", ", queryResult.Suggestions)}.";
            }

            return ValueTask.FromResult(queryResult.Status == TokenQueryStatus.InvalidArgument
                ? MetadataErrors.InvalidValue(message)
                : MetadataErrors.NotFound(queryResult.ErrorCode ?? AtomUICliErrorCodes.DataUnavailable, message));
        }

        var payload = queryResult.Payload ?? throw new InvalidOperationException("Token query payload is required.");
        object resultPayload = options.Global.Format switch
        {
            OutputFormat.Json => payload,
            OutputFormat.Markdown => renderer.RenderMarkdown(payload, options.Global.Detail),
            _ => renderer.RenderText(payload, options.Global.Detail)
        };

        return ValueTask.FromResult(AtomUICliResult.Success(resultPayload));
    }
}

public sealed class SemanticCommandHandler(
    MetadataQueryService metadata,
    SemanticQueryService semantic,
    SemanticOutputRenderer renderer) : IAtomUICliCommandHandler<SemanticCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(SemanticCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Control))
        {
            return ValueTask.FromResult(MetadataErrors.MissingRequired("Control name is required."));
        }

        if (metadata.FindControl(options.Control, options.Global.Product) is null)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.ControlNotFound, $"Control '{options.Control}' was not found."));
        }

        var payload = semantic.Query(options);
        if (payload is null)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.ControlNotFound, $"Control '{options.Control}' was not found."));
        }

        if (options.Part is not null && payload.Parts.Count == 0)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.SemanticPartNotFound, $"Semantic part '{options.Part}' was not found."));
        }

        object resultPayload = options.Global.Format switch
        {
            OutputFormat.Json => payload,
            OutputFormat.Markdown => renderer.RenderMarkdown(payload, options.Global.Detail || options.IncludeTemplate),
            _ => renderer.RenderText(payload, options.Global.Detail || options.IncludeTemplate)
        };

        return ValueTask.FromResult(AtomUICliResult.Success(resultPayload));
    }
}

public sealed class DesignCommandHandler(MetadataQueryService metadata) : IAtomUICliCommandHandler<DesignCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(DesignCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        var document = metadata.FindDocument("topic", "design-language");
        return ValueTask.FromResult(AtomUICliResult.Success(document?.Markdown ?? metadata.RenderSummary()));
    }
}

public sealed class PackageCommandHandler(MetadataQueryService metadata) : IAtomUICliCommandHandler<PackageCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(PackageCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.PackageOrProduct))
        {
            return ValueTask.FromResult(AtomUICliResult.Success(string.Join(Environment.NewLine, metadata.ListPackages(options.Global.Product).Select(package => $"{package.Id} {package.Version}"))));
        }

        var package = metadata.FindPackageOrProduct(options.PackageOrProduct);
        if (package is null)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.PackageNotFound, $"Package or product '{options.PackageOrProduct}' was not found."));
        }

        return ValueTask.FromResult(AtomUICliResult.Success($"{package.Id} {package.Version}: {package.Description}{Environment.NewLine}Controls: {string.Join(", ", package.Controls)}"));
    }
}

public sealed class ChangelogCommandHandler(MetadataQueryService metadata) : IAtomUICliCommandHandler<ChangelogCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(ChangelogCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        var target = options.Control ?? options.PackageId;
        var entries = metadata.FindChangelog(target);
        var text = string.Join(Environment.NewLine, entries.Select(entry => $"{entry.Version} [{entry.Severity}] {entry.TargetId}: {entry.Message}"));
        return ValueTask.FromResult(AtomUICliResult.Success(string.IsNullOrWhiteSpace(text) ? "No changelog entries." : text));
    }
}

internal static class MetadataErrors
{
    public static AtomUICliResult MissingRequired(string message)
    {
        return AtomUICliResult.Failure(new AtomUICliError(
            AtomUICliErrorCodes.ArgumentMissingRequired,
            AtomUICliSeverity.Error,
            message,
            null,
            "parse",
            null,
            null));
    }

    public static AtomUICliResult NotFound(string code, string message)
    {
        return AtomUICliResult.Failure(new AtomUICliError(
            code,
            AtomUICliSeverity.Error,
            message,
            null,
            "execute",
            null,
            null));
    }

    public static AtomUICliResult InvalidValue(string message)
    {
        return AtomUICliResult.Failure(new AtomUICliError(
            AtomUICliErrorCodes.ArgumentInvalidValue,
            AtomUICliSeverity.Error,
            message,
            null,
            "parse",
            null,
            null));
    }
}
