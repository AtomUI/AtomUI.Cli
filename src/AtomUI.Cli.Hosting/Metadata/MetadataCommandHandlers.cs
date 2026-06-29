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

        var control = metadata.FindControl(options.Control, options.Global.Product);
        if (control is null)
        {
            return ValueTask.FromResult(MetadataErrors.NotFound(AtomUICliErrorCodes.ControlNotFound, $"Control '{options.Control}' was not found."));
        }

        var tokens = metadata.FindTokens(control.Name);
        var demos = metadata.FindDemos(control.Name);
        var text = $"{control.Name}: {control.Description}{Environment.NewLine}Package: {control.PackageId}{Environment.NewLine}Namespace: {control.Namespace}{Environment.NewLine}Tokens: {tokens.Count}{Environment.NewLine}Demos: {demos.Count}";
        return ValueTask.FromResult(AtomUICliResult.Success(text));
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
}
