using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Models;
using AzureKvManager.Tui.Services;

namespace AzureKvManager.Tui.Views;

public partial class MainWindow
{
    private async void OnVersionSelectionChanged(object? sender, SelectedCellChangedEventArgs args)
    {
        if (_suppressVersionSelectionEvent)
        {
            return;
        }

        var selectedKeyVault = _viewModel.KeyVaults.SelectedKeyVault;
        var selectedSecret = _viewModel.Secrets.SelectedSecret;

        if (selectedKeyVault == null || selectedSecret == null)
        {
            _viewModel.SecretDetails.Clear();
            _app.Invoke(_ => ApplySecretDetailsToView());
            return;
        }

        if (!_viewModel.Versions.TrySelectByIndex(args.NewRow, out var selectedVersion) || selectedVersion == null)
        {
            _viewModel.SecretDetails.Clear();
            _app.Invoke(_ => ApplySecretDetailsToView());
            return;
        }

        await LoadVersionDetailsAsync(selectedVersion);
    }

    private async Task LoadVersionDetailsAsync(SecretVersion version)
    {
        var selectedKeyVault = _viewModel.KeyVaults.SelectedKeyVault;
        var selectedSecret = _viewModel.Secrets.SelectedSecret;

        if (selectedKeyVault == null || selectedSecret == null)
        {
            return;
        }

        var selectedVaultName = selectedKeyVault.Name;
        var selectedSecretName = selectedSecret.Name;

        var loadTask = _viewModel.SecretDetails.LoadForVersionAsync(selectedVaultName, selectedSecretName, version);

        _app.Invoke(_ =>
        {
            _statusLabel.Text = $"Loading secret value...";
            ApplySecretDetailsToView();
        });

        var loadResult = await loadTask;
        
        _app.Invoke(_ =>
        {
            if (_viewModel.KeyVaults.SelectedKeyVault?.Name != selectedVaultName ||
                _viewModel.Secrets.SelectedSecret?.Name != selectedSecretName)
            {
                return;
            }

            if (loadResult.IsStale)
            {
                return;
            }

            ApplySecretDetailsToView();

            if (!loadResult.Success)
            {
                var errorMessage = loadResult.ErrorMessage ?? "Unknown error";
                _statusLabel.Text = $"Error: {errorMessage}";
                MessageBox.ErrorQuery(_app, "Error", $"Failed to load secret value: {errorMessage}", "OK");
                return;
            }

            _statusLabel.Text = $"Loaded value for {selectedSecretName} (version {ShortVersion(version.Version)}...)";
        });
    }

    private void ShowAddVersionDialog()
    {
        if (_viewModel.KeyVaults.SelectedKeyVault == null)
        {
            MessageBox.ErrorQuery(_app, "Error", "Please select a Key Vault first", "OK");
            return;
        }

        if (_viewModel.Secrets.SelectedSecret == null)
        {
            MessageBox.ErrorQuery(_app, "Error", "Please select a secret first", "OK");
            return;
        }

        var selectedSecretName = _viewModel.Secrets.SelectedSecret.Name;

        var dialog = new Dialog
        {
            Title = $"Add New Version to '{selectedSecretName}'",
            Width = Dim.Percent(60),
            Height = 12
        };

        var valueLabel = new Label
        {
            Text = "Secret Value:",
            X = 1,
            Y = 1
        };

        var valueField = new TextView
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(1),
            Height = 3
        };

        var contentTypeLabel = new Label
        {
            Text = "Content Type (optional):",
            X = 1,
            Y = 5
        };

        var contentTypeField = new TextField
        {
            X = 1,
            Y = 6,
            Width = Dim.Fill(1)
        };

        var expirationDateLabel = new Label
        {
            Text = "Expiration Date (yyyy-MM-dd, optional):",
            X = 1,
            Y = 7
        };

        var expirationDateField = new TextField
        {
            X = 1,
            Y = 8,
            Width = Dim.Fill(1)
        };

        var okButton = new Button
        {
            Text = "OK",
            IsDefault = true,
            X = Pos.Center() - 10,
            Y = Pos.Bottom(dialog) - 2
        };

