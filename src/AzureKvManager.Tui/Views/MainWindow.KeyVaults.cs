using System.Collections.ObjectModel;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace AzureKvManager.Tui.Views;

public partial class MainWindow
{
    private async void RefreshKeyVaults()
    {
        _app.Invoke(() =>
        {
            _statusLabel.Text = "Loading Key Vaults...";
            _keyVaultsList.SetSource(new ObservableCollection<string> { "Loading..." });
        });

        var result = await _viewModel.KeyVaults.RefreshAsync();

        _app.Invoke(() =>
        {
            if (!result.Success)
            {
                if (result.IsStale)
                {
                    return;
                }

                var errorMessage = result.ErrorMessage ?? "Unknown error";
                _statusLabel.Text = $"Error: {errorMessage}";
                MessageBox.ErrorQuery(_app, "Error", $"Failed to load Key Vaults: {errorMessage}", "OK");
                return;
            }

            _viewModel.KeyVaults.ApplyFilter(_keyVaultFilter.Text?.ToString());
            RenderKeyVaultList();

            if (_viewModel.KeyVaults.AllKeyVaults.Count == 0)
            {
                _keyVaultsList.SetSource(new ObservableCollection<string> { "No Key Vaults found" });
                _statusLabel.Text = "No Key Vaults found";
            }
        });
    }

    private void FilterKeyVaults()
    {
        _viewModel.KeyVaults.ApplyFilter(_keyVaultFilter.Text?.ToString());
        RenderKeyVaultList();
    }

    private void RenderKeyVaultList()
    {
        var filteredKeyVaults = _viewModel.KeyVaults.FilteredKeyVaults;
        var totalKeyVaults = _viewModel.KeyVaults.AllKeyVaults.Count;

        _keyVaultsList.SetSource(new ObservableCollection<string>(
            filteredKeyVaults.Select(kv => $"{kv.Name} ({kv.ResourceGroup})")
        ));

        if (totalKeyVaults == 0)
        {
            _statusLabel.Text = "No Key Vaults found";
            return;
        }

        _statusLabel.Text = filteredKeyVaults.Count > 0
            ? $"Showing {filteredKeyVaults.Count} of {totalKeyVaults} Key Vault(s)"
            : $"No matches found (total: {totalKeyVaults})";
    }

    private async void OnKeyVaultSelectionChanged(object? sender, ValueChangedEventArgs<int?> args)
    {
        if (!args.NewValue.HasValue)
        {
            return;
        }

        if (!_viewModel.KeyVaults.TrySelectByIndex(args.NewValue.Value, out var selectedKeyVault) ||
            selectedKeyVault is null)
        {
            return;
        }

        var selectedVaultName = selectedKeyVault.Name;
        _viewModel.ClearFromVaultSelection(selectedVaultName);

        _suppressFilterEvents = true;
        _secretFilter.Text = string.Empty;
        _suppressFilterEvents = false;

        _app.Invoke(() =>
        {
            _statusLabel.Text = $"Loading secrets from {selectedVaultName}...";
            SetSecretsTableSource([]);
            SetVersionsTableSource([]);
            ApplySecretDetailsToView();
        });

        var loadResult = await _viewModel.Secrets.LoadForVaultAsync(selectedVaultName);

        _app.Invoke(() =>
        {
            if (_viewModel.KeyVaults.SelectedKeyVault?.Name != selectedVaultName)
            {
                return;
            }

            if (loadResult.IsStale)
            {
                return;
            }

            if (!loadResult.Success)
            {
                var errorMessage = loadResult.ErrorMessage ?? "Unknown error";
                _statusLabel.Text = $"Error: {errorMessage}";
                MessageBox.ErrorQuery(_app, "Error", $"Failed to load secrets: {errorMessage}", "OK");
                return;
            }

            FilterSecrets();

            if (_viewModel.Secrets.AllSecrets.Count == 0)
            {
                SetSecretsTableSource([]);
                _statusLabel.Text = $"No secrets in {selectedVaultName}";
                return;
            }

            _statusLabel.Text = $"Loaded {_viewModel.Secrets.AllSecrets.Count} secret(s) from {selectedVaultName}";
        });
    }
}
