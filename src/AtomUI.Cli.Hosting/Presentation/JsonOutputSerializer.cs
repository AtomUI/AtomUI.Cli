using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AtomUI.Cli;

namespace AtomUI.Cli.Hosting.Presentation;

public sealed class JsonOutputSerializer : IJsonOutputSerializer
{
    private const string SchemaVersion = "1.0";

    public string SerializeError(string commandName, AtomUICliError error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        ArgumentNullException.ThrowIfNull(error);

        var envelope = new AtomUICliErrorEnvelope(
            SchemaVersion: SchemaVersion,
            Success: false,
            Command: commandName,
            Error: AtomUICliErrorJson.FromError(error));

        return JsonSerializer.Serialize(envelope, AtomUICliJsonContext.Default.AtomUICliErrorEnvelope);
    }

    public string SerializeResult(string commandName, AtomUICliResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsSuccess && result.Error is not null)
        {
            return SerializeError(commandName, result.Error);
        }

        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        writer.WriteString("schemaVersion", SchemaVersion);
        writer.WriteBoolean("success", result.IsSuccess);
        writer.WriteString("command", commandName);
        writer.WritePropertyName("payload");
        WritePayload(writer, result.Payload);
        writer.WritePropertyName("diagnostics");
        WriteDiagnostics(writer, result.Diagnostics);
        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteDiagnostics(Utf8JsonWriter writer, IReadOnlyList<AtomUICliDiagnostic> diagnostics)
    {
        writer.WriteStartArray();
        foreach (var diagnostic in diagnostics)
        {
            writer.WriteStartObject();
            writer.WriteString("code", diagnostic.Code);
            writer.WriteString("severity", diagnostic.Severity.ToString().ToLowerInvariant());
            writer.WriteString("category", diagnostic.Category);
            writer.WriteString("message", diagnostic.Message);
            WriteNullableString(writer, "file", diagnostic.File);
            if (diagnostic.Line is null)
            {
                writer.WriteNull("line");
            }
            else
            {
                writer.WriteNumber("line", diagnostic.Line.Value);
            }

            WriteNullableString(writer, "suggestion", diagnostic.Suggestion);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WritePayload(Utf8JsonWriter writer, object? payload)
    {
        switch (payload)
        {
            case null:
                writer.WriteNullValue();
                break;
            case JsonElement element:
                element.WriteTo(writer);
                break;
            case string value:
                writer.WriteStringValue(value);
                break;
            case bool value:
                writer.WriteBooleanValue(value);
                break;
            case int value:
                writer.WriteNumberValue(value);
                break;
            case long value:
                writer.WriteNumberValue(value);
                break;
            case double value:
                writer.WriteNumberValue(value);
                break;
            case decimal value:
                writer.WriteNumberValue(value);
                break;
            case Enum value:
                writer.WriteStringValue(value.ToString());
                break;
            case IAtomUICliJsonPayload value:
                WriteObjectDictionary(writer, value.ToJsonPayload());
                break;
            case IReadOnlyDictionary<string, string> dictionary:
                WriteStringDictionary(writer, dictionary);
                break;
            case IReadOnlyDictionary<string, object?> dictionary:
                WriteObjectDictionary(writer, dictionary);
                break;
            case IEnumerable<IReadOnlyDictionary<string, object?>> dictionaries:
                WriteObjectDictionaryArray(writer, dictionaries);
                break;
            case IEnumerable<string> values:
                WriteStringArray(writer, values);
                break;
            case IEnumerable<object?> values:
                WriteObjectArray(writer, values);
                break;
            default:
                writer.WriteStringValue(payload.ToString());
                break;
        }
    }

    private static void WriteStringDictionary(Utf8JsonWriter writer, IReadOnlyDictionary<string, string> dictionary)
    {
        writer.WriteStartObject();
        foreach (var pair in dictionary.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            writer.WriteString(pair.Key, pair.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteObjectDictionary(Utf8JsonWriter writer, IReadOnlyDictionary<string, object?> dictionary)
    {
        writer.WriteStartObject();
        foreach (var pair in dictionary.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            writer.WritePropertyName(pair.Key);
            WritePayload(writer, pair.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteStringArray(Utf8JsonWriter writer, IEnumerable<string> values)
    {
        writer.WriteStartArray();
        foreach (var value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    private static void WriteObjectDictionaryArray(
        Utf8JsonWriter writer,
        IEnumerable<IReadOnlyDictionary<string, object?>> dictionaries)
    {
        writer.WriteStartArray();
        foreach (var dictionary in dictionaries)
        {
            WriteObjectDictionary(writer, dictionary);
        }

        writer.WriteEndArray();
    }

    private static void WriteObjectArray(Utf8JsonWriter writer, IEnumerable<object?> values)
    {
        writer.WriteStartArray();
        foreach (var value in values)
        {
            WritePayload(writer, value);
        }

        writer.WriteEndArray();
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string propertyName, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
        }
        else
        {
            writer.WriteString(propertyName, value);
        }
    }

}

internal sealed record AtomUICliErrorEnvelope(
    string SchemaVersion,
    bool Success,
    string Command,
    AtomUICliErrorJson Error);

internal sealed record AtomUICliErrorJson(
    string Code,
    string Severity,
    string Message,
    string? Suggestion,
    string Stage,
    CliLocation? Location,
    IReadOnlyDictionary<string, string>? Details)
{
    public static AtomUICliErrorJson FromError(AtomUICliError error)
    {
        return new AtomUICliErrorJson(
            error.Code,
            error.Severity.ToString().ToLowerInvariant(),
            error.Message,
            error.Suggestion,
            error.Stage,
            error.Location,
            error.Details);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AtomUICliErrorEnvelope))]
[JsonSerializable(typeof(AtomUICliErrorJson))]
[JsonSerializable(typeof(CliLocation))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class AtomUICliJsonContext : JsonSerializerContext;
