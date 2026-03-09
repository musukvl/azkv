using System.Threading;
using AzureKvManager.Tui.Models;
using AzureKvManager.Tui.Services;

namespace AzureKvManager.Tui.ViewModels;

public sealed class VersionsViewModel
{
    private readonly IAzureKeyVaultDataService _dataService;
    private List<SecretVersion> _versions = [];
    private long _loadGeneration;

    public VersionsViewModel(IAzureKeyVaultDataService dataService)
    {
        _dataService = dataService;
    }

    public event Action? StateChanged;

    public IReadOnlyList<SecretVersion> Versions => _versions;

    public SecretVersion? SelectedVersion { get; private set; }

    public void ClearForSecretSwitch()
    {
        SelectedVersion = null;
        _versions = [];
        Interlocked.Increment(ref _loadGeneration);
        StateChanged?.Invoke();
    }

    public async Task<OperationResult> LoadForSecretAsync(string vaultName, string secretName)
    {
        SelectedVersion = null;

        var requestGeneration = Interlocked.Increment(ref _loadGeneration);

        try
        {
            var loadedVersions = await _dataService.GetSecretVersionsAsync(vaultName, secretName);

            if (requestGeneration != Volatile.Read(ref _loadGeneration))
            {
                return OperationResult.Stale();
            }

            _versions = loadedVersions
                .OrderByDescending(GetVersionSortKey)
                .ThenByDescending(version => version.Version, StringComparer.Ordinal)
                .ToList();

            StateChanged?.Invoke();

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

    public async Task<OperationResult> RefreshAsync(string vaultName, string secretName)
    {
        return await LoadForSecretAsync(vaultName, secretName);
    }

    public async Task<OperationResult> CreateVersionAsync(string vaultName, string secretName, string value, string? contentType, DateTime? expiresAt)
    {
        try
        {
            var success = await _dataService.SetSecretAsync(vaultName, secretName, value, contentType, expiresAt);
            return success
                ? OperationResult.Ok()
                : OperationResult.Fail($"Failed to create new version for '{secretName}'");
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
