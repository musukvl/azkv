using Terminal.Gui;
using AzureKvManager.Tui.Models;
using System.Collections.ObjectModel;

namespace AzureKvManager.Tui.Views;

public partial class MainWindow
{
    private async void RefreshKeyVaults()
    {
        Application.Invoke(() =>
        {
            _statusLabel.Text = "Loading Key Vaults...";
            _keyVaultsList.SetSource(new ObservableCollection<string> { "Loading..." });
        });
        
        try
        {
            _keyVaults = await _azureService.GetAllKeyVaultsAsync();
            
            Application.Invoke(() =>
            {
                if (_keyVaults.Any())
                {
                    // Apply filter (will handle initial filter from command line if present)
                    FilterKeyVaults();
                }
                else
                {
                    _keyVaultsList.SetSource(new ObservableCollection<string> { "No Key Vaults found" });
                    _statusLabel.Text = "No Key Vaults found";
                }
            });
        }
        catch (Exception ex)
        {
            Application.Invoke(() =>
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                MessageBox.ErrorQuery("Error", $"Failed to load Key Vaults: {ex.Message}", "OK");
            });
        }
    }

    private void FilterKeyVaults()
    {
        var filterText = _keyVaultFilter.Text?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(filterText))
        {
            _filteredKeyVaults = new List<KeyVault>(_keyVaults);
        }
        else
        {
            _filteredKeyVaults = _keyVaults
                .Where(kv => kv.Name.ToLowerInvariant().Contains(filterText) || 
                            kv.ResourceGroup.ToLowerInvariant().Contains(filterText))
                .ToList();
        }
        
        _keyVaultsList.SetSource(new ObservableCollection<string>(
            _filteredKeyVaults.Select(kv => $"{kv.Name} ({kv.ResourceGroup})")
        ));
        
        _statusLabel.Text = _filteredKeyVaults.Any() 
            ? $"Showing {_filteredKeyVaults.Count} of {_keyVaults.Count} Key Vault(s)"
            : $"No matches found (total: {_keyVaults.Count})";
    }

    private async void OnKeyVaultSelected(object? sender, ListViewItemEventArgs args)
    {
        if (args.Item < 0 || args.Item >= _filteredKeyVaults.Count)
            return;
        
        _selectedKeyVault = _filteredKeyVaults[args.Item];
        _selectedSecret = null;
        _secrets.Clear();
        _filteredSecrets.Clear();
        _versions.Clear();
        
        // Clear secret filter when switching key vaults
        _secretFilter.Text = string.Empty;
        
        Application.Invoke(() =>
        {
            _statusLabel.Text = $"Loading secrets from {_selectedKeyVault.Name}...";
            _secretsList.SetSource(new ObservableCollection<string> { "Loading..." });
            _versionsList.SetSource(new ObservableCollection<string>());
            _valueView.Text = string.Empty;
        });
        
        try
        {
            _secrets = await _azureService.GetSecretsAsync(_selectedKeyVault.Name);
            _filteredSecrets = new List<Secret>(_secrets);
            
            Application.Invoke(() =>
            {
                if (_secrets.Any())
                {
                    _secretsList.SetSource(new ObservableCollection<string>(_filteredSecrets.Select(s => 
                        $"{s.Name} {(s.Enabled ? "✓" : "✗")}"
                    )));
                    _statusLabel.Text = $"Loaded {_secrets.Count} secret(s) from {_selectedKeyVault.Name}";
                }
                else
                {
                    _secretsList.SetSource(new ObservableCollection<string> { "No secrets found" });
                    _statusLabel.Text = $"No secrets in {_selectedKeyVault.Name}";
                }
            });
        }
        catch (Exception ex)
        {
            Application.Invoke(() =>
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                MessageBox.ErrorQuery("Error", $"Failed to load secrets: {ex.Message}", "OK");
            });
        }
    }
}
