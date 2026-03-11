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

        _listView = new ListView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            TabStop = TabBehavior.TabStop
        };

        _filterField.TextChanged += (s, e) => _viewModel.ApplyFilter(_filterField.Text?.ToString());
        _listView.ValueChanged += OnKeyVaultSelectionChanged;

        _viewModel.StateChanged += () => _app.Invoke(RenderFromViewModel);

        Add(filterLabel, _filterField, _listView);
    }

    public void SetInitialFilter(string filter)
    {
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
            if (result.IsStale)
            {
                StatusChanged?.Invoke("Key Vaults load canceled.");
                return;
            }

            if (!result.Success)
            {
                var errorMessage = result.ErrorMessage ?? "Unknown error";
                StatusChanged?.Invoke($"Error loading Key Vaults: {errorMessage}");
                MessageBox.ErrorQuery(_app, "Error", $"Failed to load Key Vaults: {errorMessage}", "OK");
                return;
            }

            // RefreshAsync calls ApplyFilter internally, which raises StateChanged → RenderFromViewModel.
            if (_viewModel.AllKeyVaults.Count == 0)
            {
                _listView.SetSource(new ObservableCollection<string> { "No Key Vaults found" });
                StatusChanged?.Invoke("Key Vaults loaded. No Key Vaults found.");
                return;
            }

            StatusChanged?.Invoke("Key Vaults loaded.");
        });
    }

    private void RenderFromViewModel()
    {
        var filteredKeyVaults = _viewModel.FilteredKeyVaults;

        _listView.SetSource(new ObservableCollection<string>(
            filteredKeyVaults.Select(kv => $"{kv.Name} ({kv.ResourceGroup})")
        ));
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
