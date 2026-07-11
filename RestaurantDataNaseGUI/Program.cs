using Avalonia;
using System;

namespace RestaurantDataNaseGUI;

sealed class Program
{
    // Cod de initializare. Nu folositi API-uri Avalonia inainte de AppMain.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Configurare Avalonia, nu stergeti; necesara pentru designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}