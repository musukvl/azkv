using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Models;
using AzureKvManager.Tui.ViewModels;
using AzureKvManager.Tui.Views.Dialogs;

namespace AzureKvManager.Tui.Views.Panels;

public sealed class SecretDetailsPanel : View
{
    private readonly IApplication _app;
    private readonly SecretDetailsViewModel _viewModel;
    private readonly Button _copyButton;
    private readonly TextView _contentTypeView;
    private readonly Label _expirationLabel;
    private readonly TextView _valueView;

    public event Action? AddVersionRequested;
    public event Action<string>? StatusChanged;

    public FrameView ActionsFrame { get; }
    public FrameView ContentTypeFrame { get; }
    public FrameView ExpirationFrame { get; }
    public FrameView ValueFrame { get; }

    public SecretDetailsPanel(IApplication app, SecretDetailsViewModel viewModel)
    {
        _app = app;
        _viewModel = viewModel;

        ActionsFrame = new FrameView { Title = "Actions", TabStop = TabBehavior.TabGroup };

        var addVersionButton = new Button
        {
            Text = "New _Version",
            X = 0,
            Y = 0,
            TabStop = TabBehavior.TabStop
        };
        addVersionButton.Accepting += (s, e) => AddVersionRequested?.Invoke();

        _copyButton = new Button
        {
            Text = "Copy _Value",
            X = Pos.Right(addVersionButton) + 1,
            Y = 0,
            Enabled = false,
            TabStop = TabBehavior.TabStop
        };
        _copyButton.Accepting += (s, e) => CopySecretValue();

        ActionsFrame.Add(addVersionButton, _copyButton);

        ContentTypeFrame = new FrameView { Title = "Content Type", TabStop = TabBehavior.TabGroup };

        _contentTypeView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            SchemeName = "Base"
        };

        ApplyReadableTextScheme(_contentTypeView);
        ContentTypeFrame.Add(_contentTypeView);

        ExpirationFrame = new FrameView { Title = "Expiration Date", TabStop = TabBehavior.TabGroup };

        _expirationLabel = new Label
        {
            Text = "Expiration: (select a version)",
            X = 0,
            Y = 0,
            Width = Dim.Fill()
        };

        ExpirationFrame.Add(_expirationLabel);

        ValueFrame = new FrameView { Title = "Secret Value", TabStop = TabBehavior.TabGroup };

        _valueView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            SchemeName = "Base"
        };

        ApplyReadableTextScheme(_valueView);
        ValueFrame.Add(_valueView);

        // Replace default Ctrl+C (text selection copy) with full secret value copy
        _valueView.KeyBindings.ReplaceCommands(Key.C.WithCtrl, [Command.Copy]);
        AddCommand(Command.Copy, () => { CopySecretValue(); return true; });
        _valueView.KeyBindings.Add(Key.C.WithAlt, [Command.Copy]);

        _viewModel.StateChanged += () => _app.Invoke(RenderFromViewModel);

        RenderFromViewModel();
    }

    public async Task LoadVersionDetailsAsync(string vaultName, string secretName, SecretVersion version)
    {
        var shortVer = version.Version.Length > 8 ? version.Version[..8] : version.Version;
        _app.Invoke(() => StatusChanged?.Invoke($"Loading version {shortVer} for secret {secretName}..."));

        // LoadForVersionAsync raises StateChanged at key points → RenderFromViewModel auto-updates UI
        var loadResult = await _viewModel.LoadForVersionAsync(vaultName, secretName, version);

        _app.Invoke(() =>
        {
            if (loadResult.IsStale)
            {
                StatusChanged?.Invoke("Secret data load canceled.");
                return;
            }

            if (!loadResult.Success)
            {
                var errorMessage = loadResult.ErrorMessage ?? "Unknown error";
                StatusChanged?.Invoke($"Error loading secret data for '{secretName}': {errorMessage}");
                return;
            }

            StatusChanged?.Invoke($"Secret {secretName} data loaded.");
        });
    }

    public void ApplyTheme()
    {
        ApplyReadableTextScheme(_contentTypeView);
        ApplyReadableTextScheme(_valueView);
    }

    private void RenderFromViewModel()
    {
        _contentTypeView.Text = _viewModel.ContentTypeText;
        _expirationLabel.Text = _viewModel.ExpirationText;
        _valueView.Text = _viewModel.ValueText;
        _copyButton.Enabled = _viewModel.CanCopy;
    }

    private void CopySecretValue()
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.CopyableValue))
        {
            if (_app.Clipboard is null)
            {
                MessageBox.ErrorQuery(_app, "Error", "Clipboard is not available.", "OK");
                return;
            }

            _app.Clipboard.SetClipboardData(_viewModel.CopyableValue);
        }
    }

    private static void ApplyReadableTextScheme(TextView textView)
    {
        var currentScheme = textView.GetScheme();

        if (currentScheme is null)
        {
            return;
        }

        textView.SetScheme(new Scheme(currentScheme)
        {
            Editable = currentScheme.Normal,
            ReadOnly = currentScheme.Normal
        });
    }
}
