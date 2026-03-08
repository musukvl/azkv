using Terminal.Gui.App;
using AzureKvManager.Tui.Views;
using AzureKvManager.Tui.Services;
using AzureKvManager.Tui.ViewModels;
using AzureKvManager.Tui.Themes;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureKvManager.Tui;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
        {
            ShowHelp();
            return 0;
        }

        var app = new CommandApp<LaunchCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("azkv");
        });

        return app.Run(args);
    }

    static int RunApplication(string? subscription, string? themeName, string? filter)
    {
        if (!string.IsNullOrEmpty(subscription))
        {
            var subscriptionService = new SubscriptionService();
            if (!subscriptionService.TrySetSubscription(subscription, out var errorMessage))
            {
                AnsiConsole.MarkupLine($"[red]Failed to switch subscription:[/] {errorMessage}");
                return 1;
            }
        }

        var azureService = new AzureCliService();
        var mainWindowViewModel = new MainWindowViewModel(azureService);

        ThemeProvider.Initialize();

        using IApplication app = Application.Create();
        app.Init();

        if (!string.IsNullOrEmpty(themeName))
        {
            ThemeProvider.ApplyTheme(themeName);
        }

        using var mainWindow = new MainWindow(app, mainWindowViewModel, filter);
        app.Run(mainWindow);

        return 0;
    }

    sealed class LaunchCommand : Command<LaunchSettings>
    {
        public override int Execute(CommandContext context, LaunchSettings settings, CancellationToken cancellationToken)
        {
            return RunApplication(settings.Subscription, settings.ThemeName, settings.Filter);
        }
    }

    sealed class LaunchSettings : CommandSettings
    {
        [CommandOption("-s|--subscription <NAME>")]
        public string? Subscription { get; set; }

        [CommandOption("-t|--theme <NAME>")]
        public string? ThemeName { get; set; }

        [CommandArgument(0, "[FILTER]")]
        public string? Filter { get; set; }
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
        AnsiConsole.MarkupLine("  [cyan]-t, --theme[/] [grey]<NAME>[/]            Set color theme (e.g. Dark, Light, \"Amber Phosphor\")");
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
        AnsiConsole.MarkupLine("  azurekv -t Dark                  Launch with Dark theme");
        AnsiConsole.MarkupLine("  azurekv -t \"Amber Phosphor\"      Launch with Amber Phosphor theme");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]NAVIGATION:[/]");
        AnsiConsole.MarkupLine("  [dim]Tab/Shift+Tab[/]              Move between controls");
        AnsiConsole.MarkupLine("  [dim]Arrow keys[/]                 Navigate within lists");
        AnsiConsole.MarkupLine("  [dim]Enter[/]                      Select item");
        AnsiConsole.MarkupLine("  [dim]Ctrl+Q / Alt+F4[/]            Quit application");
    }
}
