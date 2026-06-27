namespace AtomUI.Cli;

public static class AtomUICliErrorCodes
{
    public const string SystemUnhandled = "ATOMUICLI_SYS001";
    public const string SystemCanceled = "ATOMUICLI_SYS002";

    public const string ArgumentMissingRequired = "ATOMUICLI_ARG001";
    public const string ArgumentInvalidValue = "ATOMUICLI_ARG002";
    public const string ArgumentCommandNotFound = "ATOMUICLI_ARG003";

    public const string ModuleLifecycleFailed = "ATOMUICLI_MOD001";
    public const string ModuleContributionConflict = "ATOMUICLI_MOD002";

    public const string DataUnavailable = "ATOMUICLI_DATA001";
    public const string DataSchemaIncompatible = "ATOMUICLI_DATA002";
    public const string ExternalDataSchemaIncompatible = "ATOMUICLI_DATA003";
    public const string DataVersionIndexMissing = "ATOMUICLI_DATA004";
    public const string DataUnauthorized = "ATOMUICLI_DATA005";

    public const string ControlNotFound = "ATOMUICLI_CTRL001";
    public const string ControlAmbiguous = "ATOMUICLI_CTRL002";
    public const string DemoNotFound = "ATOMUICLI_CTRL003";
    public const string TokenNotFound = "ATOMUICLI_CTRL004";
    public const string SemanticPartNotFound = "ATOMUICLI_CTRL005";

    public const string PackageNotFound = "ATOMUICLI_PKG001";
    public const string PackageAmbiguous = "ATOMUICLI_PKG002";
    public const string PackageConflict = "ATOMUICLI_PKG003";

    public const string ProjectNotFound = "ATOMUICLI_PRJ001";
    public const string ProjectFileReadFailed = "ATOMUICLI_PRJ002";
    public const string ProjectSourceReadFailed = "ATOMUICLI_PRJ003";
    public const string ProjectLintFinding = "ATOMUICLI_PRJ010";

    public const string AotFinding = "ATOMUICLI_AOT001";

    public const string McpRequestInvalid = "ATOMUICLI_MCP001";
    public const string McpToolNotFound = "ATOMUICLI_MCP002";
    public const string McpToolInvocationFailed = "ATOMUICLI_MCP003";

    public const string SetupConfigReadFailed = "ATOMUICLI_SETUP001";
    public const string SetupConflict = "ATOMUICLI_SETUP002";
    public const string SetupWriteFailed = "ATOMUICLI_SETUP003";
}
