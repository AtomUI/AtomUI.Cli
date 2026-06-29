namespace AtomUI.Cli.Hosting.Setup;

public sealed class SetupPlanService
{
    public WritePlan CreateSetupPlan(SetupCommandOptions options)
    {
        return new WritePlan(
            "1.0",
            "setup",
            options.Write,
            [
                new WriteOperation(
                    "setup-mcp-config",
                    "JsonConfigOperation",
                    options.Scope.Equals("workspace", StringComparison.OrdinalIgnoreCase)
                        ? Path.Combine(options.Workspace, ".atomui", "mcp.json")
                        : "~/.atomui/mcp.json",
                    CreatesFile: true,
                    ModifiesFile: true,
                    Preview: $"Register dotnet-atomui mcp for {options.Target}.",
                    RollbackHint: "Remove the atomui MCP server entry.")
            ],
            [],
            ["dotnet atomui mcp"]);
    }

    public WritePlan CreateInitPlan(InitCommandOptions options)
    {
        return new WritePlan(
            "1.0",
            "init",
            options.Write,
            [
                new WriteOperation(
                    "init-project",
                    "TextFileOperation",
                    Path.Combine(options.Path, ".atomui", "project.json"),
                    CreatesFile: true,
                    ModifiesFile: false,
                    Preview: "Create AtomUI project metadata file.",
                    RollbackHint: "Delete .atomui/project.json.")
            ],
            [],
            ["dotnet atomui doctor"]);
    }

    public WritePlan CreateAddPlan(AddCommandOptions options, global::AtomUI.Cli.Hosting.Metadata.MetadataQueryService metadata)
    {
        var package = metadata.FindPackageOrProduct(options.PackageOrProduct ?? string.Empty);
        var packageId = package?.Id ?? options.PackageOrProduct ?? "AtomUI.Controls";
        var version = options.Version ?? package?.Version ?? metadata.Catalog.TargetVersion;

        return new WritePlan(
            "1.0",
            "add",
            options.Write,
            [
                new WriteOperation(
                    "add-package",
                    "PackageReferenceOperation",
                    options.Project ?? options.Path,
                    CreatesFile: false,
                    ModifiesFile: true,
                    Preview: $"Add PackageReference Include=\"{packageId}\" Version=\"{version}\".",
                    RollbackHint: $"Remove PackageReference '{packageId}'.")
            ],
            package is null ? [new WriteConflict(AtomUICliErrorCodes.PackageNotFound, options.Path, $"Package or product '{options.PackageOrProduct}' is unknown.", "Run dotnet atomui package.", Blocking: false)] : [],
            ["dotnet restore", "dotnet atomui doctor"]);
    }

    public WritePlan CreateUpgradePlan(UpgradeCommandOptions options, global::AtomUI.Cli.Hosting.Metadata.MetadataQueryService metadata)
    {
        return new WritePlan(
            "1.0",
            "upgrade",
            options.Write,
            options.Packages
                ? [
                    new WriteOperation(
                        "upgrade-packages",
                        "PackageReferenceOperation",
                        options.Path,
                        CreatesFile: false,
                        ModifiesFile: true,
                        Preview: $"Update AtomUI packages to {options.To ?? metadata.Catalog.TargetVersion}.",
                        RollbackHint: "Restore package versions from source control.")
                ]
                : [],
            [],
            ["dotnet atomui changelog", "dotnet atomui doctor"]);
    }
}
