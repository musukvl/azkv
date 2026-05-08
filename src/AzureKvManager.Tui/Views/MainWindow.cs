using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Models;
using AzureKvManager.Tui.Themes;
using AzureKvManager.Tui.ViewModels;
using AzureKvManager.Tui.Views.Dialogs;
using AzureKvManager.Tui.Views.Panels;

namespace AzureKvManager.Tui.Views;

public class MainWindow : Window
{
    private static readonly string[] SpinnerFrames = ["|", "/", "-", "\\"];

    private readonly IApplication _app;
    private readonly MainWindowViewModel _viewModel;
    private readonly KeyVaultsPanel _keyVaultsPanel;
    private readonly SecretsPanel _secretsPanel;
    private readonly VersionsPanel _versionsPanel;
    private readonly SecretDetailsPanel _detailsPanel;
    private readonly StatusBar _statusBar;
    private readonly Shortcut _statusShortcut;

    private object? _spinnerTimeoutToken;
    private string _spinnerMessage = "Loading Key Vaults...";
    private int _spinnerFrameIndex;
    private bool _initialLoadStarted;
    private string? _activeErrorDetails;

    public MainWindow(IApplication app, MainWindowViewModel viewModel, string? initialFilter = null)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        Title = "Azure Key Vault Manager (TUI)";

        var menu = new MenuBar
        {
            Menus =
            [
                new("_File", [
                    new MenuItem("_Refresh All", "", () => _ = _keyVaultsPanel!.RefreshKeyVaultsAsync()),
                    new MenuItem("_Quit", "", () => _app.RequestStop())
                ]),
                new("_Theme", ThemeProvider.GetThemeNames()
                    .Select(name => new MenuItem(name, "", () => SwitchTheme(name)))
                    .ToArray()),
                new("_Help", [
                    new MenuItem("_About", "", ShowAbout)
                ])
            ]
        };

        Add(menu);

        _keyVaultsPanel = new KeyVaultsPanel(app, viewModel.KeyVaults)
        {
            X = 0,
            Y = 1,
            Width = Dim.Percent(25),
            Height = Dim.Fill(2)
        };

        _secretsPanel = new SecretsPanel(app, viewModel.Secrets)
        {
            X = Pos.Right(_keyVaultsPanel),
            Y = 1,
            Width = Dim.Percent(40),
            Height = Dim.Fill(2)
        };

        _versionsPanel = new VersionsPanel(app, viewModel.Versions)
        {
            X = Pos.Right(_secretsPanel),
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Percent(35)
        };

        _detailsPanel = new SecretDetailsPanel(app, viewModel.SecretDetails)
        {
            X = Pos.Right(_secretsPanel),
            Y = Pos.Bottom(_versionsPanel),
            Width = Dim.Fill(),
            Height = Dim.Fill(2)
        };

        _statusShortcut = new Shortcut
        {
            Title = "Loading Key Vaults...",
            Text = string.Empty,
            Key = Key.Empty,
            MinimumKeyTextSize = 0
        };
        _statusShortcut.Action = OnStatusShortcutActivated;
        _statusBar = new StatusBar(
        [
            _statusShortcut
        ]);

        Add(_keyVaultsPanel, _secretsPanel, _versionsPanel, _detailsPanel, _statusBar);

        // Wire events
        _keyVaultsPanel.KeyVaultSelected += OnKeyVaultSelected;
        _keyVaultsPanel.StatusChanged += UpdateStatus;

        _secretsPanel.SecretSelected += OnSecretSelected;
        _secretsPanel.StatusChanged += UpdateStatus;

        _versionsPanel.VersionSelected += OnVersionSelected;
        _versionsPanel.StatusChanged += UpdateStatus;

        _detailsPanel.AddVersionRequested += OnAddVersionRequested;
        _detailsPanel.StatusChanged += UpdateStatus;

