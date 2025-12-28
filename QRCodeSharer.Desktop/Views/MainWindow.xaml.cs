using System;
using System.Windows;
using QRCodeSharer.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace QRCodeSharer.Desktop.Views;

public partial class MainWindow : FluentWindow
{
    private const double FixedWidth = 850;
    private const double FixedHeight = 620;

    public MainWindow()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        StateChanged += OnWindowStateChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // 非最大化时强制固定尺寸
        if (WindowState != WindowState.Maximized)
        {
            if (Width != FixedWidth) Width = FixedWidth;
            if (Height != FixedHeight) Height = FixedHeight;
        }
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowLogPanel = WindowState == WindowState.Maximized;
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (DataContext is MainViewModel vm)
        {
            vm.ShowLogPanel = WindowState == WindowState.Maximized;
        }
    }

    private void OnSettingChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SaveSettings();
        }
    }
}
