using System.Globalization;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Models;
using AzureKvManager.Tui.ViewModels;
using AzureKvManager.Tui.Views.Dialogs;
using SecretModel = AzureKvManager.Tui.Models.Secret;

namespace AzureKvManager.Tui.Views.Panels;

public sealed class SecretsPanel : FrameView
{
    private readonly IApplication _app;
    private readonly SecretsViewModel _viewModel;
    private readonly TextField _filterField;
    private readonly TableView _tableView;

    public event Action<SecretModel>? SecretSelected;
    public event Action<string>? StatusChanged;

    public SecretsPanel(IApplication app, SecretsViewModel viewModel)
    {
        _app = app;
        _viewModel = viewModel;

        Title = "Secrets";
        TabStop = TabBehavior.TabGroup;

        var filterLabel = new Label
        {
            Text = "_Filter:",
            X = 0,
            Y = 0,
            Width = 7
        };

        _filterField = new TextField
        {
            X = Pos.Right(filterLabel),
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            TabStop = TabBehavior.TabStop
        };
        _filterField.TextChanged += (s, e) => _viewModel.ApplyFilter(_filterField.Text?.ToString());

        _tableView = new TableView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            TabStop = TabBehavior.TabStop
        };

        _tableView.FullRowSelect = true;
        _tableView.MultiSelect = false;
        _tableView.Style.ShowVerticalCellLines = false;
        _tableView.Style.ShowVerticalHeaderLines = false;
        _tableView.Style.ShowHeaders = false;
        _tableView.Style.ExpandLastColumn = true;
        _tableView.Style.ShowHorizontalHeaderOverline = false;
        _tableView.Style.ShowHorizontalHeaderUnderline = false;

        SetTableSource([]);
        _tableView.SelectedCellChanged += OnSecretSelectionChanged;

        var addSecretButton = new Button
        {
            Text = "New _Secret",
            X = 0,
            Y = Pos.Bottom(_tableView)
        };
        addSecretButton.Accepting += (s, e) => ShowAddSecretDialog();

        var reloadButton = new Button
        {
            Text = "_Reload",
            X = Pos.Right(addSecretButton) + 1,
            Y = Pos.Bottom(_tableView)
        };
        reloadButton.Accepting += async (s, e) => await ReloadSecrets();

        _viewModel.StateChanged += () => _app.Invoke(RenderFromViewModel);

