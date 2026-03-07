using Terminal.Gui;
using Terminal.Gui.App;
using AzureKvManager.Tui.Views;
using AzureKvManager.Tui.Services;
using AzureKvManager.Tui.ViewModels;
using AzureKvManager.Tui.Themes;
using Spectre.Console;

namespace AzureKvManager.Tui;

class Program
{
    static void Main(string[] args)
    {
        // Check for help flag first
        if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
        {
            ShowHelp();
            return;
        }
        
        string? subscription = null;
        string? filter = null;
        string? themeName = null;

        // Parse command-line arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-s" || args[i] == "--subscription")
            {
                if (i + 1 < args.Length)
                {
                    subscription = args[++i];
                }
            }
            else if (args[i] == "-t" || args[i] == "--theme")
            {
                if (i + 1 < args.Length)
                {
                    themeName = args[++i];
                }
            }
            else if (!args[i].StartsWith("-"))
            {
                // First non-option argument is the filter
                filter = args[i];
            }
        }

        // Change Azure subscription if specified
        if (!string.IsNullOrEmpty(subscription))
        {
            var subscriptionService = new SubscriptionService();
            if (!subscriptionService.TrySetSubscription(subscription, out var errorMessage))
            {
                AnsiConsole.MarkupLine($"[red]Failed to switch subscription:[/] {errorMessage}");
                return;
            }
        }
        
        var azureService = new AzureCliService();
        var mainWindowViewModel = new MainWindowViewModel(azureService);

        using IApplication app = Application.Create();
        app.Init();

        ThemeProvider.SaveDefaults();
        if (!string.IsNullOrEmpty(themeName))
        {
            ThemeProvider.ApplyTheme(themeName);
        }

        using var mainWindow = new MainWindow(app, mainWindowViewModel, filter);
        app.Run(mainWindow);
    }
    
    static void ShowHelp()
    {
        AnsiConsole.Write(
            new FigletText("Azure KV Manager")
                .LeftJustified()
                .Color(Spectre.Console.Color.Blue));
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]USAGE:[/]");
        AnsiConsole.MarkupLine("  azurekv [[OPTIONS]] [[FILTER]]");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]OPTIONS:[/]");
        AnsiConsole.MarkupLine("  [cyan]-s, --subscription[/] [grey]<NAME>[/]     Switch to the specified Azure subscription");
        AnsiConsole.MarkupLine("  [cyan]-t, --theme[/] [grey]<NAME>[/]            Set color theme (grayscale, far-blue, matrix)");
        AnsiConsole.MarkupLine("  [cyan]-h, --help[/]                          Show this help message");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]ARGUMENTS:[/]");
        AnsiConsole.MarkupLine("  [cyan]FILTER[/]                        Initial filter text for Key Vault list");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]EXAMPLES:[/]");
        AnsiConsole.MarkupLine("  azurekv                          Launch without any filters");
        AnsiConsole.MarkupLine("  azurekv bip                      Launch with 'bip' filter applied to Key Vaults");
        AnsiConsole.MarkupLine("  azurekv -s my-subscription       Switch subscription before launching");
        AnsiConsole.MarkupLine("  azurekv -s my-sub prod           Switch subscription and filter by 'prod'");
        AnsiConsole.MarkupLine("  azurekv -t far-blue              Launch with FAR blue theme");
        AnsiConsole.MarkupLine("  azurekv -t matrix                Launch with Matrix green theme");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]NAVIGATION:[/]");
        AnsiConsole.MarkupLine("  [dim]Tab/Shift+Tab[/]              Move between controls");
        AnsiConsole.MarkupLine("  [dim]Arrow keys[/]                 Navigate within lists");
        AnsiConsole.MarkupLine("  [dim]Enter[/]                      Select item");
        AnsiConsole.MarkupLine("  [dim]Ctrl+Q / Alt+F4[/]            Quit application");
    }
}
