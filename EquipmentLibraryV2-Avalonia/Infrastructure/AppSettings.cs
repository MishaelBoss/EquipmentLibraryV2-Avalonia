using System;
using System.IO;
using System.Text.Json;

namespace EquipmentLibraryV2_Avalonia.Infrastructure;

public class AppSettings
{
    public bool CheckLatestUpdates { get; set; } = true;
    
    private static readonly string SettingsPath = Path.Combine(
        AppPaths.UserDataDir, "settings.json");
    
    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
    
    public void Save()
    {
        try
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed
            var directory = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Не удалось сохранить настройки: {ex.Message}");
        }
    }
}