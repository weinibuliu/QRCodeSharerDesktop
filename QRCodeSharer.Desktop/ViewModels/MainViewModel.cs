using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRCodeSharer.Desktop.Models;
using QRCodeSharer.Desktop.Services;
using QRCoder;

namespace QRCodeSharer.Desktop.ViewModels;

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
    [ObservableProperty] private string _downloadedContent = "";
    [ObservableProperty] private Bitmap? _qrCodeImage;
    [ObservableProperty] private Bitmap? _placeholderImage;
    [ObservableProperty] private bool _isPolling;
    [ObservableProperty] private bool _hasContent;
    [ObservableProperty] private string _lastUpdateTime = "";
    [ObservableProperty] private string _serverUrlError = "";
    [ObservableProperty] private string _pollIntervalError = "";
    [ObservableProperty] private int _qrCodeSize = 250;
    [ObservableProperty] private bool _isBusy;

    public bool CanEditSettings => !IsPolling && !IsBusy;

    public MainViewModel()
    {
        var settings = AppSettings.Load();
        ServerUrl = settings.ServerUrl;
        UserId = settings.UserId.ToString();
        AuthKey = settings.AuthKey;
        FollowUserId = settings.FollowUserId.ToString();
        PollInterval = settings.PollInterval.ToString();
        Timeout = settings.Timeout.ToString();
        QrCodeSize = settings.QrCodeSize;
        
        GeneratePlaceholder();
    }

    private void GeneratePlaceholder()
    {
        var uuid = Guid.NewGuid().ToString();
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(uuid, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = qrCode.GetGraphic(10);
        using var stream = new MemoryStream(pngBytes);
        PlaceholderImage = new Bitmap(stream);
    }

    private AppSettings GetSettings() => new()
    {
        ServerUrl = ServerUrl,
        UserId = int.TryParse(UserId, out var id) ? id : 0,
        AuthKey = AuthKey,
        FollowUserId = int.TryParse(FollowUserId, out var fid) ? fid : 0,
        PollInterval = int.TryParse(PollInterval, out var pi) ? pi : 500,
        Timeout = int.TryParse(Timeout, out var t) ? t : 5000,
        QrCodeSize = QrCodeSize
    };

    private bool ValidateServerUrl()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            ServerUrlError = "";
            return true;
        }
        if (!ServerUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !ServerUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            ServerUrlError = "必须以 http:// 或 https:// 开头";
            return false;
        }
        ServerUrlError = "";
        return true;
    }

    private bool ValidatePollInterval()
    {
        if (!int.TryParse(PollInterval, out var val) || val <= 0)
        {
            PollIntervalError = "必须为正整数";
            return false;
        }
        PollIntervalError = "";
        return true;
    }

    public void SaveSettings()
    {
        ValidateServerUrl();
        ValidatePollInterval();
        GetSettings().Save();
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (!ValidateServerUrl())
        {
            Status = "服务器地址格式错误";
            return;
        }
        IsBusy = true;
        OnPropertyChanged(nameof(CanEditSettings));
        try
        {
            var (_, msg) = await _service.TestConnectionAsync(GetSettings());
            Status = msg;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanEditSettings));
        }
    }

    [RelayCommand]
    private async Task CopyContentAsync()
    {
        if (string.IsNullOrEmpty(DownloadedContent)) return;
        var clipboard = Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.Clipboard
            : null;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(DownloadedContent);
            Status = "已复制到剪贴板";
        }
    }

    partial void OnQrCodeSizeChanged(int value)
    {
        SaveSettings();
    }

    [RelayCommand]
    private void TogglePolling()
    {
        if (IsPolling)
        {
            StopPolling();
        }
        else
        {
            if (!ValidateServerUrl() || !ValidatePollInterval())
            {
                Status = "请检查设置";
                return;
            }
            StartPolling();
        }
    }

    private void StartPolling()
    {
        _pollCts = new CancellationTokenSource();
        IsPolling = true;
        OnPropertyChanged(nameof(CanEditSettings));
        Status = "同步已启动";
        _ = PollLoopAsync(_pollCts.Token);
    }

    private void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts = null;
        IsPolling = false;
        OnPropertyChanged(nameof(CanEditSettings));
        Status = "同步已停止";
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        var intervalMs = int.TryParse(PollInterval, out var i) ? i : 5000;
        
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var (success, content, _, elapsedMs) = await _service.DownloadAsync(GetSettings());
                if (success && !string.IsNullOrEmpty(content) && content != DownloadedContent)
                {
                    DownloadedContent = content;
                    GenerateQRCode(content);
                    HasContent = true;
                    LastUpdateTime = $"更新于 {DateTime.Now:HH:mm:ss} ({elapsedMs}ms)";
                    Status = "同步中...";
                }
                
                // 补足间隔时间
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
                Status = "同步出错，将重试...";
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
        using var stream = new MemoryStream(pngBytes);
        QrCodeImage = new Bitmap(stream);
    }
}
