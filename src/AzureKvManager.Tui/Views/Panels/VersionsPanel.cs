using System.Globalization;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Models;
using AzureKvManager.Tui.ViewModels;

namespace AzureKvManager.Tui.Views.Panels;

public sealed class VersionsPanel : FrameView
{
    private readonly IApplication _app;
    private readonly VersionsViewModel _viewModel;
    private readonly TableView _tableView;

    public event Action<SecretVersion>? VersionSelected;
    public event Action<string>? StatusChanged;

    public VersionsPanel(IApplication app, VersionsViewModel viewModel)
    {
        _app = app;
        _viewModel = viewModel;

        Title = "Secret Versions";
        TabStop = TabBehavior.TabGroup;

        _tableView = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            TabStop = TabBehavior.TabStop
        };

        _tableView.FullRowSelect = true;
        _tableView.MultiSelect = false;
        _tableView.Style.ShowVerticalCellLines = false;
        _tableView.Style.ShowVerticalHeaderLines = false;
        _tableView.Style.ExpandLastColumn = true;

        SetTableSource([]);
        _tableView.ValueChanged += OnVersionSelectionChanged;

        _viewModel.StateChanged += () => _app.Invoke(RenderFromViewModel);

        Add(_tableView);
    }

    public void Clear()
    {
        // ClearForSecretSwitch on VM raises StateChanged → RenderFromViewModel
    }

    public async Task LoadForSecretAsync(string vaultName, string secretName)
    {
        _app.Invoke(() => StatusChanged?.Invoke($"Loading versions for secret {secretName}..."));

        var result = await _viewModel.LoadForSecretAsync(vaultName, secretName);

        _app.Invoke(() =>
        {
            if (result.IsStale)
            {
                StatusChanged?.Invoke("Versions load canceled.");
                return;
            }

            if (!result.Success)
            {
                var errorMessage = result.ErrorMessage ?? "Unknown error";
                StatusChanged?.Invoke($"Error loading versions for secret {secretName}: {errorMessage}");
                return;
            }

            // StateChanged already fired from VM → RenderFromViewModel handled the table.
            StatusChanged?.Invoke($"Versions loaded for secret {secretName}. ({_viewModel.Versions.Count} version(s))");
        });
    }

    public async Task RefreshAsync(string vaultName, string secretName)
    {
        await LoadForSecretAsync(vaultName, secretName);
    }

    private void RenderFromViewModel()
    {
        SetTableSource(_viewModel.Versions);

        // Deselect to avoid auto-selecting first row after data changes
        _tableView.ValueChanged -= OnVersionSelectionChanged;
        _tableView.Value = null!;
        _tableView.ValueChanged += OnVersionSelectionChanged;
    }

    private void SetTableSource(IEnumerable<SecretVersion> versions)
    {
        var snapshot = versions.ToArray();

        _tableView.Table = new EnumerableTableSource<SecretVersion>(
            snapshot,
            new Dictionary<string, Func<SecretVersion, object>>
            {
                ["Version"] = version => ShortVersion(version.Version),
                ["Created"] = version => FormatDate(version.Created),
                ["Expires"] = version => FormatDate(version.Expires)
            }
        );

        _tableView.Update();
    }

    private void OnVersionSelectionChanged(object? sender, ValueChangedEventArgs<TableSelection?> args)
    {
        if (!_viewModel.TrySelectByIndex(args.NewValue?.Cursor.Y ?? -1, out var selectedVersion) || selectedVersion is null)
        {
            return;
        }

        VersionSelected?.Invoke(selectedVersion);
    }

    private static string ShortVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return " ";
        }

        return version.Substring(0, Math.Min(8, version.Length));
    }

    private static string FormatDate(DateTime? dateTime)
    {
        return dateTime?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? " ";
    }
}
