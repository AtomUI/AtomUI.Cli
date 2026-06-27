using AtomUI.Cli.Modularity;

namespace AtomUI.Cli.Hosting.Commands;

public static class CliCommandDescriptorContributionExtensions
{
    public static CliCommandDescriptor ToCliCommandDescriptor(this AtomUICliCommandContribution contribution)
    {
        ArgumentNullException.ThrowIfNull(contribution);

        return new CliCommandDescriptor(
            contribution.Name,
            contribution.OptionsType,
            contribution.HandlerType,
            contribution.Group,
            contribution.SupportedFormats,
            contribution.IsReadOnly,
            contribution.RequiresProject,
            contribution.RequiresWriteConfirmation,
            contribution.CreateOptions,
            contribution.ExecuteAsync);
    }
}
