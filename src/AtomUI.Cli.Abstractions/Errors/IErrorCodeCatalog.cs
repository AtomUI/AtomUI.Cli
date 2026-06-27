namespace AtomUI.Cli;

public interface IErrorCodeCatalog
{
    IReadOnlyList<ErrorCodeDescriptor> Descriptors { get; }

    ErrorCodeDescriptor? Find(string code);

    ErrorCodeDescriptor GetRequired(string code);
}
