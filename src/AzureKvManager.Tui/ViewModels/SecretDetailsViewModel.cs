using System.Threading;
using AzureKvManager.Tui.Models;
using AzureKvManager.Tui.Services;

namespace AzureKvManager.Tui.ViewModels;

public sealed class SecretDetailsViewModel
{
    private readonly IAzureKeyVaultDataService _dataService;
    private long _loadVersion;

    public SecretDetailsViewModel(IAzureKeyVaultDataService dataService)
    {
        _dataService = dataService;
        Clear();
    }

    public string ValueText { get; private set; } = string.Empty;

    public string ContentTypeText { get; private set; } = string.Empty;

    public string ExpirationText { get; private set; } = "Expiration: (select a version)";

    public string? CopyableValue { get; private set; }

    public bool CanCopy => !string.IsNullOrWhiteSpace(CopyableValue);

    public string? SelectedVersionId { get; private set; }

    public void Clear(bool clearValue = true)
    {
        Interlocked.Increment(ref _loadVersion);
        SelectedVersionId = null;
        ContentTypeText = string.Empty;
        ExpirationText = "Expiration: (select a version)";
        CopyableValue = null;

        if (clearValue)
        {
            ValueText = string.Empty;
        }
    }

    public async Task<OperationResult> LoadForVersionAsync(string vaultName, string secretName, SecretVersion selectedVersion)
    {
        SelectedVersionId = selectedVersion.Version;
        ContentTypeText = string.IsNullOrWhiteSpace(selectedVersion.ContentType) ? "(none)" : selectedVersion.ContentType;
        ExpirationText = SecretDateService.BuildExpirationDetailsText(selectedVersion.Expires);
        ValueText = "Loading...";
        CopyableValue = null;

        var requestVersion = Interlocked.Increment(ref _loadVersion);

        try
        {
            var details = await _dataService.GetSecretVersionDetailsAsync(vaultName, secretName, selectedVersion.Version);

            if (requestVersion != Volatile.Read(ref _loadVersion) || SelectedVersionId != selectedVersion.Version)
            {
                return OperationResult.Stale();
            }

            var resolvedContentType = details?.ContentType ?? selectedVersion.ContentType;
            var resolvedExpiration = details?.Expires ?? selectedVersion.Expires;
            var resolvedValue = details?.Value;

            ContentTypeText = string.IsNullOrWhiteSpace(resolvedContentType) ? "(none)" : resolvedContentType;
            ExpirationText = SecretDateService.BuildExpirationDetailsText(resolvedExpiration);
            ValueText = resolvedValue ?? "(empty)";
            CopyableValue = string.IsNullOrWhiteSpace(resolvedValue) ? null : resolvedValue;

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            if (requestVersion != Volatile.Read(ref _loadVersion) || SelectedVersionId != selectedVersion.Version)
            {
                return OperationResult.Stale();
            }

            ValueText = $"Error: {ex.Message}";
            CopyableValue = null;
            return OperationResult.Fail(ex.Message);
        }
    }
}
