namespace AtomUI.Cli.Hosting.Metadata;

public enum PackageQueryKind
{
    All,
    Package,
    Product
}

public enum PackageIncludeSection
{
    Summary,
    Dependencies,
    Controls,
    Registration,
    Compatibility,
    Conflicts,
    Replacements,
    Source,
    Diagnostics,
    All
}

public sealed record PackageQuery(
    string? Target,
    PackageQueryKind Kind,
    string? ProductId,
    string? TargetVersion,
    string Language,
    IReadOnlySet<PackageIncludeSection> Include,
    bool Tree,
    bool CommercialOnly,
    bool IncludeHidden,
    bool Strict);

public enum PackageQueryResultKind
{
    Listed,
    FoundPackage,
    FoundProduct,
    NotFound,
    Ambiguous,
    DataUnavailable,
    Invalid
}

public sealed record PackageQueryResult(
    PackageQueryResultKind Kind,
    PackageCommandPayload? Payload,
    string? ErrorCode,
    string? ErrorMessage)
{
    public bool IsSuccess => Payload is not null;

    public static PackageQueryResult Success(PackageQueryResultKind kind, PackageCommandPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return new PackageQueryResult(kind, payload, null, null);
    }

    public static PackageQueryResult Failure(PackageQueryResultKind kind, string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new PackageQueryResult(kind, null, code, message);
    }
}

public sealed class PackageQueryService(PackageSnapshotRegistry registry)
{
    private const string SchemaVersion = "1.0";
    private const string CommandName = "package";

