using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Services;
using AzureKvManager.Tui.Models;
using System.Collections.ObjectModel;
using System.Globalization;

namespace AzureKvManager.Tui.Views;

public partial class MainWindow : Window
{
    private readonly AzureCliService _azureService;
    private TextField _keyVaultFilter;
    private TextField _secretFilter;
    private ListView _keyVaultsList;
    private ListView _secretsList;
    private TableView _versionsTable;
    private Button _copyButton;
    private TextView _contentTypeView;
    private Label _expirationLabel;
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
    private bool _suppressVersionSelectionEvent;
    private string? _selectedVersionId;

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
            Width = Dim.Percent(25),
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
            Height = Dim.Fill()
        };
        
        _keyVaultsList.ValueChanged += OnKeyVaultSelectionChanged;
        keyVaultsFrame.Add(_keyVaultFilter, kvFilterLabel, _keyVaultsList);
        
        var secretsFrame = new FrameView
        {
            Title = "Secrets",
            X = Pos.Right(keyVaultsFrame),
            Y = 1,
            Width = Dim.Percent(40),
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
            Height = Dim.Fill(1)
        };
        
        _secretsList.ValueChanged += OnSecretSelectionChanged;
        
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
            Title = "Secret Versions",
            X = Pos.Right(secretsFrame),
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Percent(35)
        };
        
        _versionsTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _versionsTable.FullRowSelect = true;
        _versionsTable.MultiSelect = false;
        _versionsTable.Style.ShowVerticalCellLines = false;
        _versionsTable.Style.ShowVerticalHeaderLines = false;
        _versionsTable.Style.ExpandLastColumn = true;

        SetVersionsTableSource(_versions);
        
        _versionsTable.SelectedCellChanged += OnVersionSelectionChanged;

        versionsFrame.Add(_versionsTable);

        var actionsFrame = new FrameView
        {
            Title = "Actions",
            X = Pos.Right(secretsFrame),
            Y = Pos.Bottom(versionsFrame),
            Width = Dim.Fill(),
            Height = 3
        };
        
        var addVersionButton = new Button
        {
            Text = "New _Version",
            X = 0,
            Y = 0
        };
        addVersionButton.Accepting += (s, e) => ShowAddVersionDialog();
        
        _copyButton = new Button
        {
            Text = "Copy _Value",
            X = Pos.Right(addVersionButton) + 1,
            Y = 0,
            Enabled = false
        };
        _copyButton.Accepting += (s, e) => CopySecretValue();

        actionsFrame.Add(addVersionButton, _copyButton);

        var contentTypeFrame = new FrameView
        {
            Title = "Content Type",
            X = Pos.Right(secretsFrame),
            Y = Pos.Bottom(actionsFrame),
            Width = Dim.Fill(),
            Height = Dim.Percent(15)
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

        ApplyReadableTextScheme(_contentTypeView);

        contentTypeFrame.Add(_contentTypeView);

        var expirationFrame = new FrameView
        {
            Title = "Expiration Date",
            X = Pos.Right(secretsFrame),
            Y = Pos.Bottom(contentTypeFrame),
            Width = Dim.Fill(),
            Height = 3
        };

        _expirationLabel = new Label
        {
            Text = "Expiration: (select a version)",
            X = 0,
            Y = 0,
            Width = Dim.Fill()
        };

        expirationFrame.Add(_expirationLabel);
        
        var valueFrame = new FrameView
        {
            Title = "Secret Value",
            X = Pos.Right(secretsFrame),
            Y = Pos.Bottom(expirationFrame),
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
            WordWrap = true,
            SchemeName = "Base"
        };

        ApplyReadableTextScheme(_valueView);
        
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

        ClearVersionSelectionDetails();
        
        _statusLabel = new Label
        {
            Text = "Loading Key Vaults...",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill()
        };
        
        Add(keyVaultsFrame, secretsFrame, versionsFrame, actionsFrame, contentTypeFrame, expirationFrame, valueFrame, _statusLabel);
        
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
            Application.Clipboard.SetClipboardData(_valueView.Text.ToString()!);
            _statusLabel.Text = "Secret value copied to clipboard!";
        }
    }

    private void ShowAbout()
    {
        MessageBox.Query(Application.Instance, "About", 
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

    private void SetVersionsTableSource(IEnumerable<SecretVersion> versions)
    {
        var snapshot = versions.ToArray();

        _versionsTable.Table = new EnumerableTableSource<SecretVersion>(
            snapshot,
            new Dictionary<string, Func<SecretVersion, object>>
            {
                ["Version"] = version => ShortVersion(version.Version),
                ["Created"] = version => FormatVersionDate(version.Created),
                ["Expires"] = version => FormatVersionDate(version.Expires)
            }
        );

        _versionsTable.Update();
    }

    private static string ShortVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "-";
        }

        return version.Substring(0, Math.Min(8, version.Length));
    }

    private static string FormatVersionDate(DateTime? dateTime)
    {
        return dateTime?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
    }

    private static string FormatVersionDateTime(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    private static DateTime GetVersionSortKey(SecretVersion version)
    {
        return version.Created ?? version.Updated ?? DateTime.MinValue;
    }

    private static void ApplyReadableTextScheme(TextView textView)
    {
        var currentScheme = textView.GetScheme();

        if (currentScheme is null)
        {
            return;
        }

        textView.SetScheme(new Terminal.Gui.Drawing.Scheme(currentScheme)
        {
            Editable = currentScheme.Normal,
            ReadOnly = currentScheme.Normal
        });
    }

    private string BuildExpirationDetailsText(DateTime? expiresAt)
    {
        if (!expiresAt.HasValue)
        {
            return "Expiration: (not set)";
        }

        var formatted = FormatVersionDateTime(expiresAt.Value);
        return IsExpired(expiresAt.Value)
            ? $"Expiration: ⚠ EXPIRED ({formatted})"
            : $"Expiration: {formatted}";
    }

    private void ClearVersionSelectionDetails(bool clearValue = true)
    {
        _selectedVersionId = null;
        _contentTypeView.Text = string.Empty;
        _expirationLabel.Text = "Expiration: (select a version)";

        if (clearValue)
        {
            _valueView.Text = string.Empty;
        }

        _copyButton.Enabled = false;
    }

    private static bool IsExpired(DateTime expiresAt)
    {
        var expiresUtc = expiresAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc)
            : expiresAt.ToUniversalTime();

        return expiresUtc < DateTime.UtcNow;
    }

    private static bool TryParseExpirationDate(string? input, out DateTime? expiresAt)
    {
        expiresAt = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        if (!DateTime.TryParseExact(
                input.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            return false;
        }

        expiresAt = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        return true;
    }
}
