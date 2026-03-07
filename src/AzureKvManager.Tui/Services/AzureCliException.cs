namespace AzureKvManager.Tui.Services;

/// <summary>
/// Thrown when an Azure CLI command fails (non-zero exit code) or cannot be executed.
/// The <see cref="Exception.Message"/> contains the raw stderr output from the CLI,
/// which includes Azure error codes such as ForbiddenByRbac, Unauthorized, etc.
/// </summary>
public sealed class AzureCliException : Exception
{
    public AzureCliException(string message)
        : base(message)
    {
    }

    public AzureCliException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
