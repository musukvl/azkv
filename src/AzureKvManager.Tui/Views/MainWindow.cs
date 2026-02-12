using Terminal.Gui;
using AzureKvManager.Tui.Services;
using AzureKvManager.Tui.Models;
using System.Collections.ObjectModel;

namespace AzureKvManager.Tui.Views;

public partial class MainWindow : Window
{
    private readonly AzureCliService _azureService;
    private TextField _keyVaultFilter;
    private TextField _secretFilter;
    private ListView _keyVaultsList;
    private ListView _secretsList;
    private ListView _versionsList;
    private TextView _valueView;
    private Label _statusLabel;
    
    private List<KeyVault> _keyVaults = new();
    private List<KeyVault> _filteredKeyVaults = new();
    private List<Secret> _secrets = new();
    private List<Secret> _filteredSecrets = new();
    private List<SecretVersion> _versions = new();
    private KeyVault? _selectedKeyVault;
    private Secret? _selectedSecret;
    private string? _initialFilter;

    public MainWindow(string? initialFilter = null)
    {
        _azureService = new AzureCliService();
        _initialFilter = initialFilter;
        
        Title = "Azure Key Vault Manager (TUI)";
        
        // Create menu bar
        var menu = new MenuBar
        {
            Menus = new MenuBarItem[]
            {
                new MenuBarItem("_File", new MenuItem[]
                {
                    new MenuItem("_Refresh All", "", () => RefreshKeyVaults()),
                    new MenuItem("_Quit", "", () => Application.RequestStop())
                }),
                new MenuBarItem("_Help", new MenuItem[]
                {
                    new MenuItem("_About", "", () => ShowAbout())
                })
            }
        };
        
        Add(menu);
        
        // Create main layout
        var keyVaultsFrame = new FrameView
        {
            Title = "Key Vaults",
            X = 0,
            Y = 1,
            Width = Dim.Percent(30),
            Height = Dim.Fill(2)
        };
        
        var kvFilterLabel = new Label
        {
            Text = "Filter:",
            X = 0,
            Y = 0,
            Width = 7
        };
        
        _keyVaultFilter = new TextField
        {
            X = Pos.Right(kvFilterLabel),
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };
        _keyVaultFilter.TextChanged += (s, e) => FilterKeyVaults();
        
        _keyVaultsList = new ListView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            AllowsMarking = false
        };
        
        _keyVaultsList.OpenSelectedItem += OnKeyVaultSelected;
        keyVaultsFrame.Add(_keyVaultFilter, kvFilterLabel, _keyVaultsList);
        
        var secretsFrame = new FrameView
        {
            Title = "Secrets",
            X = Pos.Right(keyVaultsFrame),
            Y = 1,
            Width = Dim.Percent(50),
            Height = Dim.Fill(2)
        };
        
        var secretFilterLabel = new Label
        {
            Text = "Filter:",
            X = 0,
            Y = 0,
            Width = 7
        };
        
        _secretFilter = new TextField
        {
            X = Pos.Right(secretFilterLabel),
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };
        _secretFilter.TextChanged += (s, e) => FilterSecrets();
        
        _secretsList = new ListView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            AllowsMarking = false
        };
        
        _secretsList.OpenSelectedItem += OnSecretSelected;
        
        var addSecretButton = new Button
        {
            Text = "New _Secret",
            X = 0,
            Y = Pos.Bottom(_secretsList)
        };
        addSecretButton.Accepting += (s, e) => ShowAddSecretDialog();
        
        secretsFrame.Add(_secretFilter, secretFilterLabel, _secretsList, addSecretButton);
        
        var versionsFrame = new FrameView
        {
            Title = "Secret Details",
            X = Pos.Right(secretsFrame),
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Percent(40)
        };
        
        _versionsList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(4),
            AllowsMarking = false
        };
        
        _versionsList.OpenSelectedItem += OnVersionSelected;
        
        var addVersionButton = new Button
        {
            Text = "New _Version",
            X = 0,
            Y = Pos.Bottom(_versionsList)
        };
        addVersionButton.Accepting += (s, e) => ShowAddVersionDialog();
        
        var copyButton = new Button
        {
            Text = "Copy _Value",
            X = Pos.Right(addVersionButton) + 1,
            Y = Pos.Bottom(_versionsList),
            Enabled = false
        };
        copyButton.Accepting += (s, e) => CopySecretValue();
        
        versionsFrame.Add(_versionsList, addVersionButton, copyButton);
        
        var valueFrame = new FrameView
        {
            Title = "Secret Value",
            X = Pos.Right(secretsFrame),
            Y = Pos.Bottom(versionsFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill(2)
        };
        
        _valueView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        
        _valueView.KeyDown += (s, e) =>
        {
            // Ctrl+C for copy (also works on Mac as terminals often translate Cmd+C to Ctrl+C)
            // Alt+C as alternative for Mac users
            if (e.KeyCode == (KeyCode.C | KeyCode.CtrlMask) || 
                e.KeyCode == (KeyCode.C | KeyCode.AltMask))
            {
                CopySecretValue();
                e.Handled = true;
            }
        };
        
        valueFrame.Add(_valueView);
        
        _statusLabel = new Label
        {
            Text = "Loading Key Vaults...",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill()
        };
        
        Add(keyVaultsFrame, secretsFrame, versionsFrame, valueFrame, _statusLabel);
        
        // Apply initial filter if provided
        if (!string.IsNullOrEmpty(_initialFilter))
        {
            _keyVaultFilter.Text = _initialFilter;
        }
        
        // Load key vaults on startup
        Task.Run(RefreshKeyVaults);
    }

    private void CopySecretValue()
    {
        if (!string.IsNullOrEmpty(_valueView.Text?.ToString()))
        {
            Clipboard.Contents = _valueView.Text;
            _statusLabel.Text = "Secret value copied to clipboard!";
        }
    }

    private void ShowAbout()
    {
        MessageBox.Query("About", 
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
}
