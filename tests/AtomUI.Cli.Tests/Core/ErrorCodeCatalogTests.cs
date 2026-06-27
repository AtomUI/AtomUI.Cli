using System.Text.RegularExpressions;
using AtomUI.Cli;
using AtomUI.Cli.Hosting.Errors;
using Xunit;

namespace AtomUI.Cli.Tests.Core;

public sealed class ErrorCodeCatalogTests
{
    private static readonly string[] ExpectedCodes =
    [
        "ATOMUICLI_SYS001",
        "ATOMUICLI_SYS002",
        "ATOMUICLI_ARG001",
        "ATOMUICLI_ARG002",
        "ATOMUICLI_ARG003",
        "ATOMUICLI_MOD001",
        "ATOMUICLI_MOD002",
        "ATOMUICLI_DATA001",
        "ATOMUICLI_DATA002",
        "ATOMUICLI_DATA003",
        "ATOMUICLI_DATA004",
        "ATOMUICLI_DATA005",
        "ATOMUICLI_CTRL001",
        "ATOMUICLI_CTRL002",
        "ATOMUICLI_CTRL003",
        "ATOMUICLI_CTRL004",
        "ATOMUICLI_CTRL005",
        "ATOMUICLI_PKG001",
        "ATOMUICLI_PKG002",
        "ATOMUICLI_PKG003",
        "ATOMUICLI_PRJ001",
        "ATOMUICLI_PRJ002",
        "ATOMUICLI_PRJ003",
        "ATOMUICLI_PRJ010",
        "ATOMUICLI_AOT001",
        "ATOMUICLI_MCP001",
        "ATOMUICLI_MCP002",
        "ATOMUICLI_MCP003",
        "ATOMUICLI_SETUP001",
        "ATOMUICLI_SETUP002",
        "ATOMUICLI_SETUP003"
    ];

    [Fact]
    public void DefaultCatalogContainsEveryDocumentedCode()
    {
        var actual = ErrorCodeCatalog.Default.Descriptors
            .Select(descriptor => descriptor.Code)
            .Order()
            .ToArray();

        Assert.Equal(ExpectedCodes.Order().ToArray(), actual);
    }

    [Fact]
    public void DefaultCatalogContainsNoDuplicateCodes()
    {
        var duplicates = ErrorCodeCatalog.Default.Descriptors
            .GroupBy(descriptor => descriptor.Code)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void EveryCodeUsesStableFormat()
    {
        var regex = new Regex("^ATOMUICLI_[A-Z]+[0-9]{3}$", RegexOptions.CultureInvariant);

        foreach (var descriptor in ErrorCodeCatalog.Default.Descriptors)
        {
            Assert.Matches(regex, descriptor.Code);
        }
    }

    [Fact]
    public void PackageNotFoundAndPackageConflictKeepDifferentExitCodes()
    {
        var packageNotFound = ErrorCodeCatalog.Default.GetRequired(AtomUICliErrorCodes.PackageNotFound);
        var packageConflict = ErrorCodeCatalog.Default.GetRequired(AtomUICliErrorCodes.PackageConflict);

        Assert.Equal(3, packageNotFound.DefaultExitCode);
        Assert.Equal(5, packageConflict.DefaultExitCode);
        Assert.Equal(ErrorCodeKind.Error, packageNotFound.Kind);
        Assert.Equal(ErrorCodeKind.ErrorOrDiagnostic, packageConflict.Kind);
    }

    [Fact]
    public void PureDiagnosticCodesUseSeverityAggregation()
    {
        var projectLint = ErrorCodeCatalog.Default.GetRequired(AtomUICliErrorCodes.ProjectLintFinding);

        Assert.Null(projectLint.DefaultExitCode);
        Assert.Equal(ErrorCodeKind.Diagnostic, projectLint.Kind);
    }
}
