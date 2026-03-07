using Terminal.Gui.Drawing;

namespace AzureKvManager.Tui.Themes;

public record AppTheme(string Name, Scheme Base, Scheme Dialog, Scheme Menu, Scheme Error, Scheme Runnable);
