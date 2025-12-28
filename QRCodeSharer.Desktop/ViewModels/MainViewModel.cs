using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRCodeSharer.Desktop.Models;
using QRCodeSharer.Desktop.Services;
using QRCoder;
using Wpf.Ui.Controls;

namespace QRCodeSharer.Desktop.ViewModels;

public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();
    
    private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(106, 175, 106));
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(212, 112, 112));
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? SuccessBrush : ErrorBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public enum StatusType { Info, Success, Warning, Error }

public class LogEntry
{
    public string Time { get; set; } = "";
    public string Request { get; set; } = "";
    public string StatusCode { get; set; } = "";
    public string ElapsedMs { get; set; } = "";
    public bool IsSuccess { get; set; }
}

public partial class MainViewModel : ObservableObject
{
    private readonly QRCodeService _service = new();
    private CancellationTokenSource? _pollCts;
    
    [ObservableProperty] private string _serverUrl = "";
    [ObservableProperty] private string _userId = "";
    [ObservableProperty] private string _authKey = "";
    [ObservableProperty] private string _followUserId = "";
    [ObservableProperty] private string _pollInterval = "500";
    [ObservableProperty] private string _timeout = "5000";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private StatusType _statusType = StatusType.Info;
    [ObservableProperty] private string _downloadedContent = "";
    [ObservableProperty] private BitmapImage? _qrCodeImage;
    [ObservableProperty] private BitmapImage? _placeholderImage;
    [ObservableProperty] private bool _isPolling;
    [ObservableProperty] private bool _hasContent;
    [ObservableProperty] private string _lastUpdateTime = "";
    [ObservableProperty] private string _serverUrlError = "";
    [ObservableProperty] private string _pollIntervalError = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _showLogPanel;

    public ObservableCollection<LogEntry> Logs { get; } = new();
    private const int MaxLogs = 100;

    public bool CanEditSettings => !IsPolling && !IsBusy;
    public bool HasStatus => !string.IsNullOrEmpty(Status);
    public bool HasServerUrlError => !string.IsNullOrEmpty(ServerUrlError);
    public bool HasPollIntervalError => !string.IsNullOrEmpty(PollIntervalError);
    
    public InfoBarSeverity InfoBarSeverity => StatusType switch
    {
        StatusType.Success => InfoBarSeverity.Success,
        StatusType.Warning => InfoBarSeverity.Warning,
        StatusType.Error => InfoBarSeverity.Error,
        _ => InfoBarSeverity.Informational
    };

    public MainViewModel()
    {
        var settings = AppSettings.Load();
        ServerUrl = settings.ServerUrl;
        UserId = settings.UserId.ToString();
        AuthKey = settings.AuthKey;
        FollowUserId = settings.FollowUserId.ToString();
        PollInterval = settings.PollInterval.ToString();
        Timeout = settings.Timeout.ToString();
        
        GeneratePlaceholder();
    }

    private void GeneratePlaceholder()
    {
        var uuid = Guid.NewGuid().ToString();
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(uuid, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = qrCode.GetGraphic(10);
        PlaceholderImage = BytesToBitmapImage(pngBytes);
    }

    private static BitmapImage BytesToBitmapImage(byte[] bytes)
    {
        var image = new BitmapImage();
        using var stream = new MemoryStream(bytes);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private AppSettings GetSettings() => new()
    {
        ServerUrl = ServerUrl,
        UserId = int.TryParse(UserId, out var id) ? id : 0,
        AuthKey = AuthKey,
        FollowUserId = int.TryParse(FollowUserId, out var fid) ? fid : 0,
        PollInterval = int.TryParse(PollInterval, out var pi) ? pi : 500,
        Timeout = int.TryParse(Timeout, out var t) ? t : 5000
    };

    private bool ValidateServerUrl()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            ServerUrlError = "";
            OnPropertyChanged(nameof(HasServerUrlError));
            return true;
        }
        if (!ServerUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !ServerUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            ServerUrlError = "必须以 http:// 或 https:// 开头";
            OnPropertyChanged(nameof(HasServerUrlError));
            return false;
        }
        ServerUrlError = "";
        OnPropertyChanged(nameof(HasServerUrlError));
        return true;
    }

    private bool ValidatePollInterval()
    {
        if (!int.TryParse(PollInterval, out var val) || val <= 0)
        {
            PollIntervalError = "必须为正整数";
            OnPropertyChanged(nameof(HasPollIntervalError));
            return false;
        }
        PollIntervalError = "";
        OnPropertyChanged(nameof(HasPollIntervalError));
        return true;
    }

