using System;
using System.IO;
using System.Text.Json;

namespace QRCodeSharer.Desktop.Models;

public class AppSettings
{
    public string ServerUrl { get; set; } = "";
    public int UserId { get; set; }
    public string AuthKey { get; set; } = "";
    public int FollowUserId { get; set; }
    public int PollInterval { get; set; } = 500;
    public int Timeout { get; set; } = 5000;
    public int QrCodeSize { get; set; } = 220;

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
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
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
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
