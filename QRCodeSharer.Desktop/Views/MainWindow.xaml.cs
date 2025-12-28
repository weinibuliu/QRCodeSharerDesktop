using System.Windows;
using QRCodeSharer.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace QRCodeSharer.Desktop.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowLogPanel = e.NewSize.Width >= 950;
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
