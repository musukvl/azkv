namespace AzureKvManager.Tui.ViewModels;

public readonly record struct OperationResult(bool Success, bool IsStale, string? ErrorMessage)
{
    public static OperationResult Ok() => new(true, false, null);

    public static OperationResult Stale() => new(false, true, null);

    public static OperationResult Fail(string message) => new(false, false, message);
}
