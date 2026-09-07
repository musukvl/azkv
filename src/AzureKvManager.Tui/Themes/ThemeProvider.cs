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

                // Terminal.Gui 2.4.x still keeps the runtime theme/scheme dictionary in the legacy
                // ConfigurationManager; without Enable() only the "Default" theme is discoverable.
                // Drop this once 2.5.0 (which removes ConfigurationManager) is adopted.
#pragma warning disable CS0618
                ConfigurationManager.Enable(ConfigLocations.All);
#pragma warning restore CS0618
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
