using AtomUI.Cli.Entry;
using Xunit;

namespace AtomUI.Cli.Tests.FeatureModules;

public sealed class ProjectAnalysisCommandTests
{
    [Fact]
    public async Task EnvReturnsProjectNotFoundForMissingPath()
    {
        var exitCode = await Program.Main(["env", "/tmp/atomui-cli-missing-project"]);

        Assert.Equal(4, exitCode);
    }

    [Theory]
    [InlineData("env")]
    [InlineData("usage")]
    [InlineData("doctor")]
    [InlineData("lint")]
    [InlineData("migrate")]
    public async Task ProjectAnalysisCommandsReturnSuccessForFixtureProject(string command)
    {
        var fixture = CreateFixtureProject();

        var exitCode = await Program.Main([command, fixture]);

        Assert.Equal(0, exitCode);
    }

    private static string CreateFixtureProject()
    {
        var root = Path.Combine(Path.GetTempPath(), $"atomui-cli-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        File.WriteAllText(
            Path.Combine(root, "Fixture.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="AtomUI.Controls" Version="1.0.0-alpha.1" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(
            Path.Combine(root, "MainWindow.axaml"),
            """
            <Window xmlns="https://github.com/atomui">
              <Button />
            </Window>
            """);

        File.WriteAllText(
            Path.Combine(root, "MainWindow.cs"),
            """
            namespace Fixture;

            public sealed class MainWindow
            {
                public object Create() => new Button();
            }
            """);

        return root;
    }
}
