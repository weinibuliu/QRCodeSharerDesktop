using Avalonia.Controls;
using Avalonia.Interactivity;
using QRCodeSharer.Desktop.ViewModels;

namespace QRCodeSharer.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void OnSettingChanged(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SaveSettings();
        }
    }
}
