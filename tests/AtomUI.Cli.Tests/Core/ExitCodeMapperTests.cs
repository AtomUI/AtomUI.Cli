using AtomUI.Cli;
using AtomUI.Cli.Hosting.Errors;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class ExitCodeMapperTests
{
    private readonly ExitCodeMapper _mapper = new(ErrorCodeCatalog.Default);

    [Fact]
    public void SuccessMapsToZero()
    {
        Assert.Equal(0, _mapper.Map(AtomUICliResult.Success()));
    }

    [Theory]
    [InlineData(AtomUICliErrorCodes.ArgumentMissingRequired, 2)]
    [InlineData(AtomUICliErrorCodes.ControlNotFound, 3)]
    [InlineData(AtomUICliErrorCodes.DataUnavailable, 4)]
    [InlineData(AtomUICliErrorCodes.PackageConflict, 5)]
    [InlineData(AtomUICliErrorCodes.SetupWriteFailed, 6)]
    public void HardErrorMapsByConcreteCode(string code, int expectedExitCode)
    {
        var result = AtomUICliResult.Failure(new AtomUICliError(
            code,
            AtomUICliSeverity.Error,
            "failed",
            Suggestion: null,
            Stage: "execute",
            Location: null,
            Details: null));

        Assert.Equal(expectedExitCode, _mapper.Map(result));
    }

    [Fact]
    public void UnknownHardErrorMapsToUnclassifiedExitCode()
    {
        var result = AtomUICliResult.Failure(new AtomUICliError(
            "NOT_REGISTERED",
            AtomUICliSeverity.Error,
            "failed",
            Suggestion: null,
            Stage: "execute",
            Location: null,
            Details: null));

        Assert.Equal(1, _mapper.Map(result));
    }

    [Fact]
    public void DiagnosticErrorMapsToDiagnosticFailureExitCode()
    {
        var result = AtomUICliResult.FromDiagnostics(
            [
                new AtomUICliDiagnostic(
                    AtomUICliErrorCodes.ProjectLintFinding,
                    AtomUICliSeverity.Error,
                    "xaml",
                    "Invalid namespace.",
                    File: "MainView.axaml",
                    Line: 24,
                    Suggestion: "Use the canonical namespace.")
            ],
            failOnWarning: false);

        Assert.Equal(5, _mapper.Map(result));
    }

    [Fact]
    public void DiagnosticWarningMapsToZeroUnlessFailOnWarningIsEnabled()
    {
        var diagnostic = new AtomUICliDiagnostic(
            AtomUICliErrorCodes.AotFinding,
            AtomUICliSeverity.Warning,
            "aot",
            "Dynamic access detected.",
            File: "Program.cs",
            Line: 12,
            Suggestion: "Use explicit registration.");

        Assert.Equal(0, _mapper.Map(AtomUICliResult.FromDiagnostics([diagnostic], failOnWarning: false)));
        Assert.Equal(5, _mapper.Map(AtomUICliResult.FromDiagnostics([diagnostic], failOnWarning: true)));
    }
}
