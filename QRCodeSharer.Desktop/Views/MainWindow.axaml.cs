using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using QRCodeSharer.Desktop.ViewModels;

namespace QRCodeSharer.Desktop.Views;

public partial class MainWindow : Window
{
    public static readonly StyledProperty<bool> ShowLogPanelProperty =
        AvaloniaProperty.Register<MainWindow, bool>(nameof(ShowLogPanel), defaultValue: false);

    public bool ShowLogPanel
    {
        get => GetValue(ShowLogPanelProperty);
        set => SetValue(ShowLogPanelProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        
        this.GetObservable(BoundsProperty).Subscribe(bounds =>
        {
            ShowLogPanel = bounds.Width >= 950;
        });
    }

    private void OnSettingChanged(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SaveSettings();
        }
    }
}
