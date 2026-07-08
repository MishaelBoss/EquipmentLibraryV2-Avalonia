using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Serilog;

namespace EquipmentLibraryV2_Avalonia.Infrastructure;

public sealed class DbCredentials
{
    private static readonly string ConfigPath = Path.Combine(AppPaths.UserDataDir, "db.json.enc");
    private static readonly string KeyPath = Path.Combine(AppPaths.UserDataDir, ".dbkey");

    public string Ip { get; set; } = "localhost";
    public string Port { get; set; } = "5432";
    public string Database { get; set; } = "ELA_V2";
    public string User { get; set; } = "postgres";
    public string Password { get; set; } = string.Empty;

    private static string ResolvePassword()
    {
        var env = Environment.GetEnvironmentVariable("EQUIPMENT_LIBRARY_DB_PASSWORD");
        if (!string.IsNullOrEmpty(env))
            return env;

        return "cr2032";
    }

    private static byte[] GetOrCreateKey()
    {
        if (File.Exists(KeyPath))
        {
            return File.ReadAllBytes(KeyPath);
        }

        var key = RandomNumberGenerator.GetBytes(32);
        var dir = Path.GetDirectoryName(KeyPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(KeyPath, key);
        return key;
    }

    private static string Encrypt(string plainText)
    {
        var key = GetOrCreateKey();
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var input = Encoding.UTF8.GetBytes(plainText);
        var cipher = encryptor.TransformFinalBlock(input, 0, input.Length);

        var result = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);

        return Convert.ToBase64String(result);
    }

    private static string Decrypt(string cipherText)
    {
        var key = GetOrCreateKey();
        var full = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = full.AsSpan(0, 16).ToArray();

        using var decryptor = aes.CreateDecryptor();
        var input = full.AsSpan(16).ToArray();
        var output = decryptor.TransformFinalBlock(input, 0, input.Length);

        return Encoding.UTF8.GetString(output);
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(new
            {
                Ip = Encrypt(Ip),
                Port = Encrypt(Port),
                Database = Encrypt(Database),
                User = Encrypt(User),
                Password = Encrypt(Password)
            });

            File.WriteAllText(ConfigPath, json);
            Log.Information("DB credentials saved securely to {Path}", ConfigPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save encrypted DB credentials");
        }
    }

    public static DbCredentials Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                Log.Warning("DB config not found at {Path}. Creating with defaults.", ConfigPath);
                var creds = new DbCredentials
                {
                    Password = ResolvePassword()
                };
                Log.Information("DB password resolved from {Source}",
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EQUIPMENT_LIBRARY_DB_PASSWORD"))
                        ? "environment variable"
                        : "built-in default");
                creds.Save();
                return creds;
            }

            var json = File.ReadAllText(ConfigPath);
            var doc = JsonSerializer.Deserialize<JsonElement>(json);

            var result = new DbCredentials
            {
                Ip = Decrypt(doc.GetProperty("Ip").GetString()!),
                Port = Decrypt(doc.GetProperty("Port").GetString()!),
                Database = Decrypt(doc.GetProperty("Database").GetString()!),
                User = Decrypt(doc.GetProperty("User").GetString()!),
                Password = Decrypt(doc.GetProperty("Password").GetString()!)
            };

            if (string.IsNullOrEmpty(result.Password))
            {
                Log.Warning("DB password is empty in config, resolving fallback password");
                result.Password = ResolvePassword();
                result.Save();
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load encrypted DB credentials. Recreating with defaults.");
            try { File.Delete(ConfigPath); } catch { }
            var fallback = new DbCredentials { Password = ResolvePassword() };
            fallback.Save();
            return fallback;
        }
    }
}
