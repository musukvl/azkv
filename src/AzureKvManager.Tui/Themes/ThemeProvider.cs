using Terminal.Gui.Configuration;

namespace AzureKvManager.Tui.Themes;

public static class ThemeProvider
{
    private const string AppName = "azkv";

    private static TuiConfigurationBuilder? _builder;

    private static TuiConfigurationBuilder Builder
    {
        get
        {
            if (_builder is null)
            {
                _builder = new TuiConfigurationBuilder(AppName);
                _builder.ApplyToStaticFacades();
            }

            return _builder;
        }
    }

    public static void Initialize()
    {
        _ = Builder;
    }

    public static IReadOnlyList<string> GetThemeNames()
    {
        return Builder.ThemeManager.ThemeNames;
    }

    public static void ApplyTheme(string themeName)
    {
        Builder.ThemeManager.SwitchTheme(themeName);
    }
}
