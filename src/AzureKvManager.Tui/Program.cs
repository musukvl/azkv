using Terminal.Gui;
using AzureKvManager.Tui.Views;
using Spectre.Console;
using System.Diagnostics;

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
            else if (!args[i].StartsWith("-"))
            {
                // First non-option argument is the filter
                filter = args[i];
            }
        }

        // Change Azure subscription if specified
        if (!string.IsNullOrEmpty(subscription))
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"az account set --subscription '{subscription}'\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        var error = process.StandardError.ReadToEnd();
                        AnsiConsole.MarkupLine($"[red]Failed to switch subscription:[/] {error}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error switching subscription:[/] {ex.Message}");
                return;
            }
        }
        
        Application.Init();
        
        try
        {
            var mainWindow = new MainWindow(filter);
            Application.Run(mainWindow);
        }
        finally
        {
            Application.Shutdown();
        }
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
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]NAVIGATION:[/]");
        AnsiConsole.MarkupLine("  [dim]Tab/Shift+Tab[/]              Move between controls");
        AnsiConsole.MarkupLine("  [dim]Arrow keys[/]                 Navigate within lists");
        AnsiConsole.MarkupLine("  [dim]Enter[/]                      Select item");
        AnsiConsole.MarkupLine("  [dim]Ctrl+Q / Alt+F4[/]            Quit application");
    }
}
