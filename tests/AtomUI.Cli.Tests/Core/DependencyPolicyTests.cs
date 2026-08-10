using System.Xml.Linq;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class DependencyPolicyTests
{
    [Fact]
    public void ModularityProjectUsesFoundationPackages()
    {
        var repoRoot = FindRepoRoot();
        var centralPackages = XDocument.Load(Path.Combine(repoRoot, "Directory.Packages.props"));
        var versionProps = XDocument.Load(Path.Combine(repoRoot, "build/Version.props"));
        var modularityProject = XDocument.Load(Path.Combine(repoRoot, "src/AtomUI.Cli.Modularity/AtomUI.Cli.Modularity.csproj"));

        var packageVersions = centralPackages
            .Descendants("PackageVersion")
            .ToDictionary(
                element => element.Attribute("Include")?.Value ?? string.Empty,
                element => element.Attribute("Version")?.Value ?? string.Empty,
                StringComparer.Ordinal);

        Assert.Equal("$(AtomUIFoundationVersion)", packageVersions["AtomUI.Foundation"]);
        Assert.Equal("$(AtomUIFoundationVersion)", packageVersions["AtomUI.Foundation.Generator"]);
        Assert.DoesNotContain(LegacyPackageName, packageVersions.Keys);
        Assert.DoesNotContain(LegacyGeneratorPackageName, packageVersions.Keys);

        var foundationVersion = versionProps
            .Descendants("AtomUIFoundationVersion")
            .Single()
            .Value;

        Assert.Equal("1.0.0", foundationVersion);

        var packageReferences = modularityProject
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("AtomUI.Foundation", packageReferences);
        Assert.Contains("AtomUI.Foundation.Generator", packageReferences);
        Assert.DoesNotContain(LegacyPackageName, packageReferences);
        Assert.DoesNotContain(LegacyGeneratorPackageName, packageReferences);
    }

    private static string LegacyPackageName => string.Concat("AtomUI.", "Base");

    private static string LegacyGeneratorPackageName => string.Concat(LegacyPackageName, ".Generator");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AtomUICli.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate AtomUICli repository root.");
    }
}
