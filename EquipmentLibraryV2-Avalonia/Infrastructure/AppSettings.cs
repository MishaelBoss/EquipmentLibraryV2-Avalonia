using System.Text.Json;

namespace EquipmentLibraryV2_Avalonia.Infrastructure;

public class AppSettings
{
    public bool CheckLatestUpdates { get; set; } = true;
    public string Ip { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;

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
                Ip = TryGetString(root, "Ip") ?? string.Empty,
                Port = TryGetString(root, "Port") ?? string.Empty,
                Database = TryGetString(root, "Database") ?? string.Empty,
                User = TryGetString(root, "User") ?? string.Empty,
                Password = DecodePassword(root),
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Не удалось загрузить настройки: {ex.Message}");
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

            var json = JsonSerializer.Serialize(new
            {
                CheckLatestUpdates,
                Ip,
                Port,
                Database,
                User,
                EncryptedPassword = SettingsProtector.Encrypt(Password),
            }, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Не удалось сохранить настройки: {ex.Message}");
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