using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace RGui;

sealed class RgNotFoundWindow : Window
{
    public RgNotFoundWindow()
    {
        Title = "RGui — Missing Dependency";
        CanResize = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        okButton.Click += (_, _) => Close();

        Content = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "The bundled ripgrep (rg) executable was not found " +
                           "next to RGui.\n\n" +
                           "Your installation may be incomplete — try reinstalling, " +
                           "or place the rg executable alongside RGui.",
                    Margin = new Thickness(20),
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 380
                },
                okButton
            }
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app)
        {
            app.Shutdown();
        }
    }
}
