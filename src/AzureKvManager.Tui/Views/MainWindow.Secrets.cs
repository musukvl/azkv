using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Models;
using System.Collections.ObjectModel;

namespace AzureKvManager.Tui.Views;

public partial class MainWindow
{
    private void FilterSecrets()
    {
        var filterText = _secretFilter.Text?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(filterText))
        {
            _filteredSecrets = new List<Secret>(_secrets);
        }
        else
        {
            _filteredSecrets = _secrets
                .Where(s => s.Name.ToLowerInvariant().Contains(filterText))
                .ToList();
        }
        
        _secretsList.SetSource(new ObservableCollection<string>(
            _filteredSecrets.Select(s => 
            {
                var contentType = string.IsNullOrWhiteSpace(s.ContentType) ? "" : $" [{s.ContentType}]";
                return $"{s.Name}{contentType} {(s.Enabled ? "✓" : "✗")}";
            })
        ));
        
        if (_selectedKeyVault != null)
        {
            _statusLabel.Text = _filteredSecrets.Any()
                ? $"Showing {_filteredSecrets.Count} of {_secrets.Count} secret(s) from {_selectedKeyVault.Name}"
                : $"No matches found (total: {_secrets.Count})";
        }
    }

    private async void OnSecretSelectionChanged(object? sender, ValueChangedEventArgs<int?> args)
    {
        if (!args.NewValue.HasValue || args.NewValue.Value < 0 || args.NewValue.Value >= _filteredSecrets.Count || _selectedKeyVault == null)
            return;
        
        _selectedSecret = _filteredSecrets[args.NewValue.Value];
        _versions.Clear();
        
        Application.Invoke(() =>
        {
            _statusLabel.Text = $"Loading versions for {_selectedSecret.Name}...";
            _versionsList.SetSource(new ObservableCollection<string> { "Loading..." });
            _valueView.Text = string.Empty;
        });
        
        try
        {
            _versions = await _azureService.GetSecretVersionsAsync(_selectedKeyVault.Name, _selectedSecret.Name);
            
            Application.Invoke(() =>
            {
                if (_versions.Any())
                {
                    _versionsList.SetSource(new ObservableCollection<string>(_versions.Select(FormatVersionDisplay)));
                    _statusLabel.Text = $"Loaded {_versions.Count} version(s) for {_selectedSecret.Name}";
                }
                else
                {
                    _versionsList.SetSource(new ObservableCollection<string> { "No versions found" });
                    _statusLabel.Text = $"No versions for {_selectedSecret.Name}";
                }
            });
        }
        catch (Exception ex)
        {
            Application.Invoke(() =>
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                MessageBox.ErrorQuery(Application.Instance, "Error", $"Failed to load versions: {ex.Message}", "OK");
            });
        }
    }

    private void ShowAddSecretDialog()
    {
        if (_selectedKeyVault == null)
        {
            MessageBox.ErrorQuery(Application.Instance, "Error", "Please select a Key Vault first", "OK");
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

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.ErrorQuery(Application.Instance, "Error", "Secret name is required", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.ErrorQuery(Application.Instance, "Error", "Secret value is required", "OK");
                return;
            }

            if (!TryParseExpirationDate(expirationDateText, out var expiresAt))
            {
                MessageBox.ErrorQuery(Application.Instance, "Error", "Expiration date must be in yyyy-MM-dd format", "OK");
                return;
            }

            dialog.RequestStop();
            await CreateSecret(name, value, contentType, expiresAt);
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(dialog) - 2
        };

        cancelButton.Accepting += (s, e) => dialog.RequestStop();

        dialog.Add(nameLabel, nameField, valueLabel, valueField, contentTypeLabel, contentTypeField, expirationDateLabel, expirationDateField, okButton, cancelButton);
        Application.Run(dialog);
    }

    private async Task CreateSecret(string name, string value, string? contentType, DateTime? expiresAt)
    {
        if (_selectedKeyVault == null)
            return;

        Application.Invoke(() =>
        {
            _statusLabel.Text = $"Creating secret '{name}'...";
        });

        try
        {
            var success = await _azureService.SetSecretAsync(_selectedKeyVault.Name, name, value, contentType, expiresAt);

            if (success)
            {
                Application.Invoke(() =>
                {
                    _statusLabel.Text = $"Secret '{name}' created successfully";
                    MessageBox.Query(Application.Instance, "Success", $"Secret '{name}' has been created successfully!", "OK");
                });

                // Refresh the secrets list
                await RefreshSecretsForSelectedVault();
            }
            else
            {
                Application.Invoke(() =>
                {
                    _statusLabel.Text = $"Failed to create secret '{name}'";
                    MessageBox.ErrorQuery(Application.Instance, "Error", $"Failed to create secret '{name}'", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            Application.Invoke(() =>
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                MessageBox.ErrorQuery(Application.Instance, "Error", $"Failed to create secret: {ex.Message}", "OK");
            });
        }
    }

    private async Task RefreshSecretsForSelectedVault()
    {
        if (_selectedKeyVault == null)
            return;

        try
        {
            _secrets = await _azureService.GetSecretsAsync(_selectedKeyVault.Name);
            _filteredSecrets = new List<Secret>(_secrets);
            
            // Reapply filter if there is one
            var filterText = _secretFilter.Text?.ToString()?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(filterText))
            {
                FilterSecrets();
            }
            else
            {
                Application.Invoke(() =>
                {
                    _secretsList.SetSource(new ObservableCollection<string>(_filteredSecrets.Select(s => 
                    {
                        var contentType = string.IsNullOrWhiteSpace(s.ContentType) ? "" : $" [{s.ContentType}]";
                        return $"{s.Name}{contentType} {(s.Enabled ? "✓" : "✗")}";
                    })));
                    _statusLabel.Text = $"Loaded {_secrets.Count} secret(s) from {_selectedKeyVault.Name}";
                });
            }
        }
        catch (Exception ex)
        {
            Application.Invoke(() =>
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                MessageBox.ErrorQuery(Application.Instance, "Error", $"Failed to refresh secrets: {ex.Message}", "OK");
            });
        }
    }
}
