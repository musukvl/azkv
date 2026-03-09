using AzureKvManager.Tui.Models;
using AzureKvManager.Tui.Services;

namespace AzureKvManager.Tui.ViewModels;

public sealed class KeyVaultsViewModel
{
    private readonly IAzureKeyVaultDataService _dataService;
    private List<KeyVault> _allKeyVaults = [];
    private List<KeyVault> _filteredKeyVaults = [];

    public KeyVaultsViewModel(IAzureKeyVaultDataService dataService)
    {
        _dataService = dataService;
    }

    public event Action? StateChanged;

    public IReadOnlyList<KeyVault> AllKeyVaults => _allKeyVaults;

    public IReadOnlyList<KeyVault> FilteredKeyVaults => _filteredKeyVaults;

    public KeyVault? SelectedKeyVault { get; private set; }

    public string FilterText { get; private set; } = string.Empty;

    public async Task<OperationResult> RefreshAsync()
    {
        try
        {
            _allKeyVaults = await _dataService.GetAllKeyVaultsAsync();
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
            _filteredKeyVaults = [.. _allKeyVaults];
        }
        else
        {
            _filteredKeyVaults = _allKeyVaults
                .Where(kv =>
                    kv.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                    kv.ResourceGroup.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                    kv.Subscription.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (SelectedKeyVault is not null && _filteredKeyVaults.All(kv => kv.Name != SelectedKeyVault.Name))
        {
            SelectedKeyVault = null;
        }

        StateChanged?.Invoke();
    }

    public bool TrySelectByIndex(int index, out KeyVault? selectedKeyVault)
    {
        selectedKeyVault = null;

        if (index < 0 || index >= _filteredKeyVaults.Count)
        {
            return false;
        }

        SelectedKeyVault = _filteredKeyVaults[index];
        selectedKeyVault = SelectedKeyVault;
        return true;
    }

    public void ClearSelection()
    {
        SelectedKeyVault = null;
    }
}
