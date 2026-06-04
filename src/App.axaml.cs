using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace RGui;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var rgAvailable = RGuiUtils.IsOnPath("rg") ||
                              File.Exists(Path.Combine(AppContext.BaseDirectory,
                                  OperatingSystem.IsWindows() ? "rg.exe" : "rg"));

            desktop.MainWindow = rgAvailable ? new MainWindow() : new RgNotFoundWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
