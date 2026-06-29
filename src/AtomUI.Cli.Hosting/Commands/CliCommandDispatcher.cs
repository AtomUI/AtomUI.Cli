using AtomUI.Cli;
using Microsoft.Extensions.DependencyInjection;

namespace AtomUI.Cli.Hosting.Commands;

public sealed class CliCommandDispatcher(
    CliCommandParser parser,
    IServiceScopeFactory serviceScopeFactory,
    IOutputWriter? outputWriter = null,
    IErrorWriter? errorWriter = null,
    IJsonOutputSerializer? jsonOutputSerializer = null)
{
    public async ValueTask<AtomUICliResult> DispatchAsync(
        IReadOnlyList<string> args,
        CliCommandDescriptorCatalog catalog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(catalog);

        var parseResult = parser.Parse(args, catalog);
        if (!parseResult.IsSuccess)
        {
            var failure = AtomUICliResult.Failure(parseResult.Error!);
            await WriteResultAsync(GetFallbackCommandName(args), failure, parseResult.GlobalOptions.Format, cancellationToken);
            return failure;
        }

        var descriptor = parseResult.Descriptor!;
        var options = descriptor.CreateOptions(parseResult.GlobalOptions, parseResult.CommandArguments);
        var context = new CliInvocationContext(
            descriptor.Name,
            Array.AsReadOnly(args.ToArray()),
            parseResult.GlobalOptions,
            Guid.NewGuid().ToString("N"));

        using var scope = serviceScopeFactory.CreateScope();
        var result = await descriptor.ExecuteAsync(scope.ServiceProvider, options, context, cancellationToken);
        await WriteResultAsync(descriptor.Name, result, parseResult.GlobalOptions.Format, cancellationToken);
        return result;
    }

    private async ValueTask WriteResultAsync(
        string commandName,
        AtomUICliResult result,
        OutputFormat format,
        CancellationToken cancellationToken)
    {
        if (result.Error is not null && errorWriter is not null)
        {
            await errorWriter.WriteErrorAsync(commandName, result.Error, format, cancellationToken);
            return;
        }

        if (result.IsSuccess && outputWriter is not null)
        {
            var text = format == OutputFormat.Json
                ? (jsonOutputSerializer ?? new AtomUI.Cli.Hosting.Output.JsonOutputSerializer()).SerializeResult(commandName, result)
                : RenderTextPayload(result.Payload);

            if (text is null)
            {
                return;
            }

            await outputWriter.WriteLineAsync(text, cancellationToken);
        }
    }

    private static string? RenderTextPayload(object? payload)
    {
        return payload switch
        {
            null => null,
            string text => text,
            _ => payload.ToString()
        };
    }

    private static string GetFallbackCommandName(IReadOnlyList<string> args)
    {
        return args.FirstOrDefault(item => !item.StartsWith("--", StringComparison.Ordinal)) ?? "atomui";
    }
}
