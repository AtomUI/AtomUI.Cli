namespace AtomUI.Cli;

public interface IAtomUICliJsonPayload
{
    IReadOnlyDictionary<string, object?> ToJsonPayload();
}