        // Apply initial filter
        if (!string.IsNullOrEmpty(initialFilter))
        {
            _keyVaultsPanel.SetInitialFilter(initialFilter);
        }
    }

    protected override void OnIsRunningChanged(bool newIsRunning)
    {
        base.OnIsRunningChanged(newIsRunning);

        if (!newIsRunning || _initialLoadStarted)
        {
            return;
        }

        _initialLoadStarted = true;
        UpdateStatus("Loading Key Vaults...");
        _ = _keyVaultsPanel.RefreshKeyVaultsAsync();
    }

    private void OnKeyVaultSelected(KeyVault keyVault)
    {
        // SelectVault clears child VMs which raise StateChanged → panels auto-render
        _viewModel.SelectVault(keyVault.Name);
        _secretsPanel.Clear();

        _ = _secretsPanel.LoadForVaultAsync(keyVault.Name);
    }

    private void OnSecretSelected(Secret secret)
    {
        // SelectSecret clears child VMs which raise StateChanged → panels auto-render
        _viewModel.SelectSecret(secret.Name);

        if (_viewModel.SelectedVaultName is not null)
        {
            _ = _versionsPanel.LoadForSecretAsync(_viewModel.SelectedVaultName, secret.Name);
        }
    }

    private void OnVersionSelected(SecretVersion version)
    {
        if (_viewModel.SelectedVaultName is null || _viewModel.SelectedSecretName is null)
        {
            return;
        }

        _ = _detailsPanel.LoadVersionDetailsAsync(_viewModel.SelectedVaultName, _viewModel.SelectedSecretName, version);
    }

    private void OnAddVersionRequested()
    {
        if (_viewModel.SelectedVaultName is null)
        {
            MessageBox.ErrorQuery(_app, "Error", "Please select a Key Vault first", "OK");
            return;
        }

        if (_viewModel.SelectedSecretName is null)
        {
            MessageBox.ErrorQuery(_app, "Error", "Please select a secret first", "OK");
            return;
        }

        using var dialog = new AddVersionDialog(_viewModel.SelectedSecretName);
        _app.Run(dialog);

        if (dialog.Result is null)
        {
            return;
        }

        _ = CreateVersionAsync(dialog.Result);
    }

    private async Task CreateVersionAsync(AddVersionResult addResult)
    {
        var vaultName = _viewModel.SelectedVaultName;
        var secretName = _viewModel.SelectedSecretName;
        if (vaultName is null || secretName is null) return;

        _app.Invoke(() => UpdateStatus($"Creating version for secret '{secretName}'..."));

        var result = await _viewModel.Versions.CreateVersionAsync(vaultName, secretName, addResult.Value, addResult.ContentType, addResult.ExpiresAt);

        if (result.Success)
        {
            _app.Invoke(() =>
            {
                UpdateStatus($"Version created for secret '{secretName}'.");
                MessageBox.Query(_app, "Success", $"New version of '{secretName}' has been created successfully!", "OK");
            });

            await _versionsPanel.RefreshAsync(vaultName, secretName);
            return;
        }

        _app.Invoke(() =>
        {
            var errorMessage = result.ErrorMessage ?? $"Failed to create new version for '{secretName}'";
            UpdateStatus($"Error creating version for secret '{secretName}': {errorMessage}");
        });
    }

    private void SwitchTheme(string themeName)
    {
        ThemeProvider.ApplyTheme(themeName);
        SetNeedsDraw();
    }

    private void ShowAbout()
    {
        MessageBox.Query(_app, "About",
            "Azure Key Vault Manager (TUI)\n\n" +
            "A Terminal UI application for managing Azure Key Vaults\n" +
            "Built with Terminal.Gui\n\n" +
            "Navigation:\n" +
            "- Tab: Move to next control\n" +
            "- Shift+Tab: Move to previous control\n" +
            "- Arrow keys: Navigate within lists\n" +
            "- Enter: Select item\n" +
            "- Ctrl+C / Alt+C: Copy secret value\n" +
            "- Ctrl+Q / Alt+F4: Quit",
            "OK");
    }

    private void UpdateStatus(string message)
    {
        if (IsProgressMessage(message))
        {
            ClearActiveError();
            StartSpinner(message);
            return;
        }

        StopSpinner();

        if (IsErrorMessage(message))
        {
            SetActiveError(message);
            SetStatusLine($"! {message} (click status for details)");
            return;
        }

        ClearActiveError();
        SetStatusLine(message);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopSpinner();
        }

        base.Dispose(disposing);
    }

    private static bool IsProgressMessage(string message)
    {
        return message.StartsWith("Loading ", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("Creating ", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("Reloading ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsErrorMessage(string message)
    {
        return message.StartsWith("Error", StringComparison.OrdinalIgnoreCase);
    }

    private void SetActiveError(string message)
    {
        _activeErrorDetails = message;
    }

    private void ClearActiveError()
    {
        _activeErrorDetails = null;
    }

    private void OnStatusShortcutActivated()
    {
        var errorDetails = _activeErrorDetails;

        if (string.IsNullOrWhiteSpace(errorDetails))
        {
            return;
        }

        MessageBox.ErrorQuery(_app, "Operation Error", errorDetails, "OK");
    }

    private void StartSpinner(string message)
    {
        _spinnerMessage = message;

        if (_spinnerTimeoutToken is not null)
        {
            return;
        }

        _spinnerFrameIndex = 0;
        RenderSpinnerFrame();
        _spinnerTimeoutToken = _app.AddTimeout(TimeSpan.FromMilliseconds(120), UpdateSpinner);
    }

    private void StopSpinner()
    {
        if (_spinnerTimeoutToken is not { } token)
        {
            return;
        }

        _spinnerTimeoutToken = null;
        _app.RemoveTimeout(token);
    }

    private bool UpdateSpinner()
    {
        if (_spinnerTimeoutToken is null)
        {
            return false;
        }

        RenderSpinnerFrame();
        return true;
    }

    private void RenderSpinnerFrame()
    {
        var frame = SpinnerFrames[_spinnerFrameIndex % SpinnerFrames.Length];
        _spinnerFrameIndex++;
        SetStatusLine($"{frame} {_spinnerMessage}");
    }

    private void SetStatusLine(string message)
    {
        _statusShortcut.Title = message;
        _statusShortcut.Text = string.Empty;
        _statusShortcut.SetNeedsDraw();
        _statusBar.SetNeedsDraw();
    }
}
