using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class SourceDirectoryNamingTests
{
    [Fact]
    public void SourceTreeDoesNotUseOutputDirectoryName()
    {
        var srcRoot = Path.Combine(ResolveRepoRoot(), "src");
        var outputDirectories = Directory
            .EnumerateDirectories(srcRoot, "*", SearchOption.AllDirectories)
            .Where(directory => string.Equals(Path.GetFileName(directory), "Output", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(outputDirectories);
    }

    private static string ResolveRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "AtomUICli.slnx");
            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Cannot locate AtomUICli repository root.");
    }
}
