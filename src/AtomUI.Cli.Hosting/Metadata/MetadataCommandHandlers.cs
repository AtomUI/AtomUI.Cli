namespace AtomUI.Cli.Hosting.Metadata;

public sealed class ListCommandHandler(MetadataQueryService metadata) : IAtomUICliCommandHandler<ListCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(ListCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        var text = options.Kind switch
        {
            ListKind.Products => string.Join(Environment.NewLine, metadata.ListProducts().Select(item => $"{item.Id}: {item.Name}")),
            ListKind.Packages => string.Join(Environment.NewLine, metadata.ListPackages(options.Global.Product).Select(item => $"{item.Id} {item.Version}")),
            ListKind.Categories => string.Join(Environment.NewLine, metadata.ListControls(options.Global.Product).Select(item => item.Category).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)),
            ListKind.All => metadata.RenderSummary(),
            _ => string.Join(Environment.NewLine, metadata.ListControls(options.Global.Product, options.Category).Select(item => $"{item.Name} ({item.PackageId})"))
        };

        return ValueTask.FromResult(AtomUICliResult.Success(string.IsNullOrWhiteSpace(text) ? metadata.RenderSummary() : text));
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
