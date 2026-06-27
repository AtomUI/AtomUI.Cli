namespace AtomUI.Cli;

public interface IExitCodeMapper
{
    int Map(AtomUICliResult result);
}
