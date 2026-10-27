using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace EquipmentLibraryV2_Avalonia.Infrastructure;

public class AppSettings
{
    public bool CheckLatestUpdates { get; set; } = true;
    public string? FolderDownload { get; set; } = string.Empty;
    public bool UseRawSelectedFolder { get; set; } = false;
    public string Ip { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;

    [JsonIgnore] 
    public string Password { get; set; } = string.Empty;
    
    public string? EncryptedPassword { get; set; }
    
    private static readonly string SettingsPath = Path.Combine(AppPaths.UserDataDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppPaths.UserDataDir)
                .AddJsonFile("settings.json", optional: true, reloadOnChange: false)
                .Build();

            var settings = new AppSettings();
            
            configuration.Bind(settings);

            if (!string.IsNullOrEmpty(settings.EncryptedPassword))
            {
                var decrypted = SettingsProtector.TryDecrypt(settings.EncryptedPassword);
                if (decrypted is not null)
                {
                    settings.Password = decrypted;
                }
                else
                {
                    Serilog.Log.Warning("Stored encrypted password could not be decrypted (key missing or changed)");
                    settings.Password = string.Empty;
                }
            }
            else
            {
                settings.Password = configuration["Password"] ?? string.Empty;
            }

            return settings;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Не удалось загрузить настройки");
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (!string.IsNullOrEmpty(Password))
                EncryptedPassword = SettingsProtector.Encrypt(Password);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Не удалось сохранить настройки"); 
        }
    }
}