    public void SaveSettings()
    {
        ValidateServerUrl();
        ValidatePollInterval();
        GetSettings().Save();
    }

    private void SetStatus(string message, StatusType type = StatusType.Info)
    {
        Status = message;
        StatusType = type;
        OnPropertyChanged(nameof(HasStatus));
        OnPropertyChanged(nameof(InfoBarSeverity));
    }

    private void AddLog(string request, int statusCode, long elapsedMs, bool isSuccess = true)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var entry = new LogEntry
            {
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                Request = request,
                StatusCode = statusCode > 0 ? statusCode.ToString() : "ERR",
                ElapsedMs = $"{elapsedMs}ms",
                IsSuccess = isSuccess
            };
            Logs.Insert(0, entry);
            while (Logs.Count > MaxLogs) Logs.RemoveAt(Logs.Count - 1);
        });
    }

    [RelayCommand]
    private void ClearLogs() => Logs.Clear();

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (!ValidateServerUrl())
        {
            SetStatus("服务器地址格式错误", StatusType.Error);
            return;
        }
        IsBusy = true;
        OnPropertyChanged(nameof(CanEditSettings));
        try
        {
            var (success, msg, statusCode, elapsedMs) = await _service.TestConnectionAsync(GetSettings());
            AddLog("GET /", statusCode, elapsedMs, success);
            SetStatus(msg, success ? StatusType.Success : StatusType.Error);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanEditSettings));
        }
    }

    [RelayCommand]
    private void CopyContent()
    {
        if (string.IsNullOrEmpty(DownloadedContent)) return;
        Clipboard.SetText(DownloadedContent);
    }

    [RelayCommand]
    private async Task TogglePollingAsync()
    {
        if (IsPolling)
        {
            StopPolling();
        }
        else
        {
            if (!ValidateServerUrl() || !ValidatePollInterval())
            {
                SetStatus("请检查设置", StatusType.Error);
                return;
            }
            await StartPollingAsync();
        }
    }

    private async Task StartPollingAsync()
    {
        IsBusy = true;
        OnPropertyChanged(nameof(CanEditSettings));
        SetStatus("正在验证用户...", StatusType.Info);
        
        var settings = GetSettings();
        var (exists, msg, statusCode, elapsedMs) = await _service.CheckUserExistsAsync(settings, settings.FollowUserId);
        AddLog("GET /user/get", statusCode, elapsedMs, exists);
        
        if (!exists)
        {
            SetStatus(msg, StatusType.Error);
            IsBusy = false;
            OnPropertyChanged(nameof(CanEditSettings));
            return;
        }
        
        _pollCts = new CancellationTokenSource();
        IsPolling = true;
        IsBusy = false;
        OnPropertyChanged(nameof(CanEditSettings));
        SetStatus("同步已启动", StatusType.Success);
        _ = PollLoopAsync(_pollCts.Token);
    }

    private void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts = null;
        IsPolling = false;
        OnPropertyChanged(nameof(CanEditSettings));
        SetStatus("同步已停止", StatusType.Info);
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        var intervalMs = int.TryParse(PollInterval, out var i) ? i : 5000;
        
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var (success, content, msg, statusCode, elapsedMs) = await _service.DownloadAsync(GetSettings());
                AddLog("GET /code/get", statusCode, elapsedMs, success);
                
                if (success && !string.IsNullOrEmpty(content))
                {
                    if (content != DownloadedContent)
                    {
                        DownloadedContent = content;
                        GenerateQRCode(content);
                        HasContent = true;
                        LastUpdateTime = $"更新于 {DateTime.Now:HH:mm:ss}";
                        SetStatus($"内容已更新 ({elapsedMs}ms)", StatusType.Success);
                    }
                    else
                    {
                        SetStatus($"内容无变化 ({elapsedMs}ms)", StatusType.Warning);
                    }
                }
                else if (!success)
                {
                    SetStatus($"{msg} ({elapsedMs}ms)", StatusType.Error);
                }
                
                var sleepMs = intervalMs - (int)elapsedMs;
                if (sleepMs > 0)
                {
                    await Task.Delay(sleepMs, ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                SetStatus("同步出错，将重试...", StatusType.Error);
                await Task.Delay(intervalMs, ct);
            }
        }
    }

    private void GenerateQRCode(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = qrCode.GetGraphic(10);
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            QrCodeImage = BytesToBitmapImage(pngBytes);
        });
    }
}
