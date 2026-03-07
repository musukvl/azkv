using Terminal.Gui.Configuration;

namespace AzureKvManager.Tui.Themes;

public static class ThemeProvider
{
    public static void Initialize()
    {
        ConfigurationManager.Enable(ConfigLocations.All);
    }

    public static IReadOnlyList<string> GetThemeNames()
    {
        return ThemeManager.GetThemeNames();
    }

    public static string CurrentThemeName => ThemeManager.Theme;

    public static void ApplyTheme(string themeName)
    {
        ThemeManager.Theme = themeName;
        ConfigurationManager.Apply();
    }
}
