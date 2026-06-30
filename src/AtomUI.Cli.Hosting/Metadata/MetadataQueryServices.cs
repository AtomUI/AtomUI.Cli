namespace AtomUI.Cli.Hosting.Metadata;

public sealed class MetadataQueryService(MetadataCatalog catalog)
{
    private static readonly Lazy<DocumentSnapshotRegistry> DefaultDocuments = new(DocumentSnapshotRegistry.CreateDefault);

    public MetadataCatalog Catalog => catalog;

    public IReadOnlyList<ControlDescriptor> ListControls(
        string? productId = null,
        string? category = null,
        string? packageId = null,
        bool includeHidden = false)
    {
        var normalizedCategory = NormalizeFilter(category);
        return catalog.Controls
            .Where(control => Matches(control.ProductId, productId)
                              && Matches(control.PackageId, packageId)
                              && (includeHidden || !control.IsHidden)
                              && (string.IsNullOrWhiteSpace(normalizedCategory) || MatchesCategory(control, normalizedCategory)))
            .OrderBy(control => GetCategoryOrder(control.CategoryId))
            .ThenBy(control => control.DisplayOrder)
            .ToArray();
    }

    public IReadOnlyList<ControlCategoryDescriptor> ListCategories(string? productId = null, bool includeHidden = false)
    {
        var categoryIds = ListControls(productId, includeHidden: includeHidden)
            .Select(control => control.CategoryId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return catalog.Categories
            .Where(category => categoryIds.Contains(category.Id))
            .OrderBy(category => category.DisplayOrder)
            .ToArray();
    }

    public IReadOnlyList<ProductDescriptor> ListProducts()
    {
        return catalog.Products.ToArray();
    }

    public IReadOnlyList<PackageDescriptor> ListPackages(string? productId = null)
    {
        return catalog.Packages
            .Where(package => Matches(package.ProductId, productId))
            .ToArray();
    }

    public ProductDescriptor? FindProduct(string productId)
    {
        return catalog.Products.FirstOrDefault(product => product.Id.Equals(productId, StringComparison.OrdinalIgnoreCase));
    }

    public PackageDescriptor? FindPackage(string packageId)
    {
        return catalog.Packages.FirstOrDefault(package => package.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase));
    }

    public ControlDescriptor? FindControl(string name, string? productId = null)
    {
        return catalog.Controls.FirstOrDefault(control =>
            control.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            && Matches(control.ProductId, productId));
    }

