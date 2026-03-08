using System.Threading;
using AzureKvManager.Tui.Models;
using AzureKvManager.Tui.Services;

namespace AzureKvManager.Tui.ViewModels;

public sealed class SecretsViewModel
{
    private readonly IAzureKeyVaultDataService _dataService;
    private List<Secret> _allSecrets = [];
    private List<Secret> _filteredSecrets = [];
    private long _loadGeneration;

    public SecretsViewModel(IAzureKeyVaultDataService dataService)
    {
        _dataService = dataService;
    }

    public IReadOnlyList<Secret> AllSecrets => _allSecrets;

    public IReadOnlyList<Secret> FilteredSecrets => _filteredSecrets;

    public Secret? SelectedSecret { get; private set; }

    public string FilterText { get; private set; } = string.Empty;

    public void ClearForVaultSwitch()
    {
        SelectedSecret = null;
        FilterText = string.Empty;
        _allSecrets = [];
        _filteredSecrets = [];
        Interlocked.Increment(ref _loadGeneration);
    }

    public async Task<OperationResult> LoadForVaultAsync(string vaultName)
    {
        SelectedSecret = null;

        var requestGeneration = Interlocked.Increment(ref _loadGeneration);

        try
        {
            var loadedSecrets = await _dataService.GetSecretsAsync(vaultName);

            if (requestGeneration != Volatile.Read(ref _loadGeneration))
            {
                return OperationResult.Stale();
            }

            _allSecrets = loadedSecrets;
            ApplyFilter(FilterText);

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message);
        }
    }

    public void ApplyFilter(string? filterText)
    {
        FilterText = filterText?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(FilterText))
        {
            _filteredSecrets = [.. _allSecrets];
        }
        else
        {
            var loweredFilter = FilterText.ToLowerInvariant();
            _filteredSecrets = _allSecrets
                .Where(secret =>
                    secret.Name.ToLowerInvariant().Contains(loweredFilter) ||
                    (secret.ContentType ?? string.Empty).ToLowerInvariant().Contains(loweredFilter))
                .ToList();
        }

        if (SelectedSecret is not null &&
            !_filteredSecrets.Any(secret => secret.Name == SelectedSecret.Name))
        {
            SelectedSecret = null;
        }
    }

    public bool TrySelectByIndex(int index, out Secret? selectedSecret)
    {
        selectedSecret = null;

        if (index < 0 || index >= _filteredSecrets.Count)
        {
            return false;
        }

        SelectedSecret = _filteredSecrets[index];
        selectedSecret = SelectedSecret;
        return true;
    }

    public async Task<OperationResult> RefreshAsync(string vaultName)
    {
        return await LoadForVaultAsync(vaultName);
    }

    public async Task<OperationResult> CreateSecretAsync(string vaultName, string name, string value, string? contentType, DateTime? expiresAt)
    {
        try
        {
            var success = await _dataService.SetSecretAsync(vaultName, name, value, contentType, expiresAt);
            return success
                ? OperationResult.Ok()
                : OperationResult.Fail($"Failed to create secret '{name}'");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message);
        }
    }
}
