namespace AtomUI.Cli.Hosting.Commands;

public sealed class CommandOptionsReader
{
    private readonly Dictionary<string, string?> _options = new(StringComparer.OrdinalIgnoreCase);

    public CommandOptionsReader(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var positionals = new List<string>();
        var index = 0;
        while (index < args.Count)
        {
            var token = args[index];
            if (!token.StartsWith("--", StringComparison.Ordinal) || token.Length == 2)
            {
                positionals.Add(token);
                index++;
                continue;
            }

            var name = token[2..];
            if (index + 1 < args.Count && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                _options[name] = args[index + 1];
                index += 2;
            }
            else
            {
                _options[name] = null;
                index++;
            }
        }

        Positionals = Array.AsReadOnly(positionals.ToArray());
    }

    public IReadOnlyList<string> Positionals { get; }

    public string? GetOption(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _options.GetValueOrDefault(name);
    }

    public string? GetOption(string name, string? defaultValue)
    {
        return GetOption(name) ?? defaultValue;
    }

    public bool HasFlag(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _options.ContainsKey(name);
    }

    public bool GetBool(string name, bool defaultValue = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!_options.TryGetValue(name, out var value))
        {
            return defaultValue;
        }

        if (value is null)
        {
            return true;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
               || value.Equals("1", StringComparison.Ordinal)
               || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    public TEnum GetEnum<TEnum>(string name, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        var value = GetOption(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : defaultValue;
    }
}
