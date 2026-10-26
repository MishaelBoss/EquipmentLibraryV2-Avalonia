using System.Text.Json;
using System.Text.Json.Serialization;

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
    
    private static readonly string SettingsPath = Path.Combine(
        AppPaths.UserDataDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new AppSettings
            {
                CheckLatestUpdates = TryGetBool(root, "CheckLatestUpdates") ?? true,
                FolderDownload = TryGetString(root, "FolderDownload") ?? string.Empty,
                UseRawSelectedFolder = TryGetBool(root, "UseRawSelectedFolder") ?? false,
                Ip = TryGetString(root, "Ip") ?? string.Empty,
                Port = TryGetString(root, "Port") ?? string.Empty,
                Database = TryGetString(root, "Database") ?? string.Empty,
                User = TryGetString(root, "User") ?? string.Empty,
                Password = DecodePassword(root),
            };
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Не удалось загрузить настройки");
            return new AppSettings();
        }
    }

    private static string DecodePassword(JsonElement root)
    {
        if (root.TryGetProperty("EncryptedPassword", out var encrypted) &&
            encrypted.ValueKind == JsonValueKind.String)
        {
            var decrypted = SettingsProtector.TryDecrypt(encrypted.GetString());
            if (decrypted is not null)
                return decrypted;

            Serilog.Log.Warning("Stored encrypted password could not be decrypted (key missing or changed)");
            return string.Empty;
        }

        if (root.TryGetProperty("Password", out var legacy) &&
            legacy.ValueKind == JsonValueKind.String)
        {
            return legacy.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

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

    private static string? TryGetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool? TryGetBool(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;
}