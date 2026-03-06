using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Models;

namespace AzureKvManager.Tui.Views;

public partial class MainWindow
{
    private async void OnVersionSelectionChanged(object? sender, SelectedCellChangedEventArgs args)
    {
        if (_suppressVersionSelectionEvent)
        {
            return;
        }

        if (args.NewRow < 0 || args.NewRow >= _versions.Count || 
            _selectedKeyVault == null || _selectedSecret == null)
        {
            _app.Invoke(_ => ClearVersionSelectionDetails());
            return;
        }
        
        var version = _versions[args.NewRow];
        await LoadVersionDetailsAsync(version);
    }

    private async Task LoadVersionDetailsAsync(SecretVersion version)
    {
        if (_selectedKeyVault == null || _selectedSecret == null)
        {
            return;
        }

        var selectedVaultName = _selectedKeyVault.Name;
        var selectedSecretName = _selectedSecret.Name;
        var selectedVersionId = version.Version;
        
        _app.Invoke(_ =>
        {
            _selectedVersionId = selectedVersionId;
            _statusLabel.Text = $"Loading secret value...";
            _contentTypeView.Text = string.IsNullOrWhiteSpace(version.ContentType) ? "(none)" : version.ContentType;
            _expirationLabel.Text = BuildExpirationDetailsText(version.Expires);
            _valueView.Text = "Loading...";
            _copyButton.Enabled = false;
        });
        
        try
        {
            var details = await _azureService.GetSecretVersionDetailsAsync(
                selectedVaultName,
                selectedSecretName,
                version.Version
            );
            
            _app.Invoke(_ =>
            {
                if (_selectedKeyVault?.Name != selectedVaultName || _selectedSecret?.Name != selectedSecretName)
                {
                    return;
                }

                if (_selectedVersionId != selectedVersionId)
                {
                    return;
                }

                var resolvedContentType = details?.ContentType ?? version.ContentType;
                var resolvedExpiration = details?.Expires ?? version.Expires;
                var resolvedValue = details?.Value;

                _contentTypeView.Text = string.IsNullOrWhiteSpace(resolvedContentType) ? "(none)" : resolvedContentType;
                _expirationLabel.Text = BuildExpirationDetailsText(resolvedExpiration);
                _valueView.Text = resolvedValue ?? "(empty)";
                _copyButton.Enabled = !string.IsNullOrWhiteSpace(resolvedValue);
                _statusLabel.Text = $"Loaded value for {selectedSecretName} (version {ShortVersion(version.Version)}...)";
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(_ =>
            {
                if (_selectedKeyVault?.Name != selectedVaultName || _selectedSecret?.Name != selectedSecretName)
                {
                    return;
                }

                if (_selectedVersionId != selectedVersionId)
                {
                    return;
                }

                _statusLabel.Text = $"Error: {ex.Message}";
                _valueView.Text = $"Error: {ex.Message}";
                _copyButton.Enabled = false;
                MessageBox.ErrorQuery(_app, "Error", $"Failed to load secret value: {ex.Message}", "OK");
            });
        }
    }

    private void ShowAddVersionDialog()
    {
        if (_selectedKeyVault == null)
        {
            MessageBox.ErrorQuery(_app, "Error", "Please select a Key Vault first", "OK");
            return;
        }

        if (_selectedSecret == null)
        {
            MessageBox.ErrorQuery(_app, "Error", "Please select a secret first", "OK");
            return;
        }

        var dialog = new Dialog
        {
            Title = $"Add New Version to '{_selectedSecret.Name}'",
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

            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.ErrorQuery(_app, "Error", "Secret value is required", "OK");
                return;
            }

            if (!TryParseExpirationDate(expirationDateText, out var expiresAt))
            {
                MessageBox.ErrorQuery(_app, "Error", "Expiration date must be in yyyy-MM-dd format", "OK");
                return;
            }

            dialog.RequestStop();
            await CreateSecretVersion(value, contentType, expiresAt);
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
        if (_selectedKeyVault == null || _selectedSecret == null)
            return;

        _app.Invoke(_ =>
        {
            _statusLabel.Text = $"Creating new version for '{_selectedSecret.Name}'...";
        });

        try
        {
            var success = await _azureService.SetSecretAsync(_selectedKeyVault.Name, _selectedSecret.Name, value, contentType, expiresAt);

            if (success)
            {
                _app.Invoke(_ =>
                {
                    _statusLabel.Text = $"New version created for '{_selectedSecret.Name}'";
                    MessageBox.Query(_app, "Success", $"New version of '{_selectedSecret.Name}' has been created successfully!", "OK");
                });

                // Refresh the versions list
                await RefreshVersionsForSelectedSecret();
            }
            else
            {
                _app.Invoke(_ =>
                {
                    _statusLabel.Text = $"Failed to create new version for '{_selectedSecret.Name}'";
                    MessageBox.ErrorQuery(_app, "Error", $"Failed to create new version for '{_selectedSecret.Name}'", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            _app.Invoke(_ =>
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                MessageBox.ErrorQuery(_app, "Error", $"Failed to create secret version: {ex.Message}", "OK");
            });
        }
    }

    private async Task RefreshVersionsForSelectedSecret()
    {
        if (_selectedKeyVault == null || _selectedSecret == null)
            return;

        _app.Invoke(_ =>
        {
            _statusLabel.Text = $"Loading versions for {_selectedSecret.Name}...";
            SetVersionsTableSource([]);
            ClearVersionSelectionDetails();
        });

        try
        {
            var versions = await _azureService.GetSecretVersionsAsync(_selectedKeyVault.Name, _selectedSecret.Name);
            _versions = versions
                .OrderByDescending(GetVersionSortKey)
                .ThenByDescending(v => v.Version, StringComparer.Ordinal)
                .ToList();
            
            _app.Invoke(_ =>
            {
                SetVersionsTableSource(_versions);

                if (_versions.Any())
                {
                    _statusLabel.Text = $"Loaded {_versions.Count} version(s) for {_selectedSecret.Name}";

                    _suppressVersionSelectionEvent = true;
                    _versionsTable.SelectedRow = 0;
                    _versionsTable.EnsureSelectedCellIsVisible();
                    _suppressVersionSelectionEvent = false;
                }
                else
                {
                    _statusLabel.Text = $"No versions for {_selectedSecret.Name}";
                }
            });

            if (_versions.Any())
            {
                await LoadVersionDetailsAsync(_versions[0]);
            }
        }
        catch (Exception ex)
        {
            _app.Invoke(_ =>
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                MessageBox.ErrorQuery(_app, "Error", $"Failed to refresh versions: {ex.Message}", "OK");
            });
        }
    }
}
