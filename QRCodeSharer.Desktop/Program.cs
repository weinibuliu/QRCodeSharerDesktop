using Avalonia;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace QRCodeSharer.Desktop;

class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            var msg = $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}";
            
            // 写入错误日志
            try
            {
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "QRCodeSharer_crash.log");
                File.WriteAllText(logPath, $"{DateTime.Now}\n{ex}");
            }
            catch { }
            
            // 显示错误弹窗
            MessageBox(IntPtr.Zero, msg, "QRCodeSharer 启动失败", 0x10);
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
