using System.Text;

namespace AtomUI.Cli.Hosting.Metadata;

public sealed class PackageOutputRenderer
{
    public string RenderText(PackageCommandPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return payload.Query.Mode switch
        {
            "package" when payload.Package is not null => RenderPackageText(payload.Package, payload.DependencyTree),
            "product" when payload.Product is not null => RenderProductText(payload.Product, payload.Packages),
            _ => RenderListText(payload.Products, payload.Packages)
        };
    }

    public string RenderMarkdown(PackageCommandPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return payload.Query.Mode switch
        {
            "package" when payload.Package is not null => RenderPackageMarkdown(payload.Package, payload.DependencyTree),
            "product" when payload.Product is not null => RenderProductMarkdown(payload.Product, payload.Packages),
            _ => RenderListMarkdown(payload.Products, payload.Packages)
        };
    }

    private static string RenderListText(
        IReadOnlyList<ProductPackagePayload> products,
        IReadOnlyList<PackageSummaryPayload> packages)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"AtomUI packages: {packages.Count}");
        foreach (var product in products)
        {
            builder.AppendLine();
            builder.AppendLine(product.Name.Replace("AtomUI ", string.Empty, StringComparison.Ordinal));
            foreach (var package in packages.Where(package => package.ProductId.Equals(product.Id, StringComparison.OrdinalIgnoreCase)))
            {
                builder.AppendLine($"  {package.Id.PadRight(38)} {package.Version} {FormatPackageFlags(package)}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderProductText(
        ProductPackagePayload product,
        IReadOnlyList<PackageSummaryPayload> packages)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Product: {product.Id}");
        builder.AppendLine($"Name: {product.Name}");
        builder.AppendLine();
        AppendPackageGroup(builder, "Required packages:", packages.Where(package => package.IsRequired));
        AppendPackageGroup(builder, "Optional packages:", packages.Where(package => package.IsOptional && !package.IsCommercial));
        AppendPackageGroup(builder, "Commercial packages:", packages.Where(package => package.IsCommercial));
        builder.AppendLine();
        builder.AppendLine("Install:");
        foreach (var packageId in product.RequiredPackages)
        {
            builder.AppendLine($"  dotnet add package {packageId}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderPackageText(
        PackageDetailPayload package,
        PackageDependencyTreePayload? dependencyTree)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Package: {package.Summary.Id}");
        builder.AppendLine($"Version: {package.Summary.Version}");
        builder.AppendLine($"Product: {package.Summary.ProductId}");
        builder.AppendLine($"Visibility: {(package.Summary.IsCommercial ? "commercial" : "public")}");
        builder.AppendLine($"Required: {(package.Summary.IsRequired ? "yes" : "no")}");
        builder.AppendLine($"Root namespace: {package.RootNamespace}");
        builder.AppendLine($"Assembly: {package.AssemblyName}");
        AppendDependencies(builder, package.Dependencies);
        AppendRegistration(builder, package.Registration);
        AppendControls(builder, package.Controls);
        if (dependencyTree is not null)
        {
            builder.AppendLine();
            builder.AppendLine("Dependency tree:");
            foreach (var node in dependencyTree.Nodes)
            {
                AppendDependencyNode(builder, node);
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderListMarkdown(
        IReadOnlyList<ProductPackagePayload> products,
        IReadOnlyList<PackageSummaryPayload> packages)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# AtomUI Packages");
        foreach (var product in products)
        {
            builder.AppendLine();
            builder.AppendLine($"## {product.Name}");
            builder.AppendLine();
            builder.AppendLine("| Package | Version | Kind | Controls |");
            builder.AppendLine("| --- | --- | --- | --- |");
            foreach (var package in packages.Where(package => package.ProductId.Equals(product.Id, StringComparison.OrdinalIgnoreCase)))
            {
                builder.AppendLine($"| {package.Id} | {package.Version} | {FormatPackageFlags(package)} | {package.ControlCount} |");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderProductMarkdown(
        ProductPackagePayload product,
        IReadOnlyList<PackageSummaryPayload> packages)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# AtomUI Product: {product.Id}");
        builder.AppendLine();
        builder.AppendLine(product.Description);
        builder.AppendLine();
        builder.AppendLine("| Package | Version | Kind | Controls |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var package in packages)
        {
            builder.AppendLine($"| {package.Id} | {package.Version} | {FormatPackageFlags(package)} | {package.ControlCount} |");
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderPackageMarkdown(
        PackageDetailPayload package,
        PackageDependencyTreePayload? dependencyTree)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# AtomUI Package: {package.Summary.Id}");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"| Field | Value |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine($"| Version | {package.Summary.Version} |");
        builder.AppendLine($"| Product | {package.Summary.ProductId} |");
        builder.AppendLine($"| Visibility | {(package.Summary.IsCommercial ? "commercial" : "public")} |");
        builder.AppendLine($"| Root namespace | {package.RootNamespace} |");
        builder.AppendLine($"| Assembly | {package.AssemblyName} |");
        builder.AppendLine();
        builder.AppendLine("## Dependencies");
        builder.AppendLine();
        builder.AppendLine("| Package | Version | Kind |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var dependency in package.Dependencies)
        {
            builder.AppendLine($"| {dependency.Id} | {dependency.Version} | {dependency.Kind} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Registration");
        builder.AppendLine();
        foreach (var registration in package.Registration)
        {
            builder.AppendLine($"- `{registration.Name}` - {registration.Description}");
        }

        builder.AppendLine();
        builder.AppendLine("## Controls");
        builder.AppendLine();
        builder.AppendLine("| Control | Category |");
        builder.AppendLine("| --- | --- |");
        foreach (var control in package.Controls)
        {
            builder.AppendLine($"| {control.Name} | {control.CategoryName} |");
        }

        if (dependencyTree is not null)
        {
            builder.AppendLine();
            builder.AppendLine("## Dependency Tree");
            foreach (var node in dependencyTree.Nodes)
            {
                AppendDependencyNode(builder, node);
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendPackageGroup(StringBuilder builder, string title, IEnumerable<PackageSummaryPayload> packages)
    {
        var packageArray = packages.ToArray();
        if (packageArray.Length == 0)
        {
            return;
        }

        builder.AppendLine(title);
        foreach (var package in packageArray)
        {
            builder.AppendLine($"  {package.Id} {package.Version}{(package.IsCommercial ? " commercial" : string.Empty)}");
        }
    }

    private static void AppendDependencies(StringBuilder builder, IReadOnlyList<PackageDependencyPayload> dependencies)
    {
        if (dependencies.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Dependencies:");
        foreach (var dependency in dependencies)
        {
            var version = string.IsNullOrWhiteSpace(dependency.Version) ? string.Empty : $" {dependency.Version}";
            builder.AppendLine($"  {dependency.Id}{version}");
        }
    }

    private static void AppendRegistration(StringBuilder builder, IReadOnlyList<PackageRegistrationPayload> registration)
    {
        if (registration.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Registration:");
        foreach (var item in registration)
        {
            builder.AppendLine($"  {item.Name}");
        }
    }

    private static void AppendControls(StringBuilder builder, IReadOnlyList<PackageControlPayload> controls)
    {
        if (controls.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Controls:");
        builder.AppendLine($"  {string.Join(", ", controls.Select(control => control.Name))}");
    }

    private static void AppendDependencyNode(StringBuilder builder, PackageDependencyTreeNodePayload node)
    {
        builder.AppendLine($"{new string(' ', node.Depth * 2)}- {node.Id}");
        foreach (var child in node.Children)
        {
            AppendDependencyNode(builder, child);
        }
    }

    private static string FormatPackageFlags(PackageSummaryPayload package)
    {
        var visibility = package.IsCommercial ? "commercial" : "public";
        var requirement = package.IsRequired ? "required" : "optional";
        return $"{requirement} {visibility}";
    }
}
