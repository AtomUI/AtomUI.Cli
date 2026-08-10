using System.Xml.Linq;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class SourceProvisioningBuildTests
{
    [Fact]
    public void HostingProjectDefinesSourceProvisioningProperties()
    {
        var project = LoadHostingProject();
        var properties = project
            .Descendants()
            .Where(element => !element.HasElements)
            .GroupBy(element => element.Name.LocalName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(element => element.Value).ToArray(),
                StringComparer.Ordinal);

        Assert.Contains("false", properties["AtomUIAutoProvisionSource"]);
        Assert.Contains("https://github.com/AtomUI/AtomUI.git", properties["AtomUISourceRepository"]);
        Assert.True(properties.ContainsKey("AtomUISourceCommit"));
        Assert.Contains(properties["AtomUISourceRoot"], value => value.Contains(".workspace", StringComparison.Ordinal));
        Assert.Contains(properties["AtomUISourceRoot"], value => value.Contains("AtomUI", StringComparison.Ordinal));
    }

    [Fact]
    public void HostingProjectDefinesExplicitSourceProvisioningTargets()
    {
        var project = LoadHostingProject();
        var text = File.ReadAllText(HostingProjectPath());
        var ensure = FindTarget(project, "EnsureAtomUISource");
        var validate = FindTarget(project, "ValidateAtomUISourceIdentity");
        var generate = FindTarget(project, "GenerateBuiltInSourceAnalysisSnapshots");

        Assert.Equal("GenerateBuiltInSourceAnalysisSnapshots", ensure.Attribute("BeforeTargets")?.Value);
        Assert.Equal("GenerateBuiltInSourceAnalysisSnapshots", validate.Attribute("BeforeTargets")?.Value);
        Assert.Contains("git clone --branch", text, StringComparison.Ordinal);
        Assert.Contains("git -C &quot;$(AtomUISourceRoot)&quot; checkout --quiet &quot;$(AtomUISourceCommit)&quot;", text, StringComparison.Ordinal);
        Assert.Contains("git -C &quot;$(AtomUISourceRoot)&quot; rev-parse --verify &quot;$(AtomUISourceCommit)^{commit}&quot;", text, StringComparison.Ordinal);
        Assert.Contains("git -C &quot;$(AtomUISourceRoot)&quot; rev-parse HEAD", text, StringComparison.Ordinal);
        Assert.Contains("EnsureAtomUISource", generate.Attribute("DependsOnTargets")?.Value ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("ValidateAtomUISourceIdentity", generate.Attribute("DependsOnTargets")?.Value ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void HostingProjectNeverPullsOrOverwritesExistingAtomUISource()
    {
        var text = File.ReadAllText(HostingProjectPath());

        Assert.DoesNotContain("git pull", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("git reset", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rm -rf", text, StringComparison.OrdinalIgnoreCase);
    }

    private static XElement FindTarget(XDocument project, string name)
    {
        return project
            .Descendants("Target")
            .Single(element => element.Attribute("Name")?.Value == name);
    }

    private static XDocument LoadHostingProject()
    {
        return XDocument.Load(HostingProjectPath());
    }

    private static string HostingProjectPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "AtomUI.Cli.Hosting", "AtomUI.Cli.Hosting.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Cannot locate AtomUI.Cli.Hosting.csproj.");
    }
}
