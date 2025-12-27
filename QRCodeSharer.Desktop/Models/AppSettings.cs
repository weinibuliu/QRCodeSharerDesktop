using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QRCodeSharer.Desktop.Models;

// JSON 源代码生成器，支持裁剪
[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class AppSettingsContext : JsonSerializerContext;

public class AppSettings
{
    public string ServerUrl { get; set; } = "";
    public int UserId { get; set; }
    public string AuthKey { get; set; } = "";
    public int FollowUserId { get; set; }
    public int PollInterval { get; set; } = 500;
    public int Timeout { get; set; } = 5000;

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QRCodeSharer", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize(json, AppSettingsContext.Default.AppSettings) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, AppSettingsContext.Default.AppSettings));
        }
        catch { }
    }
}
