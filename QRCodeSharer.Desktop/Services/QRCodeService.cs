using System;
using System.Diagnostics;
using System.Net;
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

    private static string GetErrorMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.OK => "成功",
        HttpStatusCode.Forbidden => "认证失败，请检查用户ID和密钥",
        HttpStatusCode.NotFound => "资源不存在",
        HttpStatusCode.TooManyRequests => "请求过于频繁，请稍后重试",
        HttpStatusCode.InternalServerError => "服务器内部错误",
        _ => $"未知错误: {(int)statusCode}"
    };

    private static string GetRequestErrorMessage(Exception ex) => ex switch
    {
        HttpRequestException hre when hre.InnerException is System.Net.Sockets.SocketException => "无法连接到服务器，请检查网络或服务器地址",
        HttpRequestException => "请求失败，请检查服务器地址",
        TaskCanceledException or OperationCanceledException => "连接超时",
        _ => $"请求错误: {ex.Message}"
    };

    public async Task<(bool success, string message, int statusCode, long elapsedMs)> TestConnectionAsync(AppSettings settings)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(settings.Timeout));
            var url = $"{settings.ServerUrl.TrimEnd('/')}/?id={settings.UserId}&auth={Uri.EscapeDataString(settings.AuthKey)}";
            var response = await _http.GetAsync(url, cts.Token);
            sw.Stop();
            var code = (int)response.StatusCode;
            return response.IsSuccessStatusCode 
                ? (true, $"连接成功 ({sw.ElapsedMilliseconds}ms)", code, sw.ElapsedMilliseconds) 
                : (false, GetErrorMessage(response.StatusCode), code, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, GetRequestErrorMessage(ex), 0, sw.ElapsedMilliseconds);
        }
    }

    public async Task<(bool success, string? content, string message, int statusCode, long elapsedMs)> DownloadAsync(AppSettings settings)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(settings.Timeout));
            var url = $"{settings.ServerUrl.TrimEnd('/')}/code/get?follow_user_id={settings.FollowUserId}&id={settings.UserId}&auth={Uri.EscapeDataString(settings.AuthKey)}";
            var response = await _http.GetAsync(url, cts.Token);
            var code = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                sw.Stop();
                return (false, null, GetErrorMessage(response.StatusCode), code, sw.ElapsedMilliseconds);
            }
            
            var json = await response.Content.ReadAsStringAsync();
            sw.Stop();
            var result = JsonSerializer.Deserialize<CodeResult>(json);
            return (true, result?.Content, "下载成功", code, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, null, GetRequestErrorMessage(ex), 0, sw.ElapsedMilliseconds);
        }
    }

    public async Task<(bool exists, string message, int statusCode, long elapsedMs)> CheckUserExistsAsync(AppSettings settings, int checkUserId)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(settings.Timeout));
            var url = $"{settings.ServerUrl.TrimEnd('/')}/user/get?id={settings.UserId}&auth={Uri.EscapeDataString(settings.AuthKey)}&check_id={checkUserId}";
            var response = await _http.GetAsync(url, cts.Token);
            sw.Stop();
            var code = (int)response.StatusCode;
            
            if (!response.IsSuccessStatusCode)
            {
                var msg = response.StatusCode == HttpStatusCode.NotFound 
                    ? $"用户 {checkUserId} 不存在" 
                    : GetErrorMessage(response.StatusCode);
                return (false, msg, code, sw.ElapsedMilliseconds);
            }
            return (true, $"用户验证成功 ({sw.ElapsedMilliseconds}ms)", code, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, GetRequestErrorMessage(ex), 0, sw.ElapsedMilliseconds);
        }
    }
}
