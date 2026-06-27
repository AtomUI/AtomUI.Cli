using AtomUI.Cli;

namespace AtomUI.Cli.Hosting.Errors;

public sealed class ErrorCodeCatalog : IErrorCodeCatalog
{
    public static ErrorCodeCatalog Default { get; } = new(CreateDescriptors());

    private readonly Dictionary<string, ErrorCodeDescriptor> _descriptorsByCode;

    public ErrorCodeCatalog(IEnumerable<ErrorCodeDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var descriptorArray = descriptors.ToArray();
        var duplicate = descriptorArray
            .GroupBy(descriptor => descriptor.Code, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new ArgumentException($"Error code '{duplicate.Key}' is registered more than once.", nameof(descriptors));
        }

        Descriptors = Array.AsReadOnly(descriptorArray);
        _descriptorsByCode = descriptorArray.ToDictionary(descriptor => descriptor.Code, StringComparer.Ordinal);
    }

    public IReadOnlyList<ErrorCodeDescriptor> Descriptors { get; }

    public ErrorCodeDescriptor? Find(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return _descriptorsByCode.GetValueOrDefault(code);
    }

    public ErrorCodeDescriptor GetRequired(string code)
    {
        var descriptor = Find(code);
        return descriptor ?? throw new KeyNotFoundException($"Error code '{code}' is not registered.");
    }

    private static ErrorCodeDescriptor[] CreateDescriptors()
    {
        return
        [
            Error(AtomUICliErrorCodes.SystemUnhandled, "SYS", 1, "Unhandled or unclassified runtime error."),
            Error(AtomUICliErrorCodes.SystemCanceled, "SYS", 1, "User or process cancellation."),
            Error(AtomUICliErrorCodes.ArgumentMissingRequired, "ARG", 2, "Required argument or option is missing."),
            Error(AtomUICliErrorCodes.ArgumentInvalidValue, "ARG", 2, "Argument value, range, enum, or option combination is invalid."),
            Error(AtomUICliErrorCodes.ArgumentCommandNotFound, "ARG", 2, "Command is unknown, ambiguous, or not registered."),
            Error(AtomUICliErrorCodes.ModuleLifecycleFailed, "MOD", 1, "Module lifecycle failed."),
            Error(AtomUICliErrorCodes.ModuleContributionConflict, "MOD", 1, "Module contribution catalog conflict."),
            Error(AtomUICliErrorCodes.DataUnavailable, "DATA", 4, "Metadata or data block is unavailable."),
            Error(AtomUICliErrorCodes.DataSchemaIncompatible, "DATA", 4, "Built-in metadata schema is incompatible."),
            Error(AtomUICliErrorCodes.ExternalDataSchemaIncompatible, "DATA", 4, "External data schema is incompatible."),
            Error(AtomUICliErrorCodes.DataVersionIndexMissing, "DATA", 4, "Version index is missing or cannot resolve the target version."),
            Error(AtomUICliErrorCodes.DataUnauthorized, "DATA", 4, "External or commercial data authorization failed."),
            Error(AtomUICliErrorCodes.ControlNotFound, "CTRL", 3, "Control was not found."),
            Error(AtomUICliErrorCodes.ControlAmbiguous, "CTRL", 3, "Control name is ambiguous."),
            Error(AtomUICliErrorCodes.DemoNotFound, "CTRL", 3, "Demo was not found."),
            Error(AtomUICliErrorCodes.TokenNotFound, "CTRL", 3, "Token was not found."),
            Error(AtomUICliErrorCodes.SemanticPartNotFound, "CTRL", 3, "Semantic part was not found."),
            Error(AtomUICliErrorCodes.PackageNotFound, "PKG", 3, "Package or product was not found."),
            Error(AtomUICliErrorCodes.PackageAmbiguous, "PKG", 3, "Package or product id is ambiguous."),
            ErrorOrDiagnostic(AtomUICliErrorCodes.PackageConflict, "PKG", 5, "Package conflict or compatibility failure."),
            Error(AtomUICliErrorCodes.ProjectNotFound, "PRJ", 3, "Project, solution, or path was not found."),
            Error(AtomUICliErrorCodes.ProjectFileReadFailed, "PRJ", 4, "Project file cannot be read or parsed."),
            Error(AtomUICliErrorCodes.ProjectSourceReadFailed, "PRJ", 4, "Source file cannot be read or scanned."),
            Diagnostic(AtomUICliErrorCodes.ProjectLintFinding, "PRJ", "Project or XAML lint finding."),
            Diagnostic(AtomUICliErrorCodes.AotFinding, "AOT", "Native AOT, trim, single-file, or dynamic access finding."),
            Error(AtomUICliErrorCodes.McpRequestInvalid, "MCP", 1, "MCP request is invalid."),
            Error(AtomUICliErrorCodes.McpToolNotFound, "MCP", 1, "MCP tool was not found."),
            Error(AtomUICliErrorCodes.McpToolInvocationFailed, "MCP", 1, "MCP tool invocation failed."),
            Error(AtomUICliErrorCodes.SetupConfigReadFailed, "SETUP", 6, "Configuration read or parse failed."),
            Error(AtomUICliErrorCodes.SetupConflict, "SETUP", 6, "Write plan has a blocking conflict."),
            Error(AtomUICliErrorCodes.SetupWriteFailed, "SETUP", 6, "Write, update, rollback, or verification failed.")
        ];
    }

    private static ErrorCodeDescriptor Error(string code, string domain, int exitCode, string description)
    {
        return new ErrorCodeDescriptor(code, domain, ErrorCodeKind.Error, exitCode, description);
    }

    private static ErrorCodeDescriptor ErrorOrDiagnostic(string code, string domain, int exitCode, string description)
    {
        return new ErrorCodeDescriptor(code, domain, ErrorCodeKind.ErrorOrDiagnostic, exitCode, description);
    }

    private static ErrorCodeDescriptor Diagnostic(string code, string domain, string description)
    {
        return new ErrorCodeDescriptor(code, domain, ErrorCodeKind.Diagnostic, null, description);
    }
}
