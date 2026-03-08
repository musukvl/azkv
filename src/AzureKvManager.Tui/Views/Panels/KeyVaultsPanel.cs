using System.Collections.ObjectModel;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Models;
using AzureKvManager.Tui.ViewModels;

namespace AzureKvManager.Tui.Views.Panels;

public sealed class KeyVaultsPanel : FrameView
{
    private readonly IApplication _app;
    private readonly KeyVaultsViewModel _viewModel;
    private readonly TextField _filterField;
    private readonly ListView _listView;

    public event Action<KeyVault>? KeyVaultSelected;
    public event Action<string>? StatusChanged;

    public KeyVaultsPanel(IApplication app, KeyVaultsViewModel viewModel)
    {
        _app = app;
        _viewModel = viewModel;

        Title = "Key Vaults";

        var filterLabel = new Label
        {
            Text = "Filter:",
            X = 0,
            Y = 0,
            Width = 7
        };

        _filterField = new TextField
        {
            X = Pos.Right(filterLabel),
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };

        _listView = new ListView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _filterField.TextChanged += (s, e) => FilterKeyVaults();
        _listView.ValueChanged += OnKeyVaultSelectionChanged;

        Add(filterLabel, _filterField, _listView);
    }

    public void SetInitialFilter(string filter)
    {
        _filterField.TextChanged -= null!;
        _filterField.Text = filter;
    }

    public async void RefreshKeyVaults()
    {
        _app.Invoke(() =>
        {
            StatusChanged?.Invoke("Loading Key Vaults...");
            _listView.SetSource(new ObservableCollection<string> { "Loading..." });
        });

        var result = await _viewModel.RefreshAsync();

        _app.Invoke(() =>
        {
            if (!result.Success)
            {
                if (result.IsStale)
                {
                    return;
                }

                var errorMessage = result.ErrorMessage ?? "Unknown error";
                StatusChanged?.Invoke($"Error: {errorMessage}");
                MessageBox.ErrorQuery(_app, "Error", $"Failed to load Key Vaults: {errorMessage}", "OK");
                return;
            }

            _viewModel.ApplyFilter(_filterField.Text?.ToString());
            RenderKeyVaultList();

            if (_viewModel.AllKeyVaults.Count == 0)
            {
                _listView.SetSource(new ObservableCollection<string> { "No Key Vaults found" });
                StatusChanged?.Invoke("No Key Vaults found");
            }
        });
    }

    private void FilterKeyVaults()
    {
        _viewModel.ApplyFilter(_filterField.Text?.ToString());
        RenderKeyVaultList();
    }

    private void RenderKeyVaultList()
    {
        var filteredKeyVaults = _viewModel.FilteredKeyVaults;
        var totalKeyVaults = _viewModel.AllKeyVaults.Count;

        _listView.SetSource(new ObservableCollection<string>(
            filteredKeyVaults.Select(kv => $"{kv.Name} ({kv.ResourceGroup})")
        ));

        if (totalKeyVaults == 0)
        {
            StatusChanged?.Invoke("No Key Vaults found");
            return;
        }

        StatusChanged?.Invoke(filteredKeyVaults.Count > 0
            ? $"Showing {filteredKeyVaults.Count} of {totalKeyVaults} Key Vault(s)"
            : $"No matches found (total: {totalKeyVaults})");
    }

    private void OnKeyVaultSelectionChanged(object? sender, ValueChangedEventArgs<int?> args)
    {
        if (!args.NewValue.HasValue)
        {
            return;
        }

        if (!_viewModel.TrySelectByIndex(args.NewValue.Value, out var selectedKeyVault) ||
            selectedKeyVault is null)
        {
            return;
        }

        KeyVaultSelected?.Invoke(selectedKeyVault);
    }
}
