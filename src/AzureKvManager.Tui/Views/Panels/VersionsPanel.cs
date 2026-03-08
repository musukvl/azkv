using System.Globalization;
using Terminal.Gui;
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

        _tableView = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _tableView.FullRowSelect = true;
        _tableView.MultiSelect = false;
        _tableView.Style.ShowVerticalCellLines = false;
        _tableView.Style.ShowVerticalHeaderLines = false;
        _tableView.Style.ExpandLastColumn = true;

        SetTableSource([]);
        _tableView.SelectedCellChanged += OnVersionSelectionChanged;

        Add(_tableView);
    }

    public void Clear()
    {
        SetTableSource([]);
    }

    public async Task LoadForSecretAsync(string vaultName, string secretName)
    {
        _app.Invoke(() =>
        {
            StatusChanged?.Invoke($"Loading versions for {secretName}...");
            SetTableSource([]);
        });

        var result = await _viewModel.LoadForSecretAsync(vaultName, secretName);

        _app.Invoke(() =>
        {
            if (result.IsStale)
            {
                return;
            }

            if (!result.Success)
            {
                var errorMessage = result.ErrorMessage ?? "Unknown error";
                StatusChanged?.Invoke($"Error: {errorMessage}");
                MessageBox.ErrorQuery(_app, "Error", $"Failed to load versions: {errorMessage}", "OK");
                return;
            }

            SetTableSource(_viewModel.Versions);

            if (_viewModel.Versions.Count > 0)
            {
                StatusChanged?.Invoke($"Loaded {_viewModel.Versions.Count} version(s) for {secretName}");

                _tableView.SelectedCellChanged -= OnVersionSelectionChanged;
                _tableView.SelectedRow = -1;
                _tableView.SelectedCellChanged += OnVersionSelectionChanged;
            }
            else
            {
                StatusChanged?.Invoke($"No versions for {secretName}");
            }
        });
    }

    public async Task RefreshAsync(string vaultName, string secretName)
    {
        await LoadForSecretAsync(vaultName, secretName);
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

    private void OnVersionSelectionChanged(object? sender, SelectedCellChangedEventArgs args)
    {
        if (!_viewModel.TrySelectByIndex(args.NewRow, out var selectedVersion) || selectedVersion is null)
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
