using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Services;
using SecretModel = AzureKvManager.Tui.Models.Secret;

namespace AzureKvManager.Tui.Views;

public partial class MainWindow
{
    private void FilterSecrets()
    {
        _viewModel.Secrets.ApplyFilter(_secretFilter.Text?.ToString());
        SetSecretsTableSource(_viewModel.Secrets.FilteredSecrets);

        var selectedKeyVault = _viewModel.KeyVaults.SelectedKeyVault;
        if (selectedKeyVault != null)
        {
            var filteredCount = _viewModel.Secrets.FilteredSecrets.Count;
            var totalCount = _viewModel.Secrets.AllSecrets.Count;

            _statusLabel.Text = filteredCount > 0
                ? $"Showing {filteredCount} of {totalCount} secret(s) from {selectedKeyVault.Name}"
                : $"No matches found (total: {totalCount})";
        }
    }

    private void SetSecretsTableSource(IEnumerable<SecretModel> secrets)
    {
        var snapshot = secrets.ToArray();

        _secretsTable.Table = new EnumerableTableSource<SecretModel>(
            snapshot,
            new Dictionary<string, Func<SecretModel, object>>
            {
                ["Secret Name"] = secret => string.IsNullOrWhiteSpace(secret.Name) ? "(unnamed secret)" : secret.Name,
                ["Expiration"] = secret => FormatVersionDate(secret.Expires),
                ["Content Type"] = secret => string.IsNullOrWhiteSpace(secret.ContentType) ? " " : secret.ContentType
            }
        );

        _secretsTable.Update();
    }

    private async void OnSecretSelectionChanged(object? sender, SelectedCellChangedEventArgs args)
    {
        var selectedVault = _viewModel.KeyVaults.SelectedKeyVault;
        if (selectedVault is null)
        {
            return;
        }

        if (!_viewModel.Secrets.TrySelectByIndex(args.NewRow, out var selectedSecret) || selectedSecret is null)
        {
            return;
        }

        _viewModel.ClearFromSecretSelection(selectedVault.Name, selectedSecret.Name);
        
        _app.Invoke(() =>
        {
            _statusLabel.Text = $"Loading versions for {selectedSecret.Name}...";
            SetVersionsTableSource([]);
            ApplySecretDetailsToView();
        });

        await RefreshVersionsForSelectedSecret();
    }

    private void ShowAddSecretDialog()
    {
        if (_viewModel.KeyVaults.SelectedKeyVault == null)
        {
            MessageBox.ErrorQuery(_app, "Error", "Please select a Key Vault first", "OK");
            return;
        }

        var dialog = new Dialog
        {
            Title = "Add New Secret",
            Width = Dim.Percent(60),
            Height = 14
        };

        var nameLabel = new Label
        {
            Text = "Secret Name:",
            X = 1,
            Y = 1
        };

        var nameField = new TextField
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(1)
        };

        var valueLabel = new Label
        {
            Text = "Secret Value:",
            X = 1,
            Y = 3
        };

        var valueField = new TextView
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill(1),
            Height = 3
        };

        var contentTypeLabel = new Label
        {
            Text = "Content Type (optional):",
            X = 1,
            Y = 7
        };

        var contentTypeField = new TextField
        {
            X = 1,
            Y = 8,
            Width = Dim.Fill(1)
        };

        var expirationDateLabel = new Label
        {
            Text = "Expiration Date (yyyy-MM-dd, optional):",
            X = 1,
            Y = 9
        };

        var expirationDateField = new TextField
        {
            X = 1,
            Y = 10,
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
            var name = nameField.Text?.ToString()?.Trim();
            var value = valueField.Text?.ToString()?.Trim();
            var contentType = contentTypeField.Text?.ToString()?.Trim();
            var expirationDateText = expirationDateField.Text?.ToString();

            if (!SecretFormValidator.TryValidateNewSecret(
                    name,
                    value,
                    expirationDateText,
                    out var expiresAt,
                    out var errorMessage))
            {
                MessageBox.ErrorQuery(_app, "Error", errorMessage ?? "Invalid input", "OK");
                return;
            }

            dialog.RequestStop();
            await CreateSecret(name!, value!, contentType, expiresAt);
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(dialog) - 2
        };

        cancelButton.Accepting += (s, e) => dialog.RequestStop();

        dialog.Add(nameLabel, nameField, valueLabel, valueField, contentTypeLabel, contentTypeField, expirationDateLabel, expirationDateField, okButton, cancelButton);
        _app.Run(dialog);
    }

    private async Task CreateSecret(string name, string value, string? contentType, DateTime? expiresAt)
    {
        if (_viewModel.KeyVaults.SelectedKeyVault == null)
            return;

        _app.Invoke(() =>
        {
            _statusLabel.Text = $"Creating secret '{name}'...";
        });

        var result = await _viewModel.Secrets.CreateSecretAsync(name, value, contentType, expiresAt);

        if (result.Success)
        {
            _app.Invoke(() =>
            {
                _statusLabel.Text = $"Secret '{name}' created successfully";
                MessageBox.Query(_app, "Success", $"Secret '{name}' has been created successfully!", "OK");
            });

            // Refresh the secrets list
            await RefreshSecretsForSelectedVault();
            return;
        }

        _app.Invoke(() =>
        {
            var errorMessage = result.ErrorMessage ?? $"Failed to create secret '{name}'";
            _statusLabel.Text = $"Error: {errorMessage}";
            MessageBox.ErrorQuery(_app, "Error", $"Failed to create secret: {errorMessage}", "OK");
        });
    }

    private async Task RefreshSecretsForSelectedVault()
    {
        var selectedVault = _viewModel.KeyVaults.SelectedKeyVault;
        if (selectedVault == null)
            return;

        var refreshResult = await _viewModel.Secrets.RefreshAsync();

        _app.Invoke(() =>
        {
            if (_viewModel.KeyVaults.SelectedKeyVault?.Name != selectedVault.Name)
            {
                return;
            }

            if (refreshResult.IsStale)
            {
                return;
            }

            if (!refreshResult.Success)
            {
                var errorMessage = refreshResult.ErrorMessage ?? "Unknown error";
                _statusLabel.Text = $"Error: {errorMessage}";
                MessageBox.ErrorQuery(_app, "Error", $"Failed to refresh secrets: {errorMessage}", "OK");
                return;
            }

            var filterText = _secretFilter.Text?.ToString()?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(filterText))
            {
                FilterSecrets();
                return;
            }

            SetSecretsTableSource(_viewModel.Secrets.FilteredSecrets);
            _statusLabel.Text = $"Loaded {_viewModel.Secrets.AllSecrets.Count} secret(s) from {selectedVault.Name}";
        });
    }
}
