using System;
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
    private readonly HttpClient _http = new();

    public async Task<(bool success, string message)> TestConnectionAsync(AppSettings settings)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(settings.Timeout));
            var url = $"{settings.ServerUrl.TrimEnd('/')}/?id={settings.UserId}&auth={Uri.EscapeDataString(settings.AuthKey)}";
            var response = await _http.GetAsync(url, cts.Token);
            return response.IsSuccessStatusCode 
                ? (true, "连接成功") 
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

    public async Task<(bool success, string? content, string message)> DownloadAsync(AppSettings settings)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(settings.Timeout));
            var url = $"{settings.ServerUrl.TrimEnd('/')}/code/get?follow_user_id={settings.FollowUserId}&id={settings.UserId}&auth={Uri.EscapeDataString(settings.AuthKey)}";
            var response = await _http.GetAsync(url, cts.Token);
            if (!response.IsSuccessStatusCode)
                return (false, null, $"下载失败: {response.StatusCode}");
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CodeResult>(json);
            return (true, result?.Content, "下载成功");
        }
        catch (OperationCanceledException)
        {
            return (false, null, "连接超时");
        }
        catch (Exception ex)
        {
            return (false, null, $"下载错误: {ex.Message}");
        }
    }
}
