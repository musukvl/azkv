using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Models;
using AzureKvManager.Tui.ViewModels;

namespace AzureKvManager.Tui.Views.Panels;

public sealed class SecretDetailsPanel : View
{
    private readonly IApplication _app;
    private readonly SecretDetailsViewModel _viewModel;
    private readonly Button _copyButton;
    private readonly FrameView _actionsFrame;
    private readonly FrameView _contentTypeFrame;
    private readonly FrameView _expirationFrame;
    private readonly FrameView _valueFrame;
    private readonly TextView _contentTypeView;
    private readonly Label _expirationLabel;
    private readonly TextView _valueView;

    public event Action? AddVersionRequested;
    public event Action<string>? StatusChanged;

    public SecretDetailsPanel(IApplication app, SecretDetailsViewModel viewModel)
    {
        _app = app;
        _viewModel = viewModel;

        TabStop = TabBehavior.TabGroup;

        _actionsFrame = new FrameView
        {
            Title = "Actions",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 3,
            TabStop = TabBehavior.TabGroup
        };

        var addVersionButton = new Button
        {
            Text = "New _Version",
            X = 0,
            Y = 0,
            TabStop = TabBehavior.TabStop
        };
        addVersionButton.Accepted += (s, e) => AddVersionRequested?.Invoke();

        _copyButton = new Button
        {
            Text = "Copy _Value",
            X = Pos.Right(addVersionButton) + 1,
            Y = 0,
            Enabled = false,
            TabStop = TabBehavior.TabStop
        };
        _copyButton.Accepted += (s, e) => CopySecretValue();

        _actionsFrame.Add(addVersionButton, _copyButton);

        _contentTypeFrame = new FrameView
        {
            Title = "Content Type",
            X = 0,
            Y = Pos.Bottom(_actionsFrame),
            Width = Dim.Fill(),
            Height = Dim.Percent(15),
            TabStop = TabBehavior.TabGroup
        };

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

        _contentTypeFrame.Add(_contentTypeView);

        _expirationFrame = new FrameView
        {
            Title = "Expiration Date",
            X = 0,
            Y = Pos.Bottom(_contentTypeFrame),
            Width = Dim.Fill(),
            Height = 3,
            TabStop = TabBehavior.TabGroup
        };

        _expirationLabel = new Label
        {
            Text = "Expiration: (select a version)",
            X = 0,
            Y = 0,
            Width = Dim.Fill()
        };

        _expirationFrame.Add(_expirationLabel);

        _valueFrame = new FrameView
        {
            Title = "Secret Value",
            X = 0,
            Y = Pos.Bottom(_expirationFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            TabStop = TabBehavior.TabGroup
        };

        _valueView = new SecretValueTextView(CopySecretValue)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            SchemeName = "Base"
        };

        _valueFrame.Add(_valueView);

        _viewModel.StateChanged += () => _app.Invoke(RenderFromViewModel);

        Add(_actionsFrame, _contentTypeFrame, _expirationFrame, _valueFrame);
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

    private sealed class SecretValueTextView : TextView
    {
        public SecretValueTextView(Action copyAction)
        {
            AddCommand(Command.Copy, () =>
            {
                copyAction();
                return true;
            });

            KeyBindings.ReplaceCommands(Key.C.WithCtrl, [Command.Copy]);
            KeyBindings.Add(Key.C.WithAlt, [Command.Copy]);
        }
    }
}
