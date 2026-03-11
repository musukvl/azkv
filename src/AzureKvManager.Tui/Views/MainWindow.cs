using Terminal.Gui;
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
    private readonly object _statusLock = new();

    private CancellationTokenSource? _spinnerCts;
    private string _spinnerMessage = "Loading Key Vaults...";
    private int _spinnerFrameIndex;
    private long _spinnerGeneration;
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
                    new MenuItem("_Refresh All", "", () => _keyVaultsPanel!.RefreshKeyVaults()),
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

        _detailsPanel = new SecretDetailsPanel(app, viewModel.SecretDetails);

        _detailsPanel.ActionsFrame.X = Pos.Right(_secretsPanel);
        _detailsPanel.ActionsFrame.Y = Pos.Bottom(_versionsPanel);
        _detailsPanel.ActionsFrame.Width = Dim.Fill();
        _detailsPanel.ActionsFrame.Height = 3;

        _detailsPanel.ContentTypeFrame.X = Pos.Right(_secretsPanel);
        _detailsPanel.ContentTypeFrame.Y = Pos.Bottom(_detailsPanel.ActionsFrame);
        _detailsPanel.ContentTypeFrame.Width = Dim.Fill();
        _detailsPanel.ContentTypeFrame.Height = Dim.Percent(15);

        _detailsPanel.ExpirationFrame.X = Pos.Right(_secretsPanel);
        _detailsPanel.ExpirationFrame.Y = Pos.Bottom(_detailsPanel.ContentTypeFrame);
        _detailsPanel.ExpirationFrame.Width = Dim.Fill();
        _detailsPanel.ExpirationFrame.Height = 3;

        _detailsPanel.ValueFrame.X = Pos.Right(_secretsPanel);
        _detailsPanel.ValueFrame.Y = Pos.Bottom(_detailsPanel.ExpirationFrame);
        _detailsPanel.ValueFrame.Width = Dim.Fill();
        _detailsPanel.ValueFrame.Height = Dim.Fill(2);

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

        Add(_keyVaultsPanel, _secretsPanel, _versionsPanel,
            _detailsPanel.ActionsFrame, _detailsPanel.ContentTypeFrame,
            _detailsPanel.ExpirationFrame, _detailsPanel.ValueFrame,
            _statusBar);

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

        // Keep initial state animated until first operation result arrives.
        UpdateStatus("Loading Key Vaults...");

        // Load key vaults on startup
        Task.Run(() => _keyVaultsPanel.RefreshKeyVaults());
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

        var dialog = new AddVersionDialog(_app, _viewModel.SelectedSecretName);
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
        _detailsPanel.ApplyTheme();
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
        lock (_statusLock)
        {
            _activeErrorDetails = message;
        }
    }

    private void ClearActiveError()
    {
        lock (_statusLock)
        {
            _activeErrorDetails = null;
        }
    }

    private void OnStatusShortcutActivated()
    {
        string? errorDetails;

        lock (_statusLock)
        {
            errorDetails = _activeErrorDetails;
        }

        if (string.IsNullOrWhiteSpace(errorDetails))
        {
            return;
        }

        MessageBox.ErrorQuery(_app, "Operation Error", errorDetails, "OK");
    }

    private void StartSpinner(string message)
    {
        CancellationToken token;
        long generation;

        lock (_statusLock)
        {
            _spinnerMessage = message;

            if (_spinnerCts is not null)
            {
                return;
            }

            _spinnerFrameIndex = 0;
            _spinnerCts = new CancellationTokenSource();
            generation = ++_spinnerGeneration;
            token = _spinnerCts.Token;
        }

        _ = RunSpinnerAsync(generation, token);
    }

    private void StopSpinner()
    {
        CancellationTokenSource? cts;

        lock (_statusLock)
        {
            cts = _spinnerCts;
            _spinnerCts = null;
            _spinnerGeneration++;
        }

        if (cts is null)
        {
            return;
        }

        cts.Cancel();
        cts.Dispose();
    }

    private async Task RunSpinnerAsync(long generation, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            string frame;
            string message;

            lock (_statusLock)
            {
                if (generation != _spinnerGeneration)
                {
                    return;
                }

                frame = SpinnerFrames[_spinnerFrameIndex % SpinnerFrames.Length];
                _spinnerFrameIndex++;
                message = _spinnerMessage;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            _app.Invoke(() => SetStatusLine($"{frame} {message}"));

            try
            {
                await Task.Delay(120, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private void SetStatusLine(string message)
    {
        _statusShortcut.Title = message;
        _statusShortcut.Text = string.Empty;
        _statusShortcut.SetNeedsDraw();
        _statusBar.SetNeedsDraw();
    }
}