        okButton.Accepting += async (s, e) =>
        {
            var value = valueField.Text?.ToString()?.Trim();
            var contentType = contentTypeField.Text?.ToString()?.Trim();
            var expirationDateText = expirationDateField.Text?.ToString();

            if (!SecretFormValidator.TryValidateNewVersion(
                    value,
                    expirationDateText,
                    out var expiresAt,
                    out var errorMessage))
            {
                MessageBox.ErrorQuery(_app, "Error", errorMessage ?? "Invalid input", "OK");
                return;
            }

            dialog.RequestStop();
            await CreateSecretVersion(value!, contentType, expiresAt);
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(dialog) - 2
        };

        cancelButton.Accepting += (s, e) => dialog.RequestStop();

        dialog.Add(valueLabel, valueField, contentTypeLabel, contentTypeField, expirationDateLabel, expirationDateField, okButton, cancelButton);
        _app.Run(dialog);
    }

    private async Task CreateSecretVersion(string value, string? contentType, DateTime? expiresAt)
    {
        var selectedSecret = _viewModel.Secrets.SelectedSecret;
        if (_viewModel.KeyVaults.SelectedKeyVault == null || selectedSecret == null)
            return;

        var selectedSecretName = selectedSecret.Name;

        _app.Invoke(_ =>
        {
            _statusLabel.Text = $"Creating new version for '{selectedSecretName}'...";
        });

        var createResult = await _viewModel.Versions.CreateVersionAsync(value, contentType, expiresAt);

        if (createResult.Success)
        {
            _app.Invoke(_ =>
            {
                _statusLabel.Text = $"New version created for '{selectedSecretName}'";
                MessageBox.Query(_app, "Success", $"New version of '{selectedSecretName}' has been created successfully!", "OK");
            });

            // Refresh the versions list
            await RefreshVersionsForSelectedSecret();
            return;
        }

        _app.Invoke(_ =>
        {
            var errorMessage = createResult.ErrorMessage ?? $"Failed to create new version for '{selectedSecretName}'";
            _statusLabel.Text = $"Error: {errorMessage}";
            MessageBox.ErrorQuery(_app, "Error", $"Failed to create secret version: {errorMessage}", "OK");
        });
    }

    private async Task RefreshVersionsForSelectedSecret()
    {
        var selectedKeyVault = _viewModel.KeyVaults.SelectedKeyVault;
        var selectedSecret = _viewModel.Secrets.SelectedSecret;

        if (selectedKeyVault == null || selectedSecret == null)
            return;

        var selectedVaultName = selectedKeyVault.Name;
        var selectedSecretName = selectedSecret.Name;

        _app.Invoke(_ =>
        {
            _statusLabel.Text = $"Loading versions for {selectedSecretName}...";
            SetVersionsTableSource([]);
            _viewModel.SecretDetails.Clear();
            ApplySecretDetailsToView();
        });

        var loadResult = await _viewModel.Versions.LoadForSecretAsync(selectedVaultName, selectedSecretName);

        _app.Invoke(_ =>
        {
            if (_viewModel.KeyVaults.SelectedKeyVault?.Name != selectedVaultName ||
                _viewModel.Secrets.SelectedSecret?.Name != selectedSecretName)
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
                MessageBox.ErrorQuery(_app, "Error", $"Failed to refresh versions: {errorMessage}", "OK");
                return;
            }

            SetVersionsTableSource(_viewModel.Versions.Versions);

            if (_viewModel.Versions.Versions.Count > 0)
            {
                _statusLabel.Text = $"Loaded {_viewModel.Versions.Versions.Count} version(s) for {selectedSecretName}";

                // Do not auto-select any version. Load details only when user selects one.
                _suppressVersionSelectionEvent = true;
                _versionsTable.SelectedRow = -1;
                _suppressVersionSelectionEvent = false;
            }
            else
            {
                _statusLabel.Text = $"No versions for {selectedSecretName}";
            }
        });
    }
}
