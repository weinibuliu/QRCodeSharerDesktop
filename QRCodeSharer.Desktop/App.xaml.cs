using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui.Appearance;

namespace QRCodeSharer.Desktop;

public partial class App : Application
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 全局异常处理
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        
        ApplicationThemeManager.ApplySystemTheme();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        HandleException(e.Exception);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            HandleException(ex);
        }
    }

    private static void HandleException(Exception ex)
    {
        var msg = $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}";
        
        // 写入错误日志到 AppData
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QRCodeSharer", "logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            
            var logPath = Path.Combine(logDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            File.WriteAllText(logPath, $"{DateTime.Now}\n{ex}");
        }
        catch { }
        
        // 显示错误弹窗
        MessageBox(IntPtr.Zero, msg, "QRCodeSharer 发生错误", 0x10);
    }
}
