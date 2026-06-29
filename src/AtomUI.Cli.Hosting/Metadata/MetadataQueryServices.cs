namespace AtomUI.Cli.Hosting.Metadata;

public sealed class MetadataQueryService(MetadataCatalog catalog)
{
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
