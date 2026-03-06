using System.Threading;
using AzureKvManager.Tui.Models;
using AzureKvManager.Tui.Services;

namespace AzureKvManager.Tui.ViewModels;

public sealed class VersionsViewModel
{
    private readonly IAzureKeyVaultDataService _dataService;
    private List<SecretVersion> _versions = [];
    private long _loadVersion;

    public VersionsViewModel(IAzureKeyVaultDataService dataService)
    {
        _dataService = dataService;
    }

    public IReadOnlyList<SecretVersion> Versions => _versions;

    public SecretVersion? SelectedVersion { get; private set; }

    public string? CurrentVaultName { get; private set; }

    public string? CurrentSecretName { get; private set; }

    public void ClearForSecretSwitch(string? vaultName = null, string? secretName = null)
    {
        CurrentVaultName = vaultName;
        CurrentSecretName = secretName;
        SelectedVersion = null;
        _versions = [];
        Interlocked.Increment(ref _loadVersion);
    }

    public async Task<OperationResult> LoadForSecretAsync(string vaultName, string secretName)
    {
        CurrentVaultName = vaultName;
        CurrentSecretName = secretName;
        SelectedVersion = null;

        var requestVersion = Interlocked.Increment(ref _loadVersion);

        try
        {
            var loadedVersions = await _dataService.GetSecretVersionsAsync(vaultName, secretName);

            if (requestVersion != Volatile.Read(ref _loadVersion) ||
                CurrentVaultName != vaultName ||
                CurrentSecretName != secretName)
            {
                return OperationResult.Stale();
            }

            _versions = loadedVersions
                .OrderByDescending(GetVersionSortKey)
                .ThenByDescending(version => version.Version, StringComparer.Ordinal)
                .ToList();

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message);
        }
    }

    public bool TrySelectByIndex(int index, out SecretVersion? version)
    {
        version = null;

        if (index < 0 || index >= _versions.Count)
        {
            return false;
        }

        SelectedVersion = _versions[index];
        version = SelectedVersion;
        return true;
    }

    public async Task<OperationResult> RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentVaultName) || string.IsNullOrWhiteSpace(CurrentSecretName))
        {
            return OperationResult.Ok();
        }

        return await LoadForSecretAsync(CurrentVaultName, CurrentSecretName);
    }

    public async Task<OperationResult> CreateVersionAsync(string value, string? contentType, DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(CurrentVaultName) || string.IsNullOrWhiteSpace(CurrentSecretName))
        {
            return OperationResult.Fail("Please select a secret first");
        }

        try
        {
            var success = await _dataService.SetSecretAsync(CurrentVaultName, CurrentSecretName, value, contentType, expiresAt);
            return success
                ? OperationResult.Ok()
                : OperationResult.Fail($"Failed to create new version for '{CurrentSecretName}'");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message);
        }
    }

    private static DateTime GetVersionSortKey(SecretVersion version)
    {
        return version.Created ?? version.Updated ?? DateTime.MinValue;
    }
}
