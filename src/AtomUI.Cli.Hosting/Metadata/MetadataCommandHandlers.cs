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

public sealed class DocCommandHandler(MetadataQueryService metadata) : IAtomUICliCommandHandler<DocCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(DocCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        var target = options.Topic ?? options.Target;
        if (string.IsNullOrWhiteSpace(target))
        {
            return ValueTask.FromResult(MetadataErrors.MissingRequired("Document target or --topic is required."));
        }

        var kind = options.Topic is null ? "control" : "topic";
        var document = metadata.FindDocument(kind, target);
        if (document is null)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.DataReferenceMissing, $"Document '{target}' was not found."));
        }

        return ValueTask.FromResult(AtomUICliResult.Success(document.Markdown));
    }
}

public sealed class DemoCommandHandler(MetadataQueryService metadata) : IAtomUICliCommandHandler<DemoCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(DemoCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Control))
        {
            return ValueTask.FromResult(MetadataErrors.MissingRequired("Control name is required."));
        }

        if (metadata.FindControl(options.Control, options.Global.Product) is null)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.ControlNotFound, $"Control '{options.Control}' was not found."));
        }

        var demos = metadata.FindDemos(options.Control, options.DemoName);
        if (options.DemoName is not null && demos.Count == 0)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.DemoNotFound, $"Demo '{options.DemoName}' was not found."));
        }

        var text = options.CodeOnly && demos.Count > 0
            ? demos[0].Xaml
            : string.Join(Environment.NewLine, demos.Select(demo => $"{demo.Name}: {demo.Title}{Environment.NewLine}{demo.Xaml}"));
        return ValueTask.FromResult(AtomUICliResult.Success(text));
    }
}

public sealed class TokenCommandHandler(MetadataQueryService metadata) : IAtomUICliCommandHandler<TokenCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(TokenCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        if (options.Control is not null && metadata.FindControl(options.Control, options.Global.Product) is null)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.ControlNotFound, $"Control '{options.Control}' was not found."));
        }

        var tokens = metadata.FindTokens(options.Control, options.Name, options.Match);
        if (!string.IsNullOrWhiteSpace(options.Name) && tokens.Count == 0)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.TokenNotFound, $"Token '{options.Name}' was not found."));
        }

        return ValueTask.FromResult(AtomUICliResult.Success(string.Join(Environment.NewLine, tokens.Select(token => $"{token.Name}: {token.DefaultValue} ({token.Type})"))));
    }
}

public sealed class SemanticCommandHandler(MetadataQueryService metadata) : IAtomUICliCommandHandler<SemanticCommandOptions>
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

        var parts = metadata.FindSemanticParts(options.Control, options.Part);
        if (options.Part is not null && parts.Count == 0)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.SemanticPartNotFound, $"Semantic part '{options.Part}' was not found."));
        }

        return ValueTask.FromResult(AtomUICliResult.Success(string.Join(Environment.NewLine, parts.Select(part => $"{part.Name}: {part.Description}"))));
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
