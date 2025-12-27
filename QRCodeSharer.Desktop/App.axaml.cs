using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using QRCodeSharer.Desktop.Views;

namespace QRCodeSharer.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        UpdateAccentColor();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }

    public void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateAccentColor();
    }

    private void UpdateAccentColor()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var color = GetWindowsAccentColor();
                if (color.HasValue)
                {
                    Resources["SystemAccentColor"] = color.Value;
                    Resources["SystemAccentColorLight1"] = LightenColor(color.Value, 0.15);
                    Resources["SystemAccentColorLight2"] = LightenColor(color.Value, 0.30);
                    Resources["SystemAccentColorLight3"] = LightenColor(color.Value, 0.45);
                    Resources["SystemAccentColorDark1"] = DarkenColor(color.Value, 0.15);
                    Resources["SystemAccentColorDark2"] = DarkenColor(color.Value, 0.30);
                    Resources["SystemAccentColorDark3"] = DarkenColor(color.Value, 0.45);
                }
            }
            catch { }
        }
    }

    private static Color? GetWindowsAccentColor()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int accentColor)
            {
                var a = (byte)((accentColor >> 24) & 0xFF);
                var b = (byte)((accentColor >> 16) & 0xFF);
                var g = (byte)((accentColor >> 8) & 0xFF);
                var r = (byte)(accentColor & 0xFF);
                return Color.FromArgb(255, r, g, b);
            }
        }
        catch { }
        return null;
    }

    private static Color LightenColor(Color color, double amount)
    {
        return Color.FromArgb(color.A,
            (byte)Math.Min(255, color.R + (255 - color.R) * amount),
            (byte)Math.Min(255, color.G + (255 - color.G) * amount),
            (byte)Math.Min(255, color.B + (255 - color.B) * amount));
    }

    private static Color DarkenColor(Color color, double amount)
    {
        return Color.FromArgb(color.A,
            (byte)(color.R * (1 - amount)),
            (byte)(color.G * (1 - amount)),
            (byte)(color.B * (1 - amount)));
    }
}
