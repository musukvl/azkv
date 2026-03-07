using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace AzureKvManager.Tui.Themes;

public static class ThemeProvider
{
    public const string DefaultThemeName = "default";

    public static IReadOnlyList<string> ThemeNames { get; } = [DefaultThemeName, "grayscale", "far-blue", "matrix"];

    public static string CurrentThemeName { get; private set; } = DefaultThemeName;

    public static AppTheme? GetTheme(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "grayscale" => CreateGrayscaleTheme(),
            "far-blue" => CreateFarBlueTheme(),
            "matrix" => CreateMatrixTheme(),
            _ => null
        };
    }

    private static Dictionary<string, Scheme>? _defaultSchemes;

    public static void SaveDefaults()
    {
        _defaultSchemes = new Dictionary<string, Scheme>
        {
            ["Base"] = new Scheme(SchemeManager.GetScheme(Schemes.Base)),
            ["Dialog"] = new Scheme(SchemeManager.GetScheme(Schemes.Dialog)),
            ["Menu"] = new Scheme(SchemeManager.GetScheme(Schemes.Menu)),
            ["Error"] = new Scheme(SchemeManager.GetScheme(Schemes.Error)),
            ["Runnable"] = new Scheme(SchemeManager.GetScheme(Schemes.Runnable))
        };
    }

    public static void ApplyTheme(string themeName)
    {
        var theme = GetTheme(themeName);
        CurrentThemeName = theme != null ? themeName.ToLowerInvariant() : DefaultThemeName;

        var schemes = SchemeManager.Schemes;

        if (theme == null)
        {
            if (_defaultSchemes != null && schemes != null)
            {
                foreach (var kvp in _defaultSchemes)
                {
                    schemes[kvp.Key] = new Scheme(kvp.Value);
                }
            }
            return;
        }

        if (schemes == null)
        {
            return;
        }

        schemes["Base"] = theme.Base;
        schemes["Dialog"] = theme.Dialog;
        schemes["Menu"] = theme.Menu;
        schemes["Error"] = theme.Error;
        schemes["Runnable"] = theme.Runnable;
    }

    private static AppTheme CreateGrayscaleTheme()
    {
        var baseScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.White, ColorName16.Black),
            Focus = new Attribute(ColorName16.Black, ColorName16.White),
            HotNormal = new Attribute(ColorName16.Gray, ColorName16.Black),
            HotFocus = new Attribute(ColorName16.DarkGray, ColorName16.White),
            Active = new Attribute(ColorName16.White, ColorName16.DarkGray),
            HotActive = new Attribute(ColorName16.BrightYellow, ColorName16.DarkGray),
            Highlight = new Attribute(ColorName16.Black, ColorName16.Gray),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.Black)
        };

        var dialogScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.Black, ColorName16.Gray),
            Focus = new Attribute(ColorName16.White, ColorName16.DarkGray),
            HotNormal = new Attribute(ColorName16.DarkGray, ColorName16.Gray),
            HotFocus = new Attribute(ColorName16.White, ColorName16.DarkGray),
            Active = new Attribute(ColorName16.Black, ColorName16.White),
            HotActive = new Attribute(ColorName16.DarkGray, ColorName16.White),
            Highlight = new Attribute(ColorName16.White, ColorName16.DarkGray),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.Gray)
        };

        var menuScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.Black, ColorName16.White),
            Focus = new Attribute(ColorName16.White, ColorName16.Black),
            HotNormal = new Attribute(ColorName16.DarkGray, ColorName16.White),
            HotFocus = new Attribute(ColorName16.Gray, ColorName16.Black),
            Active = new Attribute(ColorName16.Black, ColorName16.Gray),
            HotActive = new Attribute(ColorName16.DarkGray, ColorName16.Gray),
            Highlight = new Attribute(ColorName16.White, ColorName16.DarkGray),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.White)
        };

        var errorScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.White, ColorName16.DarkGray),
            Focus = new Attribute(ColorName16.Black, ColorName16.White),
            HotNormal = new Attribute(ColorName16.Gray, ColorName16.DarkGray),
            HotFocus = new Attribute(ColorName16.Black, ColorName16.Gray),
            Active = new Attribute(ColorName16.White, ColorName16.Black),
            HotActive = new Attribute(ColorName16.Gray, ColorName16.Black),
            Highlight = new Attribute(ColorName16.Black, ColorName16.White),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.DarkGray)
        };

        var runnableScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.White, ColorName16.DarkGray),
            Focus = new Attribute(ColorName16.Black, ColorName16.White),
            HotNormal = new Attribute(ColorName16.Gray, ColorName16.DarkGray),
            HotFocus = new Attribute(ColorName16.Black, ColorName16.Gray),
            Active = new Attribute(ColorName16.White, ColorName16.Black),
            HotActive = new Attribute(ColorName16.Gray, ColorName16.Black),
            Highlight = new Attribute(ColorName16.Black, ColorName16.White),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.DarkGray)
        };

        return new AppTheme("grayscale", baseScheme, dialogScheme, menuScheme, errorScheme, runnableScheme);
    }

    private static AppTheme CreateFarBlueTheme()
    {
        var baseScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.White, ColorName16.Blue),
            Focus = new Attribute(ColorName16.Black, ColorName16.Cyan),
            HotNormal = new Attribute(ColorName16.BrightYellow, ColorName16.Blue),
            HotFocus = new Attribute(ColorName16.BrightYellow, ColorName16.Cyan),
            Active = new Attribute(ColorName16.White, ColorName16.DarkGray),
            HotActive = new Attribute(ColorName16.BrightYellow, ColorName16.DarkGray),
            Highlight = new Attribute(ColorName16.Black, ColorName16.Cyan),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.Blue)
        };

        var dialogScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.Black, ColorName16.Gray),
            Focus = new Attribute(ColorName16.Blue, ColorName16.Cyan),
            HotNormal = new Attribute(ColorName16.DarkGray, ColorName16.Gray),
            HotFocus = new Attribute(ColorName16.Blue, ColorName16.Cyan),
            Active = new Attribute(ColorName16.Black, ColorName16.White),
            HotActive = new Attribute(ColorName16.DarkGray, ColorName16.White),
            Highlight = new Attribute(ColorName16.Blue, ColorName16.Cyan),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.Gray)
        };

        var menuScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.White, ColorName16.Blue),
            Focus = new Attribute(ColorName16.Black, ColorName16.Cyan),
            HotNormal = new Attribute(ColorName16.BrightYellow, ColorName16.Blue),
            HotFocus = new Attribute(ColorName16.BrightYellow, ColorName16.Cyan),
            Active = new Attribute(ColorName16.White, ColorName16.DarkGray),
            HotActive = new Attribute(ColorName16.BrightYellow, ColorName16.DarkGray),
            Highlight = new Attribute(ColorName16.Black, ColorName16.Cyan),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.Blue)
        };

        var errorScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.White, ColorName16.Red),
            Focus = new Attribute(ColorName16.Black, ColorName16.BrightRed),
            HotNormal = new Attribute(ColorName16.BrightYellow, ColorName16.Red),
            HotFocus = new Attribute(ColorName16.BrightYellow, ColorName16.BrightRed),
            Active = new Attribute(ColorName16.White, ColorName16.DarkGray),
            HotActive = new Attribute(ColorName16.BrightYellow, ColorName16.DarkGray),
            Highlight = new Attribute(ColorName16.Black, ColorName16.BrightRed),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.Red)
        };

        var runnableScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.White, ColorName16.Blue),
            Focus = new Attribute(ColorName16.Black, ColorName16.Cyan),
            HotNormal = new Attribute(ColorName16.BrightGreen, ColorName16.Blue),
            HotFocus = new Attribute(ColorName16.BrightGreen, ColorName16.Cyan),
            Active = new Attribute(ColorName16.White, ColorName16.DarkGray),
            HotActive = new Attribute(ColorName16.BrightGreen, ColorName16.DarkGray),
            Highlight = new Attribute(ColorName16.Black, ColorName16.Cyan),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.Blue)
        };

        return new AppTheme("far-blue", baseScheme, dialogScheme, menuScheme, errorScheme, runnableScheme);
    }

    private static AppTheme CreateMatrixTheme()
    {
        var baseScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.Green, ColorName16.Black),
            Focus = new Attribute(ColorName16.Black, ColorName16.Green),
            HotNormal = new Attribute(ColorName16.BrightGreen, ColorName16.Black),
            HotFocus = new Attribute(ColorName16.Black, ColorName16.BrightGreen),
            Active = new Attribute(ColorName16.Green, ColorName16.DarkGray),
            HotActive = new Attribute(ColorName16.BrightGreen, ColorName16.DarkGray),
            Highlight = new Attribute(ColorName16.Black, ColorName16.Green),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.Black)
        };

        var dialogScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.Green, ColorName16.Black),
            Focus = new Attribute(ColorName16.Black, ColorName16.Green),
            HotNormal = new Attribute(ColorName16.BrightGreen, ColorName16.Black),
            HotFocus = new Attribute(ColorName16.Black, ColorName16.BrightGreen),
            Active = new Attribute(ColorName16.Green, ColorName16.DarkGray),
            HotActive = new Attribute(ColorName16.BrightGreen, ColorName16.DarkGray),
            Highlight = new Attribute(ColorName16.Black, ColorName16.Green),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.Black)
        };

        var menuScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.Black, ColorName16.Green),
            Focus = new Attribute(ColorName16.Green, ColorName16.Black),
            HotNormal = new Attribute(ColorName16.Black, ColorName16.BrightGreen),
            HotFocus = new Attribute(ColorName16.BrightGreen, ColorName16.Black),
            Active = new Attribute(ColorName16.Black, ColorName16.DarkGray),
            HotActive = new Attribute(ColorName16.Green, ColorName16.DarkGray),
            Highlight = new Attribute(ColorName16.Green, ColorName16.Black),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.Green)
        };

        var errorScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.Red, ColorName16.Black),
            Focus = new Attribute(ColorName16.Black, ColorName16.Red),
            HotNormal = new Attribute(ColorName16.BrightRed, ColorName16.Black),
            HotFocus = new Attribute(ColorName16.Black, ColorName16.BrightRed),
            Active = new Attribute(ColorName16.Red, ColorName16.DarkGray),
            HotActive = new Attribute(ColorName16.BrightRed, ColorName16.DarkGray),
            Highlight = new Attribute(ColorName16.Black, ColorName16.Red),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.Black)
        };

        var runnableScheme = new Scheme
        {
            Normal = new Attribute(ColorName16.BrightGreen, ColorName16.Black),
            Focus = new Attribute(ColorName16.Black, ColorName16.BrightGreen),
            HotNormal = new Attribute(ColorName16.Green, ColorName16.Black),
            HotFocus = new Attribute(ColorName16.Black, ColorName16.Green),
            Active = new Attribute(ColorName16.BrightGreen, ColorName16.DarkGray),
            HotActive = new Attribute(ColorName16.Green, ColorName16.DarkGray),
            Highlight = new Attribute(ColorName16.Black, ColorName16.BrightGreen),
            Disabled = new Attribute(ColorName16.DarkGray, ColorName16.Black)
        };

        return new AppTheme("matrix", baseScheme, dialogScheme, menuScheme, errorScheme, runnableScheme);
    }
}
