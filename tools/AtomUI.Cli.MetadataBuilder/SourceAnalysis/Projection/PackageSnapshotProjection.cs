namespace AtomUI.Cli.MetadataBuilder.SourceAnalysis.Projection;

internal static class PackageSnapshotProjection
{
    public static ExtractedPackageSnapshot Create(SourceAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var products = context.Facts.GetByPrefix("product:")
            .Select(fact => fact.Value)
            .OfType<ExtractedCatalogProduct>()
            .OrderBy(product => GetProductDisplayOrder(product.Id))
            .ThenBy(product => product.Id, StringComparer.Ordinal)
            .Select(product => new ExtractedPackageProduct(
                product.Id,
                product.Name,
                product.Description,
                GetProductDisplayOrder(product.Id)))
            .ToArray();
        var packages = context.Facts.GetByPrefix("package-document:")
            .Select(fact => fact.Value)
            .OfType<ExtractedPackageDocument>()
            .OrderBy(package => package.ProductId, StringComparer.Ordinal)
            .ThenBy(package => package.IsRequired ? 0 : package.IsCommercial ? 2 : 1)
            .ThenBy(package => package.Id, StringComparer.Ordinal)
            .ToArray();
        if (products.Length == 0 || packages.Length == 0)
        {
            throw new InvalidOperationException("Package facts have not been produced by the source analysis pipeline.");
        }

        return new ExtractedPackageSnapshot(
            "1.0",
            $"atomui-package-source-{context.Identity.SourceCommit}",
            context.Identity.TargetVersion,
            new ExtractedPackageSourceIdentity(
                context.Identity.SourceRootConvention,
                context.Identity.SourceRef,
                context.Identity.SourceCommit,
                DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
            products,
            packages,
            [],
            [],
            []);
    }

    private static int GetProductDisplayOrder(string productId)
    {
        return productId.ToLowerInvariant() switch
        {
            "desktop" => 10,
            "datagrid" => 20,
            "colorpicker" => 30,
            _ => 100
        };
    }
}
