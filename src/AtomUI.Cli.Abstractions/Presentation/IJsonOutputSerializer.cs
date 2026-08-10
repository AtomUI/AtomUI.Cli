namespace AtomUI.Cli;

public interface IJsonOutputSerializer
{
    string SerializeError(string commandName, AtomUICliError error);

    string SerializeResult(string commandName, AtomUICliResult result);
}
