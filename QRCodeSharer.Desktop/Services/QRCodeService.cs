using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using QRCodeSharer.Desktop.Models;

namespace QRCodeSharer.Desktop.Services;

public class CodeResult
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("update_at")]
    public long? UpdateAt { get; set; }
}

public class QRCodeService
{
    private static readonly HttpClient _http = new(new SocketsHttpHandler
    {
        UseProxy = false,
        EnableMultipleHttp2Connections = true,
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
        ConnectTimeout = TimeSpan.FromSeconds(10)
    });

    public async Task<(bool success, string message)> TestConnectionAsync(AppSettings settings)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(settings.Timeout));
            var url = $"{settings.ServerUrl.TrimEnd('/')}/?id={settings.UserId}&auth={Uri.EscapeDataString(settings.AuthKey)}";
            var response = await _http.GetAsync(url, cts.Token);
            sw.Stop();
            return response.IsSuccessStatusCode 
                ? (true, $"连接成功 ({sw.ElapsedMilliseconds}ms)") 
                : (false, $"连接失败: {response.StatusCode}");
        }
        catch (OperationCanceledException)
        {
            return (false, "连接超时");
        }
        catch (Exception ex)
        {
            return (false, $"连接错误: {ex.Message}");
        }
    }

    public async Task<(bool success, string? content, string message, long elapsedMs)> DownloadAsync(AppSettings settings)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(settings.Timeout));
            var url = $"{settings.ServerUrl.TrimEnd('/')}/code/get?follow_user_id={settings.FollowUserId}&id={settings.UserId}&auth={Uri.EscapeDataString(settings.AuthKey)}";
            var response = await _http.GetAsync(url, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                sw.Stop();
                return (false, null, $"下载失败: {response.StatusCode}", sw.ElapsedMilliseconds);
            }
            
            var json = await response.Content.ReadAsStringAsync();
            sw.Stop();
            var result = JsonSerializer.Deserialize<CodeResult>(json);
            return (true, result?.Content, "下载成功", sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return (false, null, "连接超时", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, null, $"下载错误: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }
}
