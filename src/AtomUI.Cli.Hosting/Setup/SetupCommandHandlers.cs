namespace AtomUI.Cli.Hosting.Setup;

public sealed class SetupCommandHandler(SetupPlanService plans) : IAtomUICliCommandHandler<SetupCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(SetupCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(SetupCommandHandlerSupport.Render(plans.CreateSetupPlan(options)));
    }
}

public sealed class InitCommandHandler(SetupPlanService plans) : IAtomUICliCommandHandler<InitCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(InitCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(SetupCommandHandlerSupport.Render(plans.CreateInitPlan(options)));
    }
}

public sealed class AddCommandHandler(SetupPlanService plans, global::AtomUI.Cli.Hosting.Metadata.MetadataQueryService metadata) : IAtomUICliCommandHandler<AddCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(AddCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.PackageOrProduct))
        {
            return ValueTask.FromResult(AtomUICliResult.Failure(new AtomUICliError(
                AtomUICliErrorCodes.ArgumentMissingRequired,
                AtomUICliSeverity.Error,
                "Package or product is required.",
                null,
                "parse",
                null,
                null)));
        }

        return ValueTask.FromResult(SetupCommandHandlerSupport.Render(plans.CreateAddPlan(options, metadata)));
    }
}

public sealed class UpgradeCommandHandler(SetupPlanService plans, global::AtomUI.Cli.Hosting.Metadata.MetadataQueryService metadata) : IAtomUICliCommandHandler<UpgradeCommandOptions>
{
    public ValueTask<AtomUICliResult> ExecuteAsync(UpgradeCommandOptions options, CliInvocationContext context, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(SetupCommandHandlerSupport.Render(plans.CreateUpgradePlan(options, metadata)));
    }
}

internal static class SetupCommandHandlerSupport
{
    public static AtomUICliResult Render(WritePlan plan)
    {
        var blocking = plan.Conflicts.FirstOrDefault(conflict => conflict.Blocking);
        if (blocking is not null)
        {
            return AtomUICliResult.Failure(new AtomUICliError(
                AtomUICliErrorCodes.SetupConflict,
                AtomUICliSeverity.Error,
                blocking.Message,
                blocking.SuggestedResolution,
                "plan",
                null,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["targetPath"] = blocking.TargetPath,
                    ["conflictCode"] = blocking.Code
                }));
        }

        return AtomUICliResult.Success(plan.RenderText());
    }
}
