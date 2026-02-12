using Terminal.Gui;
using System.Collections.ObjectModel;

namespace AzureKvManager.Tui.Views;

public partial class MainWindow
{
    private async void OnVersionSelected(object? sender, ListViewItemEventArgs args)
    {
        if (args.Item < 0 || args.Item >= _versions.Count || 
            _selectedKeyVault == null || _selectedSecret == null)
            return;
        
        var version = _versions[args.Item];
        
        Application.Invoke(() =>
        {
            _statusLabel.Text = $"Loading secret value...";
            _valueView.Text = "Loading...";
        });
        
        try
        {
            var value = await _azureService.GetSecretValueAsync(
                _selectedKeyVault.Name, 
                _selectedSecret.Name, 
                version.Version
            );
            
            Application.Invoke(() =>
            {
                _valueView.Text = value ?? "(empty)";
                _statusLabel.Text = $"Loaded value for {_selectedSecret.Name} (version {version.Version.Substring(0, 8)}...)";
            });
        }
        catch (Exception ex)
        {
            Application.Invoke(() =>
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                _valueView.Text = $"Error: {ex.Message}";
                MessageBox.ErrorQuery("Error", $"Failed to load secret value: {ex.Message}", "OK");
            });
        }
    }

    private void ShowAddVersionDialog()
    {
        if (_selectedKeyVault == null)
        {
            MessageBox.ErrorQuery("Error", "Please select a Key Vault first", "OK");
            return;
        }

        if (_selectedSecret == null)
        {
            MessageBox.ErrorQuery("Error", "Please select a secret first", "OK");
            return;
        }

        var dialog = new Dialog
        {
            Title = $"Add New Version to '{_selectedSecret.Name}'",
            Width = Dim.Percent(60),
            Height = 10
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

            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.ErrorQuery("Error", "Secret value is required", "OK");
                return;
            }

            dialog.RequestStop();
            await CreateSecretVersion(value, contentType);
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(dialog) - 2
        };

        cancelButton.Accepting += (s, e) => dialog.RequestStop();

        dialog.Add(valueLabel, valueField, contentTypeLabel, contentTypeField, okButton, cancelButton);
        Application.Run(dialog);
    }

    private async Task CreateSecretVersion(string value, string? contentType)
    {
        if (_selectedKeyVault == null || _selectedSecret == null)
            return;

        Application.Invoke(() =>
        {
            _statusLabel.Text = $"Creating new version for '{_selectedSecret.Name}'...";
        });

        try
        {
            var success = await _azureService.SetSecretAsync(_selectedKeyVault.Name, _selectedSecret.Name, value, contentType);

            if (success)
            {
                Application.Invoke(() =>
                {
                    _statusLabel.Text = $"New version created for '{_selectedSecret.Name}'";
                    MessageBox.Query("Success", $"New version of '{_selectedSecret.Name}' has been created successfully!", "OK");
                });

                // Refresh the versions list
                await RefreshVersionsForSelectedSecret();
            }
            else
            {
                Application.Invoke(() =>
                {
                    _statusLabel.Text = $"Failed to create new version for '{_selectedSecret.Name}'";
                    MessageBox.ErrorQuery("Error", $"Failed to create new version for '{_selectedSecret.Name}'", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            Application.Invoke(() =>
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                MessageBox.ErrorQuery("Error", $"Failed to create secret version: {ex.Message}", "OK");
            });
        }
    }

    private async Task RefreshVersionsForSelectedSecret()
    {
        if (_selectedKeyVault == null || _selectedSecret == null)
            return;

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
                MessageBox.ErrorQuery("Error", $"Failed to refresh versions: {ex.Message}", "OK");
            });
        }
    }
}