        Add(filterLabel, _filterField, _tableView, addSecretButton, reloadButton);
    }

    public string? CurrentVaultName { get; private set; }

    public void Clear()
    {
        CurrentVaultName = null;
        _filterField.Text = string.Empty;
        // ClearForVaultSwitch on VM raises StateChanged → RenderFromViewModel
    }

    public async Task LoadForVaultAsync(string vaultName)
    {
        CurrentVaultName = vaultName;

        StatusChanged?.Invoke($"Loading secrets for kv {vaultName}...");

        var result = await _viewModel.LoadForVaultAsync(vaultName);

        _app.Invoke(() =>
        {
            if (CurrentVaultName != vaultName || result.IsStale)
            {
                StatusChanged?.Invoke("Secrets load canceled.");
                return;
            }

            if (!result.Success)
            {
                var errorMessage = result.ErrorMessage ?? "Unknown error";
                StatusChanged?.Invoke($"Error loading secrets for kv {vaultName}: {errorMessage}");
                return;
            }

            // LoadForVaultAsync calls ApplyFilter internally, which raises StateChanged → RenderFromViewModel.
            StatusChanged?.Invoke($"Secrets loaded for kv {vaultName}. ({_viewModel.AllSecrets.Count} secret(s))");
        });
    }

    private void RenderFromViewModel()
    {
        SetTableSource(_viewModel.FilteredSecrets);

        // Sync filter field if VM was cleared (e.g., vault switch)
        var vmFilterText = _viewModel.FilterText;
        var fieldText = _filterField.Text?.ToString()?.Trim() ?? string.Empty;
        if (vmFilterText != fieldText)
        {
            _filterField.Text = vmFilterText;
        }
    }

    private void SetTableSource(IEnumerable<SecretModel> secrets)
    {
        var snapshot = secrets.ToArray();

        _tableView.Table = new EnumerableTableSource<SecretModel>(
            snapshot,
            new Dictionary<string, Func<SecretModel, object>>
            {
                ["Secret Name"] = secret => string.IsNullOrWhiteSpace(secret.Name) ? "(unnamed secret)" : secret.Name,
                ["Expiration"] = secret => FormatDate(secret.Expires),
                ["Content Type"] = secret => string.IsNullOrWhiteSpace(secret.ContentType) ? " " : secret.ContentType
            }
        );

        _tableView.Update();
    }

    private void OnSecretSelectionChanged(object? sender, SelectedCellChangedEventArgs args)
    {
        if (!_viewModel.TrySelectByIndex(args.NewRow, out var selectedSecret) || selectedSecret is null)
        {
            return;
        }

        SecretSelected?.Invoke(selectedSecret);
    }

    private void ShowAddSecretDialog()
    {
        if (CurrentVaultName is null)
        {
            MessageBox.ErrorQuery(_app, "Error", "Please select a Key Vault first", "OK");
            return;
        }

        var dialog = new AddSecretDialog(_app);
        _app.Run(dialog);

        if (dialog.Result is null)
        {
            return;
        }

        _ = CreateSecretAsync(dialog.Result);
    }

    private async Task CreateSecretAsync(AddSecretResult addResult)
    {
        var vaultName = CurrentVaultName;
        if (vaultName is null) return;

        _app.Invoke(() => StatusChanged?.Invoke($"Creating secret '{addResult.Name}' in kv {vaultName}..."));

        var result = await _viewModel.CreateSecretAsync(vaultName, addResult.Name, addResult.Value, addResult.ContentType, addResult.ExpiresAt);

        if (result.Success)
        {
            _app.Invoke(() =>
            {
                StatusChanged?.Invoke($"Secret '{addResult.Name}' created in kv {vaultName}.");
                MessageBox.Query(_app, "Success", $"Secret '{addResult.Name}' has been created successfully!", "OK");
            });

            await RefreshAsync(vaultName);
            return;
        }

        _app.Invoke(() =>
        {
            var errorMessage = result.ErrorMessage ?? $"Failed to create secret '{addResult.Name}'";
            StatusChanged?.Invoke($"Error creating secret '{addResult.Name}': {errorMessage}");
        });
    }

    private async Task ReloadSecrets()
    {
        if (CurrentVaultName is null)
        {
            MessageBox.ErrorQuery(_app, "Error", "Please select a Key Vault first", "OK");
            return;
        }

        StatusChanged?.Invoke($"Reloading secrets for kv {CurrentVaultName}...");
        await RefreshAsync(CurrentVaultName);
    }

    private async Task RefreshAsync(string vaultName)
    {
        var refreshResult = await _viewModel.RefreshAsync(vaultName);

        _app.Invoke(() =>
        {
            if (CurrentVaultName != vaultName || refreshResult.IsStale)
            {
                StatusChanged?.Invoke("Secrets load canceled.");
                return;
            }

            if (!refreshResult.Success)
            {
                var errorMessage = refreshResult.ErrorMessage ?? "Unknown error";
                StatusChanged?.Invoke($"Error loading secrets for kv {vaultName}: {errorMessage}");
                return;
            }

            // StateChanged already fired from VM → RenderFromViewModel handled the table update.
            StatusChanged?.Invoke($"Secrets loaded for kv {vaultName}. ({_viewModel.AllSecrets.Count} secret(s))");
        });
    }

    private static string FormatDate(DateTime? dateTime)
    {
        return dateTime?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? " ";
    }
}