    public PackageQueryResult Query(PackageQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var snapshot = SelectSnapshot(query.TargetVersion);
        if (snapshot is null)
        {
            var targetVersion = string.IsNullOrWhiteSpace(query.TargetVersion) ? "latest" : query.TargetVersion;
            return PackageQueryResult.Failure(
                PackageQueryResultKind.DataUnavailable,
                AtomUICliErrorCodes.DataVersionUnresolved,
                $"Package snapshot for version '{targetVersion}' was not found.");
        }

        if (!string.IsNullOrWhiteSpace(query.ProductId)
            && snapshot.Products.All(product => !product.Id.Equals(query.ProductId, StringComparison.OrdinalIgnoreCase)))
        {
            return PackageQueryResult.Failure(
                PackageQueryResultKind.NotFound,
                AtomUICliErrorCodes.PackageNotFound,
                $"Product '{query.ProductId}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(query.Target))
        {
            return PackageQueryResult.Success(
                PackageQueryResultKind.Listed,
                CreatePayload(snapshot, query, "list", null, null));
        }

        var packageMatches = query.Kind == PackageQueryKind.Product
            ? []
            : ResolvePackageMatches(snapshot, query.Target, query.Strict, exactOnly: true);
        var productMatches = query.Kind == PackageQueryKind.Package
            ? []
            : ResolveProductMatches(snapshot, query.Target, query.Strict, exactOnly: true);

        if (packageMatches.Length == 0 && productMatches.Length == 0 && !query.Strict)
        {
            packageMatches = query.Kind == PackageQueryKind.Product
                ? []
                : ResolvePackageMatches(snapshot, query.Target, query.Strict, exactOnly: false);
            productMatches = query.Kind == PackageQueryKind.Package
                ? []
                : ResolveProductMatches(snapshot, query.Target, query.Strict, exactOnly: false);
        }

        if (packageMatches.Length > 0 && productMatches.Length > 0 && query.Kind == PackageQueryKind.All)
        {
            return PackageQueryResult.Failure(
                PackageQueryResultKind.Ambiguous,
                AtomUICliErrorCodes.PackageAmbiguous,
                $"Package or product '{query.Target}' is ambiguous. Use --kind package or --kind product.");
        }

        if (packageMatches.Length > 1 || productMatches.Length > 1)
        {
            return PackageQueryResult.Failure(
                PackageQueryResultKind.Ambiguous,
                AtomUICliErrorCodes.PackageAmbiguous,
                $"Package or product '{query.Target}' matched multiple candidates.");
        }

        if (packageMatches.Length == 1)
        {
            var package = packageMatches[0];
            if (!string.IsNullOrWhiteSpace(query.ProductId)
                && !package.ProductId.Equals(query.ProductId, StringComparison.OrdinalIgnoreCase))
            {
                return PackageQueryResult.Failure(
                    PackageQueryResultKind.NotFound,
                    AtomUICliErrorCodes.PackageNotFound,
                    $"Package '{package.Id}' does not belong to product '{query.ProductId}'.");
            }

            return PackageQueryResult.Success(
                PackageQueryResultKind.FoundPackage,
                CreatePayload(snapshot, query, "package", package, null));
        }

        if (productMatches.Length == 1)
        {
            var product = productMatches[0];
            if (!string.IsNullOrWhiteSpace(query.ProductId)
                && !product.Id.Equals(query.ProductId, StringComparison.OrdinalIgnoreCase))
            {
                return PackageQueryResult.Failure(
                    PackageQueryResultKind.NotFound,
                    AtomUICliErrorCodes.PackageNotFound,
                    $"Product '{product.Id}' does not match product filter '{query.ProductId}'.");
            }

            return PackageQueryResult.Success(
                PackageQueryResultKind.FoundProduct,
                CreatePayload(snapshot, query, "product", null, product));
        }

        return PackageQueryResult.Failure(
            PackageQueryResultKind.NotFound,
            AtomUICliErrorCodes.PackageNotFound,
            $"Package or product '{query.Target}' was not found.");
    }

    private PackageSnapshot? SelectSnapshot(string? targetVersion)
    {
        var normalizedVersion = string.IsNullOrWhiteSpace(targetVersion) ? null : targetVersion;
        return registry.Snapshots
            .Where(snapshot => normalizedVersion is null
                               || snapshot.TargetVersion.Equals(normalizedVersion, StringComparison.OrdinalIgnoreCase)
                               || snapshot.TargetVersion.StartsWith(normalizedVersion, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(snapshot => snapshot.TargetVersion, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static PackageDocument[] ResolvePackageMatches(PackageSnapshot snapshot, string target, bool strict, bool exactOnly)
    {
        var exact = snapshot.Packages
            .Where(package => package.Id.Equals(target, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (exact.Length > 0 || strict || exactOnly)
        {
            return exact;
        }

        return snapshot.Packages
            .Where(package => package.Id.Contains(target, StringComparison.OrdinalIgnoreCase))
            .OrderBy(package => package.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static ProductPackageProfile[] ResolveProductMatches(PackageSnapshot snapshot, string target, bool strict, bool exactOnly)
    {
        var exact = snapshot.Products
            .Where(product => product.Id.Equals(target, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (exact.Length > 0 || strict || exactOnly)
        {
            return exact;
        }

        return snapshot.Products
            .Where(product => product.Id.Contains(target, StringComparison.OrdinalIgnoreCase)
                              || product.Name.Contains(target, StringComparison.OrdinalIgnoreCase))
            .OrderBy(product => product.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static PackageCommandPayload CreatePayload(
        PackageSnapshot snapshot,
        PackageQuery query,
        string mode,
        PackageDocument? selectedPackage,
        ProductPackageProfile? selectedProduct)
    {
        var include = NormalizeIncludes(query.Include, query.Tree);
        var packages = SelectVisiblePackages(snapshot, query, selectedPackage, selectedProduct)
            .Select(CreateSummary)
            .ToArray();
        var products = SelectProducts(snapshot, packages)
            .Select(product => CreateProductPayload(product, packages))
            .ToArray();
        var productPayload = selectedProduct is null
            ? null
            : CreateProductPayload(selectedProduct, packages);
        var packagePayload = selectedPackage is null
            ? null
            : CreateDetail(selectedPackage, include);
        var dependencyTree = query.Tree && selectedPackage is not null
            ? CreateDependencyTree(snapshot, selectedPackage)
            : null;
        var conflicts = include.Contains(PackageIncludeSection.Conflicts) || include.Contains(PackageIncludeSection.All)
            ? snapshot.Conflicts.Select(CreateConflict).ToArray()
            : [];
        var replacements = include.Contains(PackageIncludeSection.Replacements) || include.Contains(PackageIncludeSection.All)
            ? snapshot.Replacements.Select(CreateReplacement).ToArray()
            : [];
        var diagnostics = include.Contains(PackageIncludeSection.Diagnostics) || include.Contains(PackageIncludeSection.All)
            ? snapshot.Diagnostics.Select(CreateDiagnostic).ToArray()
            : [];

        return new PackageCommandPayload(
            SchemaVersion,
            CommandName,
            snapshot.TargetVersion,
            new PackageQueryPayload(
                mode,
                query.Target,
                query.Kind.ToString().ToLowerInvariant(),
                query.ProductId,
                include.Select(item => item.ToString().ToLowerInvariant()).OrderBy(item => item, StringComparer.Ordinal).ToArray(),
                query.Tree,
                query.CommercialOnly,
                query.IncludeHidden,
                query.Strict),
            new PackageSourcePayload(
                snapshot.SnapshotId,
                snapshot.Source.SourceRootConvention,
                snapshot.Source.SourceRef,
                snapshot.Source.SourceCommit,
                snapshot.Source.GeneratedAt),
            products,
            productPayload,
            packages,
            packagePayload,
            dependencyTree,
            conflicts,
            replacements,
            diagnostics,
            CreateSuggestions(selectedPackage, selectedProduct));
    }

    private static IReadOnlySet<PackageIncludeSection> NormalizeIncludes(IReadOnlySet<PackageIncludeSection> include, bool tree)
    {
        var normalized = include.Count == 0
            ? new HashSet<PackageIncludeSection> { PackageIncludeSection.Summary }
            : new HashSet<PackageIncludeSection>(include);
        if (tree)
        {
            normalized.Add(PackageIncludeSection.Dependencies);
        }

        return normalized;
    }

    private static IReadOnlyList<PackageDocument> SelectVisiblePackages(
        PackageSnapshot snapshot,
        PackageQuery query,
        PackageDocument? selectedPackage,
        ProductPackageProfile? selectedProduct)
    {
        if (selectedPackage is not null)
        {
            return [selectedPackage];
        }

        var productFilter = selectedProduct?.Id ?? query.ProductId;
        return snapshot.Packages
            .Where(package => query.IncludeHidden || !package.IsHidden)
            .Where(package => string.IsNullOrWhiteSpace(productFilter) || package.ProductId.Equals(productFilter, StringComparison.OrdinalIgnoreCase))
            .Where(package => !query.CommercialOnly || package.IsCommercial)
            .OrderBy(package => package.ProductId, StringComparer.Ordinal)
            .ThenBy(package => package.IsRequired ? 0 : package.IsCommercial ? 2 : 1)
            .ThenBy(package => package.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<ProductPackageProfile> SelectProducts(
        PackageSnapshot snapshot,
        IReadOnlyList<PackageSummaryPayload> packages)
    {
        var productIds = packages
            .Select(package => package.ProductId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return snapshot.Products
            .Where(product => productIds.Contains(product.Id))
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static ProductPackagePayload CreateProductPayload(
        ProductPackageProfile product,
        IReadOnlyList<PackageSummaryPayload> packages)
    {
        var productPackages = packages
            .Where(package => package.ProductId.Equals(product.Id, StringComparison.OrdinalIgnoreCase))
            .OrderBy(package => package.IsRequired ? 0 : package.IsCommercial ? 2 : 1)
            .ThenBy(package => package.Id, StringComparer.Ordinal)
            .ToArray();
        return new ProductPackagePayload(
            product.Id,
            product.Name,
            product.Description,
            product.DisplayOrder,
            productPackages.Where(package => package.IsRequired).Select(package => package.Id).ToArray(),
            productPackages.Where(package => package.IsOptional && !package.IsCommercial).Select(package => package.Id).ToArray(),
            productPackages.Where(package => package.IsCommercial).Select(package => package.Id).ToArray());
    }

    private static PackageSummaryPayload CreateSummary(PackageDocument package)
    {
        return new PackageSummaryPayload(
            package.Id,
            package.ProductId,
            package.Version,
            package.Description,
            package.PackageKind,
            package.IsRequired,
            package.IsOptional,
            package.IsCommercial,
            package.Controls.Count);
    }

    private static PackageDetailPayload CreateDetail(
        PackageDocument package,
        IReadOnlySet<PackageIncludeSection> include)
    {
        var includeAll = include.Contains(PackageIncludeSection.All);
        return new PackageDetailPayload(
            CreateSummary(package),
            package.TargetFrameworks,
            package.RootNamespace,
            package.AssemblyName,
            includeAll || include.Contains(PackageIncludeSection.Dependencies)
                ? package.Dependencies.Select(CreateDependency).ToArray()
                : [],
            includeAll || include.Contains(PackageIncludeSection.Registration)
                ? package.Registration.Select(CreateRegistration).ToArray()
                : [],
            includeAll || include.Contains(PackageIncludeSection.Compatibility)
                ? CreateCompatibility(package.Compatibility)
                : new PackageCompatibilityPayload(package.TargetFrameworks, package.Compatibility.IsAotCompatible, package.Compatibility.IsTrimCompatible, []),
            includeAll || include.Contains(PackageIncludeSection.Controls)
                ? package.Controls.Select(CreateControl).ToArray()
                : [],
            includeAll || include.Contains(PackageIncludeSection.Source)
                ? package.SourceFiles.Select(CreateSourceFile).ToArray()
                : []);
    }

    private static PackageDependencyTreePayload CreateDependencyTree(PackageSnapshot snapshot, PackageDocument package)
    {
        var nodes = package.Dependencies
            .Select(dependency => CreateDependencyNode(
                snapshot,
                dependency,
                depth: 1,
                visited: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { package.Id }))
            .ToArray();
        return new PackageDependencyTreePayload(package.Id, nodes);
    }

    private static PackageDependencyTreeNodePayload CreateDependencyNode(
        PackageSnapshot snapshot,
        PackageDependencyDocument dependency,
        int depth,
        IReadOnlySet<string> visited)
    {
        var childPackage = snapshot.Packages.FirstOrDefault(package => package.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase));
        if (childPackage is null || visited.Contains(childPackage.Id))
        {
            return new PackageDependencyTreeNodePayload(dependency.Id, dependency.Version, dependency.Kind, depth, []);
        }

        var nextVisited = visited.ToHashSet(StringComparer.OrdinalIgnoreCase);
        nextVisited.Add(childPackage.Id);
        return new PackageDependencyTreeNodePayload(
            dependency.Id,
            dependency.Version,
            dependency.Kind,
            depth,
            childPackage.Dependencies.Select(child => CreateDependencyNode(snapshot, child, depth + 1, nextVisited)).ToArray());
    }

    private static PackageDependencyPayload CreateDependency(PackageDependencyDocument dependency)
    {
        return new PackageDependencyPayload(dependency.Id, dependency.Version, dependency.Kind, dependency.IsAtomUI);
    }

    private static PackageRegistrationPayload CreateRegistration(PackageRegistrationDocument registration)
    {
        return new PackageRegistrationPayload(registration.Name, registration.Kind, registration.Description, registration.SourcePath);
    }

    private static PackageCompatibilityPayload CreateCompatibility(PackageCompatibilityDocument compatibility)
    {
        return new PackageCompatibilityPayload(
            compatibility.TargetFrameworks,
            compatibility.IsAotCompatible,
            compatibility.IsTrimCompatible,
            compatibility.Notes);
    }

    private static PackageControlPayload CreateControl(PackageControlDocument control)
    {
        return new PackageControlPayload(control.Name, control.DisplayName, control.CategoryId, control.CategoryName, control.IsCommercial);
    }

    private static PackageSourceFilePayload CreateSourceFile(PackageSourceFileDocument sourceFile)
    {
        return new PackageSourceFilePayload(sourceFile.Kind, sourceFile.Path);
    }

    private static PackageConflictPayload CreateConflict(PackageConflictDocument conflict)
    {
        return new PackageConflictPayload(conflict.PackageId, conflict.ConflictingPackageId, conflict.Reason, conflict.Severity);
    }

    private static PackageReplacementPayload CreateReplacement(PackageReplacementDocument replacement)
    {
        return new PackageReplacementPayload(replacement.PackageId, replacement.ReplacementPackageId, replacement.Reason);
    }

    private static PackageDiagnosticPayload CreateDiagnostic(PackageDiagnostic diagnostic)
    {
        return new PackageDiagnosticPayload(diagnostic.Code, diagnostic.Severity, diagnostic.Message, diagnostic.PackageId);
    }

    private static IReadOnlyList<PackageSuggestionPayload> CreateSuggestions(
        PackageDocument? package,
        ProductPackageProfile? product)
    {
        if (package is not null)
        {
            return
            [
                new PackageSuggestionPayload($"dotnet add package {package.Id}", "Add the package to the current project."),
                new PackageSuggestionPayload($"dotnet atomui package {package.ProductId} --kind product", "Show related product packages.")
            ];
        }

        if (product is not null)
        {
            return
            [
                new PackageSuggestionPayload($"dotnet atomui package --product {product.Id}", "List all packages in this product.")
            ];
        }

        return [];
    }
}