    public PackageDescriptor? FindPackageOrProduct(string value)
    {
        return catalog.Packages.FirstOrDefault(package => package.Id.Equals(value, StringComparison.OrdinalIgnoreCase))
               ?? catalog.Products
                   .Select(product => catalog.Packages.FirstOrDefault(package => package.ProductId.Equals(product.Id, StringComparison.OrdinalIgnoreCase)))
                   .FirstOrDefault(package => package is not null && package.ProductId.Equals(value, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<TokenDescriptor> FindTokens(string? controlName, string? name = null, string? match = null)
    {
        return catalog.Tokens
            .Where(token => controlName is null
                ? token.Scope.Equals("global", StringComparison.OrdinalIgnoreCase)
                : token.ControlName?.Equals(controlName, StringComparison.OrdinalIgnoreCase) == true)
            .Where(token => string.IsNullOrWhiteSpace(name) || token.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .Where(token => string.IsNullOrWhiteSpace(match)
                            || token.Name.Contains(match, StringComparison.OrdinalIgnoreCase)
                            || token.Description.Contains(match, StringComparison.OrdinalIgnoreCase))
            .OrderBy(token => token.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<DemoDescriptor> FindDemos(string controlName, string? demoName = null)
    {
        return catalog.Demos
            .Where(demo => demo.ControlName.Equals(controlName, StringComparison.OrdinalIgnoreCase))
            .Where(demo => string.IsNullOrWhiteSpace(demoName) || demo.Name.Equals(demoName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(demo => demo.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public DocumentDescriptor? FindDocument(string kind, string targetId)
    {
        return catalog.Documents.FirstOrDefault(document =>
            document.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase)
            && document.TargetId.Equals(targetId, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<SemanticPartDescriptor> FindSemanticParts(string controlName, string? part = null)
    {
        return catalog.SemanticParts
            .Where(item => item.ControlName.Equals(controlName, StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(part) || item.Name.Equals(part, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<ChangelogEntryDescriptor> FindChangelog(string? targetId = null)
    {
        return catalog.Changelog
            .Where(item => string.IsNullOrWhiteSpace(targetId) || item.TargetId.Equals(targetId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Version, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.TargetId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string RenderSummary()
    {
        return $"AtomUI metadata {catalog.TargetVersion}: {catalog.Controls.Count} controls, {catalog.Packages.Count} packages.";
    }

    public InfoCommandPayload CreateInfoPayload(ControlDescriptor control, InfoCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(options);

        var targetVersion = options.Global.TargetVersion ?? catalog.TargetVersion;
        var document = FindControlDocument(control, targetVersion, options.Global.Language);
        return document is null
            ? CreateInfoPayloadFromCatalog(control, targetVersion)
            : CreateInfoPayloadFromDocument(control, document, targetVersion);
    }

    public ListCommandPayload CreateListPayload(ListCommandOptions options)
    {
        var controls = options.Kind is ListKind.Products or ListKind.Packages
            ? Array.Empty<ControlDescriptor>()
            : ListControls(options.Global.Product, options.Category, options.PackageId, options.IncludeHidden);
        var products = options.Kind is ListKind.Controls or ListKind.Categories
            ? Array.Empty<ProductDescriptor>()
            : ListProducts().Where(product => Matches(product.Id, options.Global.Product)).ToArray();
        var packages = options.Kind is ListKind.Controls or ListKind.Categories or ListKind.Products
            ? Array.Empty<PackageDescriptor>()
            : ListPackages(options.Global.Product);
        var categories = CreateCategoryPayloads(controls);

        return new ListCommandPayload(
            "1.0",
            catalog.TargetVersion,
            options.Kind,
            options.Global.Product,
            options.Category,
            options.PackageId,
            categories,
            products.Select(ListProductPayload.FromDescriptor).ToArray(),
            packages.Select(ListPackagePayload.FromDescriptor).ToArray(),
            []);
    }

    private static ControlDocument? FindControlDocument(ControlDescriptor control, string targetVersion, string language)
    {
        var snapshots = DefaultDocuments.Value.Snapshots
            .Where(snapshot => snapshot.TargetVersion.Equals(targetVersion, StringComparison.OrdinalIgnoreCase)
                               || snapshot.TargetVersion.StartsWith(targetVersion, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var languageMatches = snapshots.Where(snapshot => snapshot.Language.Equals(language, StringComparison.OrdinalIgnoreCase)).ToArray();
        var candidates = languageMatches.Length > 0 ? languageMatches : snapshots;
        return candidates
            .SelectMany(snapshot => snapshot.Controls)
            .FirstOrDefault(document =>
                document.Name.Equals(control.Name, StringComparison.OrdinalIgnoreCase)
                && document.ProductId.Equals(control.ProductId, StringComparison.OrdinalIgnoreCase));
    }

    private InfoCommandPayload CreateInfoPayloadFromDocument(
        ControlDescriptor control,
        ControlDocument document,
        string targetVersion)
    {
        return new InfoCommandPayload(
            "1.0",
            "info",
            targetVersion,
            CreateIdentity(control),
            new InfoControlTypePayload(
                document.Identity.Namespace,
                document.PackageId,
                $"{document.Identity.Namespace}.{document.Name}",
                document.Identity.BaseType ?? "Avalonia.Controls.Control",
                document.ApiSurface.InheritedContracts
                    .Where(item => item.Kind.Equals("interface", StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.MemberName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .ToArray(),
                document.Identity.XamlNamespace,
                "atom"),
            new InfoControlDescriptionPayload(
                document.Summary,
                document.Usage.Summary,
                string.Join(" ", document.Usage.WhenToUse.Take(2))),
            new InfoControlUsagePayload(
                [document.PackageId],
                [document.Identity.Namespace],
                document.Usage.MinimalSnippets.FirstOrDefault(item => item.Language.Equals("xml", StringComparison.OrdinalIgnoreCase))?.Code
                    ?? $"<atom:{document.Name} />",
                document.Usage.MinimalSnippets.FirstOrDefault(item => item.Language.Equals("csharp", StringComparison.OrdinalIgnoreCase))?.Code),
            document.ApiSurface.Members.Select(CreateApi).ToArray(),
            new InfoTemplateContractPayload(
                document.Theme.Templates.Select(template => template.SourcePath).Distinct(StringComparer.Ordinal).ToArray(),
                document.Theme.Templates
                    .SelectMany(template => template.Roots)
                    .Where(node => !string.IsNullOrWhiteSpace(node.Name))
                    .Select(node => new InfoTemplatePartPayload(
                        node.Name!,
                        node.ElementType,
                        IsRequired: true,
                        document.Theme.Templates.First(template => template.Roots.Contains(node)).SourcePath,
                        document.SemanticParts.FirstOrDefault(part => part.Part.Equals(node.Name, StringComparison.Ordinal))?.Responsibility
                            ?? $"{node.Name} template part extracted from ControlTheme."))
                    .DistinctBy(part => part.Name)
                    .ToArray(),
                ["Template contract is generated from ControlTheme source."]),
            document.LogicStructure.StateFlows
                .Select(flow => new InfoControlStatePayload(
                    flow.Id.StartsWith(':') ? flow.Id : $":{flow.Id}",
                    "pseudo-class",
                    string.Join(" ", flow.Steps),
                    "control-source"))
                .ToArray(),
            CreateTokenSummaries(document),
            document.Examples
                .Select(example => new InfoDemoSummaryPayload(
                    example.SourceKey,
                    example.Title,
                    example.Description,
                    $"{control.GalleryRoute}/{example.SourceKey}",
                    [example.Kind]))
                .ToArray(),
            document.Related.Select(item => new InfoRelatedCommandPayload(item.Command, item.Title)).ToArray(),
            CreateDiagnostics(control));
    }

    private InfoCommandPayload CreateInfoPayloadFromCatalog(ControlDescriptor control, string targetVersion)
    {
        return new InfoCommandPayload(
            "1.0",
            "info",
            targetVersion,
            CreateIdentity(control),
            new InfoControlTypePayload(
                control.Namespace,
                control.PackageId,
                $"{control.Namespace}.{control.Name}",
                "Avalonia.Controls.Control",
                [],
                "https://atomui.net",
                "atom"),
            new InfoControlDescriptionPayload(
                control.Description,
                control.Description,
                $"Use {control.Name} from {control.PackageId}."),
            new InfoControlUsagePayload(
                [control.PackageId],
                [control.Namespace],
                $"<atom:{control.Name} />",
                $"new {control.Name}();"),
            [],
            new InfoTemplateContractPayload([], [], []),
            [],
            [],
            [],
            CreateRelatedCommands(control.Name),
            CreateDiagnostics(control));
    }

    private static InfoControlIdentityPayload CreateIdentity(ControlDescriptor control)
    {
        return new InfoControlIdentityPayload(
            control.Name,
            control.DisplayName,
            control.CategoryId,
            control.CategoryName,
            control.ProductId,
            control.PackageId,
            control.IsCommercial,
            control.IsOptionalPackage,
            NormalizeGalleryRoute(control.GalleryRoute),
            "Stable");
    }

    private static InfoApiMemberPayload CreateApi(ApiMemberDocument member)
    {
        return new InfoApiMemberPayload(
            member.Name,
            member.Kind.Contains("method", StringComparison.OrdinalIgnoreCase) ? "method" : member.Kind.Contains("constructor", StringComparison.OrdinalIgnoreCase) ? "constructor" : "property",
            member.Type ?? string.Empty,
            member.DefaultValue ?? string.Empty,
            NormalizeMemberKind(member.Kind),
            member.DeclaringType,
            IsBindable: member.Kind is "styled-property" or "direct-property",
            IsInherited: false,
            IsCurated: true,
            IsDeprecated: false,
            Since: null,
            Replacement: null,
            member.Description);
    }

    private static IReadOnlyList<InfoTokenSummaryPayload> CreateTokenSummaries(ControlDocument document)
    {
        if (document.Tokens.Count == 0)
        {
            return [];
        }

        var summaries = new List<InfoTokenSummaryPayload>
        {
            new(
                $"{document.Name}Token",
                "control",
                "Token",
                "generated",
                null,
                $"{document.Name}TokenResource",
                $"Control token resource set generated from {document.Name} token source.")
        };
        summaries.AddRange(document.Tokens
            .Take(24)
            .Select(token => new InfoTokenSummaryPayload(
                token.Name,
                token.Scope,
                "Token",
                token.Status,
                null,
                $"{document.Name}TokenResource {token.Name}",
                token.Description)));

        return summaries;
    }

    private static string NormalizeMemberKind(string kind)
    {
        return kind switch
        {
            "styled-property" => "styled",
            "direct-property" => "direct",
            "protected-method" => "protected",
            "public-method" => "public",
            "constructor" => "constructor",
            _ => kind
        };
    }

    private static IReadOnlyList<InfoRelatedCommandPayload> CreateRelatedCommands(string controlName)
    {
        return
        [
            new InfoRelatedCommandPayload($"dotnet atomui demo {controlName}", "Show Gallery demos for this control."),
            new InfoRelatedCommandPayload($"dotnet atomui token {controlName}", "Show full token metadata for this control."),
            new InfoRelatedCommandPayload($"dotnet atomui semantic {controlName}", "Show semantic and template details for this control.")
        ];
    }

    private static IReadOnlyList<InfoDiagnosticPayload> CreateDiagnostics(ControlDescriptor control)
    {
        var diagnostics = new List<InfoDiagnosticPayload>();
        if (control.IsOptionalPackage)
        {
            diagnostics.Add(new InfoDiagnosticPayload(
                "ATOMUICLI_INFO_OPTIONAL_PACKAGE",
                "info",
                $"The {control.Name} control is delivered by optional package {control.PackageId}.",
                $"Run `dotnet atomui package {control.PackageId}`."));
        }

        if (control.IsCommercial)
        {
            diagnostics.Add(new InfoDiagnosticPayload(
                "ATOMUICLI_INFO_COMMERCIAL",
                "info",
                $"The {control.Name} control belongs to a commercial AtomUI product.",
                $"Run `dotnet atomui package {control.PackageId}`."));
        }

        return diagnostics;
    }

    private static string NormalizeGalleryRoute(string route)
    {
        return route;
    }

    private IReadOnlyList<ListControlCategoryPayload> CreateCategoryPayloads(IReadOnlyList<ControlDescriptor> controls)
    {
        return catalog.Categories
            .OrderBy(category => category.DisplayOrder)
            .Select(category => new ListControlCategoryPayload(
                category.Id,
                category.Name,
                category.DisplayOrder,
                controls
                    .Where(control => control.CategoryId.Equals(category.Id, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(control => control.DisplayOrder)
                    .Select(ListControlPayload.FromDescriptor)
                    .ToArray()))
            .Where(category => category.Controls.Count > 0)
            .ToArray();
    }

    private int GetCategoryOrder(string categoryId)
    {
        return catalog.Categories.FirstOrDefault(category => category.Id.Equals(categoryId, StringComparison.OrdinalIgnoreCase))?.DisplayOrder ?? int.MaxValue;
    }

    private static bool MatchesCategory(ControlDescriptor control, string normalizedFilter)
    {
        return NormalizeFilter(control.CategoryId).Equals(normalizedFilter, StringComparison.Ordinal)
               || NormalizeFilter(control.CategoryName).Equals(normalizedFilter, StringComparison.Ordinal);
    }

    internal static string NormalizeFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    }

    private static bool Matches(string value, string? filter)
    {
        return string.IsNullOrWhiteSpace(filter) || value.Equals(filter, StringComparison.OrdinalIgnoreCase);
    }
}
