using Terminal.Gui;
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

    private async void OnSecretSelected(object? sender, ListViewItemEventArgs args)
    {
        if (args.Item < 0 || args.Item >= _filteredSecrets.Count || _selectedKeyVault == null)
            return;
        
        _selectedSecret = _filteredSecrets[args.Item];
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
                    _versionsList.SetSource(new ObservableCollection<string>(_versions.Select(v =>
                    {
                        var status = v.Enabled ? "✓" : "✗";
                        var updated = v.Updated?.ToString("yyyy-MM-dd HH:mm") ?? "N/A";
                        return $"{status} {v.Version.Substring(0, Math.Min(8, v.Version.Length))}... ({updated})";
                    })));
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
                MessageBox.ErrorQuery("Error", $"Failed to load versions: {ex.Message}", "OK");
            });
        }
    }

    private void ShowAddSecretDialog()
    {
        if (_selectedKeyVault == null)
        {
            MessageBox.ErrorQuery("Error", "Please select a Key Vault first", "OK");
            return;
        }

        var dialog = new Dialog
        {
            Title = "Add New Secret",
            Width = Dim.Percent(60),
            Height = 12
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

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.ErrorQuery("Error", "Secret name is required", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.ErrorQuery("Error", "Secret value is required", "OK");
                return;
            }

            dialog.RequestStop();
            await CreateSecret(name, value, contentType);
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(dialog) - 2
        };

        cancelButton.Accepting += (s, e) => dialog.RequestStop();

        dialog.Add(nameLabel, nameField, valueLabel, valueField, contentTypeLabel, contentTypeField, okButton, cancelButton);
        Application.Run(dialog);
    }

    private async Task CreateSecret(string name, string value, string? contentType)
    {
        if (_selectedKeyVault == null)
            return;

        Application.Invoke(() =>
        {
            _statusLabel.Text = $"Creating secret '{name}'...";
        });

        try
        {
            var success = await _azureService.SetSecretAsync(_selectedKeyVault.Name, name, value, contentType);

            if (success)
            {
                Application.Invoke(() =>
                {
                    _statusLabel.Text = $"Secret '{name}' created successfully";
                    MessageBox.Query("Success", $"Secret '{name}' has been created successfully!", "OK");
                });

                // Refresh the secrets list
                await RefreshSecretsForSelectedVault();
            }
            else
            {
                Application.Invoke(() =>
                {
                    _statusLabel.Text = $"Failed to create secret '{name}'";
                    MessageBox.ErrorQuery("Error", $"Failed to create secret '{name}'", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            Application.Invoke(() =>
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                MessageBox.ErrorQuery("Error", $"Failed to create secret: {ex.Message}", "OK");
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
                MessageBox.ErrorQuery("Error", $"Failed to refresh secrets: {ex.Message}", "OK");
            });
        }
    }
}